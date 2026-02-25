using Supabase;
using UserService.Application.DTOs;
using UserService.Application.Services;
using UserService.Domain.Entities;

namespace UserService.Infrastructure.Services;

/// <summary>
///     技能服务实现
/// </summary>
public class SkillService : ISkillService
{
    private readonly ILogger<SkillService> _logger;
    private readonly Client _supabaseClient;

    public SkillService(Client supabaseClient, ILogger<SkillService> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<List<SkillDto>> GetAllSkillsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 获取所有技能");

        try
        {
            var response = await _supabaseClient
                .From<Skill>()
                .Get(cancellationToken);

            return response.Models.Select(s => new SkillDto
            {
                Id = s.Id,
                Name = s.Name,
                Category = s.Category,
                Description = s.Description,
                Icon = s.Icon,
                CreatedAt = s.CreatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取所有技能失败");
            throw;
        }
    }

    public async Task<List<SkillsByCategoryDto>> GetSkillsByCategoryAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 获取按类别分组的技能");

        try
        {
            var skills = await GetAllSkillsAsync(cancellationToken);

            return skills
                .GroupBy(s => s.Category)
                .Select(g => new SkillsByCategoryDto
                {
                    Category = g.Key,
                    Skills = g.ToList()
                })
                .OrderBy(x => x.Category)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取分类技能失败");
            throw;
        }
    }

    public async Task<List<SkillDto>> GetSkillsBySpecificCategoryAsync(string category,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 获取类别为 {Category} 的技能", category);

        try
        {
            var response = await _supabaseClient
                .From<Skill>()
                .Where(s => s.Category == category)
                .Get(cancellationToken);

            return response.Models.Select(s => new SkillDto
            {
                Id = s.Id,
                Name = s.Name,
                Category = s.Category,
                Description = s.Description,
                Icon = s.Icon,
                CreatedAt = s.CreatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取类别技能失败: {Category}", category);
            throw;
        }
    }

    public async Task<SkillDto?> GetSkillByIdAsync(string skillId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 获取技能: {SkillId}", skillId);

        try
        {
            var response = await _supabaseClient
                .From<Skill>()
                .Where(s => s.Id == skillId)
                .Single(cancellationToken);

            if (response == null) return null;

            return new SkillDto
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
            _logger.LogWarning(ex, "⚠️ 技能不存在: {SkillId}", skillId);
            return null;
        }
    }

    public async Task<List<UserSkillDto>> GetUserSkillsAsync(string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 获取用户技能: {UserId}", userId);

        try
        {
            // 先获取用户的技能关联
            var userSkillsResponse = await _supabaseClient
                .From<UserSkill>()
                .Where(us => us.UserId == userId)
                .Get(cancellationToken);

            if (userSkillsResponse.Models.Count == 0)
                return new List<UserSkillDto>();

            // 批量获取所有相关技能详情（消除 N+1 查询）
            var skillIds = userSkillsResponse.Models.Select(us => us.SkillId).Distinct().ToList();
            var skillsResponse = await _supabaseClient
                .From<Skill>()
                .Filter("id", Postgrest.Constants.Operator.In, skillIds)
                .Get(cancellationToken);

            var skillsDict = skillsResponse.Models.ToDictionary(s => s.Id, s => s);

            var results = new List<UserSkillDto>();

            foreach (var userSkill in userSkillsResponse.Models)
            {
                if (skillsDict.TryGetValue(userSkill.SkillId, out var skill))
                    results.Add(new UserSkillDto
                    {
                        Id = userSkill.Id,
                        UserId = userSkill.UserId,
                        SkillId = userSkill.SkillId,
                        SkillName = skill.Name,
                        Category = skill.Category,
                        Icon = skill.Icon,
                        ProficiencyLevel = userSkill.ProficiencyLevel,
                        YearsOfExperience = userSkill.YearsOfExperience,
                        CreatedAt = userSkill.CreatedAt
                    });
            }

            // 按类别和名称排序
            return results.OrderBy(r => r.Category).ThenBy(r => r.SkillName).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取用户技能失败: {UserId}", userId);
            throw;
        }
    }

    public async Task<UserSkillDto> AddUserSkillAsync(
        string userId,
        string skillId,
        string? proficiencyLevel = null,
        int? yearsOfExperience = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("➕ 添加用户技能: UserId={UserId}, SkillId={SkillId}", userId, skillId);

        try
        {
            // 检查技能是否存在
            var skill = await GetSkillByIdAsync(skillId, cancellationToken);
            if (skill == null) throw new KeyNotFoundException($"技能不存在: {skillId}");

            var userSkill = new UserSkill
            {
                UserId = userId,
                SkillId = skillId,
                ProficiencyLevel = proficiencyLevel,
                YearsOfExperience = yearsOfExperience
            };

            var response = await _supabaseClient
                .From<UserSkill>()
                .Insert(userSkill, cancellationToken: cancellationToken);

            var created = response.Models.FirstOrDefault();
            if (created == null) throw new InvalidOperationException("添加用户技能失败");

            return new UserSkillDto
            {
                Id = created.Id,
                UserId = created.UserId,
                SkillId = created.SkillId,
                SkillName = skill.Name,
                Category = skill.Category,
                Icon = skill.Icon,
                ProficiencyLevel = created.ProficiencyLevel,
                YearsOfExperience = created.YearsOfExperience,
                CreatedAt = created.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 添加用户技能失败: UserId={UserId}, SkillId={SkillId}", userId, skillId);
            throw;
        }
    }

    public async Task<List<UserSkillDto>> AddUserSkillsBatchAsync(
        string userId,
        List<AddUserSkillRequest> skills,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("➕ 批量添加用户技能: UserId={UserId}, Count={Count}", userId, skills.Count);

        var results = new List<UserSkillDto>();

        foreach (var skill in skills)
            try
            {
                var result = await AddUserSkillAsync(
                    userId,
                    skill.SkillId,
                    skill.ProficiencyLevel,
                    skill.YearsOfExperience,
                    cancellationToken);

                results.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ 添加技能失败: {SkillId}", skill.SkillId);
                // 继续处理其他技能
            }

        return results;
    }

    public async Task<bool> RemoveUserSkillAsync(string userId, string skillId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("➖ 删除用户技能: UserId={UserId}, UserSkillOrRecordId={SkillId}", userId, skillId);

        try
        {
            // 判断 skillId 是否为有效的 UUID（记录ID）
            var isUuid = Guid.TryParse(skillId, out _);

            if (isUuid)
            {
                // 按记录 ID 删除
                await _supabaseClient
                    .From<UserSkill>()
                    .Where(us => us.Id == skillId && us.UserId == userId)
                    .Delete();
            }
            else
            {
                // 按技能 ID 删除（如 skill_sql）
                await _supabaseClient
                    .From<UserSkill>()
                    .Where(us => us.UserId == userId && us.SkillId == skillId)
                    .Delete();
            }

            _logger.LogInformation("✅ 成功删除用户技能: UserId={UserId}, SkillOrRecordId={SkillId}", userId, skillId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除用户技能失败: UserId={UserId}, SkillOrRecordId={SkillId}", userId, skillId);
            return false;
        }
    }

    public async Task<bool> RemoveUserSkillByNameAsync(string userId, string skillName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("➖ 按名称删除用户技能: UserId={UserId}, SkillName={SkillName}", userId, skillName);

        try
        {
            // 先根据技能名称查找技能ID
            var skill = await _supabaseClient
                .From<Skill>()
                .Where(s => s.Name == skillName)
                .Single();

            if (skill == null)
            {
                _logger.LogWarning("⚠️ 未找到指定名称的技能: SkillName={SkillName}", skillName);
                return false;
            }

            // 删除用户技能
            await _supabaseClient
                .From<UserSkill>()
                .Where(us => us.UserId == userId && us.SkillId == skill.Id)
                .Delete();

            _logger.LogInformation("✅ 成功删除用户技能: UserId={UserId}, SkillName={SkillName}", userId, skillName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 按名称删除用户技能失败: UserId={UserId}, SkillName={SkillName}", userId, skillName);
            return false;
        }
    }

    public async Task<UserSkillDto> UpdateUserSkillAsync(
        string userId,
        string skillId,
        string? proficiencyLevel = null,
        int? yearsOfExperience = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("✏️ 更新用户技能: UserId={UserId}, SkillId={SkillId}", userId, skillId);

        try
        {
            var update = new UserSkill
            {
                ProficiencyLevel = proficiencyLevel,
                YearsOfExperience = yearsOfExperience
            };

            var response = await _supabaseClient
                .From<UserSkill>()
                .Where(us => us.UserId == userId && us.SkillId == skillId)
                .Update(update, cancellationToken: cancellationToken);

            var updated = response.Models.FirstOrDefault();
            if (updated == null) throw new KeyNotFoundException("用户技能不存在");

            // 获取技能详情
            var skill = await GetSkillByIdAsync(skillId, cancellationToken);

            return new UserSkillDto
            {
                Id = updated.Id,
                UserId = updated.UserId,
                SkillId = updated.SkillId,
                SkillName = skill?.Name ?? "",
                Category = skill?.Category ?? "",
                Icon = skill?.Icon,
                ProficiencyLevel = updated.ProficiencyLevel,
                YearsOfExperience = updated.YearsOfExperience,
                CreatedAt = updated.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新用户技能失败: UserId={UserId}, SkillId={SkillId}", userId, skillId);
            throw;
        }
    }
}