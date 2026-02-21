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

    public async Task<UserDTO> SyncUserAsync(string supabaseId, string email, string? role = null, string? fullName = null, string? bio = null, string? avatarUrl = null, string? username = null)
    {
        // Use direct DbContext to avoid any caching/tracking issues
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.SupabaseId == supabaseId);

        if (existingUser == null)
        {
            // New user - create with all metadata
            var newUser = new User(supabaseId, email, role ?? "Student");
            newUser.UpdateProfile(fullName, bio, avatarUrl, username);

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

        // Always attempt profile update (domain method handles internal comparison if needed, 
        // but for simplicity we'll just track if we should save)
        existingUser.UpdateProfile(fullName, bio, avatarUrl, username);
        changed = true;

        if (changed)
        {
            _context.Users.Update(existingUser);
            await _context.SaveChangesAsync();
        }

        return _mapper.Map<UserDTO>(existingUser);
    }
}

