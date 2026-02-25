using System.ComponentModel.DataAnnotations.Schema;

namespace Forssa.Domain.Entities;

[Table("success_stories")]
public class SuccessStory : BaseEntity
{
    [Column("user_id")]
    public Guid UserId { get; set; }
    
    [Column("content")]
    public string Content { get; set; } = string.Empty;
    
    [Column("rating")]
    public int Rating { get; set; }
    
    [Column("course_id")]
    public Guid? CourseId { get; set; }
    
    [Column("achievement")]
    public string? Achievement { get; set; }

    // Relationships
    public User User { get; set; } = null!;
    public Course? Course { get; set; }
}
