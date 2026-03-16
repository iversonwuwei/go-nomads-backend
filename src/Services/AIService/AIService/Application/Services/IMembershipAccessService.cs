namespace AIService.Application.Services;

public interface IMembershipAccessService
{
    Task<MembershipAccessResult> EnsurePaidMembershipAsync(CancellationToken cancellationToken = default);
}

public sealed class MembershipAccessResult
{
    public static MembershipAccessResult AllowedResult() => new() { Allowed = true, StatusCode = StatusCodes.Status200OK };

    public required bool Allowed { get; init; }
    public required int StatusCode { get; init; }
    public string Message { get; init; } = string.Empty;
}