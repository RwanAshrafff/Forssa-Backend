using System.ComponentModel.DataAnnotations.Schema;

namespace Forssa.Domain.Entities;

[Table("quizzes")]
public class Quiz : BaseEntity
{
    [Column("module_id")]
    public Guid ModuleId { get; set; }
    [Column("title")]
    public string Title { get; set; } = string.Empty;
    [Column("description")]
    public string? Description { get; set; }
    [Column("passing_score")]
    public decimal PassingScore { get; set; } = 80; // Default to 80%
    [Column("position")]
    public int Position { get; set; }

    // Relationships
    public Module Module { get; set; } = null!;
    public ICollection<Question> Questions { get; set; } = new List<Question>();
}

[Table("questions")]
public class Question : BaseEntity
{
    [Column("quiz_id")]
    public Guid QuizId { get; set; }
    [Column("text")]
    public string Text { get; set; } = string.Empty;
    [Column("points")]
    public int Points { get; set; } = 1;

    // Relationships
    public Quiz Quiz { get; set; } = null!;
    public ICollection<Option> Options { get; set; } = new List<Option>();
}

[Table("options")]
public class Option : BaseEntity
{
    [Column("question_id")]
    public Guid QuestionId { get; set; }
    [Column("text")]
    public string Text { get; set; } = string.Empty;
    [Column("is_correct")]
    public bool IsCorrect { get; set; }

    // Relationships
    public Question Question { get; set; } = null!;
}

[Table("quiz_submissions")]
public class QuizSubmission : BaseEntity
{
    [Column("user_id")]
    public Guid UserId { get; set; }
    [Column("quiz_id")]
    public Guid QuizId { get; set; }
    [Column("score")]
    public decimal Score { get; set; }
    [Column("is_passed")]
    public bool IsPassed { get; set; }
    [Column("submitted_at")]
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    // Relationships
    public User User { get; set; } = null!;
    public Quiz Quiz { get; set; } = null!;
}
