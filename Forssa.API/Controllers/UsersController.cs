using Forssa.Application.Interfaces;
using Forssa.Domain.Entities;
using Forssa.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Forssa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;
    private readonly ApplicationDbContext _context;

    public UsersController(IUserService userService, ILogger<UsersController> logger, ApplicationDbContext context)
    {
        _userService = userService;
        _logger = logger;
        _context = context;
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
        var instructorPosition = User.FindFirst("instructor_position")?.Value;
        
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
            username,
            instructorPosition);
            
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

        if (string.Equals(request.Role, "Instructor", StringComparison.OrdinalIgnoreCase)
            || string.Equals(request.Role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Direct role change to Instructor/Admin is not allowed. Submit an instructor approval request.");
        }

        var userDto = await _userService.SyncUserAsync(supabaseId, email ?? string.Empty, request.Role);
        return Ok(userDto);
    }

    [Authorize]
    [HttpPost("me/request-instructor")]
    public async Task<IActionResult> RequestInstructorApproval([FromBody] InstructorApprovalRequestDto request)
    {
        var supabaseId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(supabaseId))
            return BadRequest("User ID (sub) not found in token.");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.SupabaseId == supabaseId);
        if (user == null)
            return NotFound("User not found.");

        if (string.Equals(user.Role, "Instructor", StringComparison.OrdinalIgnoreCase))
            return BadRequest("You are already an instructor.");

        var existingPending = await _context.InstructorApprovalRequests
            .AnyAsync(r => r.UserId == user.Id && r.Status == "Pending");

        if (existingPending)
            return BadRequest("You already have a pending instructor request.");

        var approvalRequest = new Forssa.Domain.Entities.InstructorApprovalRequest
        {
            UserId = user.Id,
            RequestedPosition = string.IsNullOrWhiteSpace(request.InstructorPosition) ? null : request.InstructorPosition.Trim(),
            Status = "Pending"
        };

        _context.InstructorApprovalRequests.Add(approvalRequest);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Instructor approval request submitted successfully.",
            requestId = approvalRequest.Id,
            status = approvalRequest.Status
        });
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

        var currentRole = User.FindFirst(ClaimTypes.Role)?.Value;
        if (string.Equals(currentRole, "Instructor", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(request.InstructorPosition))
        {
            return BadRequest("Instructor position is required for instructors.");
        }

        var userDto = await _userService.SyncUserAsync(
            supabaseId, 
            email ?? string.Empty, 
            null, // Don't change role here
            request.FullName,
            request.Bio,
            request.AvatarUrl,
            request.Username,
            request.InstructorPosition);
            
        return Ok(userDto);
    }
}

public record UpdateRoleRequest(string Role);
public record UserProfileRequest(string? FullName, string? Bio, string? AvatarUrl, string? Username, string? InstructorPosition);
public record InstructorApprovalRequestDto(string? InstructorPosition);
