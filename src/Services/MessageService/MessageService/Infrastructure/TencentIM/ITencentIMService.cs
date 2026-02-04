namespace MessageService.Infrastructure.TencentIM;

/// <summary>
/// 腾讯云IM服务接口
/// </summary>
public interface ITencentIMService
{
    /// <summary>
    /// 生成用户UserSig
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>UserSig响应</returns>
    UserSigResponse GenerateUserSig(string userId);

    /// <summary>
    /// 导入单个用户账号
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="nickname">昵称</param>
    /// <param name="avatarUrl">头像URL</param>
    /// <returns>是否成功</returns>
    Task<bool> ImportAccountAsync(string userId, string? nickname = null, string? avatarUrl = null);

    /// <summary>
    /// 批量导入用户账号
    /// </summary>
    /// <param name="users">用户列表</param>
    /// <returns>导入结果</returns>
    Task<BatchImportResult> BatchImportAccountsAsync(IEnumerable<UserImportDto> users);

    /// <summary>
    /// 批量导入用户ID（不包含昵称和头像）
    /// </summary>
    /// <param name="userIds">用户ID列表</param>
    /// <returns>导入结果</returns>
    Task<BatchImportResult> BatchImportAccountIdsAsync(IEnumerable<string> userIds);

    /// <summary>
    /// 检查用户是否存在于IM系统
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>是否存在</returns>
    Task<bool> CheckUserExistsAsync(string userId);

    /// <summary>
    /// 批量查询用户在线状态
    /// </summary>
    /// <param name="userIds">用户ID列表</param>
    /// <returns>用户状态字典</returns>
    Task<Dictionary<string, string>> QueryUserStatusAsync(IEnumerable<string> userIds);

    /// <summary>
    /// 格式化用户ID（添加前缀）
    /// </summary>
    /// <param name="userId">原始用户ID</param>
    /// <returns>格式化后的用户ID</returns>
    string FormatUserId(string userId);
}
