using AccommodationService.Domain.Entities;
using GoNomads.Shared.DTOs;
using GoNomads.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Postgrest;
using Client = Supabase.Client;

namespace AccommodationService.Controllers;

[ApiController]
[Route("api/v1/admin/hotel-reviews")]
[Authorize]
public class AdminHotelReviewsController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly Client _supabase;
    private readonly ILogger<AdminHotelReviewsController> _logger;

    public AdminHotelReviewsController(
        ICurrentUserService currentUser,
        Client supabase,
        ILogger<AdminHotelReviewsController> logger)
    {
        _currentUser = currentUser;
        _supabase = supabase;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<HotelReview>>>> GetAll(
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

            var countResponse = await _supabase.From<HotelReview>().Get();
            var totalCount = countResponse.Models.Count;

            var response = await _supabase.From<HotelReview>()
                .Order("created_at", Constants.Ordering.Descending)
                .Range(from, to)
                .Get();

            return Ok(new ApiResponse<PaginatedResponse<HotelReview>>
            {
                Success = true,
                Message = "获取酒店评论列表成功",
                Data = new PaginatedResponse<HotelReview>
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
            _logger.LogError(ex, "获取酒店评论列表失败");
            return StatusCode(500, ApiResponse<PaginatedResponse<HotelReview>>.ErrorResponse("获取酒店评论列表失败"));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        try
        {
            if (!_currentUser.IsAdmin()) return Forbid();

            await _supabase.From<HotelReview>()
                .Where(r => r.Id == id)
                .Delete();

            _logger.LogInformation("管理员删除酒店评论: Id={Id}", id);
            return Ok(ApiResponse<bool>.SuccessResponse(true, "删除成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除酒店评论失败: Id={Id}", id);
            return StatusCode(500, ApiResponse<bool>.ErrorResponse("删除酒店评论失败"));
        }
    }
}
