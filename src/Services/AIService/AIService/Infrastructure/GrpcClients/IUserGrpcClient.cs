namespace AIService.Infrastructure.GrpcClients;

/// <summary>
/// 用户服务 gRPC 客户端接口
/// </summary>
public interface IUserGrpcClient
{
    /// <summary>
    /// 获取用户基本信息
    /// </summary>
    Task<UserInfo?> GetUserInfoAsync(Guid userId);

    /// <summary>
    /// 批量获取用户基本信息
    /// </summary>
    Task<Dictionary<Guid, UserInfo>> GetUsersInfoByIdsAsync(List<Guid> userIds);

    /// <summary>
    /// 验证用户是否存在
    /// </summary>
    Task<bool> IsUserExistsAsync(Guid userId);
}

/// <summary>
/// 用户信息 DTO
/// </summary>
public class UserInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string? Phone { get; set; }
}