using CityService.Domain.Entities;
using CityService.Domain.Repositories;
using Postgrest;
using Shared.Repositories;
using Client = Supabase.Client;

namespace CityService.Infrastructure.Repositories;

/// <summary>
///     基于Supabase的附近城市Repository实现
/// </summary>
public class SupabaseNearbyCityRepository : SupabaseRepositoryBase<NearbyCity>, INearbyCityRepository
{
    public SupabaseNearbyCityRepository(
        Client supabaseClient,
        ILogger<SupabaseNearbyCityRepository> logger)
        : base(supabaseClient, logger)
    {
    }

    public async Task<List<NearbyCity>> GetByUserAndSourceCityIdAsync(string userId, string sourceCityId)
    {
        try
        {
            Logger.LogInformation("🔍 从Supabase查询附近城市: userId={UserId}, sourceCityId={SourceCityId}", userId, sourceCityId);

            var response = await SupabaseClient
                .From<NearbyCity>()
                .Where(x => x.UserId == userId)
                .Where(x => x.SourceCityId == sourceCityId)
                .Order("distance_km", Constants.Ordering.Ascending)
                .Get();

            var nearbyCities = response.Models;

            Logger.LogInformation("✅ 找到 {Count} 个附近城市: userId={UserId}, sourceCityId={SourceCityId}",
                nearbyCities.Count, userId, sourceCityId);

            return nearbyCities;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ 查询附近城市失败: userId={UserId}, sourceCityId={SourceCityId}", userId, sourceCityId);
            return new List<NearbyCity>();
        }
    }

    public async Task<List<NearbyCity>> SaveBatchAsync(string userId, string sourceCityId, List<NearbyCity> nearbyCities)
    {
        try
        {
            Logger.LogInformation("🔄 批量保存附近城市: userId={UserId}, sourceCityId={SourceCityId}, count={Count}",
                userId, sourceCityId, nearbyCities.Count);

            // 先删除该用户现有的附近城市数据
            await DeleteByUserAndSourceCityIdAsync(userId, sourceCityId);

            // 设置用户ID、源城市ID和时间戳
            var now = DateTime.UtcNow;
            foreach (var city in nearbyCities)
            {
                city.Id = Guid.NewGuid().ToString();
                city.UserId = userId;
                city.SourceCityId = sourceCityId;
                city.CreatedAt = now;
                city.UpdatedAt = now;
            }

            // 批量插入
            var response = await SupabaseClient
                .From<NearbyCity>()
                .Insert(nearbyCities);

            Logger.LogInformation("✅ 批量保存成功: userId={UserId}, sourceCityId={SourceCityId}, savedCount={Count}",
                userId, sourceCityId, response.Models.Count);

            return response.Models;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ 批量保存附近城市失败: userId={UserId}, sourceCityId={SourceCityId}", userId, sourceCityId);
            throw;
        }
    }

    public async Task<NearbyCity> SaveAsync(NearbyCity nearbyCity)
    {
        try
        {
            nearbyCity.UpdatedAt = DateTime.UtcNow;

            // 检查是否已存在相同的源城市-目标城市组合
            var existing = await GetExistingAsync(nearbyCity.SourceCityId, nearbyCity.TargetCityName);

            if (existing != null)
            {
                // 更新现有记录
                Logger.LogInformation("🔄 更新附近城市: id={Id}, target={TargetCity}",
                    existing.Id, nearbyCity.TargetCityName);

                nearbyCity.Id = existing.Id;
                nearbyCity.CreatedAt = existing.CreatedAt;

                var response = await SupabaseClient
                    .From<NearbyCity>()
                    .Update(nearbyCity);

                return response.Models.First();
            }
            else
            {
                // 插入新记录
                Logger.LogInformation("➕ 创建附近城市: source={SourceCityId}, target={TargetCity}",
                    nearbyCity.SourceCityId, nearbyCity.TargetCityName);

                nearbyCity.Id = Guid.NewGuid().ToString();
                nearbyCity.CreatedAt = DateTime.UtcNow;

                var response = await SupabaseClient
                    .From<NearbyCity>()
                    .Insert(nearbyCity);

                return response.Models.First();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ 保存附近城市失败: sourceCityId={SourceCityId}, target={TargetCity}",
                nearbyCity.SourceCityId, nearbyCity.TargetCityName);
            throw;
        }
    }

    public async Task<bool> DeleteByUserAndSourceCityIdAsync(string userId, string sourceCityId)
    {
        try
        {
            Logger.LogInformation("🗑️ 删除附近城市: userId={UserId}, sourceCityId={SourceCityId}", userId, sourceCityId);

            await SupabaseClient
                .From<NearbyCity>()
                .Where(x => x.UserId == userId)
                .Where(x => x.SourceCityId == sourceCityId)
                .Delete();

            Logger.LogInformation("✅ 删除成功: userId={UserId}, sourceCityId={SourceCityId}", userId, sourceCityId);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ 删除附近城市失败: userId={UserId}, sourceCityId={SourceCityId}", userId, sourceCityId);
            return false;
        }
    }

    public async Task<bool> ExistsByUserAndSourceCityIdAsync(string userId, string sourceCityId)
    {
        try
        {
            var count = await GetCountByUserAndSourceCityIdAsync(userId, sourceCityId);
            return count > 0;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ 检查附近城市是否存在失败: userId={UserId}, sourceCityId={SourceCityId}", userId, sourceCityId);
            return false;
        }
    }

    public async Task<int> GetCountByUserAndSourceCityIdAsync(string userId, string sourceCityId)
    {
        try
        {
            var response = await SupabaseClient
                .From<NearbyCity>()
                .Where(x => x.UserId == userId)
                .Where(x => x.SourceCityId == sourceCityId)
                .Count(Constants.CountType.Exact);

            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ 获取附近城市数量失败: userId={UserId}, sourceCityId={SourceCityId}", userId, sourceCityId);
            return 0;
        }
    }

    private async Task<NearbyCity?> GetExistingAsync(string sourceCityId, string targetCityName)
    {
        try
        {
            var response = await SupabaseClient
                .From<NearbyCity>()
                .Where(x => x.SourceCityId == sourceCityId)
                .Filter("target_city_name", Constants.Operator.Equals, targetCityName)
                .Limit(1)
                .Get();

            return response.Models.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }
}
