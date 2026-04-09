using GoNomads.Shared.DTOs;
using GoNomads.Shared.Services;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs;
using UserService.Application.Services;

namespace UserService.API.Controllers;

[ApiController]
[Route("api/v1/admin/legal")]
public class AdminLegalDocumentsController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly ILegalDocumentService _legalDocumentService;
    private readonly ILogger<AdminLegalDocumentsController> _logger;

    public AdminLegalDocumentsController(
        ICurrentUserService currentUser,
        ILegalDocumentService legalDocumentService,
        ILogger<AdminLegalDocumentsController> logger)
    {
        _currentUser = currentUser;
        _legalDocumentService = legalDocumentService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<AdminLegalDocumentListItemDto>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? documentType = null,
        [FromQuery] string? language = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_currentUser.IsAdmin())
                return Forbid();

            var result = await _legalDocumentService.GetAdminDocumentsAsync(
                page,
                pageSize,
                documentType,
                language,
                search,
                cancellationToken);

            return Ok(new ApiResponse<PaginatedResponse<AdminLegalDocumentListItemDto>>
            {
                Success = true,
                Message = "获取法律文档成功",
                Data = new PaginatedResponse<AdminLegalDocumentListItemDto>
                {
                    Items = result.Items,
                    TotalCount = result.TotalCount,
                    Page = page,
                    PageSize = pageSize
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取法律文档列表失败");
            return StatusCode(500, ApiResponse<PaginatedResponse<AdminLegalDocumentListItemDto>>.ErrorResponse("获取法律文档失败"));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<AdminLegalDocumentDto>>> GetById(
        string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_currentUser.IsAdmin())
                return Forbid();

            var document = await _legalDocumentService.GetAdminDocumentByIdAsync(id, cancellationToken);
            if (document == null)
                return NotFound(ApiResponse<AdminLegalDocumentDto>.ErrorResponse("法律文档不存在"));

            return Ok(ApiResponse<AdminLegalDocumentDto>.SuccessResponse(document));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取法律文档详情失败: {Id}", id);
            return StatusCode(500, ApiResponse<AdminLegalDocumentDto>.ErrorResponse("获取法律文档详情失败"));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<AdminLegalDocumentDto>>> Create(
        [FromBody] CreateAdminLegalDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_currentUser.IsAdmin())
                return Forbid();

            var created = await _legalDocumentService.CreateAdminDocumentAsync(
                request,
                _currentUser.GetUserId(),
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = created.Id },
                ApiResponse<AdminLegalDocumentDto>.SuccessResponse(created, "法律文档创建成功"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<AdminLegalDocumentDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建法律文档失败");
            return StatusCode(500, ApiResponse<AdminLegalDocumentDto>.ErrorResponse("创建法律文档失败"));
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<AdminLegalDocumentDto>>> Update(
        string id,
        [FromBody] UpdateAdminLegalDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_currentUser.IsAdmin())
                return Forbid();

            var updated = await _legalDocumentService.UpdateAdminDocumentAsync(
                id,
                request,
                _currentUser.GetUserId(),
                cancellationToken);

            if (updated == null)
                return NotFound(ApiResponse<AdminLegalDocumentDto>.ErrorResponse("法律文档不存在"));

            return Ok(ApiResponse<AdminLegalDocumentDto>.SuccessResponse(updated, "法律文档更新成功"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<AdminLegalDocumentDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新法律文档失败: {Id}", id);
            return StatusCode(500, ApiResponse<AdminLegalDocumentDto>.ErrorResponse("更新法律文档失败"));
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_currentUser.IsAdmin())
                return Forbid();

            var deleted = await _legalDocumentService.DeleteAdminDocumentAsync(id, cancellationToken);
            if (!deleted)
                return NotFound(ApiResponse<bool>.ErrorResponse("法律文档不存在"));

            return Ok(ApiResponse<bool>.SuccessResponse(true, "法律文档删除成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除法律文档失败: {Id}", id);
            return StatusCode(500, ApiResponse<bool>.ErrorResponse("删除法律文档失败"));
        }
    }
}