using CityService.Domain.Entities;
using CityService.Domain.Repositories;
using Postgrest;
using Shared.Repositories;
using Client = Supabase.Client;

namespace CityService.Infrastructure.Repositories;

/// <summary>
///     基于 Supabase 的用户城市评论仓储实现
/// </summary>
public class SupabaseUserCityReviewRepository : SupabaseRepositoryBase<UserCityReview>, IUserCityReviewRepository
{
    public SupabaseUserCityReviewRepository(Client supabaseClient, ILogger<SupabaseUserCityReviewRepository> logger)
        : base(supabaseClient, logger)
    {
    }

    /// <summary>
    ///     创建新评论(每次都插入新记录,允许同一用户对同一城市多次评论)
    /// </summary>
    public async Task<UserCityReview> CreateAsync(UserCityReview review)
    {
        try
        {
            // ✅ 每次都创建新记录,不检查是否已存在
            review.CreatedAt = DateTime.UtcNow;
            var response = await SupabaseClient
                .From<UserCityReview>()
                .Insert(review);

            return response.Models.First();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "创建评论失败: {CityId}, {UserId}", review.CityId, review.UserId);
            throw;
        }
    }
    
    /// <summary>
    ///     根据ID获取评论
    /// </summary>
    public async Task<UserCityReview?> GetByIdAsync(Guid reviewId)
    {
        try
        {
            var response = await SupabaseClient
                .From<UserCityReview>()
                .Where(x => x.Id == reviewId)
                .Single();

            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "获取评论失败: {ReviewId}", reviewId);
            return null;
        }
    }

    public async Task<IEnumerable<UserCityReview>> GetByCityIdAsync(string cityId)
    {
        var response = await SupabaseClient
            .From<UserCityReview>()
            .Where(x => x.CityId == cityId)
            .Order(x => x.CreatedAt, Constants.Ordering.Descending)
            .Get();

        return response.Models;
    }

    public async Task<(IEnumerable<UserCityReview> Reviews, int TotalCount)> GetByCityIdPagedAsync(string cityId, int page, int pageSize)
    {
        // 获取总数
        var countResponse = await SupabaseClient
            .From<UserCityReview>()
            .Where(x => x.CityId == cityId)
            .Count(Constants.CountType.Exact);

        var totalCount = countResponse;

        // 获取分页数据
        var offset = (page - 1) * pageSize;
        var response = await SupabaseClient
            .From<UserCityReview>()
            .Where(x => x.CityId == cityId)
            .Order(x => x.CreatedAt, Constants.Ordering.Descending)
            .Range(offset, offset + pageSize - 1)
            .Get();

        return (response.Models, totalCount);
    }

    /// <summary>
    ///     获取用户对某个城市的所有评论(可能有多条)
    /// </summary>
    public async Task<IEnumerable<UserCityReview>> GetByCityIdAndUserIdAsync(string cityId, Guid userId)
    {
        try
        {
            var response = await SupabaseClient
                .From<UserCityReview>()
                .Where(x => x.CityId == cityId && x.UserId == userId)
                .Order(x => x.CreatedAt, Constants.Ordering.Descending)
                .Get();

            return response.Models;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "获取用户城市评论失败: {CityId}, {UserId}", cityId, userId);
            return Enumerable.Empty<UserCityReview>();
        }
    }

    /// <summary>
    ///     删除评论(根据 reviewId 删除)
    /// </summary>
    public async Task<bool> DeleteAsync(Guid reviewId, Guid userId)
    {
        try
        {
            await SupabaseClient
                .From<UserCityReview>()
                .Where(x => x.Id == reviewId && x.UserId == userId)
                .Delete();

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "删除评论失败: {ReviewId}, {UserId}", reviewId, userId);
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