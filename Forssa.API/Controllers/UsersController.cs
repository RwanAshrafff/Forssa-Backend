using Forssa.Domain.Entities;
using Forssa.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Forssa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public UsersController(ApplicationDbContext context)
    {
        _context = context;
    }

    [Authorize]
    [HttpPost("sync-user")]
    public async Task<IActionResult> SyncUser()
    {
        var supabaseId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                        ?? User.FindFirst("sub")?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value 
                    ?? User.FindFirst("email")?.Value;

        if (string.IsNullOrEmpty(supabaseId))
            return BadRequest("User ID (sub) not found in token.");

        var existingUser = await _context.Users
            .FirstOrDefaultAsync(x => x.SupabaseId == supabaseId);

        if (existingUser == null)
        {
            var newUser = new User
            {
                SupabaseId = supabaseId,
                Email = email ?? string.Empty,
                Role = "User"
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "User created", User = newUser });
        }

        return Ok(new { Message = "User already exists", User = existingUser });
    }
}
