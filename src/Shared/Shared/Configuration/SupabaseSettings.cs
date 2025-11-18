namespace Shared.Configuration;

/// <summary>
///     Supabase 配置设置
/// </summary>
public class SupabaseSettings
{
    /// <summary>
    ///     Supabase 项目 URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    ///     Supabase API Key (anon/public key)
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    ///     Supabase service_role Key (优先使用, 可为空)
    /// </summary>
    public string? ServiceRoleKey { get; set; }

    /// <summary>
    ///     数据库 Schema，默认为 "public"
    /// </summary>
    public string Schema { get; set; } = "public";

    /// <summary>
    ///     是否自动连接 Realtime（实时订阅功能）
    /// </summary>
    public bool AutoConnectRealtime { get; set; } = false;

    /// <summary>
    ///     是否自动刷新 Token
    /// </summary>
    public bool AutoRefreshToken { get; set; } = true;

    /// <summary>
    ///     验证配置是否完整
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Url) &&
               (!string.IsNullOrWhiteSpace(ServiceRoleKey) || !string.IsNullOrWhiteSpace(Key));
    }

    /// <summary>
    ///     获取配置验证错误信息
    /// </summary>
    public string GetValidationError()
    {
        if (string.IsNullOrWhiteSpace(Url))
            return "Supabase URL is not configured";

        if (string.IsNullOrWhiteSpace(ServiceRoleKey) && string.IsNullOrWhiteSpace(Key))
            return "Supabase API Key is not configured";

        return string.Empty;
    }

    /// <summary>
    ///     获取当前应使用的 key, 优先 service_role
    /// </summary>
    public string GetActiveKey()
    {
        return string.IsNullOrWhiteSpace(ServiceRoleKey) ? Key : ServiceRoleKey;
    }
}