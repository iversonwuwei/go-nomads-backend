using UserService.Domain.Entities;
using UserService.Domain.Repositories;
using Client = Supabase.Client;

namespace UserService.Infrastructure.Repositories;

/// <summary>
///     UserPreferences 仓储 Supabase 实现
/// </summary>
public class UserPreferencesRepository : IUserPreferencesRepository
{
    private readonly ILogger<UserPreferencesRepository> _logger;
    private readonly Client _supabaseClient;

    public UserPreferencesRepository(Client supabaseClient, ILogger<UserPreferencesRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<UserPreferences> CreateAsync(UserPreferences preferences, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 创建用户偏好设置: UserId={UserId}", preferences.UserId);

        try
        {
            var result = await _supabaseClient
                .From<UserPreferences>()
                .Insert(preferences, cancellationToken: cancellationToken);

            var createdPrefs = result.Models.FirstOrDefault();
            if (createdPrefs == null) 
                throw new InvalidOperationException("创建用户偏好设置失败");

            _logger.LogInformation("✅ 成功创建用户偏好设置: {PrefsId}", createdPrefs.Id);
            return createdPrefs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建用户偏好设置失败: UserId={UserId}", preferences.UserId);
            throw;
        }
    }

    public async Task<UserPreferences?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 根据用户ID查询偏好设置: {UserId}", userId);

        try
        {
            var response = await _supabaseClient
                .From<UserPreferences>()
                .Where(p => p.UserId == userId)
                .Single(cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "⚠️ 未找到用户偏好设置: {UserId}", userId);
            return null;
        }
    }

    public async Task<UserPreferences> UpdateAsync(UserPreferences preferences, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 更新用户偏好设置: UserId={UserId}", preferences.UserId);

        try
        {
            var result = await _supabaseClient
                .From<UserPreferences>()
                .Where(p => p.Id == preferences.Id)
                .Update(preferences, cancellationToken: cancellationToken);

            var updatedPrefs = result.Models.FirstOrDefault();
            if (updatedPrefs == null)
                throw new InvalidOperationException("更新用户偏好设置失败");

            _logger.LogInformation("✅ 成功更新用户偏好设置: {PrefsId}", updatedPrefs.Id);
            return updatedPrefs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新用户偏好设置失败: UserId={UserId}", preferences.UserId);
            throw;
        }
    }

    public async Task<UserPreferences> GetOrCreateAsync(string userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 获取或创建用户偏好设置: UserId={UserId}", userId);

        // 先尝试获取
        var existingPrefs = await GetByUserIdAsync(userId, cancellationToken);
        if (existingPrefs != null)
        {
            _logger.LogInformation("✅ 找到已有用户偏好设置: {PrefsId}", existingPrefs.Id);
            return existingPrefs;
        }

        // 不存在则创建
        _logger.LogInformation("📝 用户偏好设置不存在，创建新记录: UserId={UserId}", userId);
        var newPrefs = UserPreferences.CreateDefault(userId);
        return await CreateAsync(newPrefs, cancellationToken);
    }
}
