using Forssa.Application.DTOs;

namespace Forssa.Application.Interfaces;

public interface IInstructorService
{
    Task<InstructorStatsDTO> GetInstructorStatsAsync(string supabaseUserId);
}
