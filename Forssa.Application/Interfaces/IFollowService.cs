using Forssa.Application.DTOs;

namespace Forssa.Application.Interfaces;

public interface IFollowService
{
    Task FollowAsync(string currentSupabaseId, Guid targetUserId);
    Task UnfollowAsync(string currentSupabaseId, Guid targetUserId);
    Task<IReadOnlyList<FollowUserDTO>> GetFollowingAsync(string currentSupabaseId);
    Task<IReadOnlyList<FollowUserDTO>> GetFollowersAsync(string currentSupabaseId);
    Task<IReadOnlyList<FollowUserDTO>> GetUserFollowingAsync(Guid userId);
    Task<IReadOnlyList<FollowUserDTO>> GetUserFollowersAsync(Guid userId);
    Task<HomeFeedDTO> GetHomeFeedAsync(string currentSupabaseId);
    Task<UserConnectionsDTO> GetUserConnectionsAsync(string? currentSupabaseId, Guid targetUserId);
}