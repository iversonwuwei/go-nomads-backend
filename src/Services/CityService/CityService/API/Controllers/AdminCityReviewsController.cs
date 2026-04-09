using CityService.Domain.Entities;
using GoNomads.Shared.DTOs;
using GoNomads.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Postgrest;
using Client = Supabase.Client;

namespace CityService.API.Controllers;

[ApiController]
[Route("api/v1/admin/city-reviews")]
[Authorize]
public class AdminCityReviewsController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly Client _supabase;
    private readonly ILogger<AdminCityReviewsController> _logger;

    public AdminCityReviewsController(
        ICurrentUserService currentUser,
        Client supabase,
        ILogger<AdminCityReviewsController> logger)
    {
        _currentUser = currentUser;
        _supabase = supabase;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<UserCityReview>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageNumber = 0,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (!_currentUser.IsAdmin()) return Forbid();

            var p = pageNumber > 0 ? pageNumber : page;
            var from = (p - 1) * pageSize;
            var to = from + pageSize - 1;

            var countResponse = await _supabase.From<UserCityReview>().Get();
            var totalCount = countResponse.Models.Count;

            var response = await _supabase.From<UserCityReview>()
                .Order("created_at", Constants.Ordering.Descending)
                .Range(from, to)
                .Get();

            return Ok(new ApiResponse<PaginatedResponse<UserCityReview>>
            {
                Success = true,
                Message = "获取城市评论列表成功",
                Data = new PaginatedResponse<UserCityReview>
                {
                    Items = response.Models,
                    TotalCount = totalCount,
                    Page = p,
                    PageSize = pageSize
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取城市评论列表失败");
            return StatusCode(500, ApiResponse<PaginatedResponse<UserCityReview>>.ErrorResponse("获取城市评论列表失败"));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        try
        {
            if (!_currentUser.IsAdmin()) return Forbid();

            await _supabase.From<UserCityReview>()
                .Where(r => r.Id == id)
                .Delete();

            _logger.LogInformation("管理员删除城市评论: Id={Id}", id);
            return Ok(ApiResponse<bool>.SuccessResponse(true, "删除成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除城市评论失败: Id={Id}", id);
            return StatusCode(500, ApiResponse<bool>.ErrorResponse("删除城市评论失败"));
        }
    }
}
