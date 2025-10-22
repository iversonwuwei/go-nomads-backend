using Microsoft.AspNetCore.Mvc;
using GoNomads.Shared.Models;
using UserService.Repositories;
using System.ComponentModel.DataAnnotations;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly IRoleRepository _roleRepository;
    private readonly ILogger<RolesController> _logger;

    public RolesController(
        IRoleRepository roleRepository,
        ILogger<RolesController> logger)
    {
        _roleRepository = roleRepository;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有角色
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<RoleDto>>>> GetAllRoles(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var roles = await _roleRepository.GetAllRolesAsync(cancellationToken);
            
            var roleDtos = roles.Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            }).ToList();

            return Ok(new ApiResponse<List<RoleDto>>
            {
                Success = true,
                Message = "Roles retrieved successfully",
                Data = roleDtos
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all roles");
            return StatusCode(500, new ApiResponse<List<RoleDto>>
            {
                Success = false,
                Message = "Failed to retrieve roles"
            });
        }
    }

    /// <summary>
    /// 根据ID获取角色
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> GetRoleById(
        string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var role = await _roleRepository.GetRoleByIdAsync(id, cancellationToken);
            
            if (role == null)
            {
                return NotFound(new ApiResponse<RoleDto>
                {
                    Success = false,
                    Message = $"Role with ID '{id}' not found"
                });
            }

            var roleDto = new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                CreatedAt = role.CreatedAt,
                UpdatedAt = role.UpdatedAt
            };

            return Ok(new ApiResponse<RoleDto>
            {
                Success = true,
                Message = "Role retrieved successfully",
                Data = roleDto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role by ID: {RoleId}", id);
            return StatusCode(500, new ApiResponse<RoleDto>
            {
                Success = false,
                Message = "Failed to retrieve role"
            });
        }
    }

    /// <summary>
    /// 创建新角色
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<RoleDto>>> CreateRole(
        [FromBody] CreateRoleDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<RoleDto>
            {
                Success = false,
                Message = "Validation failed",
                Errors = errors
            });
        }

        try
        {
            // Check if role name already exists
            var existingRole = await _roleRepository.GetRoleByNameAsync(dto.Name, cancellationToken);
            if (existingRole != null)
            {
                return BadRequest(new ApiResponse<RoleDto>
                {
                    Success = false,
                    Message = $"Role with name '{dto.Name}' already exists"
                });
            }

            var role = new Role
            {
                Id = dto.Id ?? $"role_{dto.Name.ToLower()}",
                Name = dto.Name,
                Description = dto.Description
            };

            var createdRole = await _roleRepository.CreateRoleAsync(role, cancellationToken);

            var roleDto = new RoleDto
            {
                Id = createdRole.Id,
                Name = createdRole.Name,
                Description = createdRole.Description,
                CreatedAt = createdRole.CreatedAt,
                UpdatedAt = createdRole.UpdatedAt
            };

            return CreatedAtAction(
                nameof(GetRoleById),
                new { id = createdRole.Id },
                new ApiResponse<RoleDto>
                {
                    Success = true,
                    Message = "Role created successfully",
                    Data = roleDto
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role: {RoleName}", dto.Name);
            return StatusCode(500, new ApiResponse<RoleDto>
            {
                Success = false,
                Message = "Failed to create role"
            });
        }
    }

    /// <summary>
    /// 更新角色
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> UpdateRole(
        string id,
        [FromBody] UpdateRoleDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<RoleDto>
            {
                Success = false,
                Message = "Validation failed",
                Errors = errors
            });
        }

        try
        {
            var existingRole = await _roleRepository.GetRoleByIdAsync(id, cancellationToken);
            if (existingRole == null)
            {
                return NotFound(new ApiResponse<RoleDto>
                {
                    Success = false,
                    Message = $"Role with ID '{id}' not found"
                });
            }

            // Update properties
            if (!string.IsNullOrWhiteSpace(dto.Name))
                existingRole.Name = dto.Name;
            if (!string.IsNullOrWhiteSpace(dto.Description))
                existingRole.Description = dto.Description;

            var updatedRole = await _roleRepository.UpdateRoleAsync(existingRole, cancellationToken);

            var roleDto = new RoleDto
            {
                Id = updatedRole.Id,
                Name = updatedRole.Name,
                Description = updatedRole.Description,
                CreatedAt = updatedRole.CreatedAt,
                UpdatedAt = updatedRole.UpdatedAt
            };

            return Ok(new ApiResponse<RoleDto>
            {
                Success = true,
                Message = "Role updated successfully",
                Data = roleDto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role: {RoleId}", id);
            return StatusCode(500, new ApiResponse<RoleDto>
            {
                Success = false,
                Message = "Failed to update role"
            });
        }
    }

    /// <summary>
    /// 删除角色
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteRole(
        string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Prevent deletion of default roles
            if (id == "role_user" || id == "role_admin")
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Cannot delete default roles"
                });
            }

            var exists = await _roleRepository.RoleExistsAsync(id, cancellationToken);
            if (!exists)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Role with ID '{id}' not found"
                });
            }

            var deleted = await _roleRepository.DeleteRoleAsync(id, cancellationToken);
            
            if (deleted)
            {
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Role deleted successfully"
                });
            }

            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to delete role"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role: {RoleId}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to delete role"
            });
        }
    }
}

// DTOs
public class RoleDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateRoleDto
{
    public string? Id { get; set; }
    
    [Required(ErrorMessage = "角色名称不能为空")]
    [MaxLength(50, ErrorMessage = "角色名称最多50个字符")]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500, ErrorMessage = "描述最多500个字符")]
    public string? Description { get; set; }
}

public class UpdateRoleDto
{
    [MaxLength(50, ErrorMessage = "角色名称最多50个字符")]
    public string? Name { get; set; }
    
    [MaxLength(500, ErrorMessage = "描述最多500个字符")]
    public string? Description { get; set; }
}
