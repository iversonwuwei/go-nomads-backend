using CityService.Domain.Entities;
using CityService.Domain.Repositories;
using Postgrest;

namespace CityService.Infrastructure.Repositories;

/// <summary>
/// 城市评分项仓储实现
/// </summary>
public class CityRatingCategoryRepository : ICityRatingCategoryRepository
{
    private readonly Supabase.Client _supabaseClient;
    private readonly ILogger<CityRatingCategoryRepository> _logger;

    public CityRatingCategoryRepository(
        Supabase.Client supabaseClient,
        ILogger<CityRatingCategoryRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<CityRatingCategory> CreateAsync(CityRatingCategory category)
    {
        try
        {
            var response = await _supabaseClient
                .From<CityRatingCategory>()
                .Insert(category);

            var created = response.Models.FirstOrDefault();
            if (created == null)
                throw new InvalidOperationException("创建评分项失败");

            _logger.LogInformation("✅ 评分项创建成功: {Id}", created.Id);
            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建评分项失败");
            throw;
        }
    }

    public async Task<CityRatingCategory?> GetByIdAsync(Guid id)
    {
        try
        {
            var response = await _supabaseClient
                .From<CityRatingCategory>()
                .Filter("id", Constants.Operator.Equals, id.ToString())
                .Filter("is_active", Constants.Operator.Equals, "true")
                .Single();

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取评分项失败: {Id}", id);
            return null;
        }
    }

    public async Task<List<CityRatingCategory>> GetAllActiveAsync()
    {
        try
        {
            var response = await _supabaseClient
                .From<CityRatingCategory>()
                .Filter("is_active", Constants.Operator.Equals, "true")
                .Order(x => x.DisplayOrder, Constants.Ordering.Ascending)
                .Get();

            return response?.Models ?? new List<CityRatingCategory>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取评分项列表失败");
            throw;
        }
    }

    public async Task<CityRatingCategory> UpdateAsync(CityRatingCategory category)
    {
        try
        {
            var response = await _supabaseClient
                .From<CityRatingCategory>()
                .Update(category);

            var updated = response.Models.FirstOrDefault();
            if (updated == null)
                throw new InvalidOperationException("更新评分项失败");

            _logger.LogInformation("✅ 评分项更新成功: {Id}", updated.Id);
            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新评分项失败: {Id}", category.Id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        try
        {
            await _supabaseClient
                .From<CityRatingCategory>()
                .Filter("id", Constants.Operator.Equals, id.ToString())
                .Delete();

            _logger.LogInformation("✅ 评分项删除成功: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除评分项失败: {Id}", id);
            throw;
        }
    }
}
