using CityService.Application.DTOs;
using CityService.Domain.Entities;
using CityService.Domain.Repositories;
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
    private readonly ILogger<UserCityContentApplicationService> _logger;

    public UserCityContentApplicationService(
        IUserCityPhotoRepository photoRepository,
        IUserCityExpenseRepository expenseRepository,
        IUserCityReviewRepository reviewRepository,
        ILogger<UserCityContentApplicationService> logger)
    {
        _photoRepository = photoRepository;
        _expenseRepository = expenseRepository;
        _reviewRepository = reviewRepository;
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

    public async Task<UserCityReviewDto> UpsertReviewAsync(Guid userId, UpsertCityReviewRequest request)
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

        var upserted = await _reviewRepository.UpsertAsync(review);
        _logger.LogInformation("用户 {UserId} 更新了城市 {CityId} 的评论", userId, request.CityId);

        return MapReviewToDto(upserted);
    }

    public async Task<IEnumerable<UserCityReviewDto>> GetCityReviewsAsync(string cityId)
    {
        var reviews = await _reviewRepository.GetByCityIdAsync(cityId);
        var result = new List<UserCityReviewDto>();
        
        foreach (var review in reviews)
        {
            var dto = MapReviewToDto(review);
            
            // ✅ 查询该用户在该城市的所有照片
            var photos = await _photoRepository.GetByCityIdAndUserIdAsync(cityId, review.UserId);
            dto.PhotoUrls = photos.Select(p => p.ImageUrl).ToList();
            
            result.Add(dto);
        }
        
        return result;
    }

    public async Task<UserCityReviewDto?> GetUserReviewAsync(Guid userId, string cityId)
    {
        var review = await _reviewRepository.GetByCityIdAndUserIdAsync(cityId, userId);
        if (review == null) return null;
        
        var dto = MapReviewToDto(review);
        
        // ✅ 查询该用户在该城市的所有照片
        var photos = await _photoRepository.GetByCityIdAndUserIdAsync(cityId, userId);
        dto.PhotoUrls = photos.Select(p => p.ImageUrl).ToList();
        
        return dto;
    }

    public async Task<bool> DeleteReviewAsync(Guid userId, string cityId)
    {
        var deleted = await _reviewRepository.DeleteAsync(cityId, userId);
        if (deleted)
        {
            _logger.LogInformation("用户 {UserId} 删除了城市 {CityId} 的评论", userId, cityId);
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

    private static UserCityPhotoDto MapPhotoToDto(UserCityPhoto photo)
    {
        return new UserCityPhotoDto
        {
            Id = photo.Id,
            UserId = photo.UserId,
            CityId = photo.CityId,
            ImageUrl = photo.ImageUrl,
            Caption = photo.Caption,
            Location = photo.Location,
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
}
