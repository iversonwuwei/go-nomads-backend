using AccommodationService.Domain.Entities;

namespace AccommodationService.Domain.Repositories;

/// <summary>
///     酒店仓储接口
/// </summary>
public interface IHotelRepository
{
    /// <summary>
    ///     根据ID获取酒店
    /// </summary>
    Task<Hotel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取酒店列表（分页）
    /// </summary>
    Task<(List<Hotel> Hotels, int TotalCount)> GetListAsync(
        int page = 1,
        int pageSize = 20,
        Guid? cityId = null,
        string? searchQuery = null,
        bool? hasWifi = null,
        bool? hasCoworkingSpace = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     根据城市ID获取酒店列表
    /// </summary>
    Task<List<Hotel>> GetByCityIdAsync(Guid cityId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     创建酒店
    /// </summary>
    Task<Hotel> CreateAsync(Hotel hotel, CancellationToken cancellationToken = default);

    /// <summary>
    ///     更新酒店
    /// </summary>
    Task<Hotel> UpdateAsync(Hotel hotel, CancellationToken cancellationToken = default);

    /// <summary>
    ///     删除酒店（软删除）
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     检查酒店是否存在
    /// </summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取用户创建的酒店
    /// </summary>
    Task<List<Hotel>> GetByCreatorAsync(Guid userId, CancellationToken cancellationToken = default);
}
