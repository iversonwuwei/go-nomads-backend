using CityService.Application.Abstractions.Services;
using CityService.Application.DTOs;
using CityService.Domain.Entities;
using CityService.Domain.Repositories;
using CityService.Services;
using MassTransit;
using Microsoft.Extensions.Caching.Memory;
using Shared.Messages;

namespace CityService.Application.Services;

/// <summary>
///     用户城市内容应用服务实现
/// </summary>
public class UserCityContentApplicationService : IUserCityContentService
{
    private readonly IAmapGeocodingService _amapGeocodingService;
    private readonly IUserCityExpenseRepository _expenseRepository;
    private readonly ILogger<UserCityContentApplicationService> _logger;
    private readonly IUserCityPhotoRepository _photoRepository;
    private readonly IUserCityProsConsRepository _prosConsRepository;
    private readonly IUserCityReviewRepository _reviewRepository;
    private readonly IUserServiceClient _userServiceClient;
    private readonly ICacheServiceClient _cacheServiceClient;
    private readonly IMemoryCache _cache;
    private readonly IPublishEndpoint _publishEndpoint;

    public UserCityContentApplicationService(
        IUserCityPhotoRepository photoRepository,
        IUserCityExpenseRepository expenseRepository,
        IUserCityReviewRepository reviewRepository,
        IUserCityProsConsRepository prosConsRepository,
        IUserServiceClient userServiceClient,
        ICacheServiceClient cacheServiceClient,
        IAmapGeocodingService amapGeocodingService,
        IMemoryCache cache,
        IPublishEndpoint publishEndpoint,
        ILogger<UserCityContentApplicationService> logger)
    {
        _photoRepository = photoRepository;
        _expenseRepository = expenseRepository;
        _reviewRepository = reviewRepository;
        _prosConsRepository = prosConsRepository;
        _userServiceClient = userServiceClient;
        _cacheServiceClient = cacheServiceClient;
        _amapGeocodingService = amapGeocodingService;
        _cache = cache;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    /// <summary>
    /// 失效城市列表缓存（当评论、评分等数据变更时调用）
    /// </summary>
    private void InvalidateCityListCache()
    {
        var newVersion = DateTime.UtcNow.Ticks;
        _cache.Set("city_list:version", newVersion);
        _logger.LogInformation("🗑️ [Cache] 城市列表缓存已失效 (from UserCityContent), 新版本号: {Version}", newVersion);
    }

    /// <summary>
    /// 失效城市详情缓存
    /// </summary>
    private void InvalidateCityDetailCache()
    {
        var newVersion = DateTime.UtcNow.Ticks;
        _cache.Set("city_detail:version", newVersion);
        _logger.LogInformation("🗑️ [Cache] 城市详情缓存已失效 (from UserCityContent), 新版本号: {Version}", newVersion);
    }

    /// <summary>
    /// 失效所有城市相关缓存
    /// </summary>
    private void InvalidateAllCityCaches()
    {
        InvalidateCityListCache();
        InvalidateCityDetailCache();
    }

    #region 照片相关

    public async Task<UserCityPhotoDto> AddPhotoAsync(Guid userId, AddCityPhotoRequest request)
    {
        var photo = new UserCityPhoto
        {
            UserId = userId,
            CityId = request.CityId,
            ImageUrl = request.ImageUrl,
            Caption = request.Caption,
            Location = request.Location,
            TakenAt = request.TakenAt
        };

        var created = await _photoRepository.CreateAsync(photo);
        _logger.LogInformation("用户 {UserId} 为城市 {CityId} 添加了照片 {PhotoId}", userId, request.CityId, created.Id);

        return MapPhotoToDto(created);
    }

    public async Task<IEnumerable<UserCityPhotoDto>> SubmitPhotoCollectionAsync(Guid userId,
        SubmitCityPhotoBatchRequest request)
    {
        if (request.ImageUrls is null || request.ImageUrls.Count == 0)
            throw new ArgumentException("图片列表不能为空", nameof(request.ImageUrls));

        if (request.ImageUrls.Count > 10) throw new ArgumentException("一次最多支持上传 10 张照片", nameof(request.ImageUrls));

        _logger.LogInformation(
            "用户 {UserId} 正在为城市 {CityId} 提交 {Count} 张照片(标题: {Title})",
            userId,
            request.CityId,
            request.ImageUrls.Count,
            string.IsNullOrWhiteSpace(request.Title) ? "(未填写)" : request.Title);

        var enrichedLocation = await TryEnrichLocationAsync(request);
        if (enrichedLocation is null)
            _logger.LogInformation("未获取到 AMap 位置信息, 使用用户输入: {CityId}", request.CityId);
        else
            _logger.LogInformation(
                "AMap 位置增强成功: {CityId} lat={Latitude}, lng={Longitude}, place={Place}",
                request.CityId,
                enrichedLocation.Latitude,
                enrichedLocation.Longitude,
                enrichedLocation.PlaceName ?? "(未知)");

        var photos = request.ImageUrls.Select(imageUrl => new UserCityPhoto
        {
            UserId = userId,
            CityId = request.CityId,
            ImageUrl = imageUrl,
            Caption = request.Title,
            Description = request.Description,
            Location = request.LocationNote,
            PlaceName = enrichedLocation?.PlaceName ?? request.Title,
            Address = enrichedLocation?.FormattedAddress,
            Latitude = enrichedLocation?.Latitude,
            Longitude = enrichedLocation?.Longitude
        }).ToList();

        var created = await _photoRepository.CreateBatchAsync(photos);
        var createdList = created.ToList();

        _logger.LogInformation(
            "用户 {UserId} 为城市 {CityId} 批量上传 {Count} 张照片",
            userId,
            request.CityId,
            createdList.Count);

        return createdList.Select(MapPhotoToDto);
    }

    public async Task<IEnumerable<UserCityPhotoDto>> GetCityPhotosAsync(string cityId, Guid? userId = null)
    {
        var photos = userId.HasValue
            ? await _photoRepository.GetByCityIdAndUserIdAsync(cityId, userId.Value)
            : await _photoRepository.GetByCityIdAsync(cityId);

        return photos.Select(MapPhotoToDto);
    }

    public async Task<IEnumerable<UserCityPhotoDto>> GetUserPhotosAsync(Guid userId)
    {
        var photos = await _photoRepository.GetByUserIdAsync(userId);
        return photos.Select(MapPhotoToDto);
    }

    public async Task<bool> DeletePhotoAsync(Guid userId, Guid photoId)
    {
        var deleted = await _photoRepository.DeleteAsync(photoId, userId);
        if (deleted) _logger.LogInformation("用户 {UserId} 删除了照片 {PhotoId}", userId, photoId);
        return deleted;
    }

    public async Task<CityPhotoModerationResultDto?> ReviewPhotoAsync(
        Guid photoId,
        string moderatorId,
        string action,
        string? reason)
    {
        var normalized = (action ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized != "approve" && normalized != "reject")
            throw new ArgumentException("仅支持 approve / reject", nameof(action));

        var updated = await _photoRepository.ModerateAsync(photoId, moderatorId, normalized, reason);
        if (updated == null) return null;

        return new CityPhotoModerationResultDto
        {
            PhotoId = updated.Id,
            CityId = updated.CityId,
            Action = normalized,
            Reason = reason,
            ModeratorId = moderatorId,
            ReviewedAt = updated.ModeratedAt ?? DateTime.UtcNow
        };
    }

    #endregion

    #region 费用相关

    public async Task<UserCityExpenseDto> AddExpenseAsync(Guid userId, AddCityExpenseRequest request)
    {
        var expense = new UserCityExpense
        {
            UserId = userId,
            CityId = request.CityId,
            Category = request.Category,
            Amount = request.Amount,
            Currency = request.Currency,
            Description = request.Description,
            Date = request.Date
        };

        var created = await _expenseRepository.CreateAsync(expense);
        _logger.LogInformation("用户 {UserId} 为城市 {CityId} 添加了费用 {ExpenseId}", userId, request.CityId, created.Id);

        // 异步更新费用缓存(不等待,避免阻塞)
        _ = Task.Run(async () =>
        {
            try
            {
                var statistics = await GetExpenseStatisticsAsync(request.CityId);
                await _cacheServiceClient.UpdateCityCostCacheAsync(
                    request.CityId,
                    statistics.TotalAverageCost,
                    System.Text.Json.JsonSerializer.Serialize(statistics)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update cost cache after adding expense for city {CityId}", request.CityId);
            }
        });

        return MapExpenseToDto(created);
    }

    public async Task<IEnumerable<UserCityExpenseDto>> GetCityExpensesAsync(string cityId, Guid? userId = null)
    {
        var expenses = userId.HasValue
            ? await _expenseRepository.GetByCityIdAndUserIdAsync(cityId, userId.Value)
            : await _expenseRepository.GetByCityIdAsync(cityId);

        return expenses.Select(MapExpenseToDto);
    }

    public async Task<IEnumerable<UserCityExpenseDto>> GetUserExpensesAsync(Guid userId)
    {
        var expenses = await _expenseRepository.GetByUserIdAsync(userId);
        return expenses.Select(MapExpenseToDto);
    }

    public async Task<bool> DeleteExpenseAsync(Guid userId, Guid expenseId)
    {
        // 先获取费用信息以便知道城市ID
        var expense = await _expenseRepository.GetByIdAsync(expenseId);
        var cityId = expense?.CityId;

        var deleted = await _expenseRepository.DeleteAsync(expenseId, userId);
        if (deleted)
        {
            _logger.LogInformation("用户 {UserId} 删除了费用 {ExpenseId}", userId, expenseId);

            // 异步更新费用缓存
            if (!string.IsNullOrEmpty(cityId))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var statistics = await GetExpenseStatisticsAsync(cityId);
                        await _cacheServiceClient.UpdateCityCostCacheAsync(
                            cityId,
                            statistics.TotalAverageCost,
                            System.Text.Json.JsonSerializer.Serialize(statistics)
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to update cost cache after deleting expense for city {CityId}", cityId);
                    }
                });
            }
        }
        return deleted;
    }

    #endregion

    #region 评论相关

    public async Task<UserCityReviewDto> CreateReviewAsync(Guid userId, UpsertCityReviewRequest request)
    {
        var review = new UserCityReview
        {
            UserId = userId,
            CityId = request.CityId,
            Rating = request.Rating,
            Title = request.Title,
            Content = request.Content,
            VisitDate = request.VisitDate,
            ReviewText = request.ReviewText,
            InternetQualityScore = request.InternetQualityScore,
            SafetyScore = request.SafetyScore,
            CostScore = request.CostScore,
            CommunityScore = request.CommunityScore,
            WeatherScore = request.WeatherScore,
            PhotoUrls = request.PhotoUrls // ✅ 直接保存照片 URL 到评论记录
        };

        var created = await _reviewRepository.CreateAsync(review); // ✅ 改为 CreateAsync,每次都新增记录

        // 失效所有城市相关缓存，确保评论数量能同步更新
        InvalidateAllCityCaches();
        
        // 更新城市评分缓存（评论的 Rating 字段影响城市总评分）
        await UpdateCityScoreFromReviewsAsync(request.CityId);

        _logger.LogInformation("用户 {UserId} 为城市 {CityId} 添加了新评论 {ReviewId}", userId, request.CityId, created.Id);
        
        // 发布评论更新消息到 SignalR
        await PublishReviewUpdatedMessageAsync(request.CityId, "created", created.Id);

        return MapReviewToDto(created);
    }
    
    /// <summary>
    /// 发布评论更新消息到消息队列（用于 SignalR 广播）
    /// </summary>
    private async Task PublishReviewUpdatedMessageAsync(string cityId, string changeType, Guid? reviewId = null)
    {
        try
        {
            // 获取最新的评论统计数据
            var reviews = await _reviewRepository.GetByCityIdAsync(cityId);
            var reviewList = reviews.ToList();
            var reviewCount = reviewList.Count;
            var validRatings = reviewList.Where(r => r.Rating > 0).Select(r => r.Rating).ToList();
            var overallScore = validRatings.Any() ? Math.Round(validRatings.Average(), 1) : 0;
            
            var message = new CityReviewUpdatedMessage
            {
                CityId = cityId,
                ChangeType = changeType,
                ReviewCount = reviewCount,
                OverallScore = overallScore,
                ReviewId = reviewId?.ToString(),
                UpdatedAt = DateTime.UtcNow
            };
            
            await _publishEndpoint.Publish(message);
            _logger.LogInformation("📤 已发布城市评论更新消息: CityId={CityId}, ChangeType={ChangeType}, ReviewCount={ReviewCount}", 
                cityId, changeType, reviewCount);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 发布评论更新消息失败: CityId={CityId}", cityId);
        }
    }
    
    /// <summary>
    /// 根据评论重新计算并更新城市评分缓存
    /// </summary>
    private async Task UpdateCityScoreFromReviewsAsync(string cityId)
    {
        try
        {
            // 获取该城市的所有评论
            var reviews = await _reviewRepository.GetByCityIdAsync(cityId);
            var reviewList = reviews.ToList();
            
            if (!reviewList.Any())
            {
                _logger.LogInformation("城市 {CityId} 暂无评论，跳过评分更新", cityId);
                return;
            }
            
            // 计算平均评分（只统计有有效评分的评论）
            var validRatings = reviewList.Where(r => r.Rating > 0).Select(r => r.Rating).ToList();
            if (!validRatings.Any())
            {
                _logger.LogInformation("城市 {CityId} 暂无有效评分，跳过评分更新", cityId);
                return;
            }
            
            var overallScore = Math.Round((decimal)validRatings.Average(), 1);
            
            // 更新缓存
            await _cacheServiceClient.UpdateCityScoreCacheAsync(cityId, overallScore);
            
            _logger.LogInformation("✅ 城市 {CityId} 评分已更新: {OverallScore} (基于 {Count} 条评论)", 
                cityId, overallScore, validRatings.Count);
        }
        catch (Exception ex)
        {
            // 缓存更新失败不影响主流程
            _logger.LogWarning(ex, "⚠️ 更新城市 {CityId} 评分缓存失败", cityId);
        }
    }

    public async Task<IEnumerable<UserCityReviewDto>> GetCityReviewsAsync(string cityId)
    {
        var reviews = await _reviewRepository.GetByCityIdAsync(cityId);
        var result = new List<UserCityReviewDto>();

        // ✅ 收集所有唯一的 userId
        var userIds = reviews.Select(r => r.UserId.ToString()).Distinct().ToList();

        // ✅ 通过 Dapr 批量获取用户信息
        var usersInfo = await _userServiceClient.GetUsersInfoAsync(userIds);

        foreach (var review in reviews)
        {
            var dto = MapReviewToDto(review);

            // ✅ 从 UserService 获取的用户信息填充到 DTO
            if (usersInfo.TryGetValue(review.UserId.ToString(), out var userInfo))
            {
                dto.Username = userInfo.Username; // Username 属性会返回 Name 或 Email 前缀
                dto.UserAvatar = userInfo.AvatarUrl; // ✅ 使用从 UserService 获取的头像
            }
            else
            {
                // 如果获取失败,使用默认值
                dto.Username = $"User {review.UserId.ToString().Substring(0, 8)}";
                dto.UserAvatar = null;
            }

            // ✅ 直接从 review 实体读取照片 URL，不再查询 user_city_photos 表
            dto.PhotoUrls = review.PhotoUrls ?? new List<string>();

            result.Add(dto);
        }

        return result;
    }

    public async Task<PagedResult<UserCityReviewDto>> GetCityReviewsPagedAsync(string cityId, int page = 1, int pageSize = 10)
    {
        var (reviews, totalCount) = await _reviewRepository.GetByCityIdPagedAsync(cityId, page, pageSize);
        var result = new List<UserCityReviewDto>();

        // ✅ 收集所有唯一的 userId
        var userIds = reviews.Select(r => r.UserId.ToString()).Distinct().ToList();

        // ✅ 通过 Dapr 批量获取用户信息
        var usersInfo = await _userServiceClient.GetUsersInfoAsync(userIds);

        foreach (var review in reviews)
        {
            var dto = MapReviewToDto(review);

            // ✅ 从 UserService 获取的用户信息填充到 DTO
            if (usersInfo.TryGetValue(review.UserId.ToString(), out var userInfo))
            {
                dto.Username = userInfo.Username;
                dto.UserAvatar = userInfo.AvatarUrl;
            }
            else
            {
                dto.Username = $"User {review.UserId.ToString().Substring(0, 8)}";
                dto.UserAvatar = null;
            }

            dto.PhotoUrls = review.PhotoUrls ?? new List<string>();
            result.Add(dto);
        }

        return new PagedResult<UserCityReviewDto>
        {
            Items = result,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IEnumerable<UserCityReviewDto>> GetUserReviewsAsync(Guid userId, string cityId)
    {
        var reviews = await _reviewRepository.GetByCityIdAndUserIdAsync(cityId, userId);
        var dtos = new List<UserCityReviewDto>();

        foreach (var review in reviews)
        {
            var dto = MapReviewToDto(review);

            // ✅ 直接从 review 实体读取照片 URL，不再查询 user_city_photos 表
            dto.PhotoUrls = review.PhotoUrls ?? new List<string>();

            dtos.Add(dto);
        }

        return dtos;
    }

    public async Task<bool> DeleteReviewAsync(Guid userId, Guid reviewId)
    {
        // 先获取评论信息以获取 cityId（用于后续更新评分缓存）
        string? cityId = null;
        try
        {
            var review = await _reviewRepository.GetByIdAsync(reviewId);
            cityId = review?.CityId;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取评论 {ReviewId} 的城市ID失败", reviewId);
        }
        
        var deleted = await _reviewRepository.DeleteAsync(reviewId, userId);
        if (deleted)
        {
            _logger.LogInformation("用户 {UserId} 删除了评论 {ReviewId}", userId, reviewId);
            
            // 失效所有城市相关缓存
            InvalidateAllCityCaches();
            
            // 更新城市评分缓存
            if (!string.IsNullOrEmpty(cityId))
            {
                await UpdateCityScoreFromReviewsAsync(cityId);
                
                // 发布评论删除消息到 SignalR
                await PublishReviewUpdatedMessageAsync(cityId, "deleted", reviewId);
            }
        }
        return deleted;
    }

    #endregion

    #region 统计相关

    public async Task<CityUserContentStatsDto> GetCityStatsAsync(string cityId)
    {
        var photos = await _photoRepository.GetByCityIdAsync(cityId);
        var expenses = await _expenseRepository.GetByCityIdAsync(cityId);
        var reviews = await _reviewRepository.GetByCityIdAsync(cityId);
        var averageRating = await _reviewRepository.GetAverageRatingAsync(cityId);

        return new CityUserContentStatsDto
        {
            CityId = cityId,
            PhotoCount = photos.Count(),
            ExpenseCount = expenses.Count(),
            ReviewCount = reviews.Count(),
            AverageRating = averageRating
        };
    }

    /// <summary>
    ///     获取城市综合费用统计 - 基于用户提交的实际费用数据计算
    /// </summary>
    public async Task<CityCostSummaryDto> GetCityCostSummaryAsync(string cityId)
    {
        var expenses = (await _expenseRepository.GetByCityIdAsync(cityId)).ToList();

        if (!expenses.Any())
            return new CityCostSummaryDto
            {
                CityId = cityId,
                Total = 0,
                Accommodation = 0,
                Food = 0,
                Transportation = 0,
                Activity = 0,
                Shopping = 0,
                Other = 0,
                ContributorCount = 0,
                TotalExpenseCount = 0,
                Currency = "USD",
                UpdatedAt = DateTime.UtcNow
            };

        // 按分类计算平均费用
        var accommodation = expenses
            .Where(e => e.Category.Equals(ExpenseCategory.Accommodation, StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Amount).DefaultIfEmpty(0).Average();

        var food = expenses.Where(e => e.Category.Equals(ExpenseCategory.Food, StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Amount).DefaultIfEmpty(0).Average();

        var transportation = expenses
            .Where(e => e.Category.Equals(ExpenseCategory.Transport, StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Amount).DefaultIfEmpty(0).Average();

        var activity = expenses
            .Where(e => e.Category.Equals(ExpenseCategory.Activity, StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Amount).DefaultIfEmpty(0).Average();

        var shopping = expenses
            .Where(e => e.Category.Equals(ExpenseCategory.Shopping, StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Amount).DefaultIfEmpty(0).Average();

        var other = expenses.Where(e => e.Category.Equals(ExpenseCategory.Other, StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Amount).DefaultIfEmpty(0).Average();

        var total = accommodation + food + transportation + activity + shopping + other;

        // 统计贡献用户数
        var contributorCount = expenses.Select(e => e.UserId).Distinct().Count();

        return new CityCostSummaryDto
        {
            CityId = cityId,
            Total = total,
            Accommodation = accommodation,
            Food = food,
            Transportation = transportation,
            Activity = activity,
            Shopping = shopping,
            Other = other,
            ContributorCount = contributorCount,
            TotalExpenseCount = expenses.Count,
            Currency = "USD", // TODO: 支持多币种转换
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    ///     获取城市费用统计信息 - 用于缓存服务计算平均费用
    /// </summary>
    public async Task<ExpenseStatisticsDto> GetExpenseStatisticsAsync(string cityId)
    {
        var expenses = (await _expenseRepository.GetByCityIdAsync(cityId)).ToList();

        if (!expenses.Any())
        {
            return new ExpenseStatisticsDto
            {
                TotalAverageCost = 0,
                CategoryCosts = new Dictionary<string, decimal>(),
                ContributorCount = 0,
                TotalExpenseCount = 0,
                Currency = "USD",
                UpdatedAt = DateTime.UtcNow
            };
        }

        // 按分类计算平均费用
        var categoryCosts = new Dictionary<string, decimal>();

        foreach (var category in ExpenseCategory.All)
        {
            var categoryExpenses = expenses
                .Where(e => e.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (categoryExpenses.Any())
            {
                categoryCosts[category] = categoryExpenses.Select(e => e.Amount).Average();
            }
        }

        // 计算总平均费用（所有分类的平均值之和）
        var totalAverageCost = categoryCosts.Values.Sum();

        // 统计贡献用户数
        var contributorCount = expenses.Select(e => e.UserId).Distinct().Count();

        return new ExpenseStatisticsDto
        {
            TotalAverageCost = totalAverageCost,
            CategoryCosts = categoryCosts,
            ContributorCount = contributorCount,
            TotalExpenseCount = expenses.Count,
            Currency = "USD",
            UpdatedAt = DateTime.UtcNow
        };
    }

    #endregion

    #region 映射方法

    private async Task<AmapGeocodeResult?> TryEnrichLocationAsync(SubmitCityPhotoBatchRequest request)
    {
        try
        {
            var queryParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(request.Title)) queryParts.Add(request.Title);

            if (!string.IsNullOrWhiteSpace(request.LocationNote)) queryParts.Add(request.LocationNote!);

            if (queryParts.Count == 0) return null;

            var query = string.Join(" ", queryParts);
            _logger.LogDebug("调用 AMap 进行地理编码: {CityId} - {Query}", request.CityId, query);
            return await _amapGeocodingService.GeocodeAsync(query, request.CityId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "调用 AMap 地理编码失败: {CityId}", request.CityId);
            return null;
        }
    }

    private static UserCityPhotoDto MapPhotoToDto(UserCityPhoto photo)
    {
        return new UserCityPhotoDto
        {
            Id = photo.Id,
            UserId = photo.UserId,
            CityId = photo.CityId,
            ImageUrl = photo.ImageUrl,
            Caption = photo.Caption,
            Description = photo.Description,
            Location = photo.Location,
            PlaceName = photo.PlaceName,
            Address = photo.Address,
            Latitude = photo.Latitude,
            Longitude = photo.Longitude,
            TakenAt = photo.TakenAt,
            ModerationStatus = photo.ModerationStatus,
            ModeratedBy = photo.ModeratedBy,
            ModerationNote = photo.ModerationNote,
            ModeratedAt = photo.ModeratedAt,
            CreatedAt = photo.CreatedAt
        };
    }

    private static UserCityExpenseDto MapExpenseToDto(UserCityExpense expense)
    {
        return new UserCityExpenseDto
        {
            Id = expense.Id,
            UserId = expense.UserId,
            CityId = expense.CityId,
            Category = expense.Category,
            Amount = expense.Amount,
            Currency = expense.Currency,
            Description = expense.Description,
            Date = expense.Date,
            CreatedAt = expense.CreatedAt
        };
    }

    private static UserCityReviewDto MapReviewToDto(UserCityReview review)
    {
        return new UserCityReviewDto
        {
            Id = review.Id,
            UserId = review.UserId,
            CityId = review.CityId,
            Rating = review.Rating,
            Title = review.Title,
            Content = review.Content,
            VisitDate = review.VisitDate,
            ReviewText = review.ReviewText,
            InternetQualityScore = review.InternetQualityScore,
            SafetyScore = review.SafetyScore,
            CostScore = review.CostScore,
            CommunityScore = review.CommunityScore,
            WeatherScore = review.WeatherScore,
            CreatedAt = review.CreatedAt,
            UpdatedAt = review.UpdatedAt
        };
    }

    #endregion

    #region Pros & Cons 相关

    public async Task<CityProsConsDto> AddProsConsAsync(Guid userId, AddCityProsConsRequest request)
    {
        var prosCons = new CityProsCons
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CityId = request.CityId,
            Text = request.Text,
            IsPro = request.IsPro,
            Upvotes = 0,
            Downvotes = 0
        };

        var created = await _prosConsRepository.AddAsync(prosCons);
        return MapProsConsToDto(created);
    }

    public async Task<List<CityProsConsDto>> GetCityProsConsAsync(string cityId, Guid? userId = null, bool? isPro = null)
    {
        var prosConsList = await _prosConsRepository.GetByCityIdAsync(cityId, isPro);
        var dtoList = new List<CityProsConsDto>();

        foreach (var prosCons in prosConsList)
        {
            var dto = MapProsConsToDto(prosCons);
            
            // 如果提供了userId，查询用户的投票状态
            if (userId.HasValue)
            {
                var userVote = await _prosConsRepository.GetUserVoteAsync(prosCons.Id, userId.Value);
                dto.CurrentUserVoted = userVote?.IsUpvote;
            }
            
            dtoList.Add(dto);
        }

        return dtoList;
    }

    public async Task<CityProsConsDto> UpdateProsConsAsync(Guid userId, Guid id, UpdateCityProsConsRequest request)
    {
        var existing = await _prosConsRepository.GetByIdAsync(id);
        if (existing == null) throw new Exception($"Pros & Cons with id {id} not found");

        if (existing.UserId != userId)
            throw new UnauthorizedAccessException("You can only update your own Pros & Cons");

        existing.Text = request.Text;
        existing.IsPro = request.IsPro;

        var updated = await _prosConsRepository.UpdateAsync(existing);
        return MapProsConsToDto(updated);
    }

    public async Task<bool> DeleteProsConsAsync(Guid userId, Guid id)
    {
        return await _prosConsRepository.DeleteAsync(id, userId);
    }

    public async Task VoteProsConsAsync(Guid userId, Guid prosConsId, bool isUpvote)
    {
        // 1. 验证 Pros & Cons 是否存在
        var prosCons = await _prosConsRepository.GetByIdAsync(prosConsId);
        if (prosCons == null)
        {
            throw new KeyNotFoundException($"Pros & Cons with id {prosConsId} not found");
        }

        // 2. 检查用户是否已投票
        var existingVote = await _prosConsRepository.GetUserVoteAsync(prosConsId, userId);
        
        if (existingVote != null)
        {
            // 如果用户已投相同类型的票，则取消投票（删除记录）
            if (existingVote.IsUpvote == isUpvote)
            {
                await _prosConsRepository.DeleteVoteAsync(existingVote.Id);
                return;
            }
            // 如果用户投了不同类型的票，则更新投票类型
            else
            {
                existingVote.IsUpvote = isUpvote;
                await _prosConsRepository.UpdateVoteAsync(existingVote);
                return;
            }
        }

        // 3. 创建新投票记录
        var vote = new CityProsConsVote
        {
            Id = Guid.NewGuid(),
            ProsConsId = prosConsId,
            VoterUserId = userId,
            IsUpvote = isUpvote,
            CreatedAt = DateTime.UtcNow
        };

        await _prosConsRepository.AddVoteAsync(vote);
    }

    private static CityProsConsDto MapProsConsToDto(CityProsCons prosCons)
    {
        return new CityProsConsDto
        {
            Id = prosCons.Id,
            UserId = prosCons.UserId,
            CityId = prosCons.CityId,
            Text = prosCons.Text,
            IsPro = prosCons.IsPro,
            Upvotes = prosCons.Upvotes,
            Downvotes = prosCons.Downvotes,
            CreatedAt = prosCons.CreatedAt,
            UpdatedAt = prosCons.UpdatedAt
        };
    }

    #endregion
}