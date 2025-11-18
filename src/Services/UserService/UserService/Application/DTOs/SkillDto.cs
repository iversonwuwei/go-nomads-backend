namespace UserService.Application.DTOs;

/// <summary>
///     技能 DTO
/// </summary>
public class SkillDto
{
    /// <summary>
    ///     技能ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    ///     技能名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     类别
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    ///     描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    ///     创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
///     用户技能 DTO (包含关联信息)
/// </summary>
public class UserSkillDto
{
    /// <summary>
    ///     关联ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    ///     用户ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    ///     技能ID
    /// </summary>
    public string SkillId { get; set; } = string.Empty;

    /// <summary>
    ///     技能名称
    /// </summary>
    public string SkillName { get; set; } = string.Empty;

    /// <summary>
    ///     类别
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    ///     图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    ///     熟练度 (beginner, intermediate, advanced, expert)
    /// </summary>
    public string? ProficiencyLevel { get; set; }

    /// <summary>
    ///     经验年限
    /// </summary>
    public int? YearsOfExperience { get; set; }

    /// <summary>
    ///     创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
///     添加用户技能请求 DTO
/// </summary>
public class AddUserSkillRequest
{
    /// <summary>
    ///     技能ID
    /// </summary>
    public string SkillId { get; set; } = string.Empty;

    /// <summary>
    ///     熟练度 (beginner, intermediate, advanced, expert)
    /// </summary>
    public string? ProficiencyLevel { get; set; }

    /// <summary>
    ///     经验年限
    /// </summary>
    public int? YearsOfExperience { get; set; }
}

/// <summary>
///     按类别分组的技能 DTO
/// </summary>
public class SkillsByCategoryDto
{
    /// <summary>
    ///     类别名称
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    ///     该类别下的技能列表
    /// </summary>
    public List<SkillDto> Skills { get; set; } = new();
}