using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GoNomads.Shared.Models;
using GoNomads.Shared.Middleware;
using UserService.Application.DTOs;
using UserService.Application.Services;
using System.ComponentModel.DataAnnotations;

namespace UserService.API.Controllers;

/// <summary>
/// Roles API - RESTful endpoints for role management
/// </summary>
[ApiController]
[Route("api/v1/roles")]
public class RolesController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<RolesController> _logger;

    public RolesController(
        IUserService userService,
        ILogger<RolesController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有角色
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<RoleDto>>>> GetAllRoles(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 获取所有角色");

        try
        {
            var roles = await _userService.GetAllRolesAsync(cancellationToken);

            return Ok(new ApiResponse<List<RoleDto>>
            {
                Success = true,
                Message = "Roles retrieved successfully",
                Data = roles
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取角色列表失败");
            return StatusCode(500, new ApiResponse<List<RoleDto>>
            {
                Success = false,
                Message = "获取角色列表失败"
            });
        }
    }

    /// <summary>
    /// 根据 ID 获取角色
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> GetRole(
        string id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 获取角色: {RoleId}", id);

        try
        {
            var role = await _userService.GetRoleByIdAsync(id, cancellationToken);

            if (role == null)
            {
                return NotFound(new ApiResponse<RoleDto>
                {
                    Success = false,
                    Message = "Role not found"
                });
            }

            return Ok(new ApiResponse<RoleDto>
            {
                Success = true,
                Message = "Role retrieved successfully",
                Data = role
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取角色失败: {RoleId}", id);
            return StatusCode(500, new ApiResponse<RoleDto>
            {
                Success = false,
                Message = "获取角色失败"
            });
        }
    }

    /// <summary>
    /// 根据名称获取角色
    /// </summary>
    [HttpGet("by-name/{name}")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> GetRoleByName(
        string name,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 根据名称获取角色: {RoleName}", name);

        try
        {
            var role = await _userService.GetRoleByNameAsync(name, cancellationToken);

            if (role == null)
            {
                return NotFound(new ApiResponse<RoleDto>
                {
                    Success = false,
                    Message = $"Role '{name}' not found"
                });
            }

            return Ok(new ApiResponse<RoleDto>
            {
                Success = true,
                Message = "Role retrieved successfully",
                Data = role
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 根据名称获取角色失败: {RoleName}", name);
            return StatusCode(500, new ApiResponse<RoleDto>
            {
                Success = false,
                Message = "获取角色失败"
            });
        }
    }

    /// <summary>
    /// 创建角色（仅管理员）
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<RoleDto>>> CreateRole(
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        // 验证用户是否为管理员
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.Role != "admin")
        {
            return Forbid();
        }

        _logger.LogInformation("📝 创建角色: {RoleName}", request.Name);

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<RoleDto>
            {
                Success = false,
                Message = "验证失败",
                Errors = errors
            });
        }

        try
        {
            var role = await _userService.CreateRoleAsync(
                request.Name,
                request.Description,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetRole),
                new { id = role.Id },
                new ApiResponse<RoleDto>
                {
                    Success = true,
                    Message = "Role created successfully",
                    Data = role
                });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "⚠️ 创建角色失败: {RoleName}", request.Name);
            return BadRequest(new ApiResponse<RoleDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建角色失败: {RoleName}", request.Name);
            return StatusCode(500, new ApiResponse<RoleDto>
            {
                Success = false,
                Message = "创建角色失败"
            });
        }
    }

    /// <summary>
    /// 更新角色（仅管理员）
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> UpdateRole(
        string id,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        // 验证用户是否为管理员
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.Role != "admin")
        {
            return Forbid();
        }

        _logger.LogInformation("📝 更新角色: {RoleId}", id);

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<RoleDto>
            {
                Success = false,
                Message = "验证失败",
                Errors = errors
            });
        }

        try
        {
            var role = await _userService.UpdateRoleAsync(
                id,
                request.Name,
                request.Description,
                cancellationToken);

            return Ok(new ApiResponse<RoleDto>
            {
                Success = true,
                Message = "Role updated successfully",
                Data = role
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<RoleDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<RoleDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新角色失败: {RoleId}", id);
            return StatusCode(500, new ApiResponse<RoleDto>
            {
                Success = false,
                Message = "更新角色失败"
            });
        }
    }

    /// <summary>
    /// 删除角色（仅管理员）
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteRole(
        string id,
        CancellationToken cancellationToken = default)
    {
        // 验证用户是否为管理员
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.Role != "admin")
        {
            return Forbid();
        }

        _logger.LogInformation("🗑️ 删除角色: {RoleId}", id);

        try
        {
            var result = await _userService.DeleteRoleAsync(id, cancellationToken);

            if (!result)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Role not found"
                });
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Role deleted successfully"
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除角色失败: {RoleId}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "删除角色失败"
            });
        }
    }

    /// <summary>
    /// 获取指定角色的所有用户
    /// </summary>
    [HttpGet("{id}/users")]
    public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetUsersByRole(
        string id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 获取角色用户: {RoleId}", id);

        try
        {
            var users = await _userService.GetUsersByRoleAsync(id, cancellationToken);

            return Ok(new ApiResponse<List<UserDto>>
            {
                Success = true,
                Message = $"Found {users.Count} users with this role",
                Data = users
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<List<UserDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取角色用户失败: {RoleId}", id);
            return StatusCode(500, new ApiResponse<List<UserDto>>
            {
                Success = false,
                Message = "获取角色用户失败"
            });
        }
    }
}

#region Request DTOs

/// <summary>
/// 创建角色请求 DTO
/// </summary>
public class CreateRoleRequest
{
    [Required(ErrorMessage = "角色名称不能为空")]
    [StringLength(50, ErrorMessage = "角色名称最多50个字符")]
    public string Name { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "角色描述最多200个字符")]
    public string? Description { get; set; }
}

/// <summary>
/// 更新角色请求 DTO
/// </summary>
public class UpdateRoleRequest
{
    [Required(ErrorMessage = "角色名称不能为空")]
    [StringLength(50, ErrorMessage = "角色名称最多50个字符")]
    public string Name { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "角色描述最多200个字符")]
    public string? Description { get; set; }
}

#endregion
