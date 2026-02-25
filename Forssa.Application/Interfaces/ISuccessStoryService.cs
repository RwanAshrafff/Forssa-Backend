using Forssa.Application.DTOs;

namespace Forssa.Application.Interfaces;

public interface ISuccessStoryService
{
    Task<IEnumerable<SuccessStoryDTO>> GetAllAsync();
    Task<SuccessStoryDTO> CreateAsync(string supabaseId, CreateSuccessStoryDTO createDto);
    Task<bool> CanUserSubmitStoryAsync(string supabaseId);
}
