using CityService.Domain.Entities;
using CityService.Domain.Repositories;
using Postgrest;
using Shared.Repositories;
using Client = Supabase.Client;

namespace CityService.Infrastructure.Repositories;

/// <summary>
///     基于Supabase的数字游民指南Repository实现
/// </summary>
public class SupabaseDigitalNomadGuideRepository : SupabaseRepositoryBase<DigitalNomadGuide>,
    IDigitalNomadGuideRepository
{
    public SupabaseDigitalNomadGuideRepository(
        Client supabaseClient,
        ILogger<SupabaseDigitalNomadGuideRepository> logger)
        : base(supabaseClient, logger)
    {
    }

    public async Task<DigitalNomadGuide?> GetByUserAndCityIdAsync(string userId, string cityId)
    {
        try
        {
            Logger.LogInformation("🔍 从Supabase查询指南: userId={UserId}, cityId={CityId}", userId, cityId);

            var response = await SupabaseClient
                .From<DigitalNomadGuide>()
                .Where(x => x.UserId == userId)
                .Where(x => x.CityId == cityId)
                .Order("updated_at", Constants.Ordering.Descending)
                .Limit(1)
                .Get();

            var guide = response.Models.FirstOrDefault();

            if (guide != null)
                Logger.LogInformation("✅ 找到指南: guideId={GuideId}, cityName={CityName}, userId={UserId}", guide.Id, guide.CityName, userId);
            else
                Logger.LogInformation("📭 未找到指南: userId={UserId}, cityId={CityId}", userId, cityId);

            return guide;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ 查询指南失败: userId={UserId}, cityId={CityId}", userId, cityId);
            return null;
        }
    }

    public async Task<DigitalNomadGuide> SaveAsync(DigitalNomadGuide guide)
    {
        try
        {
            guide.UpdatedAt = DateTime.UtcNow;

            // 检查是否已存在（按用户+城市）
            var existing = await GetByUserAndCityIdAsync(guide.UserId, guide.CityId);

            if (existing != null)
            {
                // 更新现有记录
                Logger.LogInformation("🔄 更新现有指南: guideId={GuideId}, userId={UserId}, cityId={CityId}", existing.Id, guide.UserId, guide.CityId);

                guide.Id = existing.Id;
                guide.CreatedAt = existing.CreatedAt;

                var response = await SupabaseClient
                    .From<DigitalNomadGuide>()
                    .Update(guide);

                Logger.LogInformation("✅ 指南更新成功: guideId={GuideId}", guide.Id);
                return response.Models.First();
            }
            else
            {
                // 插入新记录
                Logger.LogInformation("➕ 创建新指南: userId={UserId}, cityId={CityId}, cityName={CityName}", guide.UserId, guide.CityId, guide.CityName);

                guide.Id = Guid.NewGuid().ToString();
                guide.CreatedAt = DateTime.UtcNow;

                var response = await SupabaseClient
                    .From<DigitalNomadGuide>()
                    .Insert(guide);

                Logger.LogInformation("✅ 指南创建成功: guideId={GuideId}", guide.Id);
                return response.Models.First();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ 保存指南失败: userId={UserId}, cityId={CityId}", guide.UserId, guide.CityId);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string id)
    {
        try
        {
            Logger.LogInformation("🗑️ 删除指南: guideId={GuideId}", id);

            await SupabaseClient
                .From<DigitalNomadGuide>()
                .Where(x => x.Id == id)
                .Delete();

            Logger.LogInformation("✅ 指南删除成功: guideId={GuideId}", id);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ 删除指南失败: guideId={GuideId}", id);
            return false;
        }
    }

    public async Task<bool> ExistsByUserAndCityIdAsync(string userId, string cityId)
    {
        try
        {
            var guide = await GetByUserAndCityIdAsync(userId, cityId);
            return guide != null;
        }
        catch
        {
            return false;
        }
    }
}