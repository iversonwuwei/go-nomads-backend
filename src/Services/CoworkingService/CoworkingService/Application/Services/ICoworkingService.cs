using CoworkingService.Application.DTOs;

namespace CoworkingService.Application.Services;

/// <summary>
///     Coworking 应用服务接口
///     定义业务用例
/// </summary>
public interface ICoworkingService
{
    /// <summary>
    ///     创建共享办公空间
    /// </summary>
    Task<CoworkingSpaceResponse> CreateCoworkingSpaceAsync(CreateCoworkingSpaceRequest request);

    /// <summary>
    ///     获取共享办公空间详情
    /// </summary>
    Task<CoworkingSpaceResponse> GetCoworkingSpaceAsync(Guid id);

    /// <summary>
    ///     更新共享办公空间
    /// </summary>
    Task<CoworkingSpaceResponse> UpdateCoworkingSpaceAsync(Guid id, UpdateCoworkingSpaceRequest request);

    /// <summary>
    ///     删除共享办公空间
    /// </summary>
    Task DeleteCoworkingSpaceAsync(Guid id);

    /// <summary>
    ///     获取共享办公空间列表（分页）
    /// </summary>
    Task<PaginatedCoworkingSpacesResponse> GetCoworkingSpacesAsync(
        int page = 1,
        int pageSize = 20,
        Guid? cityId = null);

    /// <summary>
    ///     搜索共享办公空间
    /// </summary>
    Task<List<CoworkingSpaceResponse>> SearchCoworkingSpacesAsync(
        string searchTerm,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    ///     获取评分最高的共享办公空间
    /// </summary>
    Task<List<CoworkingSpaceResponse>> GetTopRatedCoworkingSpacesAsync(int limit = 10);

    /// <summary>
    ///     普通用户提交认证投票
    /// </summary>
    Task<CoworkingSpaceResponse> SubmitVerificationAsync(Guid id, Guid userId);

    /// <summary>
    ///     更新 Coworking 空间的认证状态
    /// </summary>
    Task<CoworkingSpaceResponse> UpdateVerificationStatusAsync(Guid id, UpdateCoworkingVerificationStatusRequest request);

    /// <summary>
    ///     创建预订
    /// </summary>
    Task<CoworkingBookingResponse> CreateBookingAsync(CreateBookingRequest request);

    /// <summary>
    ///     获取预订详情
    /// </summary>
    Task<CoworkingBookingResponse> GetBookingAsync(Guid id);

    /// <summary>
    ///     取消预订
    /// </summary>
    Task CancelBookingAsync(Guid id, Guid userId);

    /// <summary>
    ///     获取用户的预订列表
    /// </summary>
    Task<List<CoworkingBookingResponse>> GetUserBookingsAsync(Guid userId);

    /// <summary>
    ///     确认预订
    /// </summary>
    Task ConfirmBookingAsync(Guid id);

    /// <summary>
    ///     完成预订
    /// </summary>
    Task CompleteBookingAsync(Guid id);

    /// <summary>
    ///     创建评论
    /// </summary>
    Task<CoworkingCommentResponse> CreateCommentAsync(Guid coworkingId, Guid userId, CreateCoworkingCommentRequest request);

    /// <summary>
    ///     获取评论列表
    /// </summary>
    Task<List<CoworkingCommentResponse>> GetCommentsAsync(Guid coworkingId, int page = 1, int pageSize = 20);

    /// <summary>
    ///     获取评论数量
    /// </summary>
    Task<int> GetCommentCountAsync(Guid coworkingId);

    /// <summary>
    ///     删除评论
    /// </summary>
    Task DeleteCommentAsync(Guid id, Guid userId);
}