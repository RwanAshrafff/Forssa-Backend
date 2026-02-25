using System.ComponentModel.DataAnnotations.Schema;

namespace Forssa.Domain.Entities;

[Table("user_follows")]
public class UserFollow : BaseEntity
{
    [Column("follower_id")]
    public Guid FollowerId { get; set; }

    [Column("followed_id")]
    public Guid FollowedId { get; set; }

    public User Follower { get; set; } = null!;
    public User Followed { get; set; } = null!;
}