using CoworkingService.Application.DTOs.Review;
using CoworkingService.Domain.Entities;
using CoworkingService.Domain.Repositories;
using CoworkingService.Services;

namespace CoworkingService.Application.Services;

/// <summary>
/// Coworking 评论应用服务实现
/// </summary>
public class CoworkingReviewService : ICoworkingReviewService
{
    private readonly ICoworkingReviewRepository _reviewRepository;
    private readonly ICacheServiceClient _cacheServiceClient;
    private readonly IUserServiceClient _userServiceClient;
    private readonly ILogger<CoworkingReviewService> _logger;

    public CoworkingReviewService(
        ICoworkingReviewRepository reviewRepository,
        ICacheServiceClient cacheServiceClient,
        IUserServiceClient userServiceClient,
        ILogger<CoworkingReviewService> logger)
    {
        _reviewRepository = reviewRepository;
        _cacheServiceClient = cacheServiceClient;
        _userServiceClient = userServiceClient;
        _logger = logger;
    }

    public async Task<PaginatedReviewsResponse> GetReviewsByCoworkingIdAsync(Guid coworkingId, int page, int pageSize)
    {
        _logger.LogInformation("获取 Coworking {CoworkingId} 的评论列表，Page: {Page}, PageSize: {PageSize}", 
            coworkingId, page, pageSize);

        try
        {
            var (reviews, totalCount) = await _reviewRepository.GetReviewsByCoworkingIdAsync(coworkingId, page, pageSize);

            // 批量获取用户信息
            var userIds = reviews.Select(r => r.UserId.ToString()).Distinct().ToList();
            var usersInfo = await _userServiceClient.GetUsersInfoAsync(userIds);

            return new PaginatedReviewsResponse
            {
                Items = reviews.Select(r => MapToResponse(r, usersInfo)).ToList(),
                TotalCount = totalCount,
                CurrentPage = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取评论列表失败");
            throw;
        }
    }

    public async Task<CoworkingReviewResponse?> GetReviewByIdAsync(Guid reviewId)
    {
        _logger.LogInformation("获取评论详情: {ReviewId}", reviewId);

        var review = await _reviewRepository.GetByIdAsync(reviewId);
        if (review == null) return null;

        // 获取用户信息
        var userInfo = await _userServiceClient.GetUserInfoAsync(review.UserId.ToString());
        var usersInfo = userInfo != null 
            ? new Dictionary<string, UserInfoDto> { { review.UserId.ToString(), userInfo } } 
            : new Dictionary<string, UserInfoDto>();

        return MapToResponse(review, usersInfo);
    }

    public async Task<CoworkingReviewResponse?> GetUserReviewForCoworkingAsync(Guid coworkingId, Guid userId)
    {
        _logger.LogInformation("获取用户 {UserId} 对 Coworking {CoworkingId} 的评论", userId, coworkingId);

        var review = await _reviewRepository.GetUserReviewForCoworkingAsync(coworkingId, userId);
        if (review == null) return null;

        // 获取用户信息
        var userInfo = await _userServiceClient.GetUserInfoAsync(userId.ToString());
        var usersInfo = userInfo != null 
            ? new Dictionary<string, UserInfoDto> { { userId.ToString(), userInfo } } 
            : new Dictionary<string, UserInfoDto>();

        return MapToResponse(review, usersInfo);
    }

    public async Task<CoworkingReviewResponse> AddReviewAsync(
        Guid coworkingId, 
        Guid userId, 
        AddCoworkingReviewRequest request)
    {
        _logger.LogInformation("用户 {UserId} 添加 Coworking {CoworkingId} 评论", userId, coworkingId);

        try
        {
            // 从 UserService 获取用户信息（用于返回，不存储）
            var userInfo = await _userServiceClient.GetUserInfoAsync(userId.ToString());

            _logger.LogInformation("获取到用户信息: Username={Username}, Avatar={Avatar}", 
                userInfo?.Username ?? "匿名用户", userInfo?.AvatarUrl);

            // 验证评分
            CoworkingReview.ValidateRating(request.Rating);

            // 验证照片数量
            if (request.PhotoUrls != null && request.PhotoUrls.Count > 5)
            {
                throw new ArgumentException("照片数量不能超过 5 张");
            }

            // 创建评论实体，不再存储用户名和头像
            var review = new CoworkingReview
            {
                CoworkingId = coworkingId,
                UserId = userId,
                Rating = request.Rating,
                Title = request.Title,
                Content = request.Content,
                VisitDate = request.VisitDate,
                PhotoUrls = request.PhotoUrls
            };

            var created = await _reviewRepository.AddAsync(review);
            _logger.LogInformation("✅ 评论添加成功: {ReviewId}", created.Id);

            // 更新评分缓存
            await UpdateCoworkingScoreCacheAsync(coworkingId);

            // 返回时动态填充用户信息
            var usersInfo = userInfo != null 
                ? new Dictionary<string, UserInfoDto> { { userId.ToString(), userInfo } } 
                : new Dictionary<string, UserInfoDto>();

            return MapToResponse(created, usersInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加评论失败");
            throw;
        }
    }

    public async Task<CoworkingReviewResponse> UpdateReviewAsync(
        Guid reviewId, 
        Guid userId, 
        UpdateCoworkingReviewRequest request)
    {
        _logger.LogInformation("用户 {UserId} 更新评论 {ReviewId}", userId, reviewId);

        try
        {
            // 获取现有评论
            var review = await _reviewRepository.GetByIdAsync(reviewId);
            if (review == null)
            {
                throw new KeyNotFoundException($"未找到 ID 为 {reviewId} 的评论");
            }

            // 检查权限：只能更新自己的评论
            if (review.UserId != userId)
            {
                throw new UnauthorizedAccessException("您只能更新自己的评论");
            }

            // 验证输入
            CoworkingReview.ValidateRating(request.Rating);

            if (request.PhotoUrls != null && request.PhotoUrls.Count > 5)
            {
                throw new ArgumentException("照片数量不能超过 5 张");
            }

            // 更新评论
            review.Rating = request.Rating;
            review.Title = request.Title;
            review.Content = request.Content;
            review.VisitDate = request.VisitDate;
            review.PhotoUrls = request.PhotoUrls;

            var updated = await _reviewRepository.UpdateAsync(review);
            _logger.LogInformation("✅ 评论更新成功: {ReviewId}", updated.Id);

            // 更新评分缓存
            await UpdateCoworkingScoreCacheAsync(review.CoworkingId);

            // 返回时动态填充用户信息
            var userInfo = await _userServiceClient.GetUserInfoAsync(userId.ToString());
            var usersInfo = userInfo != null 
                ? new Dictionary<string, UserInfoDto> { { userId.ToString(), userInfo } } 
                : new Dictionary<string, UserInfoDto>();

            return MapToResponse(updated, usersInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新评论失败");
            throw;
        }
    }

    public async Task DeleteReviewAsync(Guid reviewId, Guid userId)
    {
        _logger.LogInformation("用户 {UserId} 删除评论 {ReviewId}", userId, reviewId);

        try
        {
            // 获取现有评论
            var review = await _reviewRepository.GetByIdAsync(reviewId);
            if (review == null)
            {
                throw new KeyNotFoundException($"未找到 ID 为 {reviewId} 的评论");
            }

            // 检查权限：只能删除自己的评论
            if (review.UserId != userId)
            {
                throw new UnauthorizedAccessException("您只能删除自己的评论");
            }

            var coworkingId = review.CoworkingId;
            await _reviewRepository.DeleteAsync(reviewId);
            _logger.LogInformation("✅ 评论删除成功: {ReviewId}", reviewId);

            // 更新评分缓存
            await UpdateCoworkingScoreCacheAsync(coworkingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除评论失败");
            throw;
        }
    }

    public async Task<(double AverageRating, int ReviewCount)> GetAverageRatingAsync(Guid coworkingId)
    {
        _logger.LogInformation("获取 Coworking {CoworkingId} 的平均评分", coworkingId);

        return await _reviewRepository.GetAverageRatingAsync(coworkingId);
    }

    #region 私有方法

    /// <summary>
    /// 更新 Coworking 评分缓存
    /// </summary>
    private async Task UpdateCoworkingScoreCacheAsync(Guid coworkingId)
    {
        try
        {
            var (averageRating, reviewCount) = await _reviewRepository.GetAverageRatingAsync(coworkingId);
            await _cacheServiceClient.UpdateCoworkingScoreCacheAsync(coworkingId, averageRating, reviewCount);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "更新评分缓存失败，但不影响主流程");
        }
    }

    private static CoworkingReviewResponse MapToResponse(CoworkingReview review, Dictionary<string, UserInfoDto> usersInfo)
    {
        // 从用户信息字典中获取用户名和头像
        var userIdStr = review.UserId.ToString();
        var username = usersInfo.TryGetValue(userIdStr, out var userInfo) ? userInfo.Username : "匿名用户";
        var userAvatar = userInfo?.AvatarUrl;

        return new CoworkingReviewResponse
        {
            Id = review.Id,
            UserId = review.UserId,
            Username = username,
            UserAvatar = userAvatar,
            CoworkingId = review.CoworkingId,
            Rating = review.Rating,
            Title = review.Title,
            Content = review.Content,
            VisitDate = review.VisitDate,
            PhotoUrls = review.PhotoUrls,
            IsVerified = review.IsVerified,
            CreatedAt = review.CreatedAt,
            UpdatedAt = review.UpdatedAt
        };
    }

    #endregion
}
