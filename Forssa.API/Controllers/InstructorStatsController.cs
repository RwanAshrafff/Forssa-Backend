using Forssa.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Forssa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InstructorStatsController : ControllerBase
{
    private readonly IInstructorService _instructorService;

    public InstructorStatsController(IInstructorService instructorService)
    {
        _instructorService = instructorService;
    }

    [Authorize(Roles = "Instructor,instructor")]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyStats()
    {
        var supabaseUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                           ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(supabaseUserId))
            return BadRequest("User ID not found in token.");

        var stats = await _instructorService.GetInstructorStatsAsync(supabaseUserId);
        return Ok(stats);
    }

    [AllowAnonymous]
    [HttpGet("public/{supabaseId}")]
    public async Task<IActionResult> GetPublicStats(string supabaseId)
    {
        var stats = await _instructorService.GetInstructorStatsAsync(supabaseId);
        return Ok(stats);
    }
}
