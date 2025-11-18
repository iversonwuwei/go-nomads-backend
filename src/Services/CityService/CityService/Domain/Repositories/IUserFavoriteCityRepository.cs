using CityService.Domain.Entities;

namespace CityService.Domain.Repositories;

/// <summary>
///     用户收藏城市仓储接口
/// </summary>
public interface IUserFavoriteCityRepository
{
    /// <summary>
    ///     检查城市是否已被用户收藏
    /// </summary>
    Task<bool> IsCityFavoritedAsync(Guid userId, string cityId);

    /// <summary>
    ///     添加收藏城市
    /// </summary>
    Task<UserFavoriteCity> AddFavoriteCityAsync(Guid userId, string cityId);

    /// <summary>
    ///     移除收藏城市
    /// </summary>
    Task<bool> RemoveFavoriteCityAsync(Guid userId, string cityId);

    /// <summary>
    ///     获取用户收藏的城市ID列表
    /// </summary>
    Task<List<string>> GetUserFavoriteCityIdsAsync(Guid userId);

    /// <summary>
    ///     获取用户收藏的城市列表（分页）
    /// </summary>
    Task<(List<UserFavoriteCity> Items, int Total)> GetUserFavoriteCitiesAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    ///     获取城市被收藏的次数
    /// </summary>
    Task<int> GetCityFavoriteCountAsync(string cityId);
}