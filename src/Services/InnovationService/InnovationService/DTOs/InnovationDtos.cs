namespace InnovationService.DTOs;

/// <summary>
///     创新项目响应 DTO
/// </summary>
public class InnovationResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ElevatorPitch { get; set; }
    public string? Problem { get; set; }
    public string? Solution { get; set; }
    public string? TargetAudience { get; set; }
    public string? ProductType { get; set; }
    public string? KeyFeatures { get; set; }
    public string? CompetitiveAdvantage { get; set; }
    public string? BusinessModel { get; set; }
    public string? MarketOpportunity { get; set; }
    public string? Ask { get; set; }
    public Guid CreatorId { get; set; }
    public string? CreatorName { get; set; }
    public string? CreatorAvatar { get; set; }
    public string? Category { get; set; }
    public string Stage { get; set; } = "idea";
    public string[]? Tags { get; set; }
    public string? ImageUrl { get; set; }
    public string[]? Images { get; set; }
    public string? VideoUrl { get; set; }
    public string? DemoUrl { get; set; }
    public string? GithubUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public int TeamSize { get; set; }
    public List<TeamMemberResponse> Team { get; set; } = new();
    public string[]? LookingFor { get; set; }
    public string[]? SkillsNeeded { get; set; }
    public int LikeCount { get; set; }
    public int ViewCount { get; set; }
    public int CommentCount { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsPublic { get; set; }
    public bool IsLiked { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
///     创新项目列表项 DTO（简化版）
/// </summary>
public class InnovationListItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ElevatorPitch { get; set; }
    public string? Category { get; set; }
    public string Stage { get; set; } = "idea";
    public string? ImageUrl { get; set; }
    public Guid CreatorId { get; set; }
    public string? CreatorName { get; set; }
    public string? CreatorAvatar { get; set; }
    public int TeamSize { get; set; }
    public int LikeCount { get; set; }
    public int ViewCount { get; set; }
    public int CommentCount { get; set; }
    public bool IsFeatured { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
///     团队成员响应 DTO
/// </summary>
public class TeamMemberResponse
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsFounder { get; set; }
}

/// <summary>
///     创建创新项目请求 DTO
/// </summary>
public class CreateInnovationRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ElevatorPitch { get; set; }
    public string? Problem { get; set; }
    public string? Solution { get; set; }
    public string? TargetAudience { get; set; }
    public string? ProductType { get; set; }
    public string? KeyFeatures { get; set; }
    public string? CompetitiveAdvantage { get; set; }
    public string? BusinessModel { get; set; }
    public string? MarketOpportunity { get; set; }
    public string? Ask { get; set; }
    public string? Category { get; set; }
    public string Stage { get; set; } = "idea";
    public string[]? Tags { get; set; }
    public string? ImageUrl { get; set; }
    public string[]? Images { get; set; }
    public string? VideoUrl { get; set; }
    public string? DemoUrl { get; set; }
    public string? GithubUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public string[]? LookingFor { get; set; }
    public string[]? SkillsNeeded { get; set; }
    public bool IsPublic { get; set; } = true;
    public List<TeamMemberRequest>? Team { get; set; }
}

/// <summary>
///     更新创新项目请求 DTO
/// </summary>
public class UpdateInnovationRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? ElevatorPitch { get; set; }
    public string? Problem { get; set; }
    public string? Solution { get; set; }
    public string? TargetAudience { get; set; }
    public string? ProductType { get; set; }
    public string? KeyFeatures { get; set; }
    public string? CompetitiveAdvantage { get; set; }
    public string? BusinessModel { get; set; }
    public string? MarketOpportunity { get; set; }
    public string? Ask { get; set; }
    public string? Category { get; set; }
    public string? Stage { get; set; }
    public string[]? Tags { get; set; }
    public string? ImageUrl { get; set; }
    public string[]? Images { get; set; }
    public string? VideoUrl { get; set; }
    public string? DemoUrl { get; set; }
    public string? GithubUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public string[]? LookingFor { get; set; }
    public string[]? SkillsNeeded { get; set; }
    public bool? IsPublic { get; set; }
}

/// <summary>
///     团队成员请求 DTO
/// </summary>
public class TeamMemberRequest
{
    public Guid? UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsFounder { get; set; }
}

/// <summary>
///     创建评论请求 DTO
/// </summary>
public class CreateCommentRequest
{
    public string Content { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
}

/// <summary>
///     评论响应 DTO
/// </summary>
public class CommentResponse
{
    public Guid Id { get; set; }
    public Guid InnovationId { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserAvatar { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
///     API 响应包装器
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
}

/// <summary>
///     分页响应
/// </summary>
public class PagedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
}
