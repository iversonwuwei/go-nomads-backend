using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Models;

/// <summary>
/// 评论实体模型(通用)
/// </summary>
[Table("reviews")]
public class Review
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("target_type")]
    public string TargetType { get; set; } = string.Empty; // city, coworking, hotel, event, product

    [Required]
    [Column("target_id")]
    public Guid TargetId { get; set; }

    [Required]
    [Column("rating", TypeName = "decimal(3,2)")]
    public decimal Rating { get; set; }

    [Column("content")]
    public string? Content { get; set; }

    [Column("images")]
    public string[]? Images { get; set; }

    [Column("helpful_count")]
    public int HelpfulCount { get; set; } = 0;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 收藏实体模型(通用)
/// </summary>
[Table("favorites")]
public class Favorite
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("target_type")]
    public string TargetType { get; set; } = string.Empty; // city, coworking, hotel, event, innovation, product

    [Required]
    [Column("target_id")]
    public Guid TargetId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 聊天消息实体模型
/// </summary>
[Table("chat_messages")]
public class ChatMessage
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    [Column("room_id")]
    public string RoomId { get; set; } = string.Empty;

    [Required]
    [Column("sender_id")]
    public Guid SenderId { get; set; }

    [Required]
    [Column("message")]
    public string Message { get; set; } = string.Empty;

    [MaxLength(20)]
    [Column("message_type")]
    public string MessageType { get; set; } = "text"; // text, image, file, system

    [Column("metadata", TypeName = "jsonb")]
    public string? Metadata { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 通知实体模型
/// </summary>
[Table("notifications")]
public class Notification
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("type")]
    public string Type { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("content")]
    public string? Content { get; set; }

    [Column("data", TypeName = "jsonb")]
    public string? Data { get; set; }

    [Column("is_read")]
    public bool IsRead { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
