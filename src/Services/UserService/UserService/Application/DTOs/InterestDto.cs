namespace UserService.Application.DTOs;

/// <summary>
/// 兴趣爱好 DTO
/// </summary>
public class InterestDto
{
    /// <summary>
    /// 兴趣ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 兴趣名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 类别
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 用户兴趣 DTO (包含关联信息)
/// </summary>
public class UserInterestDto
{
    /// <summary>
    /// 关联ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 用户ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 兴趣ID
    /// </summary>
    public string InterestId { get; set; } = string.Empty;

    /// <summary>
    /// 兴趣名称
    /// </summary>
    public string InterestName { get; set; } = string.Empty;

    /// <summary>
    /// 类别
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// 图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 强度 (casual, moderate, passionate)
    /// </summary>
    public string? IntensityLevel { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 添加用户兴趣请求 DTO
/// </summary>
public class AddUserInterestRequest
{
    /// <summary>
    /// 兴趣ID
    /// </summary>
    public string InterestId { get; set; } = string.Empty;

    /// <summary>
    /// 强度 (casual, moderate, passionate)
    /// </summary>
    public string? IntensityLevel { get; set; }
}

/// <summary>
/// 按类别分组的兴趣 DTO
/// </summary>
public class InterestsByCategoryDto
{
    /// <summary>
    /// 类别名称
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// 该类别下的兴趣列表
    /// </summary>
    public List<InterestDto> Interests { get; set; } = new();
}
