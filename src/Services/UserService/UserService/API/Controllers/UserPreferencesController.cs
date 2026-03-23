using GoNomads.Shared.Middleware;
using GoNomads.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs;
using UserService.Domain.Repositories;

namespace UserService.API.Controllers;

/// <summary>
///     用户偏好设置 API - RESTful endpoints for user preferences management
/// </summary>
[ApiController]
[Route("api/v1/users")]
public class UserPreferencesController : ControllerBase
{
    private readonly ILegalDocumentRepository _legalDocumentRepository;
    private readonly ILogger<UserPreferencesController> _logger;
    private readonly IUserPreferencesRepository _userPreferencesRepository;

    public UserPreferencesController(
        IUserPreferencesRepository userPreferencesRepository,
        ILegalDocumentRepository legalDocumentRepository,
        ILogger<UserPreferencesController> logger)
    {
        _userPreferencesRepository = userPreferencesRepository;
        _legalDocumentRepository = legalDocumentRepository;
        _logger = logger;
    }

    /// <summary>
    ///     获取当前用户的偏好设置
    /// </summary>
    [HttpGet("me/preferences")]
    public async Task<ActionResult<ApiResponse<UserPreferencesDto>>> GetCurrentUserPreferences(
        CancellationToken cancellationToken = default)
    {
        // 从 UserContext 获取当前用户 ID
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
        {
            return Unauthorized(new ApiResponse<UserPreferencesDto>
            {
                Success = false,
                Message = "未认证用户"
            });
        }

        _logger.LogInformation("🔍 获取当前用户偏好设置: {UserId}", userContext.UserId);

        try
        {
            var preferences = await _userPreferencesRepository.GetOrCreateAsync(userContext.UserId, cancellationToken);

            return Ok(new ApiResponse<UserPreferencesDto>
            {
                Success = true,
                Message = "User preferences retrieved successfully",
                Data = MapToDto(preferences)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取用户偏好设置失败: {UserId}", userContext.UserId);
            return StatusCode(500, new ApiResponse<UserPreferencesDto>
            {
                Success = false,
                Message = "获取用户偏好设置失败"
            });
        }
    }

    /// <summary>
    ///     更新当前用户的偏好设置
    /// </summary>
    [HttpPut("me/preferences")]
    public async Task<ActionResult<ApiResponse<UserPreferencesDto>>> UpdateCurrentUserPreferences(
        [FromBody] UpdateUserPreferencesRequest request,
        CancellationToken cancellationToken = default)
    {
        // 从 UserContext 获取当前用户 ID
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
        {
            return Unauthorized(new ApiResponse<UserPreferencesDto>
            {
                Success = false,
                Message = "未认证用户"
            });
        }

        _logger.LogInformation("📝 更新当前用户偏好设置: {UserId}", userContext.UserId);

        try
        {
            // 获取或创建偏好设置
            var preferences = await _userPreferencesRepository.GetOrCreateAsync(userContext.UserId, cancellationToken);

            // 更新字段
            preferences.Update(
                request.NotificationsEnabled,
                request.TravelHistoryVisible,
                request.AutoTravelDetectionEnabled,
                request.ProfilePublic,
                request.Currency,
                request.TemperatureUnit,
                request.Language
            );

            // 保存更新
            var updatedPreferences = await _userPreferencesRepository.UpdateAsync(preferences, cancellationToken);

            return Ok(new ApiResponse<UserPreferencesDto>
            {
                Success = true,
                Message = "User preferences updated successfully",
                Data = MapToDto(updatedPreferences)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新用户偏好设置失败: {UserId}", userContext.UserId);
            return StatusCode(500, new ApiResponse<UserPreferencesDto>
            {
                Success = false,
                Message = "更新用户偏好设置失败"
            });
        }
    }

    /// <summary>
    ///     部分更新当前用户的偏好设置（PATCH）
    /// </summary>
    [HttpPatch("me/preferences")]
    public async Task<ActionResult<ApiResponse<UserPreferencesDto>>> PatchCurrentUserPreferences(
        [FromBody] UpdateUserPreferencesRequest request,
        CancellationToken cancellationToken = default)
    {
        // 复用 PUT 的逻辑，因为我们的 Update 方法已经支持部分更新
        return await UpdateCurrentUserPreferences(request, cancellationToken);
    }

    /// <summary>
    ///     根据用户ID获取偏好设置
    /// </summary>
    [HttpGet("{userId}/preferences")]
    public async Task<ActionResult<ApiResponse<UserPreferencesDto>>> GetUserPreferences(
        string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 获取用户偏好设置: {UserId}", userId);

        try
        {
            var preferences = await _userPreferencesRepository.GetOrCreateAsync(userId, cancellationToken);

            return Ok(new ApiResponse<UserPreferencesDto>
            {
                Success = true,
                Message = "User preferences retrieved successfully",
                Data = MapToDto(preferences)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取用户偏好设置失败: {UserId}", userId);
            return StatusCode(500, new ApiResponse<UserPreferencesDto>
            {
                Success = false,
                Message = "获取用户偏好设置失败"
            });
        }
    }

    /// <summary>
    ///     接受隐私政策
    /// </summary>
    [HttpPost("me/accept-privacy-policy")]
    public async Task<ActionResult<ApiResponse<UserPreferencesDto>>> AcceptPrivacyPolicy(
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
        {
            return Unauthorized(new ApiResponse<UserPreferencesDto>
            {
                Success = false,
                Message = "未认证用户"
            });
        }

        try
        {
            var preferences = await _userPreferencesRepository.GetOrCreateAsync(userContext.UserId, cancellationToken);
            var currentVersion = await GetCurrentDocumentVersionAsync("privacy-policy", preferences.Language, cancellationToken);
            preferences.AcceptPrivacyPolicy(currentVersion);
            var updatedPreferences = await _userPreferencesRepository.UpdateAsync(preferences, cancellationToken);

            return Ok(new ApiResponse<UserPreferencesDto>
            {
                Success = true,
                Message = "Privacy policy accepted successfully",
                Data = MapToDto(updatedPreferences)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 接受隐私政策失败: {UserId}", userContext.UserId);
            return StatusCode(500, new ApiResponse<UserPreferencesDto>
            {
                Success = false,
                Message = "接受隐私政策失败"
            });
        }
    }

    /// <summary>
    ///     接受用户协议
    /// </summary>
    [HttpPost("me/accept-terms-of-service")]
    public async Task<ActionResult<ApiResponse<UserPreferencesDto>>> AcceptTermsOfService(
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
        {
            return Unauthorized(new ApiResponse<UserPreferencesDto>
            {
                Success = false,
                Message = "未认证用户"
            });
        }

        try
        {
            var preferences = await _userPreferencesRepository.GetOrCreateAsync(userContext.UserId, cancellationToken);
            var currentVersion = await GetCurrentDocumentVersionAsync("terms-of-service", preferences.Language, cancellationToken);
            preferences.AcceptTermsOfService(currentVersion);
            var updatedPreferences = await _userPreferencesRepository.UpdateAsync(preferences, cancellationToken);

            return Ok(new ApiResponse<UserPreferencesDto>
            {
                Success = true,
                Message = "Terms of service accepted successfully",
                Data = MapToDto(updatedPreferences)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 接受用户协议失败: {UserId}", userContext.UserId);
            return StatusCode(500, new ApiResponse<UserPreferencesDto>
            {
                Success = false,
                Message = "接受用户协议失败"
            });
        }
    }

    #region Private Methods

    private static UserPreferencesDto MapToDto(Domain.Entities.UserPreferences preferences)
    {
        return new UserPreferencesDto
        {
            Id = preferences.Id,
            UserId = preferences.UserId,
            NotificationsEnabled = preferences.NotificationsEnabled,
            TravelHistoryVisible = preferences.TravelHistoryVisible,
            AutoTravelDetectionEnabled = preferences.AutoTravelDetectionEnabled,
            ProfilePublic = preferences.ProfilePublic,
            Currency = preferences.Currency,
            TemperatureUnit = preferences.TemperatureUnit,
            Language = preferences.Language,
            PrivacyPolicyAccepted = preferences.PrivacyPolicyAccepted,
            PrivacyPolicyAcceptedAt = preferences.PrivacyPolicyAcceptedAt,
            PrivacyPolicyAcceptedVersion = preferences.PrivacyPolicyAcceptedVersion,
            TermsOfServiceAccepted = preferences.TermsOfServiceAccepted,
            TermsOfServiceAcceptedAt = preferences.TermsOfServiceAcceptedAt,
            TermsOfServiceAcceptedVersion = preferences.TermsOfServiceAcceptedVersion,
            CreatedAt = preferences.CreatedAt,
            UpdatedAt = preferences.UpdatedAt
        };
    }

    private async Task<string> GetCurrentDocumentVersionAsync(
        string documentType,
        string language,
        CancellationToken cancellationToken)
    {
        var document = await _legalDocumentRepository.GetCurrentAsync(documentType, language, cancellationToken);
        if (document == null && language != "zh")
            document = await _legalDocumentRepository.GetCurrentAsync(documentType, "zh", cancellationToken);

        return document?.Version ?? "1.0.0";
    }

    #endregion
}
