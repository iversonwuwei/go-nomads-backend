using CityService.Domain.Entities;

namespace CityService.Domain.Repositories;

/// <summary>
/// 用户城市评论仓储接口
/// </summary>
public interface IUserCityReviewRepository
{
    /// <summary>
    /// 创建新评论(每次都插入新记录,允许同一个用户对同一城市多次评论)
    /// </summary>
    Task<UserCityReview> CreateAsync(UserCityReview review);

    /// <summary>
    /// 获取城市的所有评论
    /// </summary>
    Task<IEnumerable<UserCityReview>> GetByCityIdAsync(string cityId);

    /// <summary>
    /// 获取用户对某个城市的所有评论(返回多条)
    /// </summary>
    Task<IEnumerable<UserCityReview>> GetByCityIdAndUserIdAsync(string cityId, Guid userId);

    /// <summary>
    /// 删除评论(根据 reviewId 删除)
    /// </summary>
    Task<bool> DeleteAsync(Guid reviewId, Guid userId);

    /// <summary>
    /// 获取城市的平均评分
    /// </summary>
    Task<decimal?> GetAverageRatingAsync(string cityId);
}
