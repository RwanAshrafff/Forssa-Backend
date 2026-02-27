using Forssa.Domain.Entities;

namespace Forssa.Domain.Entities;

public class User : BaseEntity
{
    public string SupabaseId { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Role { get; private set; } = "student";
    
    // Profile Metadata
    public string? FullName { get; private set; }
    public string? Bio { get; private set; }
    public string? AvatarUrl { get; private set; }
    public string? Username { get; private set; }
    public string? InstructorPosition { get; private set; }

    // Follow Counts
    public int FollowersCount { get; private set; }
    public int FollowingCount { get; private set; }

    // Gamification
    public int Points { get; private set; }

    // Required for EF Core
    private User() { }

    public User(string supabaseId, string email, string role)
    {
        SupabaseId = supabaseId;
        Email = email;
        Role = role;
    }

    public void AddPoints(int points)
    {
        Points += points;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateProfile(string? fullName, string? bio, string? avatarUrl, string? username, string? instructorPosition)
    {
        FullName = fullName;
        Bio = bio;
        AvatarUrl = avatarUrl;
        Username = username;
        InstructorPosition = instructorPosition;
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

    public void IncrementFollowersCount()
    {
        FollowersCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DecrementFollowersCount()
    {
        if (FollowersCount > 0)
            FollowersCount--;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementFollowingCount()
    {
        FollowingCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DecrementFollowingCount()
    {
        if (FollowingCount > 0)
            FollowingCount--;
        UpdatedAt = DateTime.UtcNow;
    }
}


