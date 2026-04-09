using ConfigService.Application.DTOs;
using ConfigService.Application.Services;
using GoNomads.Shared.DTOs;
using GoNomads.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConfigService.API.Controllers;

[ApiController]
[Route("api/v1/admin/static-texts")]
[Authorize]
public class AdminStaticTextsController : ControllerBase
{
    private readonly IStaticTextService _staticTextService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<AdminStaticTextsController> _logger;

    public AdminStaticTextsController(
        IStaticTextService staticTextService,
        ICurrentUserService currentUser,
        ILogger<AdminStaticTextsController> logger)
    {
        _staticTextService = staticTextService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<StaticTextDto>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? category = null,
        [FromQuery] string? key = null,
        [FromQuery] string? locale = null)
    {
        try
        {
            if (!_currentUser.IsAdmin())
                return Forbid();

            var items = await _staticTextService.GetAllAsync(page, pageSize, category, key, locale);
            var totalCount = await _staticTextService.GetTotalCountAsync(category, key, locale);

            return Ok(new ApiResponse<PaginatedResponse<StaticTextDto>>
            {
                Success = true,
                Message = "Static texts retrieved successfully",
                Data = new PaginatedResponse<StaticTextDto>
                {
                    Items = items.ToList(),
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取静态文本列表失败");
            return StatusCode(500, ApiResponse<PaginatedResponse<StaticTextDto>>.ErrorResponse("获取静态文本失败"));
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<StaticTextDto>>> GetById(Guid id)
    {
        try
        {
            if (!_currentUser.IsAdmin())
                return Forbid();

            var item = await _staticTextService.GetByIdAsync(id);
            if (item == null)
                return NotFound(ApiResponse<StaticTextDto>.ErrorResponse("静态文本不存在"));

            return Ok(ApiResponse<StaticTextDto>.SuccessResponse(item));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取静态文本详情失败: {Id}", id);
            return StatusCode(500, ApiResponse<StaticTextDto>.ErrorResponse("获取静态文本失败"));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<StaticTextDto>>> Create([FromBody] CreateStaticTextDto dto)
    {
        try
        {
            if (!_currentUser.IsAdmin())
                return Forbid();

            var userId = _currentUser.GetUserId();
            var created = await _staticTextService.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = created.Id },
                ApiResponse<StaticTextDto>.SuccessResponse(created, "静态文本创建成功"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<StaticTextDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建静态文本失败");
            return StatusCode(500, ApiResponse<StaticTextDto>.ErrorResponse("创建静态文本失败"));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<StaticTextDto>>> Update(Guid id, [FromBody] UpdateStaticTextDto dto)
    {
        try
        {
            if (!_currentUser.IsAdmin())
                return Forbid();

            var userId = _currentUser.GetUserId();
            var updated = await _staticTextService.UpdateAsync(id, dto, userId);
            if (updated == null)
                return NotFound(ApiResponse<StaticTextDto>.ErrorResponse("静态文本不存在"));

            return Ok(ApiResponse<StaticTextDto>.SuccessResponse(updated, "静态文本更新成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新静态文本失败: {Id}", id);
            return StatusCode(500, ApiResponse<StaticTextDto>.ErrorResponse("更新静态文本失败"));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        try
        {
            if (!_currentUser.IsAdmin())
                return Forbid();

            var result = await _staticTextService.DeleteAsync(id);
            if (!result)
                return NotFound(ApiResponse<bool>.ErrorResponse("静态文本不存在"));

            return Ok(ApiResponse<bool>.SuccessResponse(true, "静态文本删除成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除静态文本失败: {Id}", id);
            return StatusCode(500, ApiResponse<bool>.ErrorResponse("删除静态文本失败"));
        }
    }

    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<IEnumerable<string>>>> GetCategories()
    {
        try
        {
            if (!_currentUser.IsAdmin())
                return Forbid();

            var categories = await _staticTextService.GetCategoriesAsync();
            return Ok(ApiResponse<IEnumerable<string>>.SuccessResponse(categories));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取分类列表失败");
            return StatusCode(500, ApiResponse<IEnumerable<string>>.ErrorResponse("获取分类列表失败"));
        }
    }
}
