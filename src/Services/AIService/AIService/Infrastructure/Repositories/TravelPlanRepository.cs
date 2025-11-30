using AIService.Domain.Entities;
using AIService.Domain.Repositories;
using Client = Supabase.Client;

namespace AIService.Infrastructure.Repositories;

/// <summary>
///     旅行计划仓储实现 (Supabase)
/// </summary>
public class TravelPlanRepository : ITravelPlanRepository
{
    private readonly ILogger<TravelPlanRepository> _logger;
    private readonly Client _supabaseClient;

    public TravelPlanRepository(Client supabaseClient, ILogger<TravelPlanRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<AiTravelPlan> SaveAsync(AiTravelPlan plan)
    {
        try
        {
            plan.CreatedAt = DateTime.UtcNow;
            plan.UpdatedAt = DateTime.UtcNow;

            var response = await _supabaseClient
                .From<AiTravelPlan>()
                .Insert(plan);

            var created = response.Models.FirstOrDefault();
            if (created == null)
                throw new InvalidOperationException("保存旅行计划失败");

            _logger.LogInformation("✅ 成功保存旅行计划: Id={Id}, UserId={UserId}, CityName={CityName}",
                created.Id, created.UserId, created.CityName);

            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 保存旅行计划失败: UserId={UserId}, CityId={CityId}",
                plan.UserId, plan.CityId);
            throw;
        }
    }

    public async Task<AiTravelPlan?> GetByIdAsync(Guid id)
    {
        try
        {
            var response = await _supabaseClient
                .From<AiTravelPlan>()
                .Where(p => p.Id == id)
                .Single();

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取旅行计划失败: Id={Id}", id);
            return null;
        }
    }

    public async Task<List<AiTravelPlan>> GetByUserIdAsync(Guid userId, int page = 1, int pageSize = 20)
    {
        try
        {
            var offset = (page - 1) * pageSize;

            var response = await _supabaseClient
                .From<AiTravelPlan>()
                .Where(p => p.UserId == userId)
                .Order(p => p.CreatedAt, Postgrest.Constants.Ordering.Descending)
                .Range(offset, offset + pageSize - 1)
                .Get();

            return response.Models ?? new List<AiTravelPlan>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取用户旅行计划失败: UserId={UserId}", userId);
            throw;
        }
    }

    public async Task<List<AiTravelPlan>> GetByCityIdAsync(string cityId, int page = 1, int pageSize = 20)
    {
        try
        {
            var offset = (page - 1) * pageSize;

            var response = await _supabaseClient
                .From<AiTravelPlan>()
                .Where(p => p.CityId == cityId && p.IsPublic)
                .Order(p => p.CreatedAt, Postgrest.Constants.Ordering.Descending)
                .Range(offset, offset + pageSize - 1)
                .Get();

            return response.Models ?? new List<AiTravelPlan>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取城市旅行计划失败: CityId={CityId}", cityId);
            throw;
        }
    }

    public async Task<AiTravelPlan?> UpdateAsync(AiTravelPlan plan)
    {
        try
        {
            plan.UpdatedAt = DateTime.UtcNow;

            var response = await _supabaseClient
                .From<AiTravelPlan>()
                .Where(p => p.Id == plan.Id)
                .Update(plan);

            var updated = response.Models.FirstOrDefault();
            if (updated == null)
            {
                _logger.LogWarning("⚠️ 更新旅行计划失败,未找到: Id={Id}", plan.Id);
                return null;
            }

            _logger.LogInformation("✅ 成功更新旅行计划: Id={Id}", plan.Id);
            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新旅行计划失败: Id={Id}", plan.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            await _supabaseClient
                .From<AiTravelPlan>()
                .Where(p => p.Id == id)
                .Delete();

            _logger.LogInformation("✅ 成功删除旅行计划: Id={Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除旅行计划失败: Id={Id}", id);
            return false;
        }
    }
}
