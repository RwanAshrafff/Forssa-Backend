using Forssa.Domain.Entities;

namespace Forssa.Domain.Entities;

public class User : BaseEntity
{
    public string SupabaseId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
}
