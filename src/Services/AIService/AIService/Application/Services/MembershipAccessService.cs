using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using GoNomads.Shared.Services;

namespace AIService.Application.Services;

public class MembershipAccessService : IMembershipAccessService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<MembershipAccessService> _logger;

    public MembershipAccessService(
        IHttpClientFactory httpClientFactory,
        ICurrentUserService currentUserService,
        ILogger<MembershipAccessService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<MembershipAccessResult> EnsurePaidMembershipAsync(CancellationToken cancellationToken = default)
    {
        if (_currentUserService.IsAdmin())
        {
            return MembershipAccessResult.AllowedResult();
        }

        var userContext = _currentUserService.GetUserContext();
        if (userContext?.IsAuthenticated != true || string.IsNullOrWhiteSpace(userContext.UserId))
        {
            return new MembershipAccessResult
            {
                Allowed = false,
                StatusCode = StatusCodes.Status401Unauthorized,
                Message = "请先登录后使用 AI 旅行规划师"
            };
        }

        try
        {
            var client = _httpClientFactory.CreateClient("user-service");
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/membership");
            request.Headers.TryAddWithoutValidation("X-User-Id", userContext.UserId);

            if (!string.IsNullOrWhiteSpace(userContext.Email))
                request.Headers.TryAddWithoutValidation("X-User-Email", userContext.Email);

            if (!string.IsNullOrWhiteSpace(userContext.Role))
                request.Headers.TryAddWithoutValidation("X-User-Role", userContext.Role);

            if (!string.IsNullOrWhiteSpace(userContext.AuthorizationHeader))
                request.Headers.TryAddWithoutValidation("Authorization", userContext.AuthorizationHeader);

            using var response = await client.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return new MembershipAccessResult
                {
                    Allowed = false,
                    StatusCode = StatusCodes.Status401Unauthorized,
                    Message = "请先登录后使用 AI 旅行规划师"
                };
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Membership verification failed with status code {StatusCode}", response.StatusCode);
                return new MembershipAccessResult
                {
                    Allowed = false,
                    StatusCode = StatusCodes.Status503ServiceUnavailable,
                    Message = "会员校验暂时不可用，请稍后重试"
                };
            }

            var membership = await response.Content.ReadFromJsonAsync<MembershipResponseDto>(cancellationToken: cancellationToken);
            if (membership?.IsActive == true && membership.Level > 0)
            {
                return MembershipAccessResult.AllowedResult();
            }

            return new MembershipAccessResult
            {
                Allowed = false,
                StatusCode = StatusCodes.Status403Forbidden,
                Message = "AI 旅行规划师仅对有效会员开放，请先开通会员"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Membership verification request failed");
            return new MembershipAccessResult
            {
                Allowed = false,
                StatusCode = StatusCodes.Status503ServiceUnavailable,
                Message = "会员校验暂时不可用，请稍后重试"
            };
        }
    }

    private sealed class MembershipResponseDto
    {
        public int Level { get; set; }
        public bool IsActive { get; set; }
    }
}