using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MessageService.Infrastructure.TencentIM;

/// <summary>
/// 腾讯云IM UserSig生成器
/// 基于HMAC-SHA256算法
/// </summary>
public static class TencentIMUserSigGenerator
{
    /// <summary>
    /// 生成UserSig
    /// </summary>
    /// <param name="sdkAppId">应用ID</param>
    /// <param name="secretKey">密钥</param>
    /// <param name="userId">用户ID</param>
    /// <param name="expireSeconds">有效期（秒）</param>
    /// <returns>UserSig字符串</returns>
    public static string GenerateUserSig(long sdkAppId, string secretKey, string userId, int expireSeconds = 604800)
    {
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // 构建JSON内容
        var sigDoc = new Dictionary<string, object>
        {
            { "TLS.ver", "2.0" },
            { "TLS.identifier", userId },
            { "TLS.sdkappid", sdkAppId },
            { "TLS.expire", expireSeconds },
            { "TLS.time", currentTime }
        };

        // 生成签名内容（腾讯云要求的格式）
        var sigContent = $"TLS.identifier:{userId}\n" +
                        $"TLS.sdkappid:{sdkAppId}\n" +
                        $"TLS.time:{currentTime}\n" +
                        $"TLS.expire:{expireSeconds}\n";

        // HMAC-SHA256签名
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(sigContent));
        var sig = Convert.ToBase64String(hash);

        sigDoc["TLS.sig"] = sig;

        // 序列化为JSON
        var jsonContent = JsonSerializer.Serialize(sigDoc);
        var jsonBytes = Encoding.UTF8.GetBytes(jsonContent);

        // 使用 zlib 压缩（带 zlib header）
        var compressed = ZlibCompress(jsonBytes);
        
        // Base64编码并进行URL安全处理
        var base64 = Convert.ToBase64String(compressed)
            .Replace('+', '*')
            .Replace('/', '-')
            .Replace('=', '_');

        return base64;
    }

    /// <summary>
    /// zlib 压缩（带 zlib header，腾讯云要求的格式）
    /// </summary>
    private static byte[] ZlibCompress(byte[] data)
    {
        using var output = new MemoryStream();
        
        // 写入 zlib header (CM=8, CINFO=7 for 32K window, FCHECK使校验和能被31整除)
        // 0x78 = CMF (Compression Method and flags): CM=8 (deflate), CINFO=7
        // 0x9C = FLG (Flags): FCHECK=28, FDICT=0, FLEVEL=2 (default compression)
        output.WriteByte(0x78);
        output.WriteByte(0x9C);
        
        // 使用 Deflate 压缩
        using (var deflate = new DeflateStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            deflate.Write(data, 0, data.Length);
        }
        
        // 计算 Adler-32 校验和
        var adler32 = ComputeAdler32(data);
        
        // 写入校验和（大端序）
        output.WriteByte((byte)(adler32 >> 24));
        output.WriteByte((byte)(adler32 >> 16));
        output.WriteByte((byte)(adler32 >> 8));
        output.WriteByte((byte)adler32);
        
        return output.ToArray();
    }

    /// <summary>
    /// 计算 Adler-32 校验和
    /// </summary>
    private static uint ComputeAdler32(byte[] data)
    {
        const uint MOD_ADLER = 65521;
        uint a = 1, b = 0;
        
        foreach (var byteValue in data)
        {
            a = (a + byteValue) % MOD_ADLER;
            b = (b + a) % MOD_ADLER;
        }
        
        return (b << 16) | a;
    }
}
