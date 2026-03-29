using System.Security.Cryptography;
using System.Text;

namespace AIService.Application.Services;

/// <summary>
///     为 OpenClaw 请求生成稳定的用户级 session key，避免不同用户共享同一代理上下文。
/// </summary>
public static class OpenClawSessionKeyFactory
{
    public static string BuildUserScopedSessionKey(Guid userId, string? clientSessionId = null, string scope = "agent")
    {
        var normalizedScope = NormalizeSegment(scope, "agent");
        var normalizedClientSession = NormalizeSegment(clientSessionId, "default");
        var fingerprint = ComputeFingerprint($"{userId:N}:{normalizedScope}:{normalizedClientSession}");

        return $"gonomads-{normalizedScope}-{fingerprint}";
    }

    private static string NormalizeSegment(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        var builder = new StringBuilder(value.Length);
        foreach (var ch in value.Trim().ToLowerInvariant())
            if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_')
                builder.Append(ch);

        return builder.Length == 0 ? fallback : builder.ToString();
    }

    private static string ComputeFingerprint(string input)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant()[..24];
    }
}