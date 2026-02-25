namespace Forssa.Application.DTOs;

public class SuccessStoryDTO : BaseDTO
{
    public Guid UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public string UserAvatarUrl { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Rating { get; set; }
    public Guid? CourseId { get; set; }
    public string? CourseTitle { get; set; }
    public string? Achievement { get; set; }
}

public class CreateSuccessStoryDTO
{
    public string Content { get; set; } = string.Empty;
    public int Rating { get; set; }
    public Guid? CourseId { get; set; }
    public string? Achievement { get; set; }
}
