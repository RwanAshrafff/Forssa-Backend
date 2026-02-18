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
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.SupabaseId == supabaseId);

        if (existingUser == null)
        {
            // New user - create with role if provided
            var newUser = new User
            {
                SupabaseId = supabaseId,
                Email = email,
                Role = role ?? "Student" // Default to Student if not specified, aligning with typical signup
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();
            return _mapper.Map<UserDTO>(newUser);
        }

        // Existing user - update role and email if they changed
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
            await _context.SaveChangesAsync();
        }

        return _mapper.Map<UserDTO>(existingUser);
    }
}
