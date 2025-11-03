using Postgrest.Attributes;
using Postgrest.Models;

namespace UserService.Domain.Entities;

/// <summary>
/// 技能实体
/// </summary>
[Table("skills")]
public class Skill : BaseModel
{
    [PrimaryKey("id")]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("category")]
    public string Category { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("icon")]
    public string? Icon { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 用户技能关联实体
/// </summary>
[Table("user_skills")]
public class UserSkill : BaseModel
{
    [PrimaryKey("id")]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("skill_id")]
    public string SkillId { get; set; } = string.Empty;

    [Column("proficiency_level")]
    public string? ProficiencyLevel { get; set; }

    [Column("years_of_experience")]
    public int? YearsOfExperience { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
