using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace InnovationService.Models;

/// <summary>
/// 创新项目实体模型
/// </summary>
[Table("innovations")]
public class Innovation : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Column("creator_id")]
    public Guid CreatorId { get; set; }

    [MaxLength(50)]
    [Column("category")]
    public string? Category { get; set; } // tech, business, social, environment, education, health, other

    [MaxLength(50)]
    [Column("stage")]
    public string Stage { get; set; } = "idea"; // idea, prototype, mvp, launched, scaling

    [Column("tags")]
    public string[]? Tags { get; set; }

    [Column("image_url")]
    public string? ImageUrl { get; set; }

    [Column("images")]
    public string[]? Images { get; set; }

    [Column("video_url")]
    public string? VideoUrl { get; set; }

    [Column("demo_url")]
    public string? DemoUrl { get; set; }

    [Column("github_url")]
    public string? GithubUrl { get; set; }

    [Column("website_url")]
    public string? WebsiteUrl { get; set; }

    [Column("team_size")]
    public int TeamSize { get; set; } = 1;

    [Column("looking_for")]
    public string[]? LookingFor { get; set; } // co-founder, developer, designer, investor, etc.

    [Column("skills_needed")]
    public string[]? SkillsNeeded { get; set; }

    [Column("like_count")]
    public int LikeCount { get; set; } = 0;

    [Column("view_count")]
    public int ViewCount { get; set; } = 0;

    [Column("comment_count")]
    public int CommentCount { get; set; } = 0;

    [Column("is_featured")]
    public bool IsFeatured { get; set; }

    [Column("is_public")]
    public bool IsPublic { get; set; } = true;

    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

    [Column("updated_by")]
    public Guid? UpdatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 创新项目点赞实体模型
/// </summary>
[Table("innovation_likes")]
public class InnovationLike : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Required]
    [Column("innovation_id")]
    public Guid InnovationId { get; set; }

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 创新项目评论实体模型
/// </summary>
[Table("innovation_comments")]
public class InnovationComment : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Required]
    [Column("innovation_id")]
    public Guid InnovationId { get; set; }

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("parent_id")]
    public Guid? ParentId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
