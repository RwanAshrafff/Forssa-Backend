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
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe([FromQuery] string? role = null)
    {
        var supabaseId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("sub")?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value
                    ?? User.FindFirst("email")?.Value;

        if (string.IsNullOrEmpty(supabaseId))
            return BadRequest("User ID (sub) not found in token.");

        // Metadata extracted by SupabaseClaimsTransformation
        var fullName = User.FindFirst("full_name")?.Value;
        var bio = User.FindFirst("bio")?.Value;
        var avatarUrl = User.FindFirst("avatar_url")?.Value;
        var username = User.FindFirst("username")?.Value;
        
        if (string.IsNullOrEmpty(role))
        {
            role = User.FindFirst(ClaimTypes.Role)?.Value;
        }

        _logger.LogInformation("Syncing user {Email} (ID: {SupabaseId}) with full metadata", email, supabaseId);

        var userDto = await _userService.SyncUserAsync(
            supabaseId, 
            email ?? string.Empty, 
            role,
            fullName,
            bio,
            avatarUrl,
            username);
            
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
    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UserProfileRequest request)
    {
        var supabaseId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("sub")?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value
                    ?? User.FindFirst("email")?.Value;

        if (string.IsNullOrEmpty(supabaseId))
            return BadRequest("User ID (sub) not found in token.");

        _logger.LogInformation("Actively updating profile for user {Email}", email);

        var userDto = await _userService.SyncUserAsync(
            supabaseId, 
            email ?? string.Empty, 
            null, // Don't change role here
            request.FullName,
            request.Bio,
            request.AvatarUrl,
            request.Username);
            
        return Ok(userDto);
    }
}

public record UpdateRoleRequest(string Role);
public record UserProfileRequest(string? FullName, string? Bio, string? AvatarUrl, string? Username);
