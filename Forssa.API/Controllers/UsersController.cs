using Forssa.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Forssa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [Authorize(Roles = "instructor,student")]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe([FromQuery] string? role = null)
    {
        var supabaseId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("sub")?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value
                    ?? User.FindFirst("email")?.Value;

        if (string.IsNullOrEmpty(supabaseId))
            return BadRequest("User ID (sub) not found in token.");

        // If role not passed as query param, try to read from JWT claims (Supabase user_metadata)
        if (string.IsNullOrEmpty(role))
        {
            role = User.FindFirst("user_metadata.role")?.Value
                ?? User.FindFirst("role")?.Value;
        }

        var userDto = await _userService.SyncUserAsync(supabaseId, email ?? string.Empty, role);
        return Ok(userDto);
    }

    [Authorize]  // No role restriction — called during initial role selection (user has no role yet)
    [HttpPut("me/role")]
    public async Task<IActionResult> UpdateRole([FromBody] UpdateRoleRequest request)
    {
        var supabaseId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("sub")?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value
                    ?? User.FindFirst("email")?.Value;

        if (string.IsNullOrEmpty(supabaseId))
            return BadRequest("User ID (sub) not found in token.");

        if (string.IsNullOrEmpty(request.Role))
            return BadRequest("Role is required.");

        var userDto = await _userService.SyncUserAsync(supabaseId, email ?? string.Empty, request.Role);
        return Ok(userDto);
    }
}

public record UpdateRoleRequest(string Role);
