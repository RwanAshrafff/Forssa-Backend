using Forssa.Application.DTOs;
using Forssa.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Forssa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SuccessStoriesController : BaseController
{
    private readonly ISuccessStoryService _successStoryService;

    public SuccessStoriesController(ISuccessStoryService successStoryService)
    {
        _successStoryService = successStoryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var stories = await _successStoryService.GetAllAsync();
            return HandleResult(stories, "Success stories retrieved successfully.");
        }
        catch (Exception ex)
        {
            return InternalServerError(ex.Message);
        }
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSuccessStoryDTO createDto)
    {
        try
        {
            var supabaseId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(supabaseId))
                return BadRequestResult("User ID not found in token.");

            var story = await _successStoryService.CreateAsync(supabaseId, createDto);
            return HandleResult(story, "Success story shared successfully!");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundResult(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResult(ex.Message);
        }
        catch (Exception ex)
        {
            return InternalServerError(ex.Message);
        }
    }

    [Authorize]
    [HttpGet("can-submit")]
    public async Task<IActionResult> CanSubmit()
    {
        try
        {
            var supabaseId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(supabaseId))
                return BadRequestResult("User ID not found in token.");

            var canSubmit = await _successStoryService.CanUserSubmitStoryAsync(supabaseId);
            return HandleResult(new { canSubmit }, "Eligibility checked successfully.");
        }
        catch (Exception ex)
        {
            return InternalServerError(ex.Message);
        }
    }
}
