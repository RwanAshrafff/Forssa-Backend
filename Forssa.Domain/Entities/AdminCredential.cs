using System.ComponentModel.DataAnnotations.Schema;

namespace Forssa.Domain.Entities;

[Table("admin_credentials")]
public class AdminCredential : BaseEntity
{
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}
