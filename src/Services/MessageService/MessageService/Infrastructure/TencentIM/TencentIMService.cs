using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MessageService.Infrastructure.TencentIM;

/// <summary>
/// 腾讯云IM服务实现
/// </summary>
public class TencentIMService : ITencentIMService
{
    private readonly TencentIMConfig _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger<TencentIMService> _logger;
    private readonly Random _random = new();

    // 用户ID前缀
    private const string UserIdPrefix = "user_";

    public TencentIMService(
        IOptions<TencentIMConfig> config,
        HttpClient httpClient,
        ILogger<TencentIMService> logger)
    {
        _config = config.Value;
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public string FormatUserId(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return userId;
        if (userId.StartsWith(UserIdPrefix)) return userId;
        return $"{UserIdPrefix}{userId}";
    }

    /// <inheritdoc />
    public UserSigResponse GenerateUserSig(string userId)
    {
        var formattedUserId = FormatUserId(userId);
        var userSig = TencentIMUserSigGenerator.GenerateUserSig(
            _config.SdkAppId,
            _config.SecretKey,
            formattedUserId,
            _config.UserSigExpireSeconds
        );

        var expireAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + _config.UserSigExpireSeconds;

        return new UserSigResponse
        {
            UserId = formattedUserId,
            UserSig = userSig,
            ExpireAt = expireAt
        };
    }

    /// <inheritdoc />
    public async Task<bool> ImportAccountAsync(string userId, string? nickname = null, string? avatarUrl = null)
    {
        var formattedUserId = FormatUserId(userId);

        try
        {
            var url = BuildApiUrl("v4/im_open_login_svc/account_import");
            var request = new ImportAccountRequest
            {
                UserID = formattedUserId,
                Nick = nickname,
                FaceUrl = avatarUrl
            };

            _logger.LogInformation("正在导入用户到腾讯云IM: {UserId}", formattedUserId);

            var response = await _httpClient.PostAsJsonAsync(url, request);
            var result = await response.Content.ReadFromJsonAsync<TencentIMResponse>();

            if (result?.ErrorCode == 0)
            {
                _logger.LogInformation("用户 {UserId} 导入成功", formattedUserId);
                return true;
            }

            _logger.LogWarning("用户 {UserId} 导入失败: {ErrorCode} - {ErrorInfo}",
                formattedUserId, result?.ErrorCode, result?.ErrorInfo);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入用户 {UserId} 时发生异常", formattedUserId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<BatchImportResult> BatchImportAccountsAsync(IEnumerable<UserImportDto> users)
    {
        var userList = users.ToList();
        var result = new BatchImportResult
        {
            TotalCount = userList.Count
        };

        var failedUserIds = new List<string>();

        // 腾讯云IM单个账号导入支持设置昵称和头像，批量导入不支持
        // 所以需要逐个导入以支持昵称和头像
        foreach (var user in userList)
        {
            var success = await ImportAccountAsync(user.UserId, user.Nickname, user.AvatarUrl);
            if (success)
            {
                result.SuccessCount++;
            }
            else
            {
                result.FailedCount++;
                failedUserIds.Add(user.UserId);
            }
        }

        result.FailedUserIds = failedUserIds;
        result.Success = result.FailedCount == 0;

        return result;
    }

    /// <inheritdoc />
    public async Task<BatchImportResult> BatchImportAccountIdsAsync(IEnumerable<string> userIds)
    {
        var userIdList = userIds.Select(FormatUserId).ToList();
        var result = new BatchImportResult
        {
            TotalCount = userIdList.Count
        };

        // 腾讯云IM批量导入每次最多100个
        const int batchSize = 100;
        var batches = userIdList
            .Select((id, index) => new { Id = id, Index = index })
            .GroupBy(x => x.Index / batchSize)
            .Select(g => g.Select(x => x.Id).ToList());

        var allFailedUserIds = new List<string>();

        foreach (var batch in batches)
        {
            try
            {
                var url = BuildApiUrl("v4/im_open_login_svc/multiaccount_import");
                var request = new MultiAccountImportRequest
                {
                    Accounts = batch
                };

                _logger.LogInformation("批量导入 {Count} 个用户到腾讯云IM", batch.Count);

                var response = await _httpClient.PostAsJsonAsync(url, request);
                var batchResult = await response.Content.ReadFromJsonAsync<MultiAccountImportResponse>();

                if (batchResult?.ErrorCode == 0)
                {
                    var failedCount = batchResult.FailAccounts?.Count ?? 0;
                    result.SuccessCount += batch.Count - failedCount;
                    result.FailedCount += failedCount;

                    if (batchResult.FailAccounts != null)
                    {
                        allFailedUserIds.AddRange(batchResult.FailAccounts);
                    }

                    _logger.LogInformation("批量导入完成: 成功 {Success}, 失败 {Failed}",
                        batch.Count - failedCount, failedCount);
                }
                else
                {
                    result.FailedCount += batch.Count;
                    allFailedUserIds.AddRange(batch);
                    _logger.LogWarning("批量导入失败: {ErrorCode} - {ErrorInfo}",
                        batchResult?.ErrorCode, batchResult?.ErrorInfo);
                }
            }
            catch (Exception ex)
            {
                result.FailedCount += batch.Count;
                allFailedUserIds.AddRange(batch);
                _logger.LogError(ex, "批量导入用户时发生异常");
            }
        }

        result.FailedUserIds = allFailedUserIds;
        result.Success = result.FailedCount == 0;

        return result;
    }

    /// <inheritdoc />
    public async Task<bool> CheckUserExistsAsync(string userId)
    {
        var formattedUserId = FormatUserId(userId);

        try
        {
            var url = BuildApiUrl("v4/openim/querystate");
            var request = new QueryUserStatusRequest
            {
                To_Account = new List<string> { formattedUserId }
            };

            var response = await _httpClient.PostAsJsonAsync(url, request);
            var result = await response.Content.ReadFromJsonAsync<QueryUserStatusResponse>();

            // 如果ErrorList中包含此用户且错误码是70107，说明用户不存在
            if (result?.ErrorList != null)
            {
                var error = result.ErrorList.FirstOrDefault(e => e.To_Account == formattedUserId);
                if (error != null && error.ErrorCode == 70107)
                {
                    return false;
                }
            }

            // 如果QueryResult中有此用户，说明用户存在
            if (result?.QueryResult != null)
            {
                return result.QueryResult.Any(r => r.To_Account == formattedUserId);
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查用户 {UserId} 是否存在时发生异常", formattedUserId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, string>> QueryUserStatusAsync(IEnumerable<string> userIds)
    {
        var formattedUserIds = userIds.Select(FormatUserId).ToList();
        var statusDict = new Dictionary<string, string>();

        try
        {
            var url = BuildApiUrl("v4/openim/querystate");
            var request = new QueryUserStatusRequest
            {
                To_Account = formattedUserIds
            };

            var response = await _httpClient.PostAsJsonAsync(url, request);
            var result = await response.Content.ReadFromJsonAsync<QueryUserStatusResponse>();

            if (result?.QueryResult != null)
            {
                foreach (var item in result.QueryResult)
                {
                    statusDict[item.To_Account] = item.Status;
                }
            }

            // 标记不存在的用户
            if (result?.ErrorList != null)
            {
                foreach (var error in result.ErrorList)
                {
                    statusDict[error.To_Account] = "NotExist";
                }
            }

            return statusDict;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询用户状态时发生异常");
            return statusDict;
        }
    }

    /// <summary>
    /// 构建API URL
    /// </summary>
    private string BuildApiUrl(string path)
    {
        var adminUserSig = TencentIMUserSigGenerator.GenerateUserSig(
            _config.SdkAppId,
            _config.SecretKey,
            _config.AdminUserId,
            _config.UserSigExpireSeconds
        );

        var random = _random.Next(1000000000);
        var contentType = "json";

        return $"{_config.ApiBaseUrl}/{path}?sdkappid={_config.SdkAppId}&identifier={_config.AdminUserId}&usersig={adminUserSig}&random={random}&contenttype={contentType}";
    }
}
