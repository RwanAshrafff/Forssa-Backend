namespace Forssa.Application.DTOs;

public class UserDTO : BaseDTO
{
    public string SupabaseId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
}
