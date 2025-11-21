using CoworkingService.Application.DTOs.Review;

namespace CoworkingService.Application.Services;

/// <summary>
/// Coworking 评论应用服务接口
/// </summary>
public interface ICoworkingReviewService
{
    /// <summary>
    /// 获取 Coworking 评论列表（分页）
    /// </summary>
    Task<PaginatedReviewsResponse> GetReviewsByCoworkingIdAsync(Guid coworkingId, int page, int pageSize);

    /// <summary>
    /// 获取评论详情
    /// </summary>
    Task<CoworkingReviewResponse?> GetReviewByIdAsync(Guid reviewId);

    /// <summary>
    /// 获取当前用户对某 Coworking 的评论
    /// </summary>
    Task<CoworkingReviewResponse?> GetUserReviewForCoworkingAsync(Guid coworkingId, Guid userId);

    /// <summary>
    /// 添加评论
    /// </summary>
    Task<CoworkingReviewResponse> AddReviewAsync(Guid coworkingId, Guid userId, AddCoworkingReviewRequest request);

    /// <summary>
    /// 更新评论
    /// </summary>
    Task<CoworkingReviewResponse> UpdateReviewAsync(Guid reviewId, Guid userId, UpdateCoworkingReviewRequest request);

    /// <summary>
    /// 删除评论
    /// </summary>
    Task DeleteReviewAsync(Guid reviewId, Guid userId);

    /// <summary>
    /// 获取 Coworking 平均评分
    /// </summary>
    Task<(double AverageRating, int ReviewCount)> GetAverageRatingAsync(Guid coworkingId);
}
