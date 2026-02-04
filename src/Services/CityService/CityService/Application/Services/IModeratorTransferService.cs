using CityService.Application.DTOs;

namespace CityService.Application.Services;

/// <summary>
///     版主转让服务接口
/// </summary>
public interface IModeratorTransferService
{
    /// <summary>
    ///     发起版主转让请求
    /// </summary>
    Task<ModeratorTransferResponse> InitiateTransferAsync(Guid fromUserId, InitiateModeratorTransferRequest request);

    /// <summary>
    ///     响应转让请求（接受或拒绝）
    /// </summary>
    Task<ModeratorTransferResponse> RespondToTransferAsync(Guid userId, RespondToTransferRequest request);

    /// <summary>
    ///     取消转让请求
    /// </summary>
    Task CancelTransferAsync(Guid userId, Guid transferId);

    /// <summary>
    ///     获取用户发起的转让请求列表
    /// </summary>
    Task<List<ModeratorTransferResponse>> GetInitiatedTransfersAsync(Guid userId);

    /// <summary>
    ///     获取用户收到的转让请求列表
    /// </summary>
    Task<List<ModeratorTransferResponse>> GetReceivedTransfersAsync(Guid userId);

    /// <summary>
    ///     获取用户收到的待处理转让请求
    /// </summary>
    Task<List<ModeratorTransferResponse>> GetPendingTransfersAsync(Guid userId);

    /// <summary>
    ///     获取转让请求详情
    /// </summary>
    Task<ModeratorTransferResponse?> GetTransferByIdAsync(Guid transferId);
}
