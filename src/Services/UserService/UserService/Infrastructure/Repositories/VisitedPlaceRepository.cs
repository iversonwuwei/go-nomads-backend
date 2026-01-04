using Postgrest;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;
using Client = Supabase.Client;

namespace UserService.Infrastructure.Repositories;

/// <summary>
///     访问地点仓储 Supabase 实现
/// </summary>
public class VisitedPlaceRepository : IVisitedPlaceRepository
{
    private readonly ILogger<VisitedPlaceRepository> _logger;
    private readonly Client _supabaseClient;

    public VisitedPlaceRepository(Client supabaseClient, ILogger<VisitedPlaceRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<VisitedPlace> CreateAsync(VisitedPlace visitedPlace, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 创建访问地点记录: {PlaceName}, TravelHistoryId: {TravelHistoryId}", 
            visitedPlace.PlaceName, visitedPlace.TravelHistoryId);

        try
        {
            var result = await _supabaseClient
                .From<VisitedPlace>()
                .Insert(visitedPlace, cancellationToken: cancellationToken);

            var created = result.Models.FirstOrDefault();
            if (created == null) throw new InvalidOperationException("创建访问地点记录失败");

            _logger.LogInformation("✅ 成功创建访问地点记录: {Id}", created.Id);
            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建访问地点记录失败: {PlaceName}", visitedPlace.PlaceName);
            throw;
        }
    }

    public async Task<List<VisitedPlace>> CreateBatchAsync(List<VisitedPlace> visitedPlaces, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 批量创建访问地点记录: {Count} 条", visitedPlaces.Count);

        try
        {
            var result = await _supabaseClient
                .From<VisitedPlace>()
                .Insert(visitedPlaces, cancellationToken: cancellationToken);

            _logger.LogInformation("✅ 成功批量创建 {Count} 条访问地点记录", result.Models.Count);
            return result.Models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 批量创建访问地点记录失败");
            throw;
        }
    }

    public async Task<VisitedPlace?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 根据 ID 查询访问地点记录: {Id}", id);

        try
        {
            var response = await _supabaseClient
                .From<VisitedPlace>()
                .Where(v => v.Id == id)
                .Single(cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 未找到访问地点记录: {Id}", id);
            return null;
        }
    }

    public async Task<List<VisitedPlace>> GetByTravelHistoryIdAsync(string travelHistoryId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 查询旅行的访问地点: TravelHistoryId={TravelHistoryId}", travelHistoryId);

        try
        {
            var response = await _supabaseClient
                .From<VisitedPlace>()
                .Where(v => v.TravelHistoryId == travelHistoryId)
                .Order(v => v.ArrivalTime, Constants.Ordering.Ascending)
                .Get(cancellationToken);

            _logger.LogInformation("✅ 查询到 {Count} 条访问地点记录", response.Models.Count);
            return response.Models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 查询访问地点失败: TravelHistoryId={TravelHistoryId}", travelHistoryId);
            throw;
        }
    }

    public async Task<(List<VisitedPlace> Items, int Total)> GetByUserIdAsync(
        string userId,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 查询用户访问地点: UserId={UserId}, Page={Page}, PageSize={PageSize}",
            userId, page, pageSize);

        try
        {
            var offset = (page - 1) * pageSize;

            // 获取总数
            var total = await _supabaseClient
                .From<VisitedPlace>()
                .Where(v => v.UserId == userId)
                .Count(Constants.CountType.Exact, cancellationToken);

            // 获取分页数据
            var response = await _supabaseClient
                .From<VisitedPlace>()
                .Where(v => v.UserId == userId)
                .Order(v => v.ArrivalTime, Constants.Ordering.Descending)
                .Range(offset, offset + pageSize - 1)
                .Get(cancellationToken);

            _logger.LogInformation("✅ 查询到 {Count}/{Total} 条访问地点记录", response.Models.Count, total);
            return (response.Models, total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 查询用户访问地点失败: UserId={UserId}", userId);
            throw;
        }
    }

    public async Task<List<VisitedPlace>> GetHighlightsByTravelHistoryIdAsync(string travelHistoryId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 查询旅行的精选地点: TravelHistoryId={TravelHistoryId}", travelHistoryId);

        try
        {
            var response = await _supabaseClient
                .From<VisitedPlace>()
                .Where(v => v.TravelHistoryId == travelHistoryId)
                .Where(v => v.IsHighlight == true)
                .Order(v => v.ArrivalTime, Constants.Ordering.Ascending)
                .Get(cancellationToken);

            _logger.LogInformation("✅ 查询到 {Count} 条精选地点记录", response.Models.Count);
            return response.Models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 查询精选地点失败: TravelHistoryId={TravelHistoryId}", travelHistoryId);
            throw;
        }
    }

    public async Task<VisitedPlace> UpdateAsync(VisitedPlace visitedPlace, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 更新访问地点记录: {Id}", visitedPlace.Id);

        try
        {
            visitedPlace.UpdatedAt = DateTime.UtcNow;

            var result = await _supabaseClient
                .From<VisitedPlace>()
                .Where(v => v.Id == visitedPlace.Id)
                .Update(visitedPlace, cancellationToken: cancellationToken);

            var updated = result.Models.FirstOrDefault();
            if (updated == null) throw new InvalidOperationException("更新访问地点记录失败");

            _logger.LogInformation("✅ 成功更新访问地点记录: {Id}", updated.Id);
            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新访问地点记录失败: {Id}", visitedPlace.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🗑️ 删除访问地点记录: {Id}", id);

        try
        {
            await _supabaseClient
                .From<VisitedPlace>()
                .Where(v => v.Id == id)
                .Delete(cancellationToken: cancellationToken);

            _logger.LogInformation("✅ 成功删除访问地点记录: {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除访问地点记录失败: {Id}", id);
            return false;
        }
    }

    public async Task<int> DeleteByTravelHistoryIdAsync(string travelHistoryId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🗑️ 删除旅行的所有访问地点: TravelHistoryId={TravelHistoryId}", travelHistoryId);

        try
        {
            // 先获取数量
            var count = await _supabaseClient
                .From<VisitedPlace>()
                .Where(v => v.TravelHistoryId == travelHistoryId)
                .Count(Constants.CountType.Exact, cancellationToken);

            // 执行删除
            await _supabaseClient
                .From<VisitedPlace>()
                .Where(v => v.TravelHistoryId == travelHistoryId)
                .Delete(cancellationToken: cancellationToken);

            _logger.LogInformation("✅ 成功删除 {Count} 条访问地点记录", count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除旅行访问地点失败: TravelHistoryId={TravelHistoryId}", travelHistoryId);
            throw;
        }
    }

    public async Task<VisitedPlace?> GetByClientIdAsync(string clientId, string userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 根据客户端ID查询访问地点: ClientId={ClientId}, UserId={UserId}", clientId, userId);

        try
        {
            var response = await _supabaseClient
                .From<VisitedPlace>()
                .Where(v => v.ClientId == clientId)
                .Where(v => v.UserId == userId)
                .Single(cancellationToken);

            return response;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<bool> ExistsSimilarAsync(
        string travelHistoryId,
        double latitude,
        double longitude,
        DateTime arrivalTime,
        TimeSpan tolerance,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 检查是否存在相似访问地点: TravelHistoryId={TravelHistoryId}, Lat={Lat}, Lng={Lng}",
            travelHistoryId, latitude, longitude);

        try
        {
            var startTime = arrivalTime - tolerance;
            var endTime = arrivalTime + tolerance;

            // 位置容差 (约 100 米)
            const double locationTolerance = 0.001;

            var response = await _supabaseClient
                .From<VisitedPlace>()
                .Where(v => v.TravelHistoryId == travelHistoryId)
                .Filter(v => v.ArrivalTime, Constants.Operator.GreaterThanOrEqual, startTime)
                .Filter(v => v.ArrivalTime, Constants.Operator.LessThanOrEqual, endTime)
                .Filter(v => v.Latitude, Constants.Operator.GreaterThanOrEqual, latitude - locationTolerance)
                .Filter(v => v.Latitude, Constants.Operator.LessThanOrEqual, latitude + locationTolerance)
                .Filter(v => v.Longitude, Constants.Operator.GreaterThanOrEqual, longitude - locationTolerance)
                .Filter(v => v.Longitude, Constants.Operator.LessThanOrEqual, longitude + locationTolerance)
                .Get(cancellationToken);

            return response.Models.Any();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 检查相似访问地点失败");
            return false;
        }
    }

    public async Task<VisitedPlaceStats> GetStatsByTravelHistoryIdAsync(string travelHistoryId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📊 获取旅行访问地点统计: TravelHistoryId={TravelHistoryId}", travelHistoryId);

        try
        {
            var places = await GetByTravelHistoryIdAsync(travelHistoryId, cancellationToken);

            var stats = new VisitedPlaceStats
            {
                TotalPlaces = places.Count,
                HighlightPlaces = places.Count(p => p.IsHighlight),
                TotalDurationMinutes = places.Sum(p => p.DurationMinutes),
                PlaceTypeDistribution = places
                    .Where(p => !string.IsNullOrEmpty(p.PlaceType))
                    .GroupBy(p => p.PlaceType!)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            _logger.LogInformation("✅ 统计结果: {TotalPlaces} 个地点, {HighlightPlaces} 个精选, {TotalDuration} 分钟",
                stats.TotalPlaces, stats.HighlightPlaces, stats.TotalDurationMinutes);

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取访问地点统计失败: TravelHistoryId={TravelHistoryId}", travelHistoryId);
            throw;
        }
    }
}
