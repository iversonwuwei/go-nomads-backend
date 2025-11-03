using UserService.Application.DTOs;

namespace UserService.Application.Services;

/// <summary>
/// 技能服务接口
/// </summary>
public interface ISkillService
{
    /// <summary>
    /// 获取所有技能
    /// </summary>
    Task<List<SkillDto>> GetAllSkillsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取按类别分组的技能
    /// </summary>
    Task<List<SkillsByCategoryDto>> GetSkillsByCategoryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据类别获取技能
    /// </summary>
    Task<List<SkillDto>> GetSkillsBySpecificCategoryAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据ID获取技能
    /// </summary>
    Task<SkillDto?> GetSkillByIdAsync(string skillId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的所有技能
    /// </summary>
    Task<List<UserSkillDto>> GetUserSkillsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加用户技能
    /// </summary>
    Task<UserSkillDto> AddUserSkillAsync(
        string userId,
        string skillId,
        string? proficiencyLevel = null,
        int? yearsOfExperience = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量添加用户技能
    /// </summary>
    Task<List<UserSkillDto>> AddUserSkillsBatchAsync(
        string userId,
        List<AddUserSkillRequest> skills,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除用户技能
    /// </summary>
    Task<bool> RemoveUserSkillAsync(string userId, string skillId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按技能名称删除用户技能
    /// </summary>
    Task<bool> RemoveUserSkillByNameAsync(string userId, string skillName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户技能熟练度
    /// </summary>
    Task<UserSkillDto> UpdateUserSkillAsync(
        string userId,
        string skillId,
        string? proficiencyLevel = null,
        int? yearsOfExperience = null,
        CancellationToken cancellationToken = default);
}
