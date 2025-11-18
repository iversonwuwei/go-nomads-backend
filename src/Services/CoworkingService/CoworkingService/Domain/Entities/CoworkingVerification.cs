using Postgrest.Attributes;
using Postgrest.Models;

namespace CoworkingService.Domain.Entities;

/// <summary>
///     记录普通用户对 Coworking 空间的认证投票
/// </summary>
[Table("coworking_verifications")]
public class CoworkingVerification : BaseModel
{
    [PrimaryKey("id")] public Guid Id { get; set; }

    [Column("coworking_id")] public Guid CoworkingId { get; set; }

    [Column("user_id")] public Guid UserId { get; set; }

    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public static CoworkingVerification Create(Guid coworkingId, Guid userId)
    {
        if (coworkingId == Guid.Empty) throw new ArgumentException("coworkingId 不能为空", nameof(coworkingId));
        if (userId == Guid.Empty) throw new ArgumentException("userId 不能为空", nameof(userId));

        return new CoworkingVerification
        {
            Id = Guid.NewGuid(),
            CoworkingId = coworkingId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
    }
}
