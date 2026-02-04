using System.Text.Json.Serialization;

namespace MessageService.Infrastructure.TencentIM;

/// <summary>
/// 腾讯云IM用户导入请求
/// </summary>
public class ImportAccountRequest
{
    /// <summary>
    /// 用户ID（必填）
    /// </summary>
    [JsonPropertyName("UserID")]
    public string UserID { get; set; } = string.Empty;

    /// <summary>
    /// 用户昵称（选填）
    /// </summary>
    [JsonPropertyName("Nick")]
    public string? Nick { get; set; }

    /// <summary>
    /// 用户头像URL（选填）
    /// </summary>
    [JsonPropertyName("FaceUrl")]
    public string? FaceUrl { get; set; }
}

/// <summary>
/// 腾讯云IM批量导入请求
/// </summary>
public class MultiAccountImportRequest
{
    /// <summary>
    /// 用户ID列表，最多100个
    /// </summary>
    [JsonPropertyName("Accounts")]
    public List<string> Accounts { get; set; } = new();
}

/// <summary>
/// 查询用户状态请求
/// </summary>
public class QueryUserStatusRequest
{
    /// <summary>
    /// 用户ID列表
    /// </summary>
    [JsonPropertyName("To_Account")]
    public List<string> To_Account { get; set; } = new();
}

/// <summary>
/// 腾讯云IM API响应基类
/// </summary>
public class TencentIMResponse
{
    /// <summary>
    /// 错误码，0表示成功
    /// </summary>
    public int ErrorCode { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string ErrorInfo { get; set; } = string.Empty;

    /// <summary>
    /// 请求ID
    /// </summary>
    public string ActionStatus { get; set; } = string.Empty;
}

/// <summary>
/// 批量导入响应
/// </summary>
public class MultiAccountImportResponse : TencentIMResponse
{
    /// <summary>
    /// 导入失败的用户列表
    /// </summary>
    public List<string>? FailAccounts { get; set; }
}

/// <summary>
/// 查询用户状态响应
/// </summary>
public class QueryUserStatusResponse : TencentIMResponse
{
    /// <summary>
    /// 用户状态列表
    /// </summary>
    public List<UserStatusResult>? QueryResult { get; set; }

    /// <summary>
    /// 查询错误的用户列表
    /// </summary>
    public List<UserStatusError>? ErrorList { get; set; }
}

/// <summary>
/// 用户状态结果
/// </summary>
public class UserStatusResult
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public string To_Account { get; set; } = string.Empty;

    /// <summary>
    /// 在线状态：Online, Offline, PushOnline
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// 用户状态查询错误
/// </summary>
public class UserStatusError
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public string To_Account { get; set; } = string.Empty;

    /// <summary>
    /// 错误码
    /// </summary>
    public int ErrorCode { get; set; }
}

/// <summary>
/// 用户导入DTO（用于API请求）
/// </summary>
public class UserImportDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 用户昵称
    /// </summary>
    public string? Nickname { get; set; }

    /// <summary>
    /// 用户头像URL
    /// </summary>
    public string? AvatarUrl { get; set; }
}

/// <summary>
/// 批量导入结果
/// </summary>
public class BatchImportResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 总数量
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 成功数量
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// 失败数量
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// 失败的用户ID列表
    /// </summary>
    public List<string> FailedUserIds { get; set; } = new();

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// UserSig响应
/// </summary>
public class UserSigResponse
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// UserSig
    /// </summary>
    public string UserSig { get; set; } = string.Empty;

    /// <summary>
    /// 过期时间（Unix时间戳）
    /// </summary>
    public long ExpireAt { get; set; }
}
