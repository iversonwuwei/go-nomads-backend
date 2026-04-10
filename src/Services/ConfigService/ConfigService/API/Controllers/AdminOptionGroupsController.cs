using ConfigService.Application.DTOs;
using ConfigService.Application.Services;
using GoNomads.Shared.DTOs;
using GoNomads.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConfigService.API.Controllers;

[ApiController]
[Route("api/v1/admin/option-groups")]
public class AdminOptionGroupsController : ControllerBase
{
    private readonly IOptionGroupService _optionGroupService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<AdminOptionGroupsController> _logger;

    public AdminOptionGroupsController(
        IOptionGroupService optionGroupService,
        ICurrentUserService currentUser,
        ILogger<AdminOptionGroupsController> logger)
    {
        _optionGroupService = optionGroupService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<OptionGroupDto>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            if (!_currentUser.IsAdmin())
                return Forbid();

            var (groups, totalCount) = await _optionGroupService.GetAllAsync(page, pageSize);
            return Ok(new ApiResponse<PaginatedResponse<OptionGroupDto>>
            {
                Success = true,
                Data = new PaginatedResponse<OptionGroupDto>
                {
                    Items = groups.ToList(),
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取选项分组列表失败");
            return StatusCode(500, ApiResponse<PaginatedResponse<OptionGroupDto>>.ErrorResponse("获取选项分组失败"));
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<OptionGroupDto>>> GetById(Guid id)
    {
        try
        {
            if (!_currentUser.IsAdmin())
                return Forbid();

            var group = await _optionGroupService.GetByIdAsync(id);
            if (group == null)
                return NotFound(ApiResponse<OptionGroupDto>.ErrorResponse("选项分组不存在"));

            return Ok(ApiResponse<OptionGroupDto>.SuccessResponse(group, "获取选项分组详情成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取选项分组详情失败: {Id}", id);
            return StatusCode(500, ApiResponse<OptionGroupDto>.ErrorResponse("获取选项分组详情失败"));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<OptionGroupDto>>> Create([FromBody] CreateOptionGroupDto dto)
    {
        try
        {
            if (!_currentUser.IsAdmin())
                return Forbid();

            var userId = _currentUser.GetUserId();
            var created = await _optionGroupService.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(GetAll), ApiResponse<OptionGroupDto>.SuccessResponse(created, "选项分组创建成功"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<OptionGroupDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建选项分组失败");
            return StatusCode(500, ApiResponse<OptionGroupDto>.ErrorResponse("创建选项分组失败"));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<OptionGroupDto>>> Update(Guid id, [FromBody] UpdateOptionGroupDto dto)
    {
        try
        {
            if (!_currentUser.IsAdmin())
                return Forbid();

            var userId = _currentUser.GetUserId();
            var updated = await _optionGroupService.UpdateAsync(id, dto, userId);
            if (updated == null)
                return NotFound(ApiResponse<OptionGroupDto>.ErrorResponse("选项分组不存在"));

            return Ok(ApiResponse<OptionGroupDto>.SuccessResponse(updated, "选项分组更新成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新选项分组失败: {Id}", id);
            return StatusCode(500, ApiResponse<OptionGroupDto>.ErrorResponse("更新选项分组失败"));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        try
        {
            if (!_currentUser.IsAdmin())
                return Forbid();

            var result = await _optionGroupService.DeleteAsync(id);
            if (!result)
                return NotFound(ApiResponse<bool>.ErrorResponse("选项分组不存在"));

            return Ok(ApiResponse<bool>.SuccessResponse(true, "选项分组删除成功"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除选项分组失败: {Id}", id);
            return StatusCode(500, ApiResponse<bool>.ErrorResponse("删除选项分组失败"));
        }
    }

    // ── 选项明细 ──

    [HttpGet("{groupId:guid}/items")]
    public async Task<ActionResult<ApiResponse<IEnumerable<OptionItemDto>>>> GetItems(Guid groupId)
    {
        try
        {
            if (!_currentUser.IsAdmin())
                return Forbid();

            var items = await _optionGroupService.GetItemsAsync(groupId);
            return Ok(ApiResponse<IEnumerable<OptionItemDto>>.SuccessResponse(items));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取选项列表失败: GroupId={GroupId}", groupId);
            return StatusCode(500, ApiResponse<IEnumerable<OptionItemDto>>.ErrorResponse("获取选项列表失败"));
        }
    }

    [HttpPost("{groupId:guid}/items")]
    public async Task<ActionResult<ApiResponse<OptionItemDto>>> CreateItem(Guid groupId, [FromBody] CreateOptionItemDto dto)
    {
        try
        {
            if (!_currentUser.IsAdmin())
                return Forbid();

            var userId = _currentUser.GetUserId();
            var created = await _optionGroupService.CreateItemAsync(groupId, dto, userId);
            return CreatedAtAction(nameof(GetItems), new { groupId },
                ApiResponse<OptionItemDto>.SuccessResponse(created, "选项创建成功"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<OptionItemDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建选项失败");
            return StatusCode(500, ApiResponse<OptionItemDto>.ErrorResponse("创建选项失败"));
        }
    }

    [HttpPut("{groupId:guid}/items/{id:guid}")]
    public async Task<ActionResult<ApiResponse<OptionItemDto>>> UpdateItem(Guid groupId, Guid id, [FromBody] UpdateOptionItemDto dto)
    {
        try
        {
            if (!_currentUser.IsAdmin())
                return Forbid();

            var userId = _currentUser.GetUserId();
            var updated = await _optionGroupService.UpdateItemAsync(groupId, id, dto, userId);
            if (updated == null)
                return NotFound(ApiResponse<OptionItemDto>.ErrorResponse("选项不存在"));

            return Ok(ApiResponse<OptionItemDto>.SuccessResponse(updated, "选项更新成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新选项失败: {Id}", id);
            return StatusCode(500, ApiResponse<OptionItemDto>.ErrorResponse("更新选项失败"));
        }
    }

    [HttpDelete("{groupId:guid}/items/{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteItem(Guid groupId, Guid id)
    {
        try
        {
            if (!_currentUser.IsAdmin())
                return Forbid();

            var result = await _optionGroupService.DeleteItemAsync(groupId, id);
            if (!result)
                return NotFound(ApiResponse<bool>.ErrorResponse("选项不存在"));

            return Ok(ApiResponse<bool>.SuccessResponse(true, "选项删除成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除选项失败: {Id}", id);
            return StatusCode(500, ApiResponse<bool>.ErrorResponse("删除选项失败"));
        }
    }

    [HttpPost("{groupId:guid}/items/reorder")]
    public async Task<ActionResult<ApiResponse<bool>>> ReorderItems(Guid groupId, [FromBody] ReorderItemsDto dto)
    {
        try
        {
            if (!_currentUser.IsAdmin())
                return Forbid();

            var result = await _optionGroupService.ReorderItemsAsync(groupId, dto);
            return Ok(ApiResponse<bool>.SuccessResponse(result, "排序更新成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "排序选项失败: GroupId={GroupId}", groupId);
            return StatusCode(500, ApiResponse<bool>.ErrorResponse("排序更新失败"));
        }
    }

    [HttpPut("{groupId:guid}/items/{id:guid}/toggle")]
    public async Task<ActionResult<ApiResponse<bool>>> ToggleItem(Guid groupId, Guid id)
    {
        try
        {
            if (!_currentUser.IsAdmin())
                return Forbid();

            var result = await _optionGroupService.ToggleItemAsync(groupId, id);
            if (!result)
                return NotFound(ApiResponse<bool>.ErrorResponse("选项不存在"));

            return Ok(ApiResponse<bool>.SuccessResponse(true, "启用/禁用切换成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换选项状态失败: {Id}", id);
            return StatusCode(500, ApiResponse<bool>.ErrorResponse("切换选项状态失败"));
        }
    }

    [HttpPost("{id:guid}/toggle")]
    public async Task<ActionResult<ApiResponse<OptionGroupDto>>> ToggleGroup(Guid id)
    {
        try
        {
            if (!_currentUser.IsAdmin())
                return Forbid();

            var result = await _optionGroupService.ToggleGroupAsync(id);
            if (result == null)
                return NotFound(ApiResponse<OptionGroupDto>.ErrorResponse("选项分组不存在"));

            return Ok(ApiResponse<OptionGroupDto>.SuccessResponse(result, "分组状态切换成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换分组状态失败: {Id}", id);
            return StatusCode(500, ApiResponse<OptionGroupDto>.ErrorResponse("切换分组状态失败"));
        }
    }
}
