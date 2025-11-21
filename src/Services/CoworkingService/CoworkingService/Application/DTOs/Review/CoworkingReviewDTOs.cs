namespace CoworkingService.Application.DTOs.Review;

/// <summary>
/// 添加 Coworking 评论请求
/// </summary>
public class AddCoworkingReviewRequest
{
    /// <summary>
    /// 评分 (1.0 - 5.0)
    /// </summary>
    public double Rating { get; set; }

    /// <summary>
    /// 标题 (5-100 字符)
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 内容 (20-1000 字符)
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 访问日期 (可选)
    /// </summary>
    public DateTime? VisitDate { get; set; }

    /// <summary>
    /// 照片 URLs (最多 5 张)
    /// </summary>
    public List<string>? PhotoUrls { get; set; }
}

/// <summary>
/// 更新 Coworking 评论请求
/// </summary>
public class UpdateCoworkingReviewRequest
{
    /// <summary>
    /// 评分 (1.0 - 5.0)
    /// </summary>
    public double Rating { get; set; }

    /// <summary>
    /// 标题 (5-100 字符)
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 内容 (20-1000 字符)
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 访问日期 (可选)
    /// </summary>
    public DateTime? VisitDate { get; set; }

    /// <summary>
    /// 照片 URLs (最多 5 张)
    /// </summary>
    public List<string>? PhotoUrls { get; set; }
}

/// <summary>
/// Coworking 评论响应
/// </summary>
public class CoworkingReviewResponse
{
    /// <summary>
    /// 评论 ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 用户 ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 用户头像 URL
    /// </summary>
    public string? UserAvatar { get; set; }

    /// <summary>
    /// Coworking ID
    /// </summary>
    public Guid CoworkingId { get; set; }

    /// <summary>
    /// 评分
    /// </summary>
    public double Rating { get; set; }

    /// <summary>
    /// 标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 访问日期
    /// </summary>
    public DateTime? VisitDate { get; set; }

    /// <summary>
    /// 照片 URLs
    /// </summary>
    public List<string>? PhotoUrls { get; set; }

    /// <summary>
    /// 是否已验证
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// 分页评论列表响应
/// </summary>
public class PaginatedReviewsResponse
{
    /// <summary>
    /// 评论列表
    /// </summary>
    public List<CoworkingReviewResponse> Items { get; set; } = new();

    /// <summary>
    /// 总记录数
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 当前页码
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// 每页大小
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
