using CityService.Domain.Entities;

namespace CityService.Domain.Repositories;

/// <summary>
///     用户城市照片仓储接口
/// </summary>
public interface IUserCityPhotoRepository
{
    /// <summary>
    ///     添加照片
    /// </summary>
    Task<UserCityPhoto> CreateAsync(UserCityPhoto photo);

    /// <summary>
    ///     批量添加照片
    /// </summary>
    Task<IEnumerable<UserCityPhoto>> CreateBatchAsync(IEnumerable<UserCityPhoto> photos);

    /// <summary>
    ///     获取城市的所有照片
    /// </summary>
    Task<IEnumerable<UserCityPhoto>> GetByCityIdAsync(string cityId);

    /// <summary>
    ///     获取城市某个用户的照片
    /// </summary>
    Task<IEnumerable<UserCityPhoto>> GetByCityIdAndUserIdAsync(string cityId, Guid userId);

    /// <summary>
    ///     获取用户的所有照片
    /// </summary>
    Task<IEnumerable<UserCityPhoto>> GetByUserIdAsync(Guid userId);

    /// <summary>
    ///     根据 ID 获取照片
    /// </summary>
    Task<UserCityPhoto?> GetByIdAsync(Guid id);

    /// <summary>
    ///     删除照片
    /// </summary>
    Task<bool> DeleteAsync(Guid id, Guid userId);
}