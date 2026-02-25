using System.ComponentModel.DataAnnotations.Schema;

namespace Forssa.Domain.Entities;

[Table("courses")]
public class Course : BaseEntity
{
    [Column("title")]
    public string Title { get; set; } = string.Empty;
    [Column("description")]
    public string? Description { get; set; }
    [Column("category")]
    public string? Category { get; set; }
    [Column("level")]
    public string? Level { get; set; }
    [Column("thumbnail_url")]
    public string? ThumbnailUrl { get; set; }
    [Column("price")]
    public decimal Price { get; set; }
    [Column("instructor_id")]
    public Guid Instructor_id { get; set; }
    
    // Relationships
    public ICollection<Module> Modules { get; set; } = new List<Module>();
}

[Table("modules")]
public class Module : BaseEntity
{
    [Column("course_id")]
    public Guid Course_id { get; set; }
    [Column("title")]
    public string Title { get; set; } = string.Empty;
    [Column("position")]
    public int Position { get; set; }

    // Relationships
    public Course Course { get; set; } = null!;
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
}

[Table("lessons")]
public class Lesson : BaseEntity
{
    [Column("module_id")]
    public Guid Module_id { get; set; }
    [Column("title")]
    public string Title { get; set; } = string.Empty;
    [Column("video_url")]
    public string? Video_url { get; set; }
    [Column("position")]
    public int Position { get; set; }
    [Column("duration")]
    public string? Duration { get; set; } = "0:00";

    // Relationships
    public Module Module { get; set; } = null!;
}

[Table("lesson_progress")]
public class LessonProgress : BaseEntity
{
    [Column("user_id")]
    public Guid UserId { get; set; }
    [Column("lesson_id")]
    public Guid LessonId { get; set; }
    [Column("is_completed")]
    public bool IsCompleted { get; set; } = true;
    [Column("completed_at")]
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}

[Table("lesson_notes")]
public class LessonNote : BaseEntity
{
    [Column("user_id")]
    public Guid UserId { get; set; }
    [Column("lesson_id")]
    public Guid LessonId { get; set; }
    [Column("content")]
    public string Content { get; set; } = string.Empty;
}


[Table("enrollments")]
public class Enrollment : BaseEntity
{
    [Column("student_id")]
    public Guid StudentId { get; set; }
    [Column("course_id")]
    public Guid CourseId { get; set; }
    [Column("is_points_awarded")]
    public bool IsPointsAwarded { get; set; } = false;

    // Relationships
    public User Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
}
