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

    public async Task<UserDTO> SyncUserAsync(string supabaseId, string email, string? role = null)
    {
        // Use direct DbContext to avoid any caching/tracking issues
        // We look for existing user, but we use AsNoTracking for the initial check 
        // because we might be in a race condition with the DB trigger.
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.SupabaseId == supabaseId);

        if (existingUser == null)
        {
            // New user - create with role if provided
            var newUser = new User
            {
                SupabaseId = supabaseId,
                Email = email,
                Role = role ?? "Student"
            };

            try 
            {
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
                return _mapper.Map<UserDTO>(newUser);
            }
            catch (DbUpdateException)
            {
                // Likely a race condition where the DB trigger already inserted the user
                // Fetch again
                existingUser = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.SupabaseId == supabaseId);
                
                if (existingUser == null) throw; // Re-throw if truly failed
            }
        }

        // Existing user (either found via FirstOrDefault or after race condition)
        bool changed = false;

        if (!string.IsNullOrEmpty(role) && existingUser.Role != role)
        {
            existingUser.Role = role;
            changed = true;
        }

        if (!string.IsNullOrEmpty(email) && existingUser.Email != email)
        {
            existingUser.Email = email;
            changed = true;
        }

        if (changed)
        {
            existingUser.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(existingUser);
            await _context.SaveChangesAsync();
        }

        return _mapper.Map<UserDTO>(existingUser);
    }

}
