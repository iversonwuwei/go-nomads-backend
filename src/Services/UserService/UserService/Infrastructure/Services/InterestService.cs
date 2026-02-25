using Supabase;
using UserService.Application.DTOs;
using UserService.Application.Services;
using UserService.Domain.Entities;

namespace UserService.Infrastructure.Services;

/// <summary>
///     兴趣爱好服务实现
/// </summary>
public class InterestService : IInterestService
{
    private readonly ILogger<InterestService> _logger;
    private readonly Client _supabaseClient;

    public InterestService(Client supabaseClient, ILogger<InterestService> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<List<InterestDto>> GetAllInterestsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 获取所有兴趣");

        try
        {
            var response = await _supabaseClient
                .From<Interest>()
                .Get(cancellationToken);

            return response.Models.Select(i => new InterestDto
            {
                Id = i.Id,
                Name = i.Name,
                Category = i.Category,
                Description = i.Description,
                Icon = i.Icon,
                CreatedAt = i.CreatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取所有兴趣失败");
            throw;
        }
    }

    public async Task<List<InterestsByCategoryDto>> GetInterestsByCategoryAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 获取按类别分组的兴趣");

        try
        {
            var interests = await GetAllInterestsAsync(cancellationToken);

            return interests
                .GroupBy(i => i.Category)
                .Select(g => new InterestsByCategoryDto
                {
                    Category = g.Key,
                    Interests = g.ToList()
                })
                .OrderBy(x => x.Category)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取分类兴趣失败");
            throw;
        }
    }

    public async Task<List<InterestDto>> GetInterestsBySpecificCategoryAsync(string category,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 获取类别为 {Category} 的兴趣", category);

        try
        {
            var response = await _supabaseClient
                .From<Interest>()
                .Where(i => i.Category == category)
                .Get(cancellationToken);

            return response.Models.Select(i => new InterestDto
            {
                Id = i.Id,
                Name = i.Name,
                Category = i.Category,
                Description = i.Description,
                Icon = i.Icon,
                CreatedAt = i.CreatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取类别兴趣失败: {Category}", category);
            throw;
        }
    }

    public async Task<InterestDto?> GetInterestByIdAsync(string interestId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 获取兴趣: {InterestId}", interestId);

        try
        {
            var response = await _supabaseClient
                .From<Interest>()
                .Where(i => i.Id == interestId)
                .Single(cancellationToken);

            if (response == null) return null;

            return new InterestDto
            {
                Id = response.Id,
                Name = response.Name,
                Category = response.Category,
                Description = response.Description,
                Icon = response.Icon,
                CreatedAt = response.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 兴趣不存在: {InterestId}", interestId);
            return null;
        }
    }

    public async Task<List<UserInterestDto>> GetUserInterestsAsync(string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 获取用户兴趣: {UserId}", userId);

        try
        {
            // 先获取用户的兴趣关联
            var userInterestsResponse = await _supabaseClient
                .From<UserInterest>()
                .Where(ui => ui.UserId == userId)
                .Get(cancellationToken);

            if (userInterestsResponse.Models.Count == 0)
                return new List<UserInterestDto>();

            // 批量获取所有相关兴趣详情（消除 N+1 查询）
            var interestIds = userInterestsResponse.Models.Select(ui => ui.InterestId).Distinct().ToList();
            var interestsResponse = await _supabaseClient
                .From<Interest>()
                .Filter("id", Postgrest.Constants.Operator.In, interestIds)
                .Get(cancellationToken);

            var interestsDict = interestsResponse.Models.ToDictionary(i => i.Id, i => i);

            var results = new List<UserInterestDto>();

            foreach (var userInterest in userInterestsResponse.Models)
            {
                if (interestsDict.TryGetValue(userInterest.InterestId, out var interest))
                    results.Add(new UserInterestDto
                    {
                        Id = userInterest.Id,
                        UserId = userInterest.UserId,
                        InterestId = userInterest.InterestId,
                        InterestName = interest.Name,
                        Category = interest.Category,
                        Icon = interest.Icon,
                        IntensityLevel = userInterest.IntensityLevel,
                        CreatedAt = userInterest.CreatedAt
                    });
            }

            // 按类别和名称排序
            return results.OrderBy(r => r.Category).ThenBy(r => r.InterestName).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取用户兴趣失败: {UserId}", userId);
            throw;
        }
    }

    public async Task<UserInterestDto> AddUserInterestAsync(
        string userId,
        string interestId,
        string? intensityLevel = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("➕ 添加用户兴趣: UserId={UserId}, InterestId={InterestId}", userId, interestId);

        try
        {
            // 检查兴趣是否存在
            var interest = await GetInterestByIdAsync(interestId, cancellationToken);
            if (interest == null) throw new KeyNotFoundException($"兴趣不存在: {interestId}");

            var userInterest = new UserInterest
            {
                UserId = userId,
                InterestId = interestId,
                IntensityLevel = intensityLevel
            };

            var response = await _supabaseClient
                .From<UserInterest>()
                .Insert(userInterest, cancellationToken: cancellationToken);

            var created = response.Models.FirstOrDefault();
            if (created == null) throw new InvalidOperationException("添加用户兴趣失败");

            return new UserInterestDto
            {
                Id = created.Id,
                UserId = created.UserId,
                InterestId = created.InterestId,
                InterestName = interest.Name,
                Category = interest.Category,
                Icon = interest.Icon,
                IntensityLevel = created.IntensityLevel,
                CreatedAt = created.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 添加用户兴趣失败: UserId={UserId}, InterestId={InterestId}", userId, interestId);
            throw;
        }
    }

    public async Task<List<UserInterestDto>> AddUserInterestsBatchAsync(
        string userId,
        List<AddUserInterestRequest> interests,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("➕ 批量添加用户兴趣: UserId={UserId}, Count={Count}", userId, interests.Count);

        var results = new List<UserInterestDto>();

        foreach (var interest in interests)
            try
            {
                var result = await AddUserInterestAsync(
                    userId,
                    interest.InterestId,
                    interest.IntensityLevel,
                    cancellationToken);

                results.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ 添加兴趣失败: {InterestId}", interest.InterestId);
                // 继续处理其他兴趣
            }

        return results;
    }

    public async Task<bool> RemoveUserInterestAsync(string userId, string interestId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("➖ 删除用户兴趣: UserId={UserId}, InterestRecordOrInterestId={InterestId}", userId,
            interestId);

        try
        {
            // 判断 interestId 是否为有效的 UUID（记录ID）
            var isUuid = Guid.TryParse(interestId, out _);

            if (isUuid)
            {
                // 按记录 ID 删除
                await _supabaseClient
                    .From<UserInterest>()
                    .Where(ui => ui.Id == interestId && ui.UserId == userId)
                    .Delete();
            }
            else
            {
                // 按兴趣 ID 删除（如 interest_dancing）
                await _supabaseClient
                    .From<UserInterest>()
                    .Where(ui => ui.UserId == userId && ui.InterestId == interestId)
                    .Delete();
            }

            _logger.LogInformation("✅ 成功删除用户兴趣: UserId={UserId}, InterestRecordOrInterestId={InterestId}", userId,
                interestId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除用户兴趣失败: UserId={UserId}, InterestRecordOrInterestId={InterestId}", userId,
                interestId);
            return false;
        }
    }

    public async Task<bool> RemoveUserInterestByNameAsync(string userId, string interestName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("➖ 按名称删除用户兴趣: UserId={UserId}, InterestName={InterestName}", userId, interestName);

        try
        {
            // 先根据兴趣名称查找兴趣ID
            var interest = await _supabaseClient
                .From<Interest>()
                .Where(i => i.Name == interestName)
                .Single();

            if (interest == null)
            {
                _logger.LogWarning("⚠️ 未找到指定名称的兴趣: InterestName={InterestName}", interestName);
                return false;
            }

            // 删除用户兴趣
            await _supabaseClient
                .From<UserInterest>()
                .Where(ui => ui.UserId == userId && ui.InterestId == interest.Id)
                .Delete();

            _logger.LogInformation("✅ 成功删除用户兴趣: UserId={UserId}, InterestName={InterestName}", userId, interestName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 按名称删除用户兴趣失败: UserId={UserId}, InterestName={InterestName}", userId, interestName);
            return false;
        }
    }

    public async Task<UserInterestDto> UpdateUserInterestAsync(
        string userId,
        string interestId,
        string? intensityLevel = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("✏️ 更新用户兴趣: UserId={UserId}, InterestId={InterestId}", userId, interestId);

        try
        {
            var update = new UserInterest
            {
                IntensityLevel = intensityLevel
            };

            var response = await _supabaseClient
                .From<UserInterest>()
                .Where(ui => ui.UserId == userId && ui.InterestId == interestId)
                .Update(update, cancellationToken: cancellationToken);

            var updated = response.Models.FirstOrDefault();
            if (updated == null) throw new KeyNotFoundException("用户兴趣不存在");

            // 获取兴趣详情
            var interest = await GetInterestByIdAsync(interestId, cancellationToken);

            return new UserInterestDto
            {
                Id = updated.Id,
                UserId = updated.UserId,
                InterestId = updated.InterestId,
                InterestName = interest?.Name ?? "",
                Category = interest?.Category ?? "",
                Icon = interest?.Icon,
                IntensityLevel = updated.IntensityLevel,
                CreatedAt = updated.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新用户兴趣失败: UserId={UserId}, InterestId={InterestId}", userId, interestId);
            throw;
        }
    }
}