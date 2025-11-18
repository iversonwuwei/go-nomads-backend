using CityService.Domain.Entities;

namespace CityService.Domain.Repositories;

/// <summary>
///     用户城市费用仓储接口
/// </summary>
public interface IUserCityExpenseRepository
{
    /// <summary>
    ///     添加费用
    /// </summary>
    Task<UserCityExpense> CreateAsync(UserCityExpense expense);

    /// <summary>
    ///     获取城市的所有费用
    /// </summary>
    Task<IEnumerable<UserCityExpense>> GetByCityIdAsync(string cityId);

    /// <summary>
    ///     获取城市某个用户的费用
    /// </summary>
    Task<IEnumerable<UserCityExpense>> GetByCityIdAndUserIdAsync(string cityId, Guid userId);

    /// <summary>
    ///     获取用户的所有费用
    /// </summary>
    Task<IEnumerable<UserCityExpense>> GetByUserIdAsync(Guid userId);

    /// <summary>
    ///     根据 ID 获取费用
    /// </summary>
    Task<UserCityExpense?> GetByIdAsync(Guid id);

    /// <summary>
    ///     删除费用
    /// </summary>
    Task<bool> DeleteAsync(Guid id, Guid userId);
}