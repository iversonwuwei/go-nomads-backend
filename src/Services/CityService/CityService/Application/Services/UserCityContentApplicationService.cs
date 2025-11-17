using CityService.Application.Abstractions.Services;
using CityService.Application.DTOs;
using CityService.Domain.Entities;
using CityService.Domain.Repositories;
using CityService.Services;
using Microsoft.Extensions.Logging;

namespace CityService.Application.Services;

/// <summary>
/// 用户城市内容应用服务实现
/// </summary>
public class UserCityContentApplicationService : IUserCityContentService
{
    private readonly IUserCityPhotoRepository _photoRepository;
    private readonly IUserCityExpenseRepository _expenseRepository;
    private readonly IUserCityReviewRepository _reviewRepository;
    private readonly IUserCityProsConsRepository _prosConsRepository;
    private readonly IUserServiceClient _userServiceClient;
    private readonly IAmapGeocodingService _amapGeocodingService;
    private readonly ILogger<UserCityContentApplicationService> _logger;

    public UserCityContentApplicationService(
        IUserCityPhotoRepository photoRepository,
        IUserCityExpenseRepository expenseRepository,
        IUserCityReviewRepository reviewRepository,
        IUserCityProsConsRepository prosConsRepository,
        IUserServiceClient userServiceClient,
        IAmapGeocodingService amapGeocodingService,
        ILogger<UserCityContentApplicationService> logger)
    {
        _photoRepository = photoRepository;
        _expenseRepository = expenseRepository;
        _reviewRepository = reviewRepository;
        _prosConsRepository = prosConsRepository;
        _userServiceClient = userServiceClient;
        _amapGeocodingService = amapGeocodingService;
        _logger = logger;
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

    public async Task<IEnumerable<UserCityPhotoDto>> SubmitPhotoCollectionAsync(Guid userId, SubmitCityPhotoBatchRequest request)
    {
        if (request.ImageUrls is null || request.ImageUrls.Count == 0)
        {
            throw new ArgumentException("图片列表不能为空", nameof(request.ImageUrls));
        }

        if (request.ImageUrls.Count > 10)
        {
            throw new ArgumentException("一次最多支持上传 10 张照片", nameof(request.ImageUrls));
        }

        _logger.LogInformation(
            "用户 {UserId} 正在为城市 {CityId} 提交 {Count} 张照片(标题: {Title})",
            userId,
            request.CityId,
            request.ImageUrls.Count,
            string.IsNullOrWhiteSpace(request.Title) ? "(未填写)" : request.Title);

        var enrichedLocation = await TryEnrichLocationAsync(request);
        if (enrichedLocation is null)
        {
            _logger.LogInformation("未获取到 AMap 位置信息, 使用用户输入: {CityId}", request.CityId);
        }
        else
        {
            _logger.LogInformation(
                "AMap 位置增强成功: {CityId} lat={Latitude}, lng={Longitude}, place={Place}",
                request.CityId,
                enrichedLocation.Latitude,
                enrichedLocation.Longitude,
                enrichedLocation.PlaceName ?? "(未知)");
        }

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
        if (deleted)
        {
            _logger.LogInformation("用户 {UserId} 删除了照片 {PhotoId}", userId, photoId);
        }
        return deleted;
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
        var deleted = await _expenseRepository.DeleteAsync(expenseId, userId);
        if (deleted)
        {
            _logger.LogInformation("用户 {UserId} 删除了费用 {ExpenseId}", userId, expenseId);
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
            WeatherScore = request.WeatherScore
        };

        var created = await _reviewRepository.CreateAsync(review); // ✅ 改为 CreateAsync,每次都新增记录
        _logger.LogInformation("用户 {UserId} 为城市 {CityId} 添加了新评论 {ReviewId}", userId, request.CityId, created.Id);

        return MapReviewToDto(created);
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
                dto.UserAvatar = null; // UserService 当前不返回头像
            }
            else
            {
                // 如果获取失败,使用默认值
                dto.Username = $"User {review.UserId.ToString().Substring(0, 8)}";
                dto.UserAvatar = null;
            }

            // ✅ 查询该用户在该城市的所有照片
            var photos = await _photoRepository.GetByCityIdAndUserIdAsync(cityId, review.UserId);
            dto.PhotoUrls = photos.Select(p => p.ImageUrl).ToList();
            
            result.Add(dto);
        }
        
        return result;
    }

    public async Task<IEnumerable<UserCityReviewDto>> GetUserReviewsAsync(Guid userId, string cityId)
    {
        var reviews = await _reviewRepository.GetByCityIdAndUserIdAsync(cityId, userId);
        var dtos = new List<UserCityReviewDto>();

        foreach (var review in reviews)
        {
            var dto = MapReviewToDto(review);

            // ✅ 查询该用户在该城市的所有照片
            var photos = await _photoRepository.GetByCityIdAndUserIdAsync(cityId, userId);
            dto.PhotoUrls = photos.Select(p => p.ImageUrl).ToList();

            dtos.Add(dto);
        }

        return dtos;
    }

    public async Task<bool> DeleteReviewAsync(Guid userId, Guid reviewId)
    {
        var deleted = await _reviewRepository.DeleteAsync(reviewId, userId);
        if (deleted)
        {
            _logger.LogInformation("用户 {UserId} 删除了评论 {ReviewId}", userId, reviewId);
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
    /// 获取城市综合费用统计 - 基于用户提交的实际费用数据计算
    /// </summary>
    public async Task<CityCostSummaryDto> GetCityCostSummaryAsync(string cityId)
    {
        var expenses = (await _expenseRepository.GetByCityIdAsync(cityId)).ToList();

        if (!expenses.Any())
        {
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
        }

        // 按分类计算平均费用
        var accommodation = expenses.Where(e => e.Category.Equals(ExpenseCategory.Accommodation, StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Amount).DefaultIfEmpty(0).Average();

        var food = expenses.Where(e => e.Category.Equals(ExpenseCategory.Food, StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Amount).DefaultIfEmpty(0).Average();

        var transportation = expenses.Where(e => e.Category.Equals(ExpenseCategory.Transport, StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Amount).DefaultIfEmpty(0).Average();

        var activity = expenses.Where(e => e.Category.Equals(ExpenseCategory.Activity, StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Amount).DefaultIfEmpty(0).Average();

        var shopping = expenses.Where(e => e.Category.Equals(ExpenseCategory.Shopping, StringComparison.OrdinalIgnoreCase))
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

    #endregion

    #region 映射方法

    private async Task<AmapGeocodeResult?> TryEnrichLocationAsync(SubmitCityPhotoBatchRequest request)
    {
        try
        {
            var queryParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(request.Title))
            {
                queryParts.Add(request.Title);
            }

            if (!string.IsNullOrWhiteSpace(request.LocationNote))
            {
                queryParts.Add(request.LocationNote!);
            }

            if (queryParts.Count == 0)
            {
                return null;
            }

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

    public async Task<List<CityProsConsDto>> GetCityProsConsAsync(string cityId, bool? isPro = null)
    {
        var prosConsList = await _prosConsRepository.GetByCityIdAsync(cityId, isPro);
        return prosConsList.Select(MapProsConsToDto).ToList();
    }

    public async Task<CityProsConsDto> UpdateProsConsAsync(Guid userId, Guid id, UpdateCityProsConsRequest request)
    {
        var existing = await _prosConsRepository.GetByIdAsync(id);
        if (existing == null)
        {
            throw new Exception($"Pros & Cons with id {id} not found");
        }

        if (existing.UserId != userId)
        {
            throw new UnauthorizedAccessException("You can only update your own Pros & Cons");
        }

        existing.Text = request.Text;
        existing.IsPro = request.IsPro;

        var updated = await _prosConsRepository.UpdateAsync(existing);
        return MapProsConsToDto(updated);
    }

    public async Task<bool> DeleteProsConsAsync(Guid userId, Guid id)
    {
        return await _prosConsRepository.DeleteAsync(id, userId);
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
