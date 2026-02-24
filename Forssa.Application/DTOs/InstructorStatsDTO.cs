namespace Forssa.Application.DTOs;

public class InstructorStatsDTO
{
    public int TotalStudents { get; set; }
    public int TotalCourses { get; set; }
    public decimal TotalRevenue { get; set; }
    public double TotalReviews { get; set; }
    public string? InstructorPosition { get; set; }
    public string? FullName { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
}
