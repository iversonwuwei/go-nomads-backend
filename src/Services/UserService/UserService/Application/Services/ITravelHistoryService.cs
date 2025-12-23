using UserService.Application.DTOs;
using UserService.Domain.Repositories;

namespace UserService.Application.Services;

/// <summary>
///     旅行历史服务接口
/// </summary>
public interface ITravelHistoryService
{
    /// <summary>
    ///     获取用户旅行历史（分页）
    /// </summary>
    Task<(List<TravelHistoryDto> Items, int Total)> GetUserTravelHistoryAsync(
        string userId,
        int page = 1,
        int pageSize = 20,
        bool? isConfirmed = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取用户已确认的旅行历史（用于在 profile 中显示）
    /// </summary>
    Task<List<TravelHistoryDto>> GetConfirmedTravelHistoryAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取用户最新一条已确认的旅行历史（用于 Profile 页面显示）
    /// </summary>
    Task<TravelHistoryDto?> GetLatestTravelHistoryAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取用户未确认的旅行历史（待确认的自动检测行程）
    /// </summary>
    Task<List<TravelHistoryDto>> GetUnconfirmedTravelHistoryAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取旅行历史详情
    /// </summary>
    Task<TravelHistoryDto?> GetTravelHistoryByIdAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     创建旅行历史记录
    /// </summary>
    Task<TravelHistoryDto> CreateTravelHistoryAsync(
        string userId,
        CreateTravelHistoryDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     批量创建旅行历史记录（用于同步自动检测的行程）
    /// </summary>
    Task<List<TravelHistoryDto>> CreateBatchTravelHistoryAsync(
        string userId,
        BatchCreateTravelHistoryDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     更新旅行历史记录
    /// </summary>
    Task<TravelHistoryDto> UpdateTravelHistoryAsync(
        string id,
        UpdateTravelHistoryDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     删除旅行历史记录
    /// </summary>
    Task<bool> DeleteTravelHistoryAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     确认旅行历史记录
    /// </summary>
    Task<bool> ConfirmTravelHistoryAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     批量确认旅行历史记录
    /// </summary>
    Task<int> ConfirmBatchTravelHistoryAsync(
        List<string> ids,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取用户旅行统计
    /// </summary>
    Task<TravelHistoryStats> GetUserTravelStatsAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
