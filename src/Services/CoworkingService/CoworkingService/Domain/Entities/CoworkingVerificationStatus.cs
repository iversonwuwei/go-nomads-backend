namespace CoworkingService.Domain.Entities;

/// <summary>
///     定义 Coworking 空间的认证状态
/// </summary>
public static class CoworkingVerificationStatus
{
    public const string Verified = "verified";
    public const string Unverified = "unverified";

    public static bool IsValid(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return false;
        var normalized = status.Trim().ToLowerInvariant();
        return normalized is Verified or Unverified;
    }

    public static string Normalize(string? status)
    {
        return IsValid(status) ? status!.Trim().ToLowerInvariant() : Unverified;
    }
}
