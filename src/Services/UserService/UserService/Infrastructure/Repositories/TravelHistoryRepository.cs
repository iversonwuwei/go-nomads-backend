using Postgrest;
using Postgrest.Models;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;
using Client = Supabase.Client;

namespace UserService.Infrastructure.Repositories;

/// <summary>
///     旅行历史仓储 Supabase 实现
/// </summary>
public class TravelHistoryRepository : ITravelHistoryRepository
{
    private readonly ILogger<TravelHistoryRepository> _logger;
    private readonly Client _supabaseClient;

    public TravelHistoryRepository(Client supabaseClient, ILogger<TravelHistoryRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<TravelHistory> CreateAsync(TravelHistory travelHistory, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 创建旅行历史记录: {City}, {Country}", travelHistory.City, travelHistory.Country);

        try
        {
            var result = await _supabaseClient
                .From<TravelHistory>()
                .Insert(travelHistory, cancellationToken: cancellationToken);

            var created = result.Models.FirstOrDefault();
            if (created == null) throw new InvalidOperationException("创建旅行历史记录失败");

            _logger.LogInformation("✅ 成功创建旅行历史记录: {Id}", created.Id);
            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建旅行历史记录失败: {City}, {Country}", travelHistory.City, travelHistory.Country);
            throw;
        }
    }

    public async Task<List<TravelHistory>> CreateBatchAsync(List<TravelHistory> travelHistories, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 批量创建旅行历史记录: {Count} 条", travelHistories.Count);

        try
        {
            var result = await _supabaseClient
                .From<TravelHistory>()
                .Insert(travelHistories, cancellationToken: cancellationToken);

            _logger.LogInformation("✅ 成功批量创建 {Count} 条旅行历史记录", result.Models.Count);
            return result.Models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 批量创建旅行历史记录失败");
            throw;
        }
    }

    public async Task<TravelHistory?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 根据 ID 查询旅行历史记录: {Id}", id);

        try
        {
            var response = await _supabaseClient
                .From<TravelHistory>()
                .Where(t => t.Id == id)
                .Single(cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 未找到旅行历史记录: {Id}", id);
            return null;
        }
    }

    public async Task<(List<TravelHistory> Items, int Total)> GetByUserIdAsync(
        string userId,
        int page = 1,
        int pageSize = 20,
        bool? isConfirmed = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 查询用户旅行历史记录: UserId={UserId}, Page={Page}, PageSize={PageSize}, IsConfirmed={IsConfirmed}", 
            userId, page, pageSize, isConfirmed);

        try
        {
            var offset = (page - 1) * pageSize;

            // 构建基础查询 - 注意：Supabase 查询每次都要重新构建完整的条件链
            int total;
            List<TravelHistory> items;

            if (isConfirmed.HasValue)
            {
                // 获取总数（带 isConfirmed 筛选）
                total = await _supabaseClient
                    .From<TravelHistory>()
                    .Where(t => t.UserId == userId)
                    .Where(t => t.IsConfirmed == isConfirmed.Value)
                    .Count(Constants.CountType.Exact, cancellationToken);

                // 获取分页数据（带 isConfirmed 筛选）
                var dataResponse = await _supabaseClient
                    .From<TravelHistory>()
                    .Where(t => t.UserId == userId)
                    .Where(t => t.IsConfirmed == isConfirmed.Value)
                    .Order(t => t.ArrivalTime, Constants.Ordering.Descending)
                    .Range(offset, offset + pageSize - 1)
                    .Get(cancellationToken);
                
                items = dataResponse.Models;
            }
            else
            {
                // 获取总数（不带 isConfirmed 筛选）
                total = await _supabaseClient
                    .From<TravelHistory>()
                    .Where(t => t.UserId == userId)
                    .Count(Constants.CountType.Exact, cancellationToken);

                // 获取分页数据（不带 isConfirmed 筛选）
                var dataResponse = await _supabaseClient
                    .From<TravelHistory>()
                    .Where(t => t.UserId == userId)
                    .Order(t => t.ArrivalTime, Constants.Ordering.Descending)
                    .Range(offset, offset + pageSize - 1)
                    .Get(cancellationToken);
                
                items = dataResponse.Models;
            }

            _logger.LogInformation("✅ 查询到 {Count}/{Total} 条旅行历史记录", items.Count, total);
            return (items, total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 查询用户旅行历史记录失败: {UserId}", userId);
            throw;
        }
    }

    public async Task<List<TravelHistory>> GetConfirmedByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 查询用户已确认的旅行历史记录: {UserId}", userId);

        try
        {
            var response = await _supabaseClient
                .From<TravelHistory>()
                .Where(t => t.UserId == userId)
                .Where(t => t.IsConfirmed == true)
                .Order(t => t.ArrivalTime, Constants.Ordering.Descending)
                .Get(cancellationToken);

            _logger.LogInformation("✅ 查询到 {Count} 条已确认的旅行历史记录", response.Models.Count);
            return response.Models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 查询已确认旅行历史记录失败: {UserId}", userId);
            throw;
        }
    }

    public async Task<List<TravelHistory>> GetUnconfirmedByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 查询用户未确认的旅行历史记录: {UserId}", userId);

        try
        {
            var response = await _supabaseClient
                .From<TravelHistory>()
                .Where(t => t.UserId == userId)
                .Where(t => t.IsConfirmed == false)
                .Order(t => t.ArrivalTime, Constants.Ordering.Descending)
                .Get(cancellationToken);

            _logger.LogInformation("✅ 查询到 {Count} 条未确认的旅行历史记录", response.Models.Count);
            return response.Models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 查询未确认旅行历史记录失败: {UserId}", userId);
            throw;
        }
    }

    public async Task<TravelHistory> UpdateAsync(TravelHistory travelHistory, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 更新旅行历史记录: {Id}", travelHistory.Id);

        try
        {
            travelHistory.UpdatedAt = DateTime.UtcNow;

            var result = await _supabaseClient
                .From<TravelHistory>()
                .Where(t => t.Id == travelHistory.Id)
                .Update(travelHistory, cancellationToken: cancellationToken);

            var updated = result.Models.FirstOrDefault();
            if (updated == null) throw new InvalidOperationException("更新旅行历史记录失败");

            _logger.LogInformation("✅ 成功更新旅行历史记录: {Id}", updated.Id);
            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新旅行历史记录失败: {Id}", travelHistory.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🗑️ 删除旅行历史记录: {Id}", id);

        try
        {
            await _supabaseClient
                .From<TravelHistory>()
                .Where(t => t.Id == id)
                .Delete(cancellationToken: cancellationToken);

            _logger.LogInformation("✅ 成功删除旅行历史记录: {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除旅行历史记录失败: {Id}", id);
            return false;
        }
    }

    public async Task<int> DeleteByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🗑️ 删除用户所有旅行历史记录: {UserId}", userId);

        try
        {
            // 先获取数量
            var count = await _supabaseClient
                .From<TravelHistory>()
                .Where(t => t.UserId == userId)
                .Count(Constants.CountType.Exact, cancellationToken);

            // 删除所有记录
            await _supabaseClient
                .From<TravelHistory>()
                .Where(t => t.UserId == userId)
                .Delete(cancellationToken: cancellationToken);

            _logger.LogInformation("✅ 成功删除 {Count} 条旅行历史记录", count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除用户旅行历史记录失败: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> ConfirmAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("✅ 确认旅行历史记录: {Id}", id);

        try
        {
            var travelHistory = await GetByIdAsync(id, cancellationToken);
            if (travelHistory == null) return false;

            travelHistory.Confirm();

            await UpdateAsync(travelHistory, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 确认旅行历史记录失败: {Id}", id);
            return false;
        }
    }

    public async Task<int> ConfirmBatchAsync(List<string> ids, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("✅ 批量确认旅行历史记录: {Count} 条", ids.Count);

        var confirmed = 0;
        foreach (var id in ids)
        {
            if (await ConfirmAsync(id, cancellationToken))
                confirmed++;
        }

        _logger.LogInformation("✅ 成功确认 {Count}/{Total} 条旅行历史记录", confirmed, ids.Count);
        return confirmed;
    }

    public async Task<bool> ExistsSimilarAsync(
        string userId,
        string city,
        string country,
        DateTime arrivalTime,
        TimeSpan tolerance,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 检查是否存在相似的旅行记录: {City}, {Country}, {ArrivalTime}", city, country, arrivalTime);

        try
        {
            var startTime = arrivalTime - tolerance;
            var endTime = arrivalTime + tolerance;

            var response = await _supabaseClient
                .From<TravelHistory>()
                .Where(t => t.UserId == userId)
                .Where(t => t.City == city)
                .Where(t => t.Country == country)
                .Filter(t => t.ArrivalTime, Constants.Operator.GreaterThanOrEqual, startTime.ToString("O"))
                .Filter(t => t.ArrivalTime, Constants.Operator.LessThanOrEqual, endTime.ToString("O"))
                .Get(cancellationToken);

            var exists = response.Models.Count > 0;
            _logger.LogInformation("🔍 相似记录检查结果: {Exists}", exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 检查相似记录失败");
            return false;
        }
    }

    public async Task<TravelHistoryStats> GetUserStatsAsync(string userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📊 获取用户旅行统计: {UserId}", userId);

        try
        {
            var allTrips = await _supabaseClient
                .From<TravelHistory>()
                .Where(t => t.UserId == userId)
                .Get(cancellationToken);

            var trips = allTrips.Models;
            var confirmedTrips = trips.Where(t => t.IsConfirmed).ToList();

            var stats = new TravelHistoryStats
            {
                TotalTrips = trips.Count,
                ConfirmedTrips = confirmedTrips.Count,
                UnconfirmedTrips = trips.Count - confirmedTrips.Count,
                CountriesVisited = confirmedTrips.Select(t => t.Country).Distinct().Count(),
                CitiesVisited = confirmedTrips.Select(t => t.City).Distinct().Count(),
                TotalDays = confirmedTrips
                    .Where(t => t.DepartureTime != null)
                    .Sum(t => (t.DepartureTime!.Value - t.ArrivalTime).Days)
            };

            _logger.LogInformation("📊 用户旅行统计: {Stats}", stats);
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取用户旅行统计失败: {UserId}", userId);
            throw;
        }
    }
}
