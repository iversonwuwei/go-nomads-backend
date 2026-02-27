using System.Net;
using System.Net.Sockets;
using Npgsql;

namespace CityService.Infrastructure;

/// <summary>
/// Npgsql IPv4 连接帮助类
/// 
/// 解决 Supabase 数据库主机名 DNS 解析到 IPv6 地址，但服务器没有 IPv6 连接能力的问题。
/// 预先将主机名解析为 IPv4 地址并缓存，后续创建连接时直接使用 IPv4 地址。
/// </summary>
public static class NpgsqlIPv4Helper
{
    private static string? _cachedResolvedConnectionString;
    private static string? _cachedOriginalConnectionString;
    private static DateTime _cacheExpiry = DateTime.MinValue;
    private static readonly object _lock = new();

    /// <summary>
    /// DNS 缓存有效期（分钟）。超过后会重新解析 DNS。
    /// </summary>
    private const int CacheTtlMinutes = 30;

    /// <summary>
    /// 将连接字符串中的主机名预解析为 IPv4 地址。
    /// 结果会缓存 30 分钟，避免每次创建连接都做 DNS 查询。
    /// </summary>
    /// <param name="connectionString">原始连接字符串（含主机名）</param>
    /// <returns>主机名替换为 IPv4 地址的连接字符串</returns>
    public static string ResolveToIPv4(string connectionString)
    {
        lock (_lock)
        {
            // 命中缓存
            if (_cachedResolvedConnectionString != null
                && _cachedOriginalConnectionString == connectionString
                && DateTime.UtcNow < _cacheExpiry)
            {
                return _cachedResolvedConnectionString;
            }
        }

        var resolved = ResolveCore(connectionString);

        lock (_lock)
        {
            _cachedOriginalConnectionString = connectionString;
            _cachedResolvedConnectionString = resolved;
            _cacheExpiry = DateTime.UtcNow.AddMinutes(CacheTtlMinutes);
        }

        return resolved;
    }

    private static string ResolveCore(string connectionString)
    {
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);

            // 如果 Host 已是 IP 地址，无需解析
            if (string.IsNullOrEmpty(builder.Host) || IPAddress.TryParse(builder.Host, out _))
            {
                return connectionString;
            }

            var originalHost = builder.Host;
            var addresses = Dns.GetHostAddresses(originalHost);

            // 优先选择 IPv4 地址
            var ipv4Address = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);

            if (ipv4Address != null)
            {
                builder.Host = ipv4Address.ToString();
                return builder.ToString();
            }

            // 没有 IPv4 地址，回退使用原始连接字符串
            return connectionString;
        }
        catch
        {
            // DNS 解析失败，回退使用原始连接字符串
            return connectionString;
        }
    }
}
