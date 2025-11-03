using Microsoft.AspNetCore.Mvc;
using GoNomads.Shared.Models;
using GoNomads.Shared.Middleware;
using UserService.Application.DTOs;
using UserService.Application.Services;

namespace UserService.API.Controllers;

/// <summary>
/// 技能 API - RESTful endpoints for skills management
/// </summary>
[ApiController]
[Route("api/v1/skills")]
public class SkillsController : ControllerBase
{
    private readonly ISkillService _skillService;
    private readonly ILogger<SkillsController> _logger;

    public SkillsController(ISkillService skillService, ILogger<SkillsController> logger)
    {
        _skillService = skillService;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有技能
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<SkillDto>>>> GetAllSkills(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 获取所有技能");

        try
        {
            var skills = await _skillService.GetAllSkillsAsync(cancellationToken);

            return Ok(new ApiResponse<List<SkillDto>>
            {
                Success = true,
                Message = "Skills retrieved successfully",
                Data = skills
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取技能列表失败");
            return StatusCode(500, new ApiResponse<List<SkillDto>>
            {
                Success = false,
                Message = "Failed to retrieve skills"
            });
        }
    }

    /// <summary>
    /// 获取按类别分组的技能
    /// </summary>
    [HttpGet("by-category")]
    public async Task<ActionResult<ApiResponse<List<SkillsByCategoryDto>>>> GetSkillsByCategory(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 获取分类技能");

        try
        {
            var skills = await _skillService.GetSkillsByCategoryAsync(cancellationToken);

            return Ok(new ApiResponse<List<SkillsByCategoryDto>>
            {
                Success = true,
                Message = "Skills by category retrieved successfully",
                Data = skills
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取分类技能失败");
            return StatusCode(500, new ApiResponse<List<SkillsByCategoryDto>>
            {
                Success = false,
                Message = "Failed to retrieve skills by category"
            });
        }
    }

    /// <summary>
    /// 根据类别获取技能
    /// </summary>
    [HttpGet("category/{category}")]
    public async Task<ActionResult<ApiResponse<List<SkillDto>>>> GetSkillsBySpecificCategory(
        string category,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 获取类别技能: {Category}", category);

        try
        {
            var skills = await _skillService.GetSkillsBySpecificCategoryAsync(category, cancellationToken);

            return Ok(new ApiResponse<List<SkillDto>>
            {
                Success = true,
                Message = $"Skills in category '{category}' retrieved successfully",
                Data = skills
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取类别技能失败: {Category}", category);
            return StatusCode(500, new ApiResponse<List<SkillDto>>
            {
                Success = false,
                Message = "Failed to retrieve skills for category"
            });
        }
    }

    /// <summary>
    /// 根据ID获取技能
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<SkillDto>>> GetSkill(
        string id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 获取技能: {SkillId}", id);

        try
        {
            var skill = await _skillService.GetSkillByIdAsync(id, cancellationToken);

            if (skill == null)
            {
                return NotFound(new ApiResponse<SkillDto>
                {
                    Success = false,
                    Message = "Skill not found"
                });
            }

            return Ok(new ApiResponse<SkillDto>
            {
                Success = true,
                Message = "Skill retrieved successfully",
                Data = skill
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取技能失败: {SkillId}", id);
            return StatusCode(500, new ApiResponse<SkillDto>
            {
                Success = false,
                Message = "Failed to retrieve skill"
            });
        }
    }

    /// <summary>
    /// 获取用户的所有技能
    /// </summary>
    [HttpGet("users/{userId}")]
    public async Task<ActionResult<ApiResponse<List<UserSkillDto>>>> GetUserSkills(
        string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 获取用户技能: {UserId}", userId);

        try
        {
            var skills = await _skillService.GetUserSkillsAsync(userId, cancellationToken);

            return Ok(new ApiResponse<List<UserSkillDto>>
            {
                Success = true,
                Message = "User skills retrieved successfully",
                Data = skills
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取用户技能失败: {UserId}", userId);
            return StatusCode(500, new ApiResponse<List<UserSkillDto>>
            {
                Success = false,
                Message = "Failed to retrieve user skills"
            });
        }
    }

    /// <summary>
    /// 获取当前用户的所有技能（使用 UserContext）
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<List<UserSkillDto>>>> GetCurrentUserSkills(
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
        {
            return Unauthorized(new ApiResponse<List<UserSkillDto>>
            {
                Success = false,
                Message = "未认证用户"
            });
        }

        _logger.LogInformation("📋 获取当前用户技能: {UserId}", userContext.UserId);

        try
        {
            var skills = await _skillService.GetUserSkillsAsync(userContext.UserId!, cancellationToken);

            return Ok(new ApiResponse<List<UserSkillDto>>
            {
                Success = true,
                Message = "User skills retrieved successfully",
                Data = skills
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取当前用户技能失败: {UserId}", userContext.UserId);
            return StatusCode(500, new ApiResponse<List<UserSkillDto>>
            {
                Success = false,
                Message = "Failed to retrieve user skills"
            });
        }
    }

    /// <summary>
    /// 添加用户技能
    /// </summary>
    [HttpPost("users/{userId}")]
    public async Task<ActionResult<ApiResponse<UserSkillDto>>> AddUserSkill(
        string userId,
        [FromBody] AddUserSkillRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("➕ 添加用户技能: UserId={UserId}, SkillId={SkillId}", userId, request.SkillId);

        try
        {
            var skill = await _skillService.AddUserSkillAsync(
                userId,
                request.SkillId,
                request.ProficiencyLevel,
                request.YearsOfExperience,
                cancellationToken);

            return Ok(new ApiResponse<UserSkillDto>
            {
                Success = true,
                Message = "User skill added successfully",
                Data = skill
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<UserSkillDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 添加用户技能失败: UserId={UserId}, SkillId={SkillId}", userId, request.SkillId);
            return StatusCode(500, new ApiResponse<UserSkillDto>
            {
                Success = false,
                Message = "Failed to add user skill"
            });
        }
    }

    /// <summary>
    /// 添加当前用户技能（使用 UserContext）
    /// </summary>
    [HttpPost("me")]
    public async Task<ActionResult<ApiResponse<UserSkillDto>>> AddCurrentUserSkill(
        [FromBody] AddUserSkillRequest request,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
        {
            return Unauthorized(new ApiResponse<UserSkillDto>
            {
                Success = false,
                Message = "未认证用户"
            });
        }

        _logger.LogInformation("➕ 添加当前用户技能: UserId={UserId}, SkillId={SkillId}", userContext.UserId, request.SkillId);

        try
        {
            var skill = await _skillService.AddUserSkillAsync(
                userContext.UserId!,
                request.SkillId,
                request.ProficiencyLevel,
                request.YearsOfExperience,
                cancellationToken);

            return Ok(new ApiResponse<UserSkillDto>
            {
                Success = true,
                Message = "User skill added successfully",
                Data = skill
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<UserSkillDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 添加当前用户技能失败: UserId={UserId}, SkillId={SkillId}", userContext.UserId, request.SkillId);
            return StatusCode(500, new ApiResponse<UserSkillDto>
            {
                Success = false,
                Message = "Failed to add user skill"
            });
        }
    }

    /// <summary>
    /// 批量添加当前用户技能（使用 UserContext）
    /// </summary>
    [HttpPost("me/batch")]
    public async Task<ActionResult<ApiResponse<List<UserSkillDto>>>> AddCurrentUserSkillsBatch(
        [FromBody] List<AddUserSkillRequest> request,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
        {
            return Unauthorized(new ApiResponse<List<UserSkillDto>>
            {
                Success = false,
                Message = "未认证用户"
            });
        }

        _logger.LogInformation("➕ 批量添加当前用户技能: UserId={UserId}, Count={Count}", userContext.UserId, request.Count);

        try
        {
            var skills = await _skillService.AddUserSkillsBatchAsync(userContext.UserId!, request, cancellationToken);

            return Ok(new ApiResponse<List<UserSkillDto>>
            {
                Success = true,
                Message = $"Successfully added {skills.Count} skills",
                Data = skills
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 批量添加当前用户技能失败: UserId={UserId}", userContext.UserId);
            return StatusCode(500, new ApiResponse<List<UserSkillDto>>
            {
                Success = false,
                Message = "Failed to add user skills"
            });
        }
    }

    /// <summary>
    /// 批量添加用户技能
    /// </summary>
    [HttpPost("users/{userId}/batch")]
    public async Task<ActionResult<ApiResponse<List<UserSkillDto>>>> AddUserSkillsBatch(
        string userId,
        [FromBody] List<AddUserSkillRequest> request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("➕ 批量添加用户技能: UserId={UserId}, Count={Count}", userId, request.Count);

        try
        {
            var skills = await _skillService.AddUserSkillsBatchAsync(userId, request, cancellationToken);

            return Ok(new ApiResponse<List<UserSkillDto>>
            {
                Success = true,
                Message = $"Successfully added {skills.Count} skills",
                Data = skills
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 批量添加用户技能失败: UserId={UserId}", userId);
            return StatusCode(500, new ApiResponse<List<UserSkillDto>>
            {
                Success = false,
                Message = "Failed to add user skills"
            });
        }
    }

    /// <summary>
    /// 删除当前用户技能（使用 UserContext）
    /// </summary>
    [HttpDelete("me/{skillId}")]
    public async Task<ActionResult<ApiResponse<object>>> RemoveCurrentUserSkill(
        string skillId,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "未认证用户"
            });
        }

        _logger.LogInformation("➖ 删除当前用户技能: UserId={UserId}, SkillId={SkillId}", userContext.UserId, skillId);

        try
        {
            var result = await _skillService.RemoveUserSkillAsync(userContext.UserId!, skillId, cancellationToken);

            if (!result)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "User skill not found"
                });
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "User skill removed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除当前用户技能失败: UserId={UserId}, SkillId={SkillId}", userContext.UserId, skillId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to remove user skill"
            });
        }
    }

    /// <summary>
    /// 删除用户技能
    /// </summary>
    [HttpDelete("users/{userId}/{skillId}")]
    public async Task<ActionResult<ApiResponse<object>>> RemoveUserSkill(
        string userId,
        string skillId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("➖ 删除用户技能: UserId={UserId}, SkillId={SkillId}", userId, skillId);

        try
        {
            var result = await _skillService.RemoveUserSkillAsync(userId, skillId, cancellationToken);

            if (!result)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "User skill not found"
                });
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "User skill removed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除用户技能失败: UserId={UserId}, SkillId={SkillId}", userId, skillId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to remove user skill"
            });
        }
    }

    /// <summary>
    /// 更新当前用户技能（使用 UserContext）
    /// </summary>
    [HttpPut("me/{skillId}")]
    public async Task<ActionResult<ApiResponse<UserSkillDto>>> UpdateCurrentUserSkill(
        string skillId,
        [FromBody] AddUserSkillRequest request,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
        {
            return Unauthorized(new ApiResponse<UserSkillDto>
            {
                Success = false,
                Message = "未认证用户"
            });
        }

        _logger.LogInformation("✏️ 更新当前用户技能: UserId={UserId}, SkillId={SkillId}", userContext.UserId, skillId);

        try
        {
            var skill = await _skillService.UpdateUserSkillAsync(
                userContext.UserId!,
                skillId,
                request.ProficiencyLevel,
                request.YearsOfExperience,
                cancellationToken);

            return Ok(new ApiResponse<UserSkillDto>
            {
                Success = true,
                Message = "User skill updated successfully",
                Data = skill
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<UserSkillDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新当前用户技能失败: UserId={UserId}, SkillId={SkillId}", userContext.UserId, skillId);
            return StatusCode(500, new ApiResponse<UserSkillDto>
            {
                Success = false,
                Message = "Failed to update user skill"
            });
        }
    }

    /// <summary>
    /// 更新用户技能
    /// </summary>
    [HttpPut("users/{userId}/{skillId}")]
    public async Task<ActionResult<ApiResponse<UserSkillDto>>> UpdateUserSkill(
        string userId,
        string skillId,
        [FromBody] AddUserSkillRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("✏️ 更新用户技能: UserId={UserId}, SkillId={SkillId}", userId, skillId);

        try
        {
            var skill = await _skillService.UpdateUserSkillAsync(
                userId,
                skillId,
                request.ProficiencyLevel,
                request.YearsOfExperience,
                cancellationToken);

            return Ok(new ApiResponse<UserSkillDto>
            {
                Success = true,
                Message = "User skill updated successfully",
                Data = skill
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<UserSkillDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新用户技能失败: UserId={UserId}, SkillId={SkillId}", userId, skillId);
            return StatusCode(500, new ApiResponse<UserSkillDto>
            {
                Success = false,
                Message = "Failed to update user skill"
            });
        }
    }
}
