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
}

