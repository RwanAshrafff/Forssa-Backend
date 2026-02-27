namespace Forssa.Application.DTOs;

public class UserDTO : BaseDTO
{
    public string SupabaseId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Student";
    public string? FullName { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Username { get; set; }
    public string? InstructorPosition { get; set; }
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }
    public bool HasPendingInstructorRequest { get; set; }
    public bool HasPendingFinancialAid { get; set; }
    public bool HasApprovedFinancialAid { get; set; }
    public bool HasPendingSuccessStory { get; set; }
}

