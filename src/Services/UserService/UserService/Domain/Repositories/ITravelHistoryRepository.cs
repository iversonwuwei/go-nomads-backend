using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

/// <summary>
///     旅行历史仓储接口
/// </summary>
public interface ITravelHistoryRepository
{
    /// <summary>
    ///     创建旅行历史记录
    /// </summary>
    Task<TravelHistory> CreateAsync(TravelHistory travelHistory, CancellationToken cancellationToken = default);

    /// <summary>
    ///     批量创建旅行历史记录
    /// </summary>
    Task<List<TravelHistory>> CreateBatchAsync(List<TravelHistory> travelHistories, CancellationToken cancellationToken = default);

    /// <summary>
    ///     根据 ID 获取旅行历史记录
    /// </summary>
    Task<TravelHistory?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取用户的所有旅行历史记录（分页）
    /// </summary>
    Task<(List<TravelHistory> Items, int Total)> GetByUserIdAsync(
        string userId,
        int page = 1,
        int pageSize = 20,
        bool? isConfirmed = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取用户已确认的旅行历史记录
    /// </summary>
    Task<List<TravelHistory>> GetConfirmedByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取用户未确认的旅行历史记录（待确认的自动检测行程）
    /// </summary>
    Task<List<TravelHistory>> GetUnconfirmedByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     更新旅行历史记录
    /// </summary>
    Task<TravelHistory> UpdateAsync(TravelHistory travelHistory, CancellationToken cancellationToken = default);

    /// <summary>
    ///     删除旅行历史记录
    /// </summary>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     批量删除用户的旅行历史记录
    /// </summary>
    Task<int> DeleteByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     确认旅行历史记录
    /// </summary>
    Task<bool> ConfirmAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     批量确认旅行历史记录
    /// </summary>
    Task<int> ConfirmBatchAsync(List<string> ids, CancellationToken cancellationToken = default);

    /// <summary>
    ///     检查是否存在相似的旅行记录（用于去重）
    /// </summary>
    Task<bool> ExistsSimilarAsync(
        string userId,
        string city,
        string country,
        DateTime arrivalTime,
        TimeSpan tolerance,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取用户旅行统计
    /// </summary>
    Task<TravelHistoryStats> GetUserStatsAsync(string userId, CancellationToken cancellationToken = default);
}

/// <summary>
///     旅行历史统计
/// </summary>
public class TravelHistoryStats
{
    public int TotalTrips { get; set; }
    public int ConfirmedTrips { get; set; }
    public int UnconfirmedTrips { get; set; }
    public int CountriesVisited { get; set; }
    public int CitiesVisited { get; set; }
    public int TotalDays { get; set; }
}
