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

    #region 冗余字段更新方法（用于事件驱动的数据同步）

    /// <summary>
    ///     更新指定创建者的所有 Coworking 空间的创建者信息
    /// </summary>
    /// <param name="creatorId">创建者ID</param>
    /// <param name="creatorName">创建者名称</param>
    /// <param name="creatorAvatar">创建者头像</param>
    /// <returns>更新的记录数</returns>
    Task<int> UpdateCreatorInfoAsync(Guid creatorId, string? creatorName, string? creatorAvatar);

    /// <summary>
    ///     更新指定城市的所有 Coworking 空间的城市信息
    /// </summary>
    /// <param name="cityId">城市ID</param>
    /// <param name="cityName">城市名称</param>
    /// <param name="cityNameEn">城市英文名</param>
    /// <param name="cityCountry">城市所属国家</param>
    /// <returns>更新的记录数</returns>
    Task<int> UpdateCityInfoAsync(Guid cityId, string? cityName, string? cityNameEn, string? cityCountry);

    /// <summary>
    ///     为单个 Coworking 空间填充冗余字段（创建/更新时使用）
    /// </summary>
    Task FillRedundantFieldsAsync(CoworkingSpace coworkingSpace, string? creatorName, string? creatorAvatar,
        string? cityName, string? cityNameEn, string? cityCountry);

    /// <summary>
    ///     批量获取城市的 Coworking 空间数量（优化版：单次查询）
    /// </summary>
    /// <param name="cityIds">城市ID列表</param>
    /// <returns>城市ID到 Coworking 数量的映射</returns>
    Task<Dictionary<Guid, int>> GetCoworkingCountsByCityIdsAsync(List<Guid> cityIds);

    #endregion
}