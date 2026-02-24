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
            // Load course with all related structural data
            var course = await _context.Courses
                .Include(c => c.Modules)
                    .ThenInclude(m => m.Lessons)
                .Include(c => c.Modules)
                    .ThenInclude(m => m.Quizzes)
                        .ThenInclude(q => q.Questions)
                            .ThenInclude(qn => qn.Options)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
                return NotFoundResult("Course not found");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Delete Enrollments
                var enrollments = await _context.Enrollments.Where(e => e.CourseId == id).ToListAsync();
                if (enrollments.Any()) _context.Enrollments.RemoveRange(enrollments);

                // 2. Process Modules for related data
                foreach (var module in course.Modules)
                {
                    // a. Lesson related data
                    var lessonIds = module.Lessons.Select(l => l.Id).ToList();
                    if (lessonIds.Any())
                    {
                        var progress = await _context.LessonProgress.Where(lp => lessonIds.Contains(lp.LessonId)).ToListAsync();
                        if (progress.Any()) _context.LessonProgress.RemoveRange(progress);

                        var notes = await _context.LessonNotes.Where(ln => lessonIds.Contains(ln.LessonId)).ToListAsync();
                        if (notes.Any()) _context.LessonNotes.RemoveRange(notes);
                    }

                    // b. Quiz related data
                    foreach (var quiz in module.Quizzes)
                    {
                        // Delete submissions first (FK dependency)
                        var submissions = await _context.QuizSubmissions.Where(qs => qs.QuizId == quiz.Id).ToListAsync();
                        if (submissions.Any()) _context.QuizSubmissions.RemoveRange(submissions);
                        
                        // Questions and Options are usually handled by EF cascade if configured,
                        // but since we loaded them, EF will track them and handle it.
                    }
                }

                // 3. Delete course (Cascades to modules, lessons, and quizzes due to EF configuration)
                _context.Courses.Remove(course);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return HandleResult(true, "Course and all related data deleted successfully");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return InternalServerError($"Transaction failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            return InternalServerError(ex.Message);
        }
    }
}
