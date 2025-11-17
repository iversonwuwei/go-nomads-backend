using System.Linq;
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
        Logger.LogInformation("写入单张用户城市照片: {CityId}, {UserId}, {ImageUrl}", photo.CityId, photo.UserId, photo.ImageUrl);
        var response = await SupabaseClient
            .From<UserCityPhoto>()
            .Insert(photo);

        return response.Models.First();
    }

    public async Task<IEnumerable<UserCityPhoto>> CreateBatchAsync(IEnumerable<UserCityPhoto> photos)
    {
        var photoList = photos.ToList();
        if (!photoList.Any())
        {
            return Array.Empty<UserCityPhoto>();
        }

        var now = DateTime.UtcNow;
        foreach (var photo in photoList)
        {
            photo.CreatedAt = now;
        }

        var cities = string.Join(",", photoList.Select(p => p.CityId).Distinct());
        var users = string.Join(",", photoList.Select(p => p.UserId).Distinct());
        Logger.LogInformation(
            "批量写入 {Count} 张用户城市照片, 城市: {Cities}, 用户: {Users}",
            photoList.Count,
            string.IsNullOrWhiteSpace(cities) ? "(未知)" : cities,
            string.IsNullOrWhiteSpace(users) ? "(未知)" : users);

        var response = await SupabaseClient
            .From<UserCityPhoto>()
            .Insert(photoList);

        return response.Models;
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
