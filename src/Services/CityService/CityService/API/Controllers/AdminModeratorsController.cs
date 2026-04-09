using CityService.Domain.Entities;
using CityService.Domain.Repositories;
using GoNomads.Shared.DTOs;
using GoNomads.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Postgrest;
using Client = Supabase.Client;

namespace CityService.API.Controllers;

[ApiController]
[Route("api/v1/admin/moderators")]
[Authorize]
public class AdminModeratorsController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly ICityModeratorRepository _moderatorRepository;
    private readonly Client _supabase;
    private readonly ILogger<AdminModeratorsController> _logger;

    public AdminModeratorsController(
        ICurrentUserService currentUser,
        ICityModeratorRepository moderatorRepository,
        Client supabase,
        ILogger<AdminModeratorsController> logger)
    {
        _currentUser = currentUser;
        _moderatorRepository = moderatorRepository;
        _supabase = supabase;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<CityModerator>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (!_currentUser.IsAdmin()) return Forbid();

            var from = (page - 1) * pageSize;
            var to = from + pageSize - 1;

            var countResponse = await _supabase.From<CityModerator>()
                .Filter("is_active", Constants.Operator.Equals, "true")
                .Get();
            var totalCount = countResponse.Models.Count;

            var response = await _supabase.From<CityModerator>()
                .Filter("is_active", Constants.Operator.Equals, "true")
                .Order("created_at", Constants.Ordering.Descending)
                .Range(from, to)
                .Get();

            return Ok(new ApiResponse<PaginatedResponse<CityModerator>>
            {
                Success = true,
                Message = "获取版主列表成功",
                Data = new PaginatedResponse<CityModerator>
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
            _logger.LogError(ex, "获取版主列表失败");
            return StatusCode(500, ApiResponse<PaginatedResponse<CityModerator>>.ErrorResponse("获取版主列表失败"));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Remove(Guid id)
    {
        try
        {
            if (!_currentUser.IsAdmin()) return Forbid();

            var moderator = await _moderatorRepository.GetByIdAsync(id);
            if (moderator == null)
                return NotFound(ApiResponse<bool>.ErrorResponse("版主不存在"));

            await _moderatorRepository.RemoveAsync(moderator.CityId, moderator.UserId);

            _logger.LogInformation("管理员移除版主: Id={Id}, CityId={CityId}, UserId={UserId}",
                id, moderator.CityId, moderator.UserId);
            return Ok(ApiResponse<bool>.SuccessResponse(true, "版主已移除"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "移除版主失败: Id={Id}", id);
            return StatusCode(500, ApiResponse<bool>.ErrorResponse("移除版主失败"));
        }
    }
}
