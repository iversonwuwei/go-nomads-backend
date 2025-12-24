using AccommodationService.Domain.Entities;

namespace AccommodationService.Domain.Repositories;

/// <summary>
///     房型仓储接口
/// </summary>
public interface IRoomTypeRepository
{
    /// <summary>
    ///     根据ID获取房型
    /// </summary>
    Task<RoomType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取酒店的所有房型
    /// </summary>
    Task<List<RoomType>> GetByHotelIdAsync(Guid hotelId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     创建房型
    /// </summary>
    Task<RoomType> CreateAsync(RoomType roomType, CancellationToken cancellationToken = default);

    /// <summary>
    ///     批量创建房型
    /// </summary>
    Task<List<RoomType>> CreateManyAsync(List<RoomType> roomTypes, CancellationToken cancellationToken = default);

    /// <summary>
    ///     更新房型
    /// </summary>
    Task<RoomType> UpdateAsync(RoomType roomType, CancellationToken cancellationToken = default);

    /// <summary>
    ///     删除房型
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     删除酒店的所有房型
    /// </summary>
    Task<bool> DeleteByHotelIdAsync(Guid hotelId, CancellationToken cancellationToken = default);
}
