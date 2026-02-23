using Forssa.Application.DTOs;
using Forssa.Application.Interfaces;
using Forssa.Domain.Entities;
using Forssa.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Forssa.Infrastructure.Services;

public class InstructorService : IInstructorService
{
    private readonly ApplicationDbContext _context;

    public InstructorService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<InstructorStatsDTO> GetInstructorStatsAsync(string supabaseUserId)
    {
        // Total Courses
        if (!Guid.TryParse(supabaseUserId, out var supabaseGuid))
        {
            return new InstructorStatsDTO();
        }

        var courses = await _context.Courses
            .AsNoTracking()
            .Where(c => c.Instructor_id == supabaseGuid)
            .ToListAsync();

        if (courses.Count == 0)
        {
            return new InstructorStatsDTO();
        }

        var courseIds = courses.Select(c => c.Id).ToList();

        // Total Students (Enrollments)
        var totalStudents = await _context.Enrollments
            .AsNoTracking()
            .Where(e => courseIds.Contains(e.CourseId))
            .CountAsync();

        // Total Revenue
        var totalRevenue = await _context.Enrollments
            .AsNoTracking()
            .Where(e => courseIds.Contains(e.CourseId))
            .Join(_context.Courses,
                e => e.CourseId,
                c => c.Id,
                (e, c) => c.Price)
            .SumAsync();

        return new InstructorStatsDTO
        {
            TotalCourses = courses.Count,
            TotalStudents = totalStudents,
            TotalRevenue = totalRevenue,
            TotalReviews = 0
        };
    }
}
