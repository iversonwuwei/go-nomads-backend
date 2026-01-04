using UserService.Application.DTOs;

namespace UserService.Application.Services;

/// <summary>
///     访问地点服务接口
/// </summary>
public interface IVisitedPlaceService
{
    /// <summary>
    ///     获取旅行的访问地点列表
    /// </summary>
    Task<List<VisitedPlaceDto>> GetVisitedPlacesByTravelHistoryIdAsync(
        string travelHistoryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取旅行的精选地点
    /// </summary>
    Task<List<VisitedPlaceDto>> GetHighlightPlacesByTravelHistoryIdAsync(
        string travelHistoryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取用户的所有访问地点（分页）
    /// </summary>
    Task<(List<VisitedPlaceDto> Items, int Total)> GetUserVisitedPlacesAsync(
        string userId,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取访问地点详情
    /// </summary>
    Task<VisitedPlaceDto?> GetVisitedPlaceByIdAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     创建访问地点
    /// </summary>
    Task<VisitedPlaceDto> CreateVisitedPlaceAsync(
        string userId,
        CreateVisitedPlaceDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     批量创建访问地点（用于同步）
    /// </summary>
    Task<List<VisitedPlaceDto>> CreateBatchVisitedPlacesAsync(
        string userId,
        BatchCreateVisitedPlaceDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     更新访问地点
    /// </summary>
    Task<VisitedPlaceDto?> UpdateVisitedPlaceAsync(
        string id,
        string userId,
        UpdateVisitedPlaceDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     删除访问地点
    /// </summary>
    Task<bool> DeleteVisitedPlaceAsync(
        string id,
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     标记/取消标记为精选地点
    /// </summary>
    Task<VisitedPlaceDto?> ToggleHighlightAsync(
        string id,
        string userId,
        bool isHighlight,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取旅行访问地点统计
    /// </summary>
    Task<TravelVisitedPlaceStatsDto> GetVisitedPlaceStatsAsync(
        string travelHistoryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取城市访问摘要 - 用于 Visited Places 页面（包含城市信息、天气、评分、花费、共享办公数量、访问地点列表）
    /// </summary>
    Task<VisitedPlacesCitySummaryDto> GetCitySummaryAsync(
        string userId,
        string cityId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
}
