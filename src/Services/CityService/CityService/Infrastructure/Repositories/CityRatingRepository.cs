using CityService.Domain.Entities;
using CityService.Domain.Repositories;
using Postgrest;

namespace CityService.Infrastructure.Repositories;

/// <summary>
/// 用户城市评分仓储实现
/// </summary>
public class CityRatingRepository : ICityRatingRepository
{
    private readonly Supabase.Client _supabaseClient;
    private readonly ILogger<CityRatingRepository> _logger;

    public CityRatingRepository(
        Supabase.Client supabaseClient,
        ILogger<CityRatingRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<CityRating> CreateAsync(CityRating rating)
    {
        try
        {
            var response = await _supabaseClient
                .From<CityRating>()
                .Insert(rating);

            var created = response.Models.FirstOrDefault();
            if (created == null)
                throw new InvalidOperationException("创建评分失败");

            _logger.LogInformation("✅ 评分创建成功: {Id}", created.Id);
            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建评分失败");
            throw;
        }
    }

    public async Task<CityRating?> GetByIdAsync(Guid id)
    {
        try
        {
            var response = await _supabaseClient
                .From<CityRating>()
                .Filter("id", Constants.Operator.Equals, id.ToString())
                .Single();

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取评分失败: {Id}", id);
            return null;
        }
    }

    public async Task<CityRating?> GetUserRatingAsync(Guid cityId, Guid userId, Guid categoryId)
    {
        try
        {
            var response = await _supabaseClient
                .From<CityRating>()
                .Filter("city_id", Constants.Operator.Equals, cityId.ToString())
                .Filter("user_id", Constants.Operator.Equals, userId.ToString())
                .Filter("category_id", Constants.Operator.Equals, categoryId.ToString())
                .Single();

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取用户评分失败");
            return null;
        }
    }

    public async Task<List<CityRating>> GetCityRatingsAsync(Guid cityId)
    {
        try
        {
            var response = await _supabaseClient
                .From<CityRating>()
                .Filter("city_id", Constants.Operator.Equals, cityId.ToString())
                .Get();

            return response?.Models ?? new List<CityRating>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取城市评分列表失败: CityId={CityId}", cityId);
            throw;
        }
    }

    public async Task<List<CityRating>> GetUserRatingsAsync(Guid userId, Guid cityId)
    {
        try
        {
            var response = await _supabaseClient
                .From<CityRating>()
                .Filter("user_id", Constants.Operator.Equals, userId.ToString())
                .Filter("city_id", Constants.Operator.Equals, cityId.ToString())
                .Get();

            return response?.Models ?? new List<CityRating>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取用户评分失败: UserId={UserId}, CityId={CityId}", userId, cityId);
            throw;
        }
    }

    public async Task<Dictionary<Guid, double>> GetCityAverageRatingsAsync(Guid cityId)
    {
        try
        {
            var ratings = await GetCityRatingsAsync(cityId);
            
            return ratings
                .GroupBy(r => r.CategoryId)
                .ToDictionary(
                    g => g.Key,
                    g => Math.Round(g.Average(r => r.Rating), 1)
                );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 计算城市平均评分失败: CityId={CityId}", cityId);
            throw;
        }
    }

    public async Task<CityRating> UpdateAsync(CityRating rating)
    {
        try
        {
            var response = await _supabaseClient
                .From<CityRating>()
                .Update(rating);

            var updated = response.Models.FirstOrDefault();
            if (updated == null)
                throw new InvalidOperationException("更新评分失败");

            _logger.LogInformation("✅ 评分更新成功: {Id}", updated.Id);
            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新评分失败: {Id}", rating.Id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        try
        {
            await _supabaseClient
                .From<CityRating>()
                .Filter("id", Constants.Operator.Equals, id.ToString())
                .Delete();

            _logger.LogInformation("✅ 评分删除成功: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除评分失败: {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// 批量获取城市的评论数量（来自 user_city_reviews 表，统计评论条数）
    /// </summary>
    public async Task<Dictionary<Guid, int>> GetCityReviewCountsBatchAsync(IEnumerable<Guid> cityIds)
    {
        var result = new Dictionary<Guid, int>();
        var cityIdList = cityIds.ToList();

        if (!cityIdList.Any())
            return result;

        try
        {
            // 获取指定城市的所有评论记录并按城市分组统计条数
            var cityIdStrings = cityIdList.Select(id => id.ToString()).ToList();
            var response = await _supabaseClient
                .From<UserCityReview>()
                .Filter("city_id", Constants.Operator.In, cityIdStrings)
                .Get();

            if (response?.Models != null)
            {
                foreach (var group in response.Models.GroupBy(r => r.CityId))
                {
                    if (!Guid.TryParse(group.Key, out var cityId))
                    {
                        _logger.LogWarning("⚠️ 无法解析城市ID: {CityId}", group.Key);
                        continue;
                    }

                    result[cityId] = group.Count();
                }
            }

            _logger.LogInformation("✅ 批量获取城市评论数量: {Count} 个城市", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 批量获取城市评论数量失败");
            return result;
        }
    }

    /// <summary>
    /// 批量获取多个城市的评分数据（优化 N+1 查询）
    /// </summary>
    public async Task<List<CityRating>> GetCityRatingsBatchAsync(IEnumerable<Guid> cityIds)
    {
        var cityIdList = cityIds.ToList();

        if (!cityIdList.Any())
            return new List<CityRating>();

        try
        {
            var cityIdStrings = cityIdList.Select(id => id.ToString()).ToList();
            var response = await _supabaseClient
                .From<CityRating>()
                .Filter("city_id", Constants.Operator.In, cityIdStrings)
                .Get();

            _logger.LogInformation("✅ 批量获取城市评分: {Count} 个城市, {RatingCount} 条评分",
                cityIdList.Count, response?.Models?.Count ?? 0);

            return response?.Models ?? new List<CityRating>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 批量获取城市评分失败");
            return new List<CityRating>();
        }
    }

    /// <summary>
    /// 批量获取多个城市的平均评分（按分类）
    /// </summary>
    public async Task<Dictionary<Guid, Dictionary<Guid, double>>> GetCityAverageRatingsBatchAsync(IEnumerable<Guid> cityIds)
    {
        var result = new Dictionary<Guid, Dictionary<Guid, double>>();

        try
        {
            var ratings = await GetCityRatingsBatchAsync(cityIds);

            // 按城市分组，然后按分类计算平均分
            var grouped = ratings.GroupBy(r => r.CityId);

            foreach (var cityGroup in grouped)
            {
                var categoryAverages = cityGroup
                    .GroupBy(r => r.CategoryId)
                    .ToDictionary(
                        g => g.Key,
                        g => Math.Round(g.Average(r => r.Rating), 1)
                    );

                result[cityGroup.Key] = categoryAverages;
            }

            _logger.LogInformation("✅ 批量计算城市平均评分: {Count} 个城市", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 批量计算城市平均评分失败");
            return result;
        }
    }
}
