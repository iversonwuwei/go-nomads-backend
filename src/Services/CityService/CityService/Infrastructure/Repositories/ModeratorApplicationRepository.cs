using CityService.Domain.Entities;
using CityService.Domain.Repositories;
using Supabase;

namespace CityService.Infrastructure.Repositories;

/// <summary>
///     版主申请仓储实现
/// </summary>
public class ModeratorApplicationRepository : IModeratorApplicationRepository
{
    private readonly ILogger<ModeratorApplicationRepository> _logger;
    private readonly Client _supabaseClient;

    public ModeratorApplicationRepository(
        Client supabaseClient,
        ILogger<ModeratorApplicationRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<ModeratorApplication> CreateAsync(ModeratorApplication application)
    {
        try
        {
            var result = await _supabaseClient
                .From<ModeratorApplication>()
                .Insert(application);

            var created = result.Models.FirstOrDefault();
            if (created == null) throw new InvalidOperationException("创建申请失败");

            _logger.LogInformation("✅ 版主申请创建成功: UserId={UserId}, CityId={CityId}",
                application.UserId, application.CityId);
            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建版主申请失败");
            throw;
        }
    }

    public async Task<ModeratorApplication> UpdateAsync(ModeratorApplication application)
    {
        try
        {
            application.UpdatedAt = DateTime.UtcNow;

            var result = await _supabaseClient
                .From<ModeratorApplication>()
                .Where(a => a.Id == application.Id)
                .Update(application);

            var updated = result.Models.FirstOrDefault();
            if (updated == null) throw new InvalidOperationException("更新申请失败");

            _logger.LogInformation("✅ 版主申请更新成功: Id={Id}, Status={Status}",
                application.Id, application.Status);
            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新版主申请失败: Id={Id}", application.Id);
            throw;
        }
    }

    public async Task<ModeratorApplication?> GetByIdAsync(Guid id)
    {
        try
        {
            var result = await _supabaseClient
                .From<ModeratorApplication>()
                .Where(a => a.Id == id)
                .Get();

            return result.Models.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取版主申请失败: Id={Id}", id);
            throw;
        }
    }

    public async Task<List<ModeratorApplication>> GetByUserIdAsync(Guid userId)
    {
        try
        {
            var result = await _supabaseClient
                .From<ModeratorApplication>()
                .Where(a => a.UserId == userId)
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Get();

            return result.Models.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取用户申请列表失败: UserId={UserId}", userId);
            throw;
        }
    }

    public async Task<List<ModeratorApplication>> GetByCityIdAsync(Guid cityId)
    {
        try
        {
            var result = await _supabaseClient
                .From<ModeratorApplication>()
                .Where(a => a.CityId == cityId)
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Get();

            return result.Models.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取城市申请列表失败: CityId={CityId}", cityId);
            throw;
        }
    }

    public async Task<List<ModeratorApplication>> GetPendingApplicationsAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var from = (page - 1) * pageSize;
            var to = from + pageSize - 1;

            var result = await _supabaseClient
                .From<ModeratorApplication>()
                .Where(a => a.Status == "pending")
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Range(from, to)
                .Get();

            _logger.LogInformation("✅ 获取待处理申请列表成功: Count={Count}", result.Models.Count);
            return result.Models.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取待处理申请列表失败");
            throw;
        }
    }

    public async Task<bool> HasPendingApplicationAsync(Guid userId, Guid cityId)
    {
        try
        {
            var result = await _supabaseClient
                .From<ModeratorApplication>()
                .Filter("user_id", Postgrest.Constants.Operator.Equals, userId.ToString())
                .Filter("city_id", Postgrest.Constants.Operator.Equals, cityId.ToString())
                .Filter("status", Postgrest.Constants.Operator.Equals, "pending")
                .Get();

            return result.Models.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 检查待处理申请失败");
            throw;
        }
    }

    public async Task<(int Total, int Pending, int Approved, int Rejected)> GetStatisticsAsync()
    {
        try
        {
            var allApplications = await _supabaseClient
                .From<ModeratorApplication>()
                .Get();

            var applications = allApplications.Models.ToList();
            var total = applications.Count;
            var pending = applications.Count(a => a.Status == "pending");
            var approved = applications.Count(a => a.Status == "approved");
            var rejected = applications.Count(a => a.Status == "rejected");

            return (total, pending, approved, rejected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取申请统计失败");
            throw;
        }
    }
}
