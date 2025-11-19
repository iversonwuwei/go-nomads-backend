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
}
