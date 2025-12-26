using GoNomads.Shared.Models;
using GoNomads.Shared.Services;
using InnovationService.DTOs;
using InnovationService.Models;
using InnovationService.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace InnovationService.Controllers;

/// <summary>
///     创新项目 API 控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Route("api/v1/[controller]")]
public class InnovationsController : ControllerBase
{
    private readonly IInnovationRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<InnovationsController> _logger;

    public InnovationsController(
        IInnovationRepository repository,
        ICurrentUserService currentUserService,
        ILogger<InnovationsController> logger)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    ///     获取当前用户ID
    /// </summary>
    private Guid? GetCurrentUserId()
    {
        return _currentUserService.TryGetUserId();
    }

    /// <summary>
    ///     检查用户是否已认证
    /// </summary>
    private bool IsAuthenticated => _currentUserService.IsAuthenticated;

    /// <summary>
    ///     获取创新项目列表
    /// </summary>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页数量</param>
    /// <param name="category">分类筛选</param>
    /// <param name="stage">阶段筛选</param>
    /// <param name="search">搜索关键词</param>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<InnovationListItem>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? category = null,
        [FromQuery] string? stage = null,
        [FromQuery] string? search = null)
    {
        try
        {
            _logger.LogInformation("📋 获取创新项目列表: Page={Page}, PageSize={PageSize}, Category={Category}, Stage={Stage}, Search={Search}",
                page, pageSize, category, stage, search);

            var result = await _repository.GetAllAsync(page, pageSize, category, stage, search);

            return Ok(new ApiResponse<PagedResponse<InnovationListItem>>
            {
                Success = true,
                Message = $"获取成功，共 {result.Total} 个项目",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取创新项目列表失败");
            return StatusCode(500, new ApiResponse<PagedResponse<InnovationListItem>>
            {
                Success = false,
                Message = $"获取失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    ///     获取创新项目详情
    /// </summary>
    /// <param name="id">项目ID</param>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<InnovationResponse>>> GetById(Guid id)
    {
        try
        {
            _logger.LogInformation("🔍 获取创新项目详情: Id={Id}", id);

            var userId = GetCurrentUserId();
            var result = await _repository.GetByIdAsync(id, userId);

            if (result == null)
            {
                return NotFound(new ApiResponse<InnovationResponse>
                {
                    Success = false,
                    Message = "项目不存在"
                });
            }

            // 增加浏览次数
            await _repository.IncrementViewCountAsync(id);

            return Ok(new ApiResponse<InnovationResponse>
            {
                Success = true,
                Message = "获取成功",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取创新项目详情失败: {Id}", id);
            return StatusCode(500, new ApiResponse<InnovationResponse>
            {
                Success = false,
                Message = $"获取失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    ///     创建创新项目
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<InnovationResponse>>> Create([FromBody] CreateInnovationRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new ApiResponse<InnovationResponse>
                {
                    Success = false,
                    Message = "用户未认证"
                });
            }

            _logger.LogInformation("➕ 创建创新项目: UserId={UserId}, Title={Title}", userId, request.Title);

            var innovation = new Innovation
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description,
                ElevatorPitch = request.ElevatorPitch,
                Problem = request.Problem,
                Solution = request.Solution,
                TargetAudience = request.TargetAudience,
                ProductType = request.ProductType,
                KeyFeatures = request.KeyFeatures,
                CompetitiveAdvantage = request.CompetitiveAdvantage,
                BusinessModel = request.BusinessModel,
                MarketOpportunity = request.MarketOpportunity,
                Ask = request.Ask,
                CreatorId = userId.Value,
                Category = request.Category,
                Stage = request.Stage,
                Tags = request.Tags,
                ImageUrl = request.ImageUrl,
                Images = request.Images,
                VideoUrl = request.VideoUrl,
                DemoUrl = request.DemoUrl,
                GithubUrl = request.GithubUrl,
                WebsiteUrl = request.WebsiteUrl,
                LookingFor = request.LookingFor,
                SkillsNeeded = request.SkillsNeeded,
                IsPublic = request.IsPublic,
                CreatedBy = userId.Value,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _repository.CreateAsync(innovation);

            // 添加团队成员
            if (request.Team != null && request.Team.Any())
            {
                foreach (var member in request.Team)
                {
                    await _repository.AddTeamMemberAsync(new InnovationTeamMember
                    {
                        Id = Guid.NewGuid(),
                        InnovationId = created.Id,
                        UserId = member.UserId,
                        Name = member.Name,
                        Role = member.Role,
                        Description = member.Description,
                        AvatarUrl = member.AvatarUrl,
                        IsFounder = member.IsFounder,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            // 获取完整的响应
            var response = await _repository.GetByIdAsync(created.Id, userId);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, new ApiResponse<InnovationResponse>
            {
                Success = true,
                Message = "创建成功",
                Data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建创新项目失败");
            return StatusCode(500, new ApiResponse<InnovationResponse>
            {
                Success = false,
                Message = $"创建失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    ///     更新创新项目
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<InnovationResponse>>> Update(Guid id, [FromBody] UpdateInnovationRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new ApiResponse<InnovationResponse>
                {
                    Success = false,
                    Message = "用户未认证"
                });
            }

            _logger.LogInformation("✏️ 更新创新项目: Id={Id}, UserId={UserId}", id, userId);

            var updated = await _repository.UpdateAsync(id, request, userId.Value);

            if (updated == null)
            {
                return NotFound(new ApiResponse<InnovationResponse>
                {
                    Success = false,
                    Message = "项目不存在或无权限修改"
                });
            }

            var response = await _repository.GetByIdAsync(id, userId);

            return Ok(new ApiResponse<InnovationResponse>
            {
                Success = true,
                Message = "更新成功",
                Data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新创新项目失败: {Id}", id);
            return StatusCode(500, new ApiResponse<InnovationResponse>
            {
                Success = false,
                Message = $"更新失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    ///     删除创新项目
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = "用户未认证"
                });
            }

            _logger.LogInformation("🗑️ 删除创新项目: Id={Id}, UserId={UserId}", id, userId);

            var deleted = await _repository.DeleteAsync(id, userId.Value);

            if (!deleted)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "项目不存在或无权限删除"
                });
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "删除成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除创新项目失败: {Id}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = $"删除失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    ///     获取用户的创新项目
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<ApiResponse<List<InnovationListItem>>>> GetByUser(
        Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("📋 获取用户创新项目: UserId={UserId}", userId);

            var result = await _repository.GetByUserIdAsync(userId, page, pageSize);

            return Ok(new ApiResponse<List<InnovationListItem>>
            {
                Success = true,
                Message = $"获取成功，共 {result.Count} 个项目",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取用户创新项目失败: {UserId}", userId);
            return StatusCode(500, new ApiResponse<List<InnovationListItem>>
            {
                Success = false,
                Message = $"获取失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    ///     获取我的创新项目
    /// </summary>
    [HttpGet("my")]
    public async Task<ActionResult<ApiResponse<List<InnovationListItem>>>> GetMy(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new ApiResponse<List<InnovationListItem>>
                {
                    Success = false,
                    Message = "用户未认证"
                });
            }

            _logger.LogInformation("📋 获取我的创新项目: UserId={UserId}", userId);

            var result = await _repository.GetByUserIdAsync(userId.Value, page, pageSize);

            return Ok(new ApiResponse<List<InnovationListItem>>
            {
                Success = true,
                Message = $"获取成功，共 {result.Count} 个项目",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取我的创新项目失败");
            return StatusCode(500, new ApiResponse<List<InnovationListItem>>
            {
                Success = false,
                Message = $"获取失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    ///     获取精选项目
    /// </summary>
    [HttpGet("featured")]
    public async Task<ActionResult<ApiResponse<List<InnovationListItem>>>> GetFeatured([FromQuery] int limit = 10)
    {
        try
        {
            _logger.LogInformation("⭐ 获取精选创新项目: Limit={Limit}", limit);

            var result = await _repository.GetFeaturedAsync(limit);

            return Ok(new ApiResponse<List<InnovationListItem>>
            {
                Success = true,
                Message = $"获取成功，共 {result.Count} 个精选项目",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取精选创新项目失败");
            return StatusCode(500, new ApiResponse<List<InnovationListItem>>
            {
                Success = false,
                Message = $"获取失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    ///     获取热门项目
    /// </summary>
    [HttpGet("popular")]
    public async Task<ActionResult<ApiResponse<List<InnovationListItem>>>> GetPopular([FromQuery] int limit = 10)
    {
        try
        {
            _logger.LogInformation("🔥 获取热门创新项目: Limit={Limit}", limit);

            var result = await _repository.GetPopularAsync(limit);

            return Ok(new ApiResponse<List<InnovationListItem>>
            {
                Success = true,
                Message = $"获取成功，共 {result.Count} 个热门项目",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取热门创新项目失败");
            return StatusCode(500, new ApiResponse<List<InnovationListItem>>
            {
                Success = false,
                Message = $"获取失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    ///     点赞/取消点赞
    /// </summary>
    [HttpPost("{id:guid}/like")]
    public async Task<ActionResult<ApiResponse<object>>> ToggleLike(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = "用户未认证"
                });
            }

            _logger.LogInformation("❤️ 切换点赞状态: InnovationId={Id}, UserId={UserId}", id, userId);

            var isLiked = await _repository.ToggleLikeAsync(id, userId.Value);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = isLiked ? "点赞成功" : "已取消点赞",
                Data = new { isLiked }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 切换点赞状态失败: {Id}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = $"操作失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    ///     获取项目评论
    /// </summary>
    [HttpGet("{id:guid}/comments")]
    public async Task<ActionResult<ApiResponse<List<CommentResponse>>>> GetComments(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("💬 获取项目评论: InnovationId={Id}", id);

            var result = await _repository.GetCommentsAsync(id, page, pageSize);

            return Ok(new ApiResponse<List<CommentResponse>>
            {
                Success = true,
                Message = $"获取成功，共 {result.Count} 条评论",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取项目评论失败: {Id}", id);
            return StatusCode(500, new ApiResponse<List<CommentResponse>>
            {
                Success = false,
                Message = $"获取失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    ///     添加评论
    /// </summary>
    [HttpPost("{id:guid}/comments")]
    public async Task<ActionResult<ApiResponse<CommentResponse>>> AddComment(Guid id, [FromBody] CreateCommentRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new ApiResponse<CommentResponse>
                {
                    Success = false,
                    Message = "用户未认证"
                });
            }

            _logger.LogInformation("💬 添加评论: InnovationId={Id}, UserId={UserId}", id, userId);

            var comment = new InnovationComment
            {
                Id = Guid.NewGuid(),
                InnovationId = id,
                UserId = userId.Value,
                Content = request.Content,
                ParentId = request.ParentId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _repository.AddCommentAsync(comment);

            var response = new CommentResponse
            {
                Id = created.Id,
                InnovationId = created.InnovationId,
                UserId = created.UserId,
                Content = created.Content,
                ParentId = created.ParentId,
                CreatedAt = created.CreatedAt,
                UpdatedAt = created.UpdatedAt
            };

            return CreatedAtAction(nameof(GetComments), new { id }, new ApiResponse<CommentResponse>
            {
                Success = true,
                Message = "评论成功",
                Data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 添加评论失败: {Id}", id);
            return StatusCode(500, new ApiResponse<CommentResponse>
            {
                Success = false,
                Message = $"评论失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    ///     删除评论
    /// </summary>
    [HttpDelete("comments/{commentId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteComment(Guid commentId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = "用户未认证"
                });
            }

            _logger.LogInformation("🗑️ 删除评论: CommentId={CommentId}, UserId={UserId}", commentId, userId);

            var deleted = await _repository.DeleteCommentAsync(commentId, userId.Value);

            if (!deleted)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "评论不存在或无权限删除"
                });
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "删除成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除评论失败: {CommentId}", commentId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = $"删除失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    ///     添加团队成员
    /// </summary>
    [HttpPost("{id:guid}/team")]
    public async Task<ActionResult<ApiResponse<TeamMemberResponse>>> AddTeamMember(Guid id, [FromBody] TeamMemberRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new ApiResponse<TeamMemberResponse>
                {
                    Success = false,
                    Message = "用户未认证"
                });
            }

            _logger.LogInformation("👥 添加团队成员: InnovationId={Id}, UserId={UserId}", id, userId);

            var member = new InnovationTeamMember
            {
                Id = Guid.NewGuid(),
                InnovationId = id,
                UserId = request.UserId,
                Name = request.Name,
                Role = request.Role,
                Description = request.Description,
                AvatarUrl = request.AvatarUrl,
                IsFounder = request.IsFounder,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _repository.AddTeamMemberAsync(member);

            var response = new TeamMemberResponse
            {
                Id = created.Id,
                UserId = created.UserId,
                Name = created.Name,
                Role = created.Role,
                Description = created.Description,
                AvatarUrl = created.AvatarUrl,
                IsFounder = created.IsFounder
            };

            return Ok(new ApiResponse<TeamMemberResponse>
            {
                Success = true,
                Message = "添加成功",
                Data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 添加团队成员失败: {Id}", id);
            return StatusCode(500, new ApiResponse<TeamMemberResponse>
            {
                Success = false,
                Message = $"添加失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    ///     移除团队成员
    /// </summary>
    [HttpDelete("{id:guid}/team/{memberId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> RemoveTeamMember(Guid id, Guid memberId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = "用户未认证"
                });
            }

            _logger.LogInformation("👥 移除团队成员: InnovationId={Id}, MemberId={MemberId}, UserId={UserId}", id, memberId, userId);

            var removed = await _repository.RemoveTeamMemberAsync(id, memberId, userId.Value);

            if (!removed)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "成员不存在或无权限移除"
                });
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "移除成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 移除团队成员失败: InnovationId={Id}, MemberId={MemberId}", id, memberId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = $"移除失败: {ex.Message}"
            });
        }
    }
}
