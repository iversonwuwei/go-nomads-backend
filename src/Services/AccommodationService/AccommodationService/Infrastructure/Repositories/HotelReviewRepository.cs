using AccommodationService.Domain.Entities;
using AccommodationService.Domain.Repositories;
using Postgrest;
using Client = Supabase.Client;

namespace AccommodationService.Infrastructure.Repositories;

/// <summary>
///     酒店评论仓储实现 - 使用 Supabase 客户端
/// </summary>
public class HotelReviewRepository : IHotelReviewRepository
{
    private readonly Client _supabase;
    private readonly ILogger<HotelReviewRepository> _logger;

    public HotelReviewRepository(Client supabase, ILogger<HotelReviewRepository> logger)
    {
        _supabase = supabase;
        _logger = logger;
    }

    public async Task<HotelReview?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _supabase
                .From<HotelReview>()
                .Where(r => r.Id == id)
                .Single();

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hotel review by ID: {ReviewId}", id);
            return null;
        }
    }

    public async Task<(List<HotelReview> Reviews, int TotalCount)> GetByHotelIdAsync(
        Guid hotelId,
        int page = 1,
        int pageSize = 10,
        string? sortBy = "newest",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var offset = (page - 1) * pageSize;

            // 获取总数
            var totalCount = await _supabase
                .From<HotelReview>()
                .Where(r => r.HotelId == hotelId)
                .Count(Constants.CountType.Exact);

            // 根据排序方式构建查询
            List<HotelReview> reviews;
            switch (sortBy?.ToLower())
            {
                case "helpful":
                    var helpfulResponse = await _supabase
                        .From<HotelReview>()
                        .Where(r => r.HotelId == hotelId)
                        .Order("helpful_count", Constants.Ordering.Descending)
                        .Range(offset, offset + pageSize - 1)
                        .Get();
                    reviews = helpfulResponse.Models;
                    break;

                case "highest":
                    var highestResponse = await _supabase
                        .From<HotelReview>()
                        .Where(r => r.HotelId == hotelId)
                        .Order("rating", Constants.Ordering.Descending)
                        .Range(offset, offset + pageSize - 1)
                        .Get();
                    reviews = highestResponse.Models;
                    break;

                case "lowest":
                    var lowestResponse = await _supabase
                        .From<HotelReview>()
                        .Where(r => r.HotelId == hotelId)
                        .Order("rating", Constants.Ordering.Ascending)
                        .Range(offset, offset + pageSize - 1)
                        .Get();
                    reviews = lowestResponse.Models;
                    break;

                case "newest":
                default:
                    var newestResponse = await _supabase
                        .From<HotelReview>()
                        .Where(r => r.HotelId == hotelId)
                        .Order("created_at", Constants.Ordering.Descending)
                        .Range(offset, offset + pageSize - 1)
                        .Get();
                    reviews = newestResponse.Models;
                    break;
            }

            return (reviews, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reviews for hotel: {HotelId}", hotelId);
            return (new List<HotelReview>(), 0);
        }
    }

    public async Task<HotelReview?> GetUserReviewForHotelAsync(
        Guid hotelId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _supabase
                .From<HotelReview>()
                .Where(r => r.HotelId == hotelId && r.UserId == userId)
                .Single();

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "No review found for hotel {HotelId} by user {UserId}", hotelId, userId);
            return null;
        }
    }

    public async Task<List<HotelReview>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _supabase
                .From<HotelReview>()
                .Where(r => r.UserId == userId)
                .Order("created_at", Constants.Ordering.Descending)
                .Get();

            return response.Models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reviews by user: {UserId}", userId);
            return new List<HotelReview>();
        }
    }

    public async Task<HotelReview> CreateAsync(HotelReview review, CancellationToken cancellationToken = default)
    {
        try
        {
            review.CreatedAt = DateTime.UtcNow;
            review.UpdatedAt = DateTime.UtcNow;

            var response = await _supabase
                .From<HotelReview>()
                .Insert(review);

            var created = response.Models.FirstOrDefault();
            if (created == null)
            {
                throw new Exception("Failed to create hotel review");
            }

            _logger.LogInformation("Created hotel review {ReviewId} for hotel {HotelId}", created.Id, created.HotelId);
            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating hotel review for hotel: {HotelId}", review.HotelId);
            throw;
        }
    }

    public async Task<HotelReview> UpdateAsync(HotelReview review, CancellationToken cancellationToken = default)
    {
        try
        {
            review.UpdatedAt = DateTime.UtcNow;

            var response = await _supabase
                .From<HotelReview>()
                .Where(r => r.Id == review.Id)
                .Update(review);

            var updated = response.Models.FirstOrDefault();
            if (updated == null)
            {
                throw new Exception($"Failed to update hotel review: {review.Id}");
            }

            _logger.LogInformation("Updated hotel review: {ReviewId}", review.Id);
            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating hotel review: {ReviewId}", review.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _supabase
                .From<HotelReview>()
                .Where(r => r.Id == id)
                .Delete();

            _logger.LogInformation("Deleted hotel review: {ReviewId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting hotel review: {ReviewId}", id);
            return false;
        }
    }

    public async Task<(double AverageRating, int TotalCount, Dictionary<int, int> Distribution)> GetRatingStatsAsync(
        Guid hotelId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _supabase
                .From<HotelReview>()
                .Where(r => r.HotelId == hotelId)
                .Get();

            var reviews = response.Models;

            if (!reviews.Any())
            {
                return (0, 0, new Dictionary<int, int> { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 } });
            }

            var totalCount = reviews.Count;
            var averageRating = reviews.Average(r => r.Rating);
            var distribution = reviews
                .GroupBy(r => r.Rating)
                .ToDictionary(g => g.Key, g => g.Count());

            // 确保所有评分等级都有值
            for (var i = 1; i <= 5; i++)
            {
                if (!distribution.ContainsKey(i))
                {
                    distribution[i] = 0;
                }
            }

            return (Math.Round(averageRating, 1), totalCount, distribution);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rating stats for hotel: {HotelId}", hotelId);
            return (0, 0, new Dictionary<int, int> { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 } });
        }
    }

    public async Task<bool> IncrementHelpfulCountAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        try
        {
            var review = await GetByIdAsync(reviewId, cancellationToken);
            if (review == null)
            {
                return false;
            }

            review.HelpfulCount++;
            await UpdateAsync(review, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing helpful count for review: {ReviewId}", reviewId);
            return false;
        }
    }
}
