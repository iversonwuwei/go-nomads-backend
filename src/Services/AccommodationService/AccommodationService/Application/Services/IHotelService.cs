using AccommodationService.Application.DTOs;

namespace AccommodationService.Application.Services;

/// <summary>
///     酒店服务接口
/// </summary>
public interface IHotelService
{
    /// <summary>
    ///     获取酒店详情
    /// </summary>
    Task<HotelDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取酒店列表（分页）
    /// </summary>
    Task<HotelListResponse> GetListAsync(HotelQueryParameters parameters, CancellationToken cancellationToken = default);

    /// <summary>
    ///     根据城市ID获取酒店列表
    /// </summary>
    Task<List<HotelDto>> GetByCityIdAsync(Guid cityId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     创建酒店
    /// </summary>
    Task<HotelDto> CreateAsync(CreateHotelRequest request, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     更新酒店（只有创建者或管理员可以更新）
    /// </summary>
    Task<HotelDto?> UpdateAsync(Guid id, UpdateHotelRequest request, Guid userId, bool isAdmin = false, CancellationToken cancellationToken = default);

    /// <summary>
    ///     删除酒店（只有创建者或管理员可以删除）
    /// </summary>
    Task<bool> DeleteAsync(Guid id, Guid userId, bool isAdmin = false, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取用户创建的酒店
    /// </summary>
    Task<List<HotelDto>> GetMyHotelsAsync(Guid userId, CancellationToken cancellationToken = default);

    // ============================================================
    // 房型相关方法
    // ============================================================

    /// <summary>
    ///     获取酒店的房型列表
    /// </summary>
    Task<List<RoomTypeDto>> GetRoomTypesAsync(Guid hotelId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取房型详情
    /// </summary>
    Task<RoomTypeDto?> GetRoomTypeByIdAsync(Guid roomTypeId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     创建房型
    /// </summary>
    Task<RoomTypeDto> CreateRoomTypeAsync(Guid hotelId, CreateRoomTypeRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    ///     更新房型
    /// </summary>
    Task<RoomTypeDto?> UpdateRoomTypeAsync(Guid roomTypeId, UpdateRoomTypeRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    ///     删除房型
    /// </summary>
    Task<bool> DeleteRoomTypeAsync(Guid roomTypeId, CancellationToken cancellationToken = default);
}
