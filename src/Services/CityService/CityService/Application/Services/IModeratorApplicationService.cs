using CityService.Application.DTOs;

namespace CityService.Application.Services;

/// <summary>
///     版主申请服务接口
/// </summary>
public interface IModeratorApplicationService
{
    /// <summary>
    ///     用户申请成为版主
    /// </summary>
    Task<ModeratorApplicationResponse> ApplyAsync(Guid userId, ApplyModeratorRequest request);

    /// <summary>
    ///     管理员处理申请（批准或拒绝）
    /// </summary>
    Task<ModeratorApplicationResponse> HandleApplicationAsync(Guid adminId, HandleModeratorApplicationRequest request);

    /// <summary>
    ///     获取待处理的申请列表（管理员使用）
    /// </summary>
    Task<List<ModeratorApplicationResponse>> GetPendingApplicationsAsync(int page = 1, int pageSize = 20);

    /// <summary>
    ///     获取用户的申请列表
    /// </summary>
    Task<List<ModeratorApplicationResponse>> GetUserApplicationsAsync(Guid userId);

    /// <summary>
    ///     获取申请详情
    /// </summary>
    Task<ModeratorApplicationResponse?> GetApplicationByIdAsync(Guid applicationId);

    /// <summary>
    ///     获取申请统计
    /// </summary>
    Task<(int Total, int Pending, int Approved, int Rejected)> GetStatisticsAsync();

    /// <summary>
    ///     撤销版主资格
    /// </summary>
    Task RevokeModeratorAsync(Guid adminId, Guid applicationId);
}
