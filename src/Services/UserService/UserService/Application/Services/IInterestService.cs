using UserService.Application.DTOs;

namespace UserService.Application.Services;

/// <summary>
/// 兴趣爱好服务接口
/// </summary>
public interface IInterestService
{
    /// <summary>
    /// 获取所有兴趣
    /// </summary>
    Task<List<InterestDto>> GetAllInterestsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取按类别分组的兴趣
    /// </summary>
    Task<List<InterestsByCategoryDto>> GetInterestsByCategoryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据类别获取兴趣
    /// </summary>
    Task<List<InterestDto>> GetInterestsBySpecificCategoryAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据ID获取兴趣
    /// </summary>
    Task<InterestDto?> GetInterestByIdAsync(string interestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的所有兴趣
    /// </summary>
    Task<List<UserInterestDto>> GetUserInterestsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加用户兴趣
    /// </summary>
    Task<UserInterestDto> AddUserInterestAsync(
        string userId,
        string interestId,
        string? intensityLevel = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量添加用户兴趣
    /// </summary>
    Task<List<UserInterestDto>> AddUserInterestsBatchAsync(
        string userId,
        List<AddUserInterestRequest> interests,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除用户兴趣
    /// </summary>
    Task<bool> RemoveUserInterestAsync(string userId, string interestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按兴趣名称删除用户兴趣
    /// </summary>
    Task<bool> RemoveUserInterestByNameAsync(string userId, string interestName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户兴趣强度
    /// </summary>
    Task<UserInterestDto> UpdateUserInterestAsync(
        string userId,
        string interestId,
        string? intensityLevel = null,
        CancellationToken cancellationToken = default);
}
