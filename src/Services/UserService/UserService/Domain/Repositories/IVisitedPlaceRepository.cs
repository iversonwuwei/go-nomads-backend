using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

/// <summary>
///     访问地点仓储接口
/// </summary>
public interface IVisitedPlaceRepository
{
    /// <summary>
    ///     创建访问地点记录
    /// </summary>
    Task<VisitedPlace> CreateAsync(VisitedPlace visitedPlace, CancellationToken cancellationToken = default);

    /// <summary>
    ///     批量创建访问地点记录
    /// </summary>
    Task<List<VisitedPlace>> CreateBatchAsync(List<VisitedPlace> visitedPlaces, CancellationToken cancellationToken = default);

    /// <summary>
    ///     根据 ID 获取访问地点记录
    /// </summary>
    Task<VisitedPlace?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取旅行的所有访问地点
    /// </summary>
    Task<List<VisitedPlace>> GetByTravelHistoryIdAsync(string travelHistoryId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取用户的所有访问地点（分页）
    /// </summary>
    Task<(List<VisitedPlace> Items, int Total)> GetByUserIdAsync(
        string userId,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取旅行的精选地点
    /// </summary>
    Task<List<VisitedPlace>> GetHighlightsByTravelHistoryIdAsync(string travelHistoryId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     更新访问地点记录
    /// </summary>
    Task<VisitedPlace> UpdateAsync(VisitedPlace visitedPlace, CancellationToken cancellationToken = default);

    /// <summary>
    ///     删除访问地点记录
    /// </summary>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     删除旅行的所有访问地点
    /// </summary>
    Task<int> DeleteByTravelHistoryIdAsync(string travelHistoryId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     根据客户端 ID 查找（用于同步去重）
    /// </summary>
    Task<VisitedPlace?> GetByClientIdAsync(string clientId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     检查是否存在相同位置的访问记录（用于去重）
    /// </summary>
    Task<bool> ExistsSimilarAsync(
        string travelHistoryId,
        double latitude,
        double longitude,
        DateTime arrivalTime,
        TimeSpan tolerance,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取旅行访问地点统计
    /// </summary>
    Task<VisitedPlaceStats> GetStatsByTravelHistoryIdAsync(string travelHistoryId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取用户在指定城市的访问地点（通过 TravelHistory 的 CityId 关联，支持分页）
    /// </summary>
    Task<(List<VisitedPlace> Items, int Total)> GetByUserIdAndCityIdAsync(
        string userId,
        string cityId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     访问地点统计
/// </summary>
public class VisitedPlaceStats
{
    public int TotalPlaces { get; set; }
    public int HighlightPlaces { get; set; }
    public int TotalDurationMinutes { get; set; }
    public Dictionary<string, int> PlaceTypeDistribution { get; set; } = new();
}
