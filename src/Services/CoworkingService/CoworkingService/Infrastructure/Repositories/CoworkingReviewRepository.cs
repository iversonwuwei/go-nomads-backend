using CoworkingService.Domain.Entities;
using CoworkingService.Domain.Repositories;
using Postgrest;
using Client = Supabase.Client;

namespace CoworkingService.Infrastructure.Repositories;

/// <summary>
/// Coworking Review Repository 实现
/// </summary>
public class CoworkingReviewRepository : ICoworkingReviewRepository
{
    private readonly ILogger<CoworkingReviewRepository> _logger;
    private readonly Client _supabaseClient;

    public CoworkingReviewRepository(Client supabaseClient, ILogger<CoworkingReviewRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<(List<CoworkingReview> Reviews, int TotalCount)> GetReviewsByCoworkingIdAsync(
        Guid coworkingId,
        int page,
        int pageSize)
    {
        try
        {
            var offset = (page - 1) * pageSize;

            // 查询评论列表
            var response = await _supabaseClient
                .From<CoworkingReview>()
                .Filter("coworking_id", Constants.Operator.Equals, coworkingId.ToString())
                .Order("created_at", Constants.Ordering.Descending)
                .Range(offset, offset + pageSize - 1)
                .Get();

            var reviews = response?.Models ?? new List<CoworkingReview>();

            // 查询总数 - 使用单独的查询
            var countResponse = await _supabaseClient
                .From<CoworkingReview>()
                .Filter("coworking_id", Constants.Operator.Equals, coworkingId.ToString())
                .Get();

            var totalCount = countResponse?.Models?.Count ?? 0;

            return (reviews, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 Coworking {CoworkingId} 的评论列表失败", coworkingId);
            throw;
        }
    }

    public async Task<CoworkingReview?> GetByIdAsync(Guid reviewId)
    {
        try
        {
            var response = await _supabaseClient
                .From<CoworkingReview>()
                .Filter("id", Constants.Operator.Equals, reviewId.ToString())
                .Single();

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取评论 {ReviewId} 失败", reviewId);
            return null;
        }
    }

    public async Task<CoworkingReview?> GetUserReviewForCoworkingAsync(Guid coworkingId, Guid userId)
    {
        try
        {
            var response = await _supabaseClient
                .From<CoworkingReview>()
                .Filter("coworking_id", Constants.Operator.Equals, coworkingId.ToString())
                .Filter("user_id", Constants.Operator.Equals, userId.ToString())
                .Single();

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "用户 {UserId} 对 Coworking {CoworkingId} 没有评论", userId, coworkingId);
            return null;
        }
    }

    public async Task<CoworkingReview> AddAsync(CoworkingReview review)
    {
        try
        {
            review.Id = Guid.NewGuid();
            review.CreatedAt = DateTime.UtcNow;
            review.IsVerified = false;

            var response = await _supabaseClient
                .From<CoworkingReview>()
                .Insert(review);

            var created = response.Models.FirstOrDefault();
            if (created == null)
                throw new InvalidOperationException("创建评论失败");

            _logger.LogInformation("✅ 评论创建成功: {Id}", created.Id);
            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 添加评论失败");
            throw;
        }
    }

    public async Task<CoworkingReview> UpdateAsync(CoworkingReview review)
    {
        try
        {
            review.UpdatedAt = DateTime.UtcNow;

            var response = await _supabaseClient
                .From<CoworkingReview>()
                .Update(review);

            var updated = response.Models.FirstOrDefault();
            if (updated == null)
                throw new InvalidOperationException("更新评论失败");

            _logger.LogInformation("✅ 评论更新成功: {Id}", updated.Id);
            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新评论 {ReviewId} 失败", review.Id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid reviewId)
    {
        try
        {
            await _supabaseClient
                .From<CoworkingReview>()
                .Filter("id", Constants.Operator.Equals, reviewId.ToString())
                .Delete();

            _logger.LogInformation("✅ 删除评论 {ReviewId} 成功", reviewId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除评论 {ReviewId} 失败", reviewId);
            throw;
        }
    }

    public async Task<(double AverageRating, int ReviewCount)> GetAverageRatingAsync(Guid coworkingId)
    {
        try
        {
            var response = await _supabaseClient
                .From<CoworkingReview>()
                .Filter("coworking_id", Constants.Operator.Equals, coworkingId.ToString())
                .Get();

            var reviews = response?.Models ?? new List<CoworkingReview>();

            if (reviews.Count == 0)
                return (0, 0);

            var averageRating = reviews.Average(r => r.Rating);
            return (Math.Round(averageRating, 1), reviews.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 计算 Coworking {CoworkingId} 平均评分失败", coworkingId);
            return (0, 0);
        }
    }

    public async Task<Dictionary<Guid, (double AverageRating, int ReviewCount)>> GetAverageRatingsByCoworkingIdsAsync(
        IEnumerable<Guid> coworkingIds)
    {
        var result = new Dictionary<Guid, (double AverageRating, int ReviewCount)>();
        var idList = coworkingIds.ToList();

        if (idList.Count == 0)
            return result;

        try
        {
            // 批量查询所有相关的评论
            var coworkingIdStrings = idList.Select(id => id.ToString()).ToList();
            var response = await _supabaseClient
                .From<CoworkingReview>()
                .Filter("coworking_id", Constants.Operator.In, coworkingIdStrings)
                .Get();

            var allReviews = response?.Models ?? new List<CoworkingReview>();

            // 按 coworking_id 分组计算
            var grouped = allReviews.GroupBy(r => r.CoworkingId);

            foreach (var group in grouped)
            {
                var reviews = group.ToList();
                var averageRating = reviews.Average(r => r.Rating);
                result[group.Key] = (Math.Round(averageRating, 1), reviews.Count);
            }

            // 为没有评论的 coworking 设置默认值
            foreach (var id in idList)
            {
                if (!result.ContainsKey(id))
                {
                    result[id] = (0, 0);
                }
            }

            _logger.LogInformation("✅ 批量获取 {Count} 个 Coworking 的评分信息", idList.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 批量计算 Coworking 平均评分失败");
            // 返回空结果而不抛异常
            foreach (var id in idList)
            {
                result[id] = (0, 0);
            }
            return result;
        }
    }
}
