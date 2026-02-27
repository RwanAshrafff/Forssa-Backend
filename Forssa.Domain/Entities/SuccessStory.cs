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

    [Column("approval_status")]
    public string ApprovalStatus { get; set; } = "Pending";

    [Column("reviewed_by_admin_email")]
    public string? ReviewedByAdminEmail { get; set; }

    [Column("reviewed_at")]
    public DateTime? ReviewedAt { get; set; }

    [Column("review_note")]
    public string? ReviewNote { get; set; }

    // Relationships
    public User User { get; set; } = null!;
    public Course? Course { get; set; }
}

[Table("instructor_approval_requests")]
public class InstructorApprovalRequest : BaseEntity
{
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("requested_position")]
    public string? RequestedPosition { get; set; }

    [Column("status")]
    public string Status { get; set; } = "Pending";

    [Column("reviewed_by_admin_email")]
    public string? ReviewedByAdminEmail { get; set; }

    [Column("reviewed_at")]
    public DateTime? ReviewedAt { get; set; }

    [Column("review_note")]
    public string? ReviewNote { get; set; }

    public User User { get; set; } = null!;
}

[Table("financial_aid_documents")]
public class FinancialAidDocument : BaseEntity
{
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("document_url")]
    public string DocumentUrl { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("status")]
    public string Status { get; set; } = "Pending";

    [Column("reviewed_by_admin_email")]
    public string? ReviewedByAdminEmail { get; set; }

    [Column("reviewed_at")]
    public DateTime? ReviewedAt { get; set; }

    [Column("review_note")]
    public string? ReviewNote { get; set; }

    public User User { get; set; } = null!;
}
