using Forssa.Application.DTOs;
using Forssa.Application.Interfaces;
using Forssa.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Forssa.Infrastructure.Services;

public class CertificateService : ICertificateService
{

    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public CertificateService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<IReadOnlyList<CompletedCourseCertificateDTO>> GetCompletedCoursesAsync(string supabaseId)
    {
        var student = await _context.Users.FirstOrDefaultAsync(u => u.SupabaseId == supabaseId)
            ?? throw new KeyNotFoundException("User not found.");

        var enrolledCourseIds = await _context.Enrollments
            .Where(e => e.StudentId == student.Id)
            .Select(e => e.CourseId)
            .ToListAsync();

        if (enrolledCourseIds.Count == 0)
            return Array.Empty<CompletedCourseCertificateDTO>();

        var courseLessons = await _context.Modules
            .Where(m => enrolledCourseIds.Contains(m.Course_id))
            .SelectMany(m => m.Lessons.Select(l => new { CourseId = m.Course_id, LessonId = l.Id }))
            .ToListAsync();

        var completedProgress = await _context.LessonProgress
            .Where(lp => lp.UserId == student.Id && lp.IsCompleted)
            .Select(lp => new { lp.LessonId, lp.CompletedAt })
            .ToListAsync();

        var completedLessonSet = completedProgress.Select(x => x.LessonId).ToHashSet();
        var completedAtByLesson = completedProgress
            .GroupBy(x => x.LessonId)
            .ToDictionary(g => g.Key, g => g.Max(x => x.CompletedAt));

        var courses = await _context.Courses
            .Where(c => enrolledCourseIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Title })
            .ToListAsync();

        var result = new List<CompletedCourseCertificateDTO>();

        foreach (var course in courses)
        {
            var lessonIds = courseLessons
                .Where(x => x.CourseId == course.Id)
                .Select(x => x.LessonId)
                .Distinct()
                .ToList();

            if (lessonIds.Count == 0)
                continue;

            var isCompleted = lessonIds.All(lessonId => completedLessonSet.Contains(lessonId));
            if (!isCompleted)
                continue;

            var completedAt = lessonIds
                .Where(lessonId => completedAtByLesson.ContainsKey(lessonId))
                .Select(lessonId => completedAtByLesson[lessonId])
                .DefaultIfEmpty()
                .Max();

            result.Add(new CompletedCourseCertificateDTO
            {
                CourseId = course.Id,
                CourseTitle = course.Title,
                CompletedAt = completedAt == default ? null : completedAt
            });
        }

        return result;
    }

    public async Task<CertificateDocumentDTO> GenerateCourseCertificateAsync(string supabaseId, Guid courseId, CertificateTemplateInputDTO? input = null)
    {
        var student = await _context.Users.FirstOrDefaultAsync(u => u.SupabaseId == supabaseId)
            ?? throw new KeyNotFoundException("User not found.");

        var enrollment = await _context.Enrollments
            .AnyAsync(e => e.StudentId == student.Id && e.CourseId == courseId);

        if (!enrollment)
            throw new UnauthorizedAccessException("You are not enrolled in this course.");

        var course = await _context.Courses
            .Where(c => c.Id == courseId)
            .Select(c => new { c.Id, c.Title, c.Instructor_id })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Course not found.");

        var lessonIds = await _context.Modules
            .Where(m => m.Course_id == courseId)
            .SelectMany(m => m.Lessons.Select(l => l.Id))
            .Distinct()
            .ToListAsync();

        if (lessonIds.Count == 0)
            throw new InvalidOperationException("This course has no lessons.");

        var completedLessonCount = await _context.LessonProgress
            .Where(lp => lp.UserId == student.Id && lp.IsCompleted && lessonIds.Contains(lp.LessonId))
            .Select(lp => lp.LessonId)
            .Distinct()
            .CountAsync();

        if (completedLessonCount < lessonIds.Count)
            throw new InvalidOperationException("Course is not completed yet.");

        var uploadedByInstructorProfile = await _context.Users
            .Where(u => u.Id == course.Instructor_id)
            .Select(u => new
            {
                u.FullName,
                u.InstructorPosition
            })
            .FirstOrDefaultAsync();

        var studentName = ResolveDisplayName(
            input?.StudentFullName,
            student.FullName,
            "Student full name is missing. Please update your profile full name.");

        var instructorName = ResolveDisplayName(
            input?.InstructorFullName,
            uploadedByInstructorProfile?.FullName,
            "Instructor full name is missing for this course uploader.");

        var instructorPosition = NormalizePosition(input?.InstructorPosition)
            ?? NormalizePosition(uploadedByInstructorProfile?.InstructorPosition)
            ?? "Instructor";

        var effectiveCourseTitle = !string.IsNullOrWhiteSpace(input?.CourseTitle)
            ? input!.CourseTitle!
            : course.Title;
        if (string.IsNullOrWhiteSpace(effectiveCourseTitle))
            throw new InvalidOperationException("Course title is missing.");

        var completedAt = await _context.LessonProgress
            .Where(lp => lp.UserId == student.Id && lp.IsCompleted && lessonIds.Contains(lp.LessonId))
            .OrderByDescending(lp => lp.CompletedAt)
            .Select(lp => lp.CompletedAt)
            .FirstOrDefaultAsync();

        var pdf = await BuildCertificatePdfAsync(effectiveCourseTitle, studentName, instructorName, instructorPosition, completedAt);

        return new CertificateDocumentDTO
        {
            FileName = $"certificate-{courseId}.pdf",
            ContentType = "application/pdf",
            Content = pdf
        };
    }

    private async Task<byte[]> BuildCertificatePdfAsync(string courseTitle, string studentName, string instructorName, string instructorPosition, DateTime completedAt)
    {
        var websiteName = _configuration["Certificate:WebsiteName"] ?? "Forssa";

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(14));

                page.Content().Column(outerColumn =>
                {
                    outerColumn.Item().Height(50).Row(headerRow =>
                    {
                        headerRow.RelativeItem();
                        headerRow.AutoItem().PaddingRight(12).AlignRight().AlignMiddle().Text(websiteName).FontSize(22).SemiBold().FontColor(Colors.Green.Medium);
                    });

                    outerColumn.Item().Border(4).BorderColor(Colors.Grey.Darken3).Padding(36).Column(column =>
                    {
                        column.Spacing(8);

                        column.Item().AlignCenter().PaddingTop(6).Text("Certificate of Completion").FontSize(40).Bold();
                        column.Item().AlignCenter().PaddingTop(8).Text("This certifies that").FontSize(18).FontColor(Colors.Grey.Darken1);
                        column.Item().AlignCenter().PaddingTop(4).Text(studentName).FontSize(36).Bold();
                        column.Item().AlignCenter().PaddingTop(8).Text("has successfully completed the course").FontSize(18).FontColor(Colors.Grey.Darken1);
                        column.Item().AlignCenter().PaddingTop(8).Text(courseTitle).FontSize(24).SemiBold();
                        column.Item().AlignCenter().PaddingTop(18).Text($"Issued on {completedAt:MMMM dd, yyyy}").FontSize(14).FontColor(Colors.Grey.Darken2);

                        column.Item().PaddingTop(40).Row(row =>
                        {
                            row.RelativeItem().Column(left =>
                            {
                                left.Item().Text(instructorName).FontSize(12).SemiBold();
                                left.Item().Text(instructorPosition).FontSize(11).FontColor(Colors.Grey.Darken1);
                                left.Item().Width(220).BorderTop(1).BorderColor(Colors.Grey.Darken2);
                            });
                        });
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    

    private static string ResolveDisplayName(string? preferredValue, string? fallbackValue, string errorMessage)
    {
        var preferred = NormalizeName(preferredValue);
        if (!IsInvalidDisplayName(preferred))
            return preferred!;

        var fallback = NormalizeName(fallbackValue);
        if (!IsInvalidDisplayName(fallback))
            return fallback!;

        throw new InvalidOperationException(errorMessage);
    }

    private static string? NormalizeName(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizePosition(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool IsInvalidDisplayName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        return value.Contains('@')
            || value.Equals("Instructor Signature", StringComparison.OrdinalIgnoreCase)
            || value.Equals("Instructor", StringComparison.OrdinalIgnoreCase);
    }
}
