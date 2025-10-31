using CityService.Domain.Entities;

namespace CityService.Domain.Repositories;

/// <summary>
/// 用户城市评论仓储接口
/// </summary>
public interface IUserCityReviewRepository
{
    /// <summary>
    /// 创建或更新评论
    /// </summary>
    Task<UserCityReview> UpsertAsync(UserCityReview review);

    /// <summary>
    /// 获取城市的所有评论
    /// </summary>
    Task<IEnumerable<UserCityReview>> GetByCityIdAsync(string cityId);

    /// <summary>
    /// 获取用户对某个城市的评论
    /// </summary>
    Task<UserCityReview?> GetByCityIdAndUserIdAsync(string cityId, Guid userId);

    /// <summary>
    /// 删除评论
    /// </summary>
    Task<bool> DeleteAsync(string cityId, Guid userId);

    /// <summary>
    /// 获取城市的平均评分
    /// </summary>
    Task<decimal?> GetAverageRatingAsync(string cityId);
}
