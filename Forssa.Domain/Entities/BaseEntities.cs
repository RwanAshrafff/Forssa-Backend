using System.ComponentModel.DataAnnotations.Schema;

namespace Forssa.Domain.Entities;

public abstract class BaseEntity
{
    [Column("id")]
    public Guid Id { get; set; }
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
    [Column("is_deleted")]
    public bool IsDeleted { get; set; }
    [NotMapped]
    public DateTime? DeletedAt { get; set; }
}

