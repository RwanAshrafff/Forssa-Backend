using AutoMapper;
using Forssa.Application.DTOs;
using Forssa.Application.Interfaces;
using Forssa.Domain.Entities;
using Forssa.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Forssa.Infrastructure.Services;

public class UserService : BaseService<User, UserDTO>, IUserService
{
    private readonly ApplicationDbContext _context;

    public UserService(IBaseRepository<User> repository, IMapper mapper, ApplicationDbContext context)
        : base(repository, mapper)
    {
        _context = context;
    }

    public async Task<UserDTO> SyncUserAsync(string supabaseId, string email, string? role = null, string? fullName = null, string? bio = null, string? avatarUrl = null, string? username = null, string? instructorPosition = null)
    {
        // Use direct DbContext to avoid any caching/tracking issues
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.SupabaseId == supabaseId);

        if (existingUser == null)
        {
            // New user - create with all metadata
            var newUser = new User(supabaseId, email, role ?? "Student");
            var profileInstructorPosition = NormalizePositionForRole(role ?? "Student", instructorPosition);
            newUser.UpdateProfile(fullName, bio, avatarUrl, username, profileInstructorPosition);

            try 
            {
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
                return _mapper.Map<UserDTO>(newUser);
            }
            catch (DbUpdateException)
            {
                // Race condition fallback
                existingUser = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.SupabaseId == supabaseId);
                
                if (existingUser == null) throw;
            }
        }

        // Existing user update logic using domain methods
        bool changed = false;

        if (!string.IsNullOrEmpty(role) && existingUser.Role != role) 
        { 
            existingUser.UpdateRole(role); 
            changed = true; 
        }
        
        if (!string.IsNullOrEmpty(email) && existingUser.Email != email) 
        { 
            existingUser.UpdateEmail(email); 
            changed = true; 
        }

        var effectiveRole = role ?? existingUser.Role;
        var effectivePosition = NormalizePositionForRole(effectiveRole, instructorPosition ?? existingUser.InstructorPosition);

        // Always attempt profile update
        existingUser.UpdateProfile(fullName, bio, avatarUrl, username, effectivePosition);
        changed = true;

        if (changed)
        {
            _context.Users.Update(existingUser);
            await _context.SaveChangesAsync();
        }

        return _mapper.Map<UserDTO>(existingUser);
    }

    private static string? NormalizePositionForRole(string role, string? instructorPosition)
    {
        var normalizedRole = role.Trim();
        var normalizedPosition = string.IsNullOrWhiteSpace(instructorPosition)
            ? null
            : instructorPosition.Trim();

        if (normalizedRole.Equals("Instructor", StringComparison.OrdinalIgnoreCase))
        {
            return normalizedPosition;
        }

        return null;
    }
}

