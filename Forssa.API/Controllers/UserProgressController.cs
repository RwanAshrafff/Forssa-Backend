using Forssa.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Forssa.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class UserProgressController : BaseController
{
    private readonly IUserProgressService _progressService;

    public UserProgressController(IUserProgressService progressService)
    {
        _progressService = progressService;
    }

    [HttpPost("complete-lesson/{lessonId:guid}")]
    public async Task<IActionResult> CompleteLesson(Guid lessonId)
    {
        try
        {
            var supabaseId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(supabaseId))
                return BadRequestResult("User ID not found in token.");

            await _progressService.CompleteLessonAsync(supabaseId, lessonId);
            return HandleResult(true, "Lesson marked as complete.");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundResult(ex.Message);
        }
        catch (Exception ex)
        {
            return InternalServerError(ex.Message);
        }
    }

    [HttpGet("points")]
    public async Task<IActionResult> GetPoints()
    {
        try
        {
            var supabaseId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(supabaseId))
                return BadRequestResult("User ID not found in token.");

            var points = await _progressService.GetUserPointsAsync(supabaseId);
            return HandleResult(new { points }, "User points retrieved successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundResult(ex.Message);
        }
        catch (Exception ex)
        {
            return InternalServerError(ex.Message);
        }
    }
}
