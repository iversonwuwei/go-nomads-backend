using CityService.Application.DTOs;

namespace CityService.Application.Services;

/// <summary>
/// 用户城市内容应用服务接口
/// </summary>
public interface IUserCityContentService
{
    #region 照片相关

    Task<UserCityPhotoDto> AddPhotoAsync(Guid userId, AddCityPhotoRequest request);
    Task<IEnumerable<UserCityPhotoDto>> GetCityPhotosAsync(string cityId, Guid? userId = null);
    Task<IEnumerable<UserCityPhotoDto>> GetUserPhotosAsync(Guid userId);
    Task<bool> DeletePhotoAsync(Guid userId, Guid photoId);

    #endregion

    #region 费用相关

    Task<UserCityExpenseDto> AddExpenseAsync(Guid userId, AddCityExpenseRequest request);
    Task<IEnumerable<UserCityExpenseDto>> GetCityExpensesAsync(string cityId, Guid? userId = null);
    Task<IEnumerable<UserCityExpenseDto>> GetUserExpensesAsync(Guid userId);
    Task<bool> DeleteExpenseAsync(Guid userId, Guid expenseId);

    #endregion

    #region 评论相关

    Task<UserCityReviewDto> UpsertReviewAsync(Guid userId, UpsertCityReviewRequest request);
    Task<IEnumerable<UserCityReviewDto>> GetCityReviewsAsync(string cityId);
    Task<UserCityReviewDto?> GetUserReviewAsync(Guid userId, string cityId);
    Task<bool> DeleteReviewAsync(Guid userId, string cityId);

    #endregion

    #region 统计相关

    Task<CityUserContentStatsDto> GetCityStatsAsync(string cityId);
    Task<CityCostSummaryDto> GetCityCostSummaryAsync(string cityId);

    #endregion
}
