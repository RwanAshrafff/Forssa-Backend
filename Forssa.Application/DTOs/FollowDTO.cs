namespace Forssa.Application.DTOs;

public class FollowUserDTO
{
    public Guid UserId { get; set; }
    public string SupabaseId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Username { get; set; }
    public string? AvatarUrl { get; set; }
    public string? InstructorPosition { get; set; }
    public DateTime FollowedAt { get; set; }
}

public class HomeFeedDTO
{
    public string Role { get; set; } = string.Empty;
    public IReadOnlyList<HomeCourseFeedItemDTO> NewCourses { get; set; } = Array.Empty<HomeCourseFeedItemDTO>();
    public IReadOnlyList<HomeCertificateFeedItemDTO> NewCertificates { get; set; } = Array.Empty<HomeCertificateFeedItemDTO>();
}

public class HomeCourseFeedItemDTO
{
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid InstructorId { get; set; }
    public string? InstructorName { get; set; }
    public string? InstructorUsername { get; set; }
}

public class HomeCertificateFeedItemDTO
{
    public Guid StudentId { get; set; }
    public string? StudentName { get; set; }
    public string? StudentUsername { get; set; }
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; }
}

public class UserConnectionsDTO
{
    public Guid UserId { get; set; }
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }
    public IReadOnlyList<FollowUserDTO> Followers { get; set; } = Array.Empty<FollowUserDTO>();
    public IReadOnlyList<FollowUserDTO> Following { get; set; } = Array.Empty<FollowUserDTO>();
    public bool IsFollowedByCurrentUser { get; set; }
}