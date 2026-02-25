using Forssa.Application.Interfaces;
using Forssa.Domain.Entities;
using Forssa.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Forssa.Infrastructure.Services;

public class UserProgressService : IUserProgressService
{
    private readonly ApplicationDbContext _context;

    public UserProgressService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> GetUserPointsAsync(string supabaseId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.SupabaseId == supabaseId)
            ?? throw new KeyNotFoundException("User not found.");
        return user.Points;
    }

    public async Task<bool> CompleteLessonAsync(string supabaseId, Guid lessonId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.SupabaseId == supabaseId)
            ?? throw new KeyNotFoundException("User not found.");

        var lesson = await _context.Lessons
            .Include(l => l.Module)
            .FirstOrDefaultAsync(l => l.Id == lessonId)
            ?? throw new KeyNotFoundException("Lesson not found.");

        var progress = await _context.LessonProgress
            .FirstOrDefaultAsync(lp => lp.UserId == user.Id && lp.LessonId == lessonId);

        if (progress == null)
        {
            progress = new LessonProgress
            {
                UserId = user.Id,
                LessonId = lessonId,
                IsCompleted = true,
                CompletedAt = DateTime.UtcNow
            };
            _context.LessonProgress.Add(progress);
        }
        else
        {
            progress.IsCompleted = true;
            progress.CompletedAt = DateTime.UtcNow;
            _context.LessonProgress.Update(progress);
        }

        await _context.SaveChangesAsync();

        // Check for course completion to award points
        await CheckAndAwardPointsAsync(user.Id, lesson.Module.Course_id);

        return true;
    }

    public async Task<bool> CompleteQuizAsync(string supabaseId, Guid quizId, decimal score, bool isPassed)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.SupabaseId == supabaseId)
            ?? throw new KeyNotFoundException("User not found.");

        // Handled by QuizzesController usually, but we might want to check for course completion here too
        // if the quiz is part of the course.
        
        var quiz = await _context.Quizzes
            .Include(q => q.Module)
            .FirstOrDefaultAsync(q => q.Id == quizId)
            ?? throw new KeyNotFoundException("Quiz not found.");

        // Check for course completion to award points
        await CheckAndAwardPointsAsync(user.Id, quiz.Module.Course_id);

        return true;
    }

    private async Task CheckAndAwardPointsAsync(Guid userId, Guid courseId)
    {
        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.StudentId == userId && e.CourseId == courseId);

        if (enrollment == null || enrollment.IsPointsAwarded)
            return;

        // Check if all lessons are completed
        var allLessonIds = await _context.Modules
            .Where(m => m.Course_id == courseId)
            .SelectMany(m => m.Lessons.Select(l => l.Id))
            .ToListAsync();

        if (allLessonIds.Count == 0) return;

        var completedLessonCount = await _context.LessonProgress
            .Where(lp => lp.UserId == userId && lp.IsCompleted && allLessonIds.Contains(lp.LessonId))
            .Select(lp => lp.LessonId)
            .Distinct()
            .CountAsync();

        // Check if all quizzes are passed
        var allQuizIds = await _context.Modules
            .Where(m => m.Course_id == courseId)
            .SelectMany(m => m.Quizzes.Select(q => q.Id))
            .ToListAsync();

        var passedQuizCount = await _context.QuizSubmissions
            .Where(qs => qs.UserId == userId && qs.IsPassed && allQuizIds.Contains(qs.QuizId))
            .Select(qs => qs.QuizId)
            .Distinct()
            .CountAsync();

        if (completedLessonCount == allLessonIds.Count && passedQuizCount == allQuizIds.Count)
        {
            // Award 50 points
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.AddPoints(50);
                enrollment.IsPointsAwarded = true;
                _context.Users.Update(user);
                _context.Enrollments.Update(enrollment);
                await _context.SaveChangesAsync();
            }
        }
    }
}
