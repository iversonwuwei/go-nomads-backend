using CityService.Domain.Entities;
using CityService.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Repositories;
using Supabase;

namespace CityService.Infrastructure.Repositories;

/// <summary>
/// 基于 Supabase 的用户城市照片仓储实现
/// </summary>
public class SupabaseUserCityPhotoRepository : SupabaseRepositoryBase<UserCityPhoto>, IUserCityPhotoRepository
{
    public SupabaseUserCityPhotoRepository(Client supabaseClient, ILogger<SupabaseUserCityPhotoRepository> logger)
        : base(supabaseClient, logger)
    {
    }

    public async Task<UserCityPhoto> CreateAsync(UserCityPhoto photo)
    {
        photo.CreatedAt = DateTime.UtcNow;
        var response = await SupabaseClient
            .From<UserCityPhoto>()
            .Insert(photo);

        return response.Models.First();
    }

    public async Task<IEnumerable<UserCityPhoto>> GetByCityIdAsync(string cityId)
    {
        var response = await SupabaseClient
            .From<UserCityPhoto>()
            .Where(x => x.CityId == cityId)
            .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
            .Get();

        return response.Models;
    }

    public async Task<IEnumerable<UserCityPhoto>> GetByCityIdAndUserIdAsync(string cityId, Guid userId)
    {
        var response = await SupabaseClient
            .From<UserCityPhoto>()
            .Where(x => x.CityId == cityId && x.UserId == userId)
            .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
            .Get();

        return response.Models;
    }

    public async Task<IEnumerable<UserCityPhoto>> GetByUserIdAsync(Guid userId)
    {
        var response = await SupabaseClient
            .From<UserCityPhoto>()
            .Where(x => x.UserId == userId)
            .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
            .Get();

        return response.Models;
    }

    public async Task<UserCityPhoto?> GetByIdAsync(Guid id)
    {
        try
        {
            var response = await SupabaseClient
                .From<UserCityPhoto>()
                .Where(x => x.Id == id)
                .Single();

            return response;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            await SupabaseClient
                .From<UserCityPhoto>()
                .Where(x => x.Id == id && x.UserId == userId)
                .Delete();

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "删除照片失败: {PhotoId}, {UserId}", id, userId);
            return false;
        }
    }
}
