using Forssa.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Forssa.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class FollowsController : BaseController
{
    private readonly IFollowService _followService;

    public FollowsController(IFollowService followService)
    {
        _followService = followService;
    }

    [HttpPost("{targetUserId:guid}")]
    public async Task<IActionResult> Follow(Guid targetUserId)
    {
        try
        {
            var supabaseId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(supabaseId))
                return BadRequestResult("User ID (sub) not found in token.");

            await _followService.FollowAsync(supabaseId, targetUserId);
            return HandleResult(true, "Followed successfully.");
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

    [HttpDelete("{targetUserId:guid}")]
    public async Task<IActionResult> Unfollow(Guid targetUserId)
    {
        try
        {
            var supabaseId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(supabaseId))
                return BadRequestResult("User ID (sub) not found in token.");

            await _followService.UnfollowAsync(supabaseId, targetUserId);
            return HandleResult(true, "Unfollowed successfully.");
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

    [HttpGet("following")]
    public async Task<IActionResult> GetFollowing()
    {
        try
        {
            var supabaseId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(supabaseId))
                return BadRequestResult("User ID (sub) not found in token.");

            var users = await _followService.GetFollowingAsync(supabaseId);
            return HandleResult(users, "Following list retrieved successfully.");
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

    [HttpGet("followers")]
    public async Task<IActionResult> GetFollowers()
    {
        try
        {
            var supabaseId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(supabaseId))
                return BadRequestResult("User ID (sub) not found in token.");

            var users = await _followService.GetFollowersAsync(supabaseId);
            return HandleResult(users, "Followers list retrieved successfully.");
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

    [HttpGet("home-feed")]
    public async Task<IActionResult> GetHomeFeed()
    {
        try
        {
            var supabaseId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(supabaseId))
                return BadRequestResult("User ID (sub) not found in token.");

            var feed = await _followService.GetHomeFeedAsync(supabaseId);
            return HandleResult(feed, "Home feed retrieved successfully.");
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

    [HttpGet("users/{userId:guid}/followers")]
    public async Task<IActionResult> GetUserFollowers(Guid userId)
    {
        try
        {
            var users = await _followService.GetUserFollowersAsync(userId);
            return HandleResult(users, "User followers retrieved successfully.");
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

    [HttpGet("users/{userId:guid}/following")]
    public async Task<IActionResult> GetUserFollowing(Guid userId)
    {
        try
        {
            var users = await _followService.GetUserFollowingAsync(userId);
            return HandleResult(users, "User following retrieved successfully.");
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

    [HttpGet("users/{userId:guid}/connections")]
    public async Task<IActionResult> GetUserConnections(Guid userId)
    {
        try
        {
            var supabaseId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("sub")?.Value;

            var connections = await _followService.GetUserConnectionsAsync(supabaseId, userId);
            return HandleResult(connections, "User connections retrieved successfully.");
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