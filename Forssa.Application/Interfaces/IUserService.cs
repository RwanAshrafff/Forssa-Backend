using Forssa.Application.DTOs;
using Forssa.Application.Interfaces;
using Forssa.Domain.Entities;

namespace Forssa.Application.Interfaces;

public interface IUserService : IBaseService<User, UserDTO>
{
    Task<UserDTO> SyncUserAsync(string supabaseId, string email, string? role = null, string? fullName = null, string? bio = null, string? avatarUrl = null, string? username = null, string? instructorPosition = null);
}
