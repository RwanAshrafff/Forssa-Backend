using Forssa.Domain.Entities;

namespace Forssa.Domain.Entities;

public class User : BaseEntity
{
    public string SupabaseId { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Role { get; private set; } = "Student";
    
    // Profile Metadata
    public string? FullName { get; private set; }
    public string? Bio { get; private set; }
    public string? AvatarUrl { get; private set; }
    public string? Username { get; private set; }

    // Required for EF Core
    private User() { }

    public User(string supabaseId, string email, string role)
    {
        SupabaseId = supabaseId;
        Email = email;
        Role = role;
    }

    public void UpdateProfile(string? fullName, string? bio, string? avatarUrl, string? username)
    {
        FullName = fullName;
        Bio = bio;
        AvatarUrl = avatarUrl;
        Username = username;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateEmail(string email)
    {
        if (Email != email)
        {
            Email = email;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void UpdateRole(string role)
    {
        if (Role != role)
        {
            Role = role;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}


