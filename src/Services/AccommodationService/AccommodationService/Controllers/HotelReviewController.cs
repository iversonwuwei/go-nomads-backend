using AccommodationService.Application.DTOs;
using AccommodationService.Domain.Entities;
using AccommodationService.Domain.Repositories;
using GoNomads.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace AccommodationService.Controllers;

/// <summary>
///     酒店评论 API 控制器
/// </summary>
[ApiController]
[Route("api/v1/hotels")]
public class HotelReviewController : ControllerBase
{
    private readonly IHotelReviewRepository _reviewRepository;
    private readonly IHotelRepository _hotelRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<HotelReviewController> _logger;

    public HotelReviewController(
        IHotelReviewRepository reviewRepository,
        IHotelRepository hotelRepository,
        ICurrentUserService currentUser,
        ILogger<HotelReviewController> logger)
    {
        _reviewRepository = reviewRepository;
        _hotelRepository = hotelRepository;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    ///     获取酒店的评论列表
    /// </summary>
    [HttpGet("{hotelId:guid}/reviews")]
    public async Task<ActionResult<HotelReviewListResponse>> GetReviews(
        Guid hotelId,
        [FromQuery] HotelReviewQueryParameters parameters)
    {
        var hotel = await _hotelRepository.GetByIdAsync(hotelId);
        if (hotel == null)
        {
            return NotFound(new { message = "Hotel not found" });
        }

        var (reviews, totalCount) = await _reviewRepository.GetByHotelIdAsync(
            hotelId,
            parameters.Page,
            parameters.PageSize,
            parameters.SortBy);

        var reviewDtos = reviews.Select(MapToDto).ToList();

        // 获取评分统计
        var (averageRating, _, distribution) = await _reviewRepository.GetRatingStatsAsync(hotelId);

        var response = new HotelReviewListResponse
        {
            Reviews = reviewDtos,
            TotalCount = totalCount,
            Page = parameters.Page,
            PageSize = parameters.PageSize,
            // TotalPages 是计算属性，不需要赋值
            AverageRating = averageRating,
            RatingDistribution = distribution
        };

        return Ok(response);
    }

    /// <summary>
    ///     获取评论详情
    /// </summary>
    [HttpGet("reviews/{reviewId:guid}")]
    public async Task<ActionResult<HotelReviewDto>> GetReview(Guid reviewId)
    {
        var review = await _reviewRepository.GetByIdAsync(reviewId);
        if (review == null)
        {
            return NotFound(new { message = "Review not found" });
        }

        return Ok(MapToDto(review));
    }

    /// <summary>
    ///     获取当前用户对某酒店的评论
    /// </summary>
    [HttpGet("{hotelId:guid}/reviews/mine")]
    public async Task<ActionResult<HotelReviewDto?>> GetMyReview(Guid hotelId)
    {
        var userId = _currentUser.TryGetUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "需要登录" });
        }

        var review = await _reviewRepository.GetUserReviewForHotelAsync(hotelId, userId.Value);
        if (review == null)
        {
            return Ok(null);
        }

        return Ok(MapToDto(review));
    }

    /// <summary>
    ///     创建评论
    /// </summary>
    [HttpPost("{hotelId:guid}/reviews")]
    public async Task<ActionResult<HotelReviewDto>> CreateReview(
        Guid hotelId,
        [FromBody] CreateHotelReviewRequest request)
    {
        var userId = _currentUser.TryGetUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "需要登录" });
        }

        // 获取用户邮箱作为用户名，如果没有则使用匿名用户
        var userEmail = _currentUser.GetUserEmail();
        var userName = !string.IsNullOrEmpty(userEmail) 
            ? userEmail.Split('@')[0]  // 使用邮箱前缀作为用户名
            : "匿名用户";

        // 验证酒店是否存在
        var hotel = await _hotelRepository.GetByIdAsync(hotelId);
        if (hotel == null)
        {
            return NotFound(new { message = "Hotel not found" });
        }

        // 检查用户是否已经评论过
        var existingReview = await _reviewRepository.GetUserReviewForHotelAsync(hotelId, userId.Value);
        if (existingReview != null)
        {
            return BadRequest(new { message = "您已经评论过这家酒店了" });
        }

        // 验证评分范围
        if (request.Rating < 1 || request.Rating > 5)
        {
            return BadRequest(new { message = "评分必须在1-5之间" });
        }

        var review = new HotelReview
        {
            Id = Guid.NewGuid(),
            HotelId = hotelId,
            UserId = userId.Value,
            UserName = userName,
            Rating = request.Rating,
            Title = request.Title,
            Content = request.Content,
            VisitDate = request.VisitDate,
            PhotoUrls = request.PhotoUrls?.ToArray(),
            IsVerified = false,
            HelpfulCount = 0
        };

        try
        {
            var created = await _reviewRepository.CreateAsync(review);

            // 更新酒店的评论数
            hotel.ReviewCount++;
            await _hotelRepository.UpdateAsync(hotel);

            _logger.LogInformation("User {UserId} created review for hotel {HotelId}", userId, hotelId);
            return CreatedAtAction(nameof(GetReview), new { reviewId = created.Id }, MapToDto(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating review for hotel {HotelId}", hotelId);
            return BadRequest(new { message = "创建评论失败" });
        }
    }

    /// <summary>
    ///     更新评论（只有作者可以更新）
    /// </summary>
    [HttpPut("reviews/{reviewId:guid}")]
    public async Task<ActionResult<HotelReviewDto>> UpdateReview(
        Guid reviewId,
        [FromBody] UpdateHotelReviewRequest request)
    {
        var userId = _currentUser.TryGetUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "需要登录" });
        }

        var review = await _reviewRepository.GetByIdAsync(reviewId);
        if (review == null)
        {
            return NotFound(new { message = "Review not found" });
        }

        // 检查权限
        if (review.UserId != userId.Value)
        {
            return Forbid();
        }

        // 验证评分范围
        if (request.Rating.HasValue && (request.Rating.Value < 1 || request.Rating.Value > 5))
        {
            return BadRequest(new { message = "评分必须在1-5之间" });
        }

        // 更新字段
        if (request.Rating.HasValue)
            review.Rating = request.Rating.Value;
        if (request.Title != null)
            review.Title = request.Title;
        if (request.Content != null)
            review.Content = request.Content;
        if (request.VisitDate.HasValue)
            review.VisitDate = request.VisitDate;
        if (request.PhotoUrls != null)
            review.PhotoUrls = request.PhotoUrls.ToArray();

        try
        {
            var updated = await _reviewRepository.UpdateAsync(review);
            _logger.LogInformation("User {UserId} updated review {ReviewId}", userId, reviewId);
            return Ok(MapToDto(updated));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating review {ReviewId}", reviewId);
            return BadRequest(new { message = "更新评论失败" });
        }
    }

    /// <summary>
    ///     删除评论（只有作者可以删除）
    /// </summary>
    [HttpDelete("reviews/{reviewId:guid}")]
    public async Task<ActionResult> DeleteReview(Guid reviewId)
    {
        var userId = _currentUser.TryGetUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "需要登录" });
        }

        var review = await _reviewRepository.GetByIdAsync(reviewId);
        if (review == null)
        {
            return NotFound(new { message = "Review not found" });
        }

        // 检查权限
        if (review.UserId != userId.Value)
        {
            return Forbid();
        }

        try
        {
            var result = await _reviewRepository.DeleteAsync(reviewId);
            if (!result)
            {
                return BadRequest(new { message = "删除评论失败" });
            }

            // 更新酒店的评论数
            var hotel = await _hotelRepository.GetByIdAsync(review.HotelId);
            if (hotel != null && hotel.ReviewCount > 0)
            {
                hotel.ReviewCount--;
                await _hotelRepository.UpdateAsync(hotel);
            }

            _logger.LogInformation("User {UserId} deleted review {ReviewId}", userId, reviewId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting review {ReviewId}", reviewId);
            return BadRequest(new { message = "删除评论失败" });
        }
    }

    /// <summary>
    ///     标记评论为有帮助
    /// </summary>
    [HttpPost("reviews/{reviewId:guid}/helpful")]
    public async Task<ActionResult> MarkHelpful(Guid reviewId)
    {
        var userId = _currentUser.TryGetUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "需要登录" });
        }

        var review = await _reviewRepository.GetByIdAsync(reviewId);
        if (review == null)
        {
            return NotFound(new { message = "Review not found" });
        }

        var result = await _reviewRepository.IncrementHelpfulCountAsync(reviewId);
        if (!result)
        {
            return BadRequest(new { message = "操作失败" });
        }

        return Ok(new { message = "已标记为有帮助" });
    }

    /// <summary>
    ///     获取酒店评分统计
    /// </summary>
    [HttpGet("{hotelId:guid}/reviews/stats")]
    public async Task<ActionResult<object>> GetRatingStats(Guid hotelId)
    {
        var hotel = await _hotelRepository.GetByIdAsync(hotelId);
        if (hotel == null)
        {
            return NotFound(new { message = "Hotel not found" });
        }

        var (averageRating, totalCount, distribution) = await _reviewRepository.GetRatingStatsAsync(hotelId);

        return Ok(new
        {
            averageRating,
            totalCount,
            distribution
        });
    }

    private static HotelReviewDto MapToDto(HotelReview review)
    {
        return new HotelReviewDto
        {
            Id = review.Id,
            HotelId = review.HotelId,
            UserId = review.UserId,
            UserName = review.UserName,
            Rating = review.Rating,
            Title = review.Title,
            Content = review.Content,
            VisitDate = review.VisitDate,
            PhotoUrls = review.PhotoUrls ?? Array.Empty<string>(),
            IsVerified = review.IsVerified,
            HelpfulCount = review.HelpfulCount,
            CreatedAt = review.CreatedAt,
            UpdatedAt = review.UpdatedAt
        };
    }
}
