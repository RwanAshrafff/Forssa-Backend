using Forssa.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Forssa.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CoursesController : BaseController
{
    private readonly ApplicationDbContext _context;

    public CoursesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            // First stop the course to get related data if needed
            var course = await _context.Courses
                .Include(c => c.Modules)
                    .ThenInclude(m => m.Lessons)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
                return NotFoundResult("Course not found");

            // Define deletion in a transaction to ensure integrity
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Delete Enrollments
                var enrollments = await _context.Enrollments.Where(e => e.CourseId == id).ToListAsync();
                if (enrollments.Any())
                {
                    _context.Enrollments.RemoveRange(enrollments);
                }

                // 2. Lesson related data (Progress and Notes)
                foreach (var module in course.Modules)
                {
                    var lessonIds = module.Lessons.Select(l => l.Id).ToList();
                    if (lessonIds.Any())
                    {
                        var progress = await _context.LessonProgress.Where(lp => lessonIds.Contains(lp.LessonId)).ToListAsync();
                        if (progress.Any()) _context.LessonProgress.RemoveRange(progress);

                        var notes = await _context.LessonNotes.Where(ln => lessonIds.Contains(ln.LessonId)).ToListAsync();
                        if (notes.Any()) _context.LessonNotes.RemoveRange(notes);
                    }
                }

                // 3. Delete course (Cascades to modules and lessons due to EF configuration in ApplicationDbContext)
                // Note: Even if cascades are not perfectly synced in DB, removing the parent tracking object 
                // and calling SaveChanges will typically trigger EF cascades if configured in OnModelCreating.
                _context.Courses.Remove(course);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return HandleResult(true, "Course and all related data deleted successfully");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            return InternalServerError(ex.Message);
        }
    }
}
