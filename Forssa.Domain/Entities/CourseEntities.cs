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

    // Relationships
    public Module Module { get; set; } = null!;
}


