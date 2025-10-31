using CityService.Domain.Entities;
using CityService.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Repositories;
using Supabase;

namespace CityService.Infrastructure.Repositories;

/// <summary>
/// 基于 Supabase 的用户城市评论仓储实现
/// </summary>
public class SupabaseUserCityReviewRepository : SupabaseRepositoryBase<UserCityReview>, IUserCityReviewRepository
{
    public SupabaseUserCityReviewRepository(Client supabaseClient, ILogger<SupabaseUserCityReviewRepository> logger)
        : base(supabaseClient, logger)
    {
    }

    public async Task<UserCityReview> UpsertAsync(UserCityReview review)
    {
        try
        {
            // 尝试获取现有评论
            var existing = await GetByCityIdAndUserIdAsync(review.CityId, review.UserId);

            if (existing != null)
            {
                // 更新现有评论
                review.Id = existing.Id;
                review.CreatedAt = existing.CreatedAt;
                review.UpdatedAt = DateTime.UtcNow;

                var response = await SupabaseClient
                    .From<UserCityReview>()
                    .Update(review);

                return response.Models.First();
            }
            else
            {
                // 创建新评论
                review.CreatedAt = DateTime.UtcNow;
                var response = await SupabaseClient
                    .From<UserCityReview>()
                    .Insert(review);

                return response.Models.First();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Upsert评论失败: {CityId}, {UserId}", review.CityId, review.UserId);
            throw;
        }
    }

    public async Task<IEnumerable<UserCityReview>> GetByCityIdAsync(string cityId)
    {
        var response = await SupabaseClient
            .From<UserCityReview>()
            .Where(x => x.CityId == cityId)
            .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
            .Get();

        return response.Models;
    }

    public async Task<UserCityReview?> GetByCityIdAndUserIdAsync(string cityId, Guid userId)
    {
        try
        {
            var response = await SupabaseClient
                .From<UserCityReview>()
                .Where(x => x.CityId == cityId && x.UserId == userId)
                .Single();

            return response;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DeleteAsync(string cityId, Guid userId)
    {
        try
        {
            await SupabaseClient
                .From<UserCityReview>()
                .Where(x => x.CityId == cityId && x.UserId == userId)
                .Delete();

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "删除评论失败: {CityId}, {UserId}", cityId, userId);
            return false;
        }
    }

    public async Task<decimal?> GetAverageRatingAsync(string cityId)
    {
        try
        {
            var response = await SupabaseClient
                .From<UserCityReview>()
                .Where(x => x.CityId == cityId)
                .Get();

            var reviews = response.Models;
            if (!reviews.Any())
                return null;

            return (decimal)reviews.Average(r => r.Rating);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "获取平均评分失败: {CityId}", cityId);
            return null;
        }
    }
}
