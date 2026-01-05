using CoworkingService.Domain.Entities;

namespace CoworkingService.Domain.Repositories;

/// <summary>
///     CoworkingSpace 仓储接口
///     定义领域层的数据访问契约
/// </summary>
public interface ICoworkingRepository
{
    /// <summary>
    ///     创建共享办公空间
    /// </summary>
    Task<CoworkingSpace> CreateAsync(CoworkingSpace coworkingSpace);

    /// <summary>
    ///     根据 ID 获取共享办公空间
    /// </summary>
    Task<CoworkingSpace?> GetByIdAsync(Guid id);

    /// <summary>
    ///     更新共享办公空间
    /// </summary>
    Task<CoworkingSpace> UpdateAsync(CoworkingSpace coworkingSpace);

    /// <summary>
    ///     删除共享办公空间（逻辑删除）
    /// </summary>
    /// <param name="id">共享办公空间ID</param>
    /// <param name="deletedBy">删除操作执行者ID</param>
    Task DeleteAsync(Guid id, Guid? deletedBy = null);

    /// <summary>
    ///     获取共享办公空间列表（分页）
    /// </summary>
    Task<(List<CoworkingSpace> Items, int TotalCount)> GetListAsync(
        int page = 1,
        int pageSize = 20,
        Guid? cityId = null,
        bool? isActive = true);

    /// <summary>
    ///     根据城市 ID 获取共享办公空间列表
    /// </summary>
    Task<List<CoworkingSpace>> GetByCityIdAsync(Guid cityId);

    /// <summary>
    ///     搜索共享办公空间
    /// </summary>
    Task<List<CoworkingSpace>> SearchAsync(
        string searchTerm,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    ///     按价格范围获取共享办公空间
    /// </summary>
    Task<List<CoworkingSpace>> GetByPriceRangeAsync(
        decimal? minPrice,
        decimal? maxPrice,
        string priceType = "day");

    /// <summary>
    ///     获取评分最高的共享办公空间
    /// </summary>
    Task<List<CoworkingSpace>> GetTopRatedAsync(int limit = 10);

    /// <summary>
    ///     检查共享办公空间是否存在
    /// </summary>
    Task<bool> ExistsAsync(Guid id);
}