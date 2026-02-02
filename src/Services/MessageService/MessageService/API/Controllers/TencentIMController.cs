using MessageService.Infrastructure.TencentIM;
using MessageService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GoNomads.Shared.Services;

namespace MessageService.API.Controllers;

/// <summary>
/// 腾讯云IM API控制器
/// </summary>
[ApiController]
[Route("api/v1/im")]
public class TencentIMController : ControllerBase
{
    private readonly ITencentIMService _imService;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserServiceClient _userServiceClient;
    private readonly ILogger<TencentIMController> _logger;

    public TencentIMController(
        ITencentIMService imService,
        ICurrentUserService currentUser,
        IUserServiceClient userServiceClient,
        ILogger<TencentIMController> logger)
    {
        _imService = imService;
        _currentUser = currentUser;
        _userServiceClient = userServiceClient;
        _logger = logger;
    }

    /// <summary>
    /// 获取当前用户的UserSig
    /// </summary>
    /// <remarks>
    /// 用于客户端登录腾讯云IM SDK
    /// </remarks>
    [HttpGet("usersig")]
    public IActionResult GetUserSig()
    {
        try
        {
            var userId = _currentUser.GetUserIdString();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "未登录" });
            }

            var result = _imService.GenerateUserSig(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成UserSig失败");
            return StatusCode(500, new { error = "生成UserSig失败" });
        }
    }

    /// <summary>
    /// 获取指定用户的UserSig（管理员接口）
    /// </summary>
    /// <param name="userId">用户ID</param>
    [HttpGet("usersig/{userId}")]
    [AllowAnonymous] // 开发测试用，生产环境应限制
    public IActionResult GetUserSigForUser(string userId)
    {
        try
        {
            var result = _imService.GenerateUserSig(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成UserSig失败: {UserId}", userId);
            return StatusCode(500, new { error = "生成UserSig失败" });
        }
    }

    /// <summary>
    /// 导入单个用户到腾讯云IM
    /// </summary>
    /// <param name="request">用户信息</param>
    [HttpPost("accounts/import")]
    [AllowAnonymous] // 开发测试用，生产环境应限制
    public async Task<IActionResult> ImportAccount([FromBody] UserImportDto request)
    {
        try
        {
            _logger.LogInformation("导入用户: {UserId}", request.UserId);
            var success = await _imService.ImportAccountAsync(
                request.UserId,
                request.Nickname,
                request.AvatarUrl
            );

            if (success)
            {
                return Ok(new { success = true, userId = _imService.FormatUserId(request.UserId) });
            }

            return BadRequest(new { error = "导入用户失败" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入用户失败: {UserId}", request.UserId);
            return StatusCode(500, new { error = "导入用户失败" });
        }
    }

    /// <summary>
    /// 批量导入用户到腾讯云IM
    /// </summary>
    /// <param name="request">用户列表</param>
    [HttpPost("accounts/batch-import")]
    [AllowAnonymous] // 开发测试用，生产环境应限制
    public async Task<IActionResult> BatchImportAccounts([FromBody] BatchImportRequest request)
    {
        try
        {
            _logger.LogInformation("批量导入 {Count} 个用户", request.Users.Count);

            BatchImportResult result;

            if (request.Users.Any(u => !string.IsNullOrEmpty(u.Nickname) || !string.IsNullOrEmpty(u.AvatarUrl)))
            {
                // 如果有昵称或头像，使用逐个导入
                result = await _imService.BatchImportAccountsAsync(request.Users);
            }
            else
            {
                // 否则使用批量导入（更快）
                result = await _imService.BatchImportAccountIdsAsync(request.Users.Select(u => u.UserId));
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量导入用户失败");
            return StatusCode(500, new { error = "批量导入用户失败" });
        }
    }

    /// <summary>
    /// 批量导入用户ID（简化版，只需要ID列表）
    /// </summary>
    /// <param name="userIds">用户ID列表</param>
    [HttpPost("accounts/batch-import-ids")]
    [AllowAnonymous] // 开发测试用，生产环境应限制
    public async Task<IActionResult> BatchImportAccountIds([FromBody] List<string> userIds)
    {
        try
        {
            _logger.LogInformation("批量导入 {Count} 个用户ID", userIds.Count);
            var result = await _imService.BatchImportAccountIdsAsync(userIds);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量导入用户ID失败");
            return StatusCode(500, new { error = "批量导入用户ID失败" });
        }
    }

    /// <summary>
    /// 检查用户是否存在于腾讯云IM
    /// </summary>
    /// <param name="userId">用户ID</param>
    [HttpGet("accounts/{userId}/exists")]
    [AllowAnonymous]
    public async Task<IActionResult> CheckUserExists(string userId)
    {
        try
        {
            var exists = await _imService.CheckUserExistsAsync(userId);
            return Ok(new { userId = _imService.FormatUserId(userId), exists });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查用户是否存在失败: {UserId}", userId);
            return StatusCode(500, new { error = "检查用户失败" });
        }
    }

    /// <summary>
    /// 查询用户在线状态
    /// </summary>
    /// <param name="userIds">用户ID列表</param>
    [HttpPost("accounts/status")]
    [AllowAnonymous]
    public async Task<IActionResult> QueryUserStatus([FromBody] List<string> userIds)
    {
        try
        {
            var statusDict = await _imService.QueryUserStatusAsync(userIds);
            return Ok(statusDict);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询用户状态失败");
            return StatusCode(500, new { error = "查询用户状态失败" });
        }
    }

    /// <summary>
    /// 确保用户存在（如果不存在则导入）
    /// </summary>
    /// <remarks>
    /// 从当前登录用户的 UserContext 获取用户ID，
    /// 如果客户端未提供 nickname/avatar，则从 UserService 获取用户详细信息
    /// </remarks>
    [HttpPost("accounts/ensure")]
    public async Task<IActionResult> EnsureUserExists([FromBody] UserImportDto? request = null)
    {
        try
        {
            // 1. 从 UserContext 获取当前登录用户的ID
            var userId = _currentUser.GetUserIdString();
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("确保用户存在失败: 用户未登录");
                return Unauthorized(new { error = "用户未登录" });
            }

            _logger.LogInformation("确保用户存在 - UserId: {UserId}", userId);

            // 2. 获取 nickname 和 avatarUrl
            var nickname = request?.Nickname;
            var avatarUrl = request?.AvatarUrl;

            // 3. 如果没有 nickname 或 avatarUrl，从 UserService 获取用户详细信息
            if (string.IsNullOrEmpty(nickname) || string.IsNullOrEmpty(avatarUrl))
            {
                _logger.LogInformation("客户端未提供完整的用户信息，从 UserService 获取 - UserId: {UserId}", userId);
                
                try
                {
                    var userInfo = await _userServiceClient.GetUserInfoAsync(userId);
                    if (userInfo != null)
                    {
                        // 优先使用客户端提供的值，否则使用 UserService 的值
                        nickname = string.IsNullOrEmpty(nickname) ? userInfo.Username : nickname;
                        avatarUrl = string.IsNullOrEmpty(avatarUrl) ? userInfo.AvatarUrl : avatarUrl;
                        
                        _logger.LogInformation("从 UserService 获取用户信息成功 - Name: {Name}, AvatarUrl: {AvatarUrl}",
                            nickname, avatarUrl);
                    }
                    else
                    {
                        _logger.LogWarning("从 UserService 获取用户信息失败 - UserId: {UserId}，使用默认值", userId);
                        // 使用 email 前缀作为默认昵称
                        var email = _currentUser.GetUserEmail();
                        nickname = string.IsNullOrEmpty(nickname) 
                            ? (!string.IsNullOrEmpty(email) ? email.Split('@')[0] : $"User_{userId[..8]}") 
                            : nickname;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "从 UserService 获取用户信息时出错 - UserId: {UserId}，使用默认值", userId);
                    // 出错时使用 email 前缀作为默认昵称
                    var email = _currentUser.GetUserEmail();
                    nickname = string.IsNullOrEmpty(nickname) 
                        ? (!string.IsNullOrEmpty(email) ? email.Split('@')[0] : $"User_{userId[..8]}") 
                        : nickname;
                }
            }

            // 4. 检查用户是否已存在于腾讯云IM
            var exists = await _imService.CheckUserExistsAsync(userId);

            if (!exists)
            {
                _logger.LogInformation("用户 {UserId} 不存在于腾讯云IM，正在导入", userId);
                var success = await _imService.ImportAccountAsync(userId, nickname, avatarUrl);

                if (!success)
                {
                    return BadRequest(new { error = "导入用户到腾讯云IM失败" });
                }
                
                _logger.LogInformation("用户 {UserId} 导入成功 - Nickname: {Nickname}", userId, nickname);
            }

            return Ok(new
            {
                userId = _imService.FormatUserId(userId),
                nickname,
                avatarUrl,
                existed = exists,
                imported = !exists
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "确保用户存在失败");
            return StatusCode(500, new { error = "操作失败" });
        }
    }
}

/// <summary>
/// 批量导入请求
/// </summary>
public class BatchImportRequest
{
    /// <summary>
    /// 用户列表
    /// </summary>
    public List<UserImportDto> Users { get; set; } = new();
}
