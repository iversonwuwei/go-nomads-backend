using Grpc.Core;
using UserService.Application.Services;
using UserService.Grpc;

namespace UserService.Services.Grpc;

/// <summary>
/// gRPC 用户信息服务实现
/// </summary>
public class GrpcUserService : UserService.Grpc.UserService.UserServiceBase
{
    private readonly IUserService _userService;
    private readonly ILogger<GrpcUserService> _logger;

    public GrpcUserService(
        IUserService userService,
        ILogger<GrpcUserService> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// 获取单个用户信息
    /// </summary>
    public override async Task<UserInfoResponse> GetUserInfo(
        GetUserInfoRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("📞 gRPC GetUserInfo 调用 - UserId: {UserId}", request.UserId);

        try
        {
            // 验证请求
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                _logger.LogWarning("⚠️ UserId 为空");
                return new UserInfoResponse
                {
                    Success = false,
                    ErrorMessage = "User ID is required"
                };
            }

            // 调用应用服务获取用户
            var user = await _userService.GetUserByIdAsync(request.UserId, context.CancellationToken);

            if (user == null)
            {
                _logger.LogWarning("⚠️ 用户不存在: {UserId}", request.UserId);
                return new UserInfoResponse
                {
                    UserId = request.UserId,
                    Success = false,
                    ErrorMessage = "User not found"
                };
            }

            _logger.LogInformation("✅ 成功获取用户信息: {Username}", user.Name);

            return new UserInfoResponse
            {
                UserId = user.Id,
                Username = user.Name ?? string.Empty,
                Email = user.Email ?? string.Empty,
                AvatarUrl = string.Empty, // UserDto 暂时没有 AvatarUrl
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取用户信息失败: {UserId}", request.UserId);
            return new UserInfoResponse
            {
                UserId = request.UserId,
                Success = false,
                ErrorMessage = $"Internal error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 批量获取用户信息
    /// </summary>
    public override async Task<GetUsersInfoResponse> GetUsersInfo(
        GetUsersInfoRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("📞 gRPC GetUsersInfo 调用 - 用户数量: {Count}", request.UserIds.Count);

        var response = new GetUsersInfoResponse();

        try
        {
            // 验证请求
            if (request.UserIds == null || request.UserIds.Count == 0)
            {
                _logger.LogWarning("⚠️ UserIds 为空");
                return response;
            }

            // 批量查询用户
            var tasks = request.UserIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(async userId =>
                {
                    try
                    {
                        var user = await _userService.GetUserByIdAsync(userId, context.CancellationToken);
                        if (user != null)
                        {
                            return new UserInfoResponse
                            {
                                UserId = user.Id,
                                Username = user.Name ?? string.Empty,
                                Email = user.Email ?? string.Empty,
                                AvatarUrl = string.Empty, // UserDto 暂时没有 AvatarUrl
                                Success = true
                            };
                        }
                        else
                        {
                            return new UserInfoResponse
                            {
                                UserId = userId,
                                Success = false,
                                ErrorMessage = "User not found"
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ 获取用户失败: {UserId}", userId);
                        return new UserInfoResponse
                        {
                            UserId = userId,
                            Success = false,
                            ErrorMessage = ex.Message
                        };
                    }
                });

            var users = await Task.WhenAll(tasks);
            response.Users.AddRange(users);

            _logger.LogInformation("✅ 成功获取 {Count} 个用户信息", users.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 批量获取用户信息失败");
        }

        return response;
    }
}
