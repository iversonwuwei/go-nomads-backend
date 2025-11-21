using CoworkingService.Domain.Entities;

namespace CoworkingService.Domain.Repositories;

/// <summary>
/// Coworking Review Repository 接口
/// </summary>
public interface ICoworkingReviewRepository
{
    /// <summary>
    /// 获取指定 Coworking 的评论列表（分页）
    /// </summary>
    Task<(List<CoworkingReview> Reviews, int TotalCount)> GetReviewsByCoworkingIdAsync(
        Guid coworkingId,
        int page,
        int pageSize);

    /// <summary>
    /// 根据 ID 获取评论
    /// </summary>
    Task<CoworkingReview?> GetByIdAsync(Guid reviewId);

    /// <summary>
    /// 获取用户对指定 Coworking 的评论
    /// </summary>
    Task<CoworkingReview?> GetUserReviewForCoworkingAsync(Guid coworkingId, Guid userId);

    /// <summary>
    /// 添加评论
    /// </summary>
    Task<CoworkingReview> AddAsync(CoworkingReview review);

    /// <summary>
    /// 更新评论
    /// </summary>
    Task<CoworkingReview> UpdateAsync(CoworkingReview review);

    /// <summary>
    /// 删除评论
    /// </summary>
    Task DeleteAsync(Guid reviewId);

    /// <summary>
    /// 计算 Coworking 的平均评分
    /// </summary>
    Task<(double AverageRating, int ReviewCount)> GetAverageRatingAsync(Guid coworkingId);
}
