using CityService.Domain.Entities;
using GoNomads.Shared.DTOs;
using GoNomads.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Postgrest;
using Client = Supabase.Client;

namespace CityService.API.Controllers;

[ApiController]
[Route("api/v1/admin/pros-cons")]
[Authorize]
public class AdminProsConsController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly Client _supabase;
    private readonly ILogger<AdminProsConsController> _logger;

    public AdminProsConsController(
        ICurrentUserService currentUser,
        Client supabase,
        ILogger<AdminProsConsController> logger)
    {
        _currentUser = currentUser;
        _supabase = supabase;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<CityProsCons>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (!_currentUser.IsAdmin()) return Forbid();

            var from = (page - 1) * pageSize;
            var to = from + pageSize - 1;

            var countResponse = await _supabase.From<CityProsCons>()
                .Filter("is_deleted", Constants.Operator.Equals, "false")
                .Get();
            var totalCount = countResponse.Models.Count;

            var response = await _supabase.From<CityProsCons>()
                .Filter("is_deleted", Constants.Operator.Equals, "false")
                .Order("created_at", Constants.Ordering.Descending)
                .Range(from, to)
                .Get();

            return Ok(new ApiResponse<PaginatedResponse<CityProsCons>>
            {
                Success = true,
                Message = "获取优缺点列表成功",
                Data = new PaginatedResponse<CityProsCons>
                {
                    Items = response.Models,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取优缺点列表失败");
            return StatusCode(500, ApiResponse<PaginatedResponse<CityProsCons>>.ErrorResponse("获取优缺点列表失败"));
        }
    }

    [HttpPut("{id:guid}/hide")]
    public async Task<ActionResult<ApiResponse<bool>>> Hide(Guid id)
    {
        try
        {
            if (!_currentUser.IsAdmin()) return Forbid();

            var existing = await _supabase.From<CityProsCons>()
                .Where(p => p.Id == id)
                .Single();

            if (existing == null)
                return NotFound(ApiResponse<bool>.ErrorResponse("项目不存在"));

            existing.IsDeleted = !existing.IsDeleted;
            existing.UpdatedAt = DateTime.UtcNow;

            await _supabase.From<CityProsCons>()
                .Where(p => p.Id == id)
                .Update(existing);

            _logger.LogInformation("管理员切换优缺点隐藏状态: Id={Id}, IsDeleted={IsDeleted}", id, existing.IsDeleted);
            return Ok(ApiResponse<bool>.SuccessResponse(true, existing.IsDeleted ? "已隐藏" : "已显示"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换优缺点隐藏状态失败: Id={Id}", id);
            return StatusCode(500, ApiResponse<bool>.ErrorResponse("操作失败"));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        try
        {
            if (!_currentUser.IsAdmin()) return Forbid();

            await _supabase.From<CityProsCons>()
                .Where(p => p.Id == id)
                .Delete();

            _logger.LogInformation("管理员删除优缺点: Id={Id}", id);
            return Ok(ApiResponse<bool>.SuccessResponse(true, "删除成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除优缺点失败: Id={Id}", id);
            return StatusCode(500, ApiResponse<bool>.ErrorResponse("删除失败"));
        }
    }
}
