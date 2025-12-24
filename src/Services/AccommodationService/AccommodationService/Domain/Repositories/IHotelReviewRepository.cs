using AccommodationService.Domain.Entities;

namespace AccommodationService.Domain.Repositories;

/// <summary>
///     酒店评论仓储接口
/// </summary>
public interface IHotelReviewRepository
{
    /// <summary>
    ///     根据ID获取评论
    /// </summary>
    Task<HotelReview?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取酒店的评论列表（分页）
    /// </summary>
    Task<(List<HotelReview> Reviews, int TotalCount)> GetByHotelIdAsync(
        Guid hotelId,
        int page = 1,
        int pageSize = 10,
        string? sortBy = "newest",
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取用户对某酒店的评论
    /// </summary>
    Task<HotelReview?> GetUserReviewForHotelAsync(
        Guid hotelId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取用户的所有评论
    /// </summary>
    Task<List<HotelReview>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     创建评论
    /// </summary>
    Task<HotelReview> CreateAsync(HotelReview review, CancellationToken cancellationToken = default);

    /// <summary>
    ///     更新评论
    /// </summary>
    Task<HotelReview> UpdateAsync(HotelReview review, CancellationToken cancellationToken = default);

    /// <summary>
    ///     删除评论
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取酒店评分统计
    /// </summary>
    Task<(double AverageRating, int TotalCount, Dictionary<int, int> Distribution)> GetRatingStatsAsync(
        Guid hotelId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     增加评论的帮助数
    /// </summary>
    Task<bool> IncrementHelpfulCountAsync(Guid reviewId, CancellationToken cancellationToken = default);
}
