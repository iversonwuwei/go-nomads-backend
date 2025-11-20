using CityService.Application.DTOs;
using CityService.Domain.Entities;
using CityService.Domain.Repositories;
using Dapr.Client;
using GoNomads.Shared.Models;
using GoNomads.Shared.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CityService.API.Controllers;

[ApiController]
[Route("api/v1/cities/{cityId}/ratings")]
[Produces("application/json")]
public class CityRatingsController : ControllerBase
{
    private readonly ICityRatingCategoryRepository _categoryRepository;
    private readonly ICityRatingRepository _ratingRepository;
    private readonly DaprClient _daprClient;
    private readonly ILogger<CityRatingsController> _logger;

    public CityRatingsController(
        ICityRatingCategoryRepository categoryRepository,
        ICityRatingRepository ratingRepository,
        DaprClient daprClient,
        ILogger<CityRatingsController> logger)
    {
        _categoryRepository = categoryRepository;
        _ratingRepository = ratingRepository;
        _daprClient = daprClient;
        _logger = logger;
    }

    private Guid? GetCurrentUserId()
    {
        try
        {
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated == true && !string.IsNullOrEmpty(userContext.UserId))
            {
                if (Guid.TryParse(userContext.UserId, out var userId))
                {
                    _logger.LogInformation("✅ 获取当前用户ID: {UserId}", userId);
                    return userId;
                }
            }
            
            _logger.LogInformation("ℹ️ 未找到认证用户信息");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "获取当前用户ID失败，将返回 null");
        }

        return null;
    }

    private bool IsAdmin()
    {
        try
        {
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            return userContext?.Role == "admin";
        }
        catch
        {
            return false;
        }
    }

    private bool IsModerator(Guid cityId)
    {
        // TODO: 实现版主权限检查
        return User.IsInRole("moderator");
    }

    /// <summary>
    /// 获取城市评分信息（包含评分项和统计数据）
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<CityRatingInfoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CityRatingInfoDto>>> GetCityRatings(
        [FromRoute] Guid cityId)
    {
        try
        {
            var categories = await _categoryRepository.GetAllActiveAsync();
            var averageRatings = await _ratingRepository.GetCityAverageRatingsAsync(cityId);
            var allCityRatings = await _ratingRepository.GetCityRatingsAsync(cityId);

            var currentUserId = GetCurrentUserId();
            List<CityRating> userRatings = new();
            if (currentUserId.HasValue)
            {
                userRatings = await _ratingRepository.GetUserRatingsAsync(currentUserId.Value, cityId);
            }

            // 计算每个评分项的评分人数
            var ratingCounts = allCityRatings
                .GroupBy(r => r.CategoryId)
                .ToDictionary(g => g.Key, g => g.Count());

            var statistics = categories.Select(c =>
            {
                var userRating = userRatings.FirstOrDefault(r => r.CategoryId == c.Id);
                return new CityRatingStatisticsDto
                {
                    CategoryId = c.Id,
                    CategoryName = c.Name,
                    CategoryNameEn = c.NameEn,
                    Icon = c.Icon,
                    DisplayOrder = c.DisplayOrder,
                    RatingCount = ratingCounts.GetValueOrDefault(c.Id, 0),
                    AverageRating = averageRatings.GetValueOrDefault(c.Id, 0),
                    UserRating = userRating?.Rating
                };
            }).ToList();

            // 计算城市总得分（所有评分项的加权平均）
            var overallScore = statistics.Any(s => s.RatingCount > 0)
                ? Math.Round(statistics.Where(s => s.RatingCount > 0).Average(s => s.AverageRating), 1)
                : 0.0;

            var result = new CityRatingInfoDto
            {
                Categories = categories.Select(c => new CityRatingCategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    NameEn = c.NameEn,
                    Description = c.Description,
                    Icon = c.Icon,
                    IsDefault = c.IsDefault,
                    DisplayOrder = c.DisplayOrder,
                    CreatedBy = c.CreatedBy,
                    CreatedAt = c.CreatedAt
                }).ToList(),
                Statistics = statistics,
                OverallScore = overallScore
            };

            return Ok(new ApiResponse<CityRatingInfoDto>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取城市评分信息失败: CityId={CityId}", cityId);
            return StatusCode(500, new ApiResponse<CityRatingInfoDto>
            {
                Success = false,
                Message = "获取城市评分信息失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// 提交或更新评分
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CityRatingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CityRatingDto>>> SubmitRating(
        [FromRoute] Guid cityId,
        [FromBody] SubmitCityRatingDto request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new ApiResponse<CityRatingDto>
                {
                    Success = false,
                    Message = "用户未登录",
                    Errors = new List<string> { "请先登录" }
                });

            // 检查评分项是否存在
            var category = await _categoryRepository.GetByIdAsync(request.CategoryId);
            if (category == null)
                return NotFound(new ApiResponse<CityRatingDto>
                {
                    Success = false,
                    Message = "评分项不存在",
                    Errors = new List<string> { $"未找到ID为 {request.CategoryId} 的评分项" }
                });

            // 检查用户是否已经评过分
            var existingRating = await _ratingRepository.GetUserRatingAsync(
                cityId, userId.Value, request.CategoryId);

            CityRating rating;
            if (existingRating != null)
            {
                // 更新评分
                existingRating.UpdateRating(request.Rating);
                rating = await _ratingRepository.UpdateAsync(existingRating);
                _logger.LogInformation("✅ 用户更新评分: UserId={UserId}, CityId={CityId}, CategoryId={CategoryId}, Rating={Rating}",
                    userId.Value, cityId, request.CategoryId, request.Rating);
            }
            else
            {
                // 创建新评分
                rating = CityRating.Create(cityId, userId.Value, request.CategoryId, request.Rating);
                rating = await _ratingRepository.CreateAsync(rating);
                _logger.LogInformation("✅ 用户创建评分: UserId={UserId}, CityId={CityId}, CategoryId={CategoryId}, Rating={Rating}",
                    userId.Value, cityId, request.CategoryId, request.Rating);
            }

            var result = new CityRatingDto
            {
                Id = rating.Id,
                CityId = rating.CityId,
                UserId = rating.UserId,
                CategoryId = rating.CategoryId,
                Rating = rating.Rating,
                CreatedAt = rating.CreatedAt,
                UpdatedAt = rating.UpdatedAt
            };

            // 重新计算并更新城市评分缓存
            await UpdateCityScoreCacheAsync(cityId);

            return Ok(new ApiResponse<CityRatingDto>
            {
                Success = true,
                Data = result,
                Message = "评分提交成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提交评分失败: CityId={CityId}", cityId);
            return StatusCode(500, new ApiResponse<CityRatingDto>
            {
                Success = false,
                Message = "提交评分失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// 获取所有评分项
    /// </summary>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(ApiResponse<List<CityRatingCategoryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<CityRatingCategoryDto>>>> GetCategories()
    {
        try
        {
            var categories = await _categoryRepository.GetAllActiveAsync();
            var result = categories.Select(c => new CityRatingCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                NameEn = c.NameEn,
                Description = c.Description,
                Icon = c.Icon,
                IsDefault = c.IsDefault,
                DisplayOrder = c.DisplayOrder,
                CreatedBy = c.CreatedBy,
                CreatedAt = c.CreatedAt
            }).ToList();

            return Ok(new ApiResponse<List<CityRatingCategoryDto>>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取评分项列表失败");
            return StatusCode(500, new ApiResponse<List<CityRatingCategoryDto>>
            {
                Success = false,
                Message = "获取评分项列表失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// 创建自定义评分项（所有登录用户可用）
    /// </summary>
    [Authorize]
    [HttpPost("categories")]
    [ProducesResponseType(typeof(ApiResponse<CityRatingCategoryDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<CityRatingCategoryDto>>> CreateCategory(
        [FromBody] CreateCityRatingCategoryDto request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new ApiResponse<CityRatingCategoryDto>
                {
                    Success = false,
                    Message = "用户未登录",
                    Errors = new List<string> { "请先登录" }
                });

            var category = CityRatingCategory.Create(
                request.Name,
                request.NameEn,
                request.Description,
                request.Icon,
                userId.Value,
                request.DisplayOrder);

            category = await _categoryRepository.CreateAsync(category);

            var result = new CityRatingCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                NameEn = category.NameEn,
                Description = category.Description,
                Icon = category.Icon,
                IsDefault = category.IsDefault,
                DisplayOrder = category.DisplayOrder,
                CreatedBy = category.CreatedBy,
                CreatedAt = category.CreatedAt
            };

            return CreatedAtAction(
                nameof(GetCategories),
                new { },
                new ApiResponse<CityRatingCategoryDto>
                {
                    Success = true,
                    Data = result,
                    Message = "评分项创建成功"
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建评分项失败");
            return StatusCode(500, new ApiResponse<CityRatingCategoryDto>
            {
                Success = false,
                Message = "创建评分项失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// 更新评分项（仅管理员和版主）
    /// </summary>
    [Authorize]
    [HttpPut("categories/{categoryId}")]
    [ProducesResponseType(typeof(ApiResponse<CityRatingCategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CityRatingCategoryDto>>> UpdateCategory(
        [FromRoute] Guid cityId,
        [FromRoute] Guid categoryId,
        [FromBody] UpdateCityRatingCategoryDto request)
    {
        try
        {
            if (!IsAdmin() && !IsModerator(cityId))
                return Forbid();

            var category = await _categoryRepository.GetByIdAsync(categoryId);
            if (category == null)
                return NotFound(new ApiResponse<CityRatingCategoryDto>
                {
                    Success = false,
                    Message = "评分项不存在",
                    Errors = new List<string> { $"未找到ID为 {categoryId} 的评分项" }
                });

            category.Update(request.Name, request.NameEn, request.Description, request.Icon);
            category = await _categoryRepository.UpdateAsync(category);

            var result = new CityRatingCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                NameEn = category.NameEn,
                Description = category.Description,
                Icon = category.Icon,
                IsDefault = category.IsDefault,
                DisplayOrder = category.DisplayOrder,
                CreatedBy = category.CreatedBy,
                CreatedAt = category.CreatedAt
            };

            return Ok(new ApiResponse<CityRatingCategoryDto>
            {
                Success = true,
                Data = result,
                Message = "评分项更新成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新评分项失败: CategoryId={CategoryId}", categoryId);
            return StatusCode(500, new ApiResponse<CityRatingCategoryDto>
            {
                Success = false,
                Message = "更新评分项失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// 删除评分项（仅管理员和版主）
    /// </summary>
    [Authorize]
    [HttpDelete("categories/{categoryId}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteCategory(
        [FromRoute] Guid cityId,
        [FromRoute] Guid categoryId)
    {
        try
        {
            if (!IsAdmin() && !IsModerator(cityId))
                return Forbid();

            var category = await _categoryRepository.GetByIdAsync(categoryId);
            if (category == null)
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "评分项不存在",
                    Errors = new List<string> { $"未找到ID为 {categoryId} 的评分项" }
                });

            // 默认评分项不能删除
            if (category.IsDefault)
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "不能删除默认评分项",
                    Errors = new List<string> { "默认评分项不能删除" }
                });

            category.SoftDelete();
            await _categoryRepository.UpdateAsync(category);

            _logger.LogInformation("✅ 评分项删除成功: CategoryId={CategoryId}", categoryId);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = null,
                Message = "评分项删除成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除评分项失败: CategoryId={CategoryId}", categoryId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "删除评分项失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// 使城市评分缓存失效 (调用 CacheService)
    /// </summary>
    /// <summary>
    /// 重新计算并更新城市评分缓存
    /// </summary>
    private async Task UpdateCityScoreCacheAsync(Guid cityId)
    {
        try
        {
            _logger.LogInformation("重新计算城市评分: CityId={CityId}", cityId);

            // 1. 获取所有评分项和统计数据
            var categories = await _categoryRepository.GetAllActiveAsync();
            var averageRatings = await _ratingRepository.GetCityAverageRatingsAsync(cityId);
            var allCityRatings = await _ratingRepository.GetCityRatingsAsync(cityId);

            // 2. 计算每个评分项的统计数据
            var ratingCounts = allCityRatings
                .GroupBy(r => r.CategoryId)
                .ToDictionary(g => g.Key, g => g.Count());

            var statistics = categories.Select(c => new
            {
                CategoryId = c.Id,
                CategoryName = c.Name,
                CategoryNameEn = c.NameEn,
                RatingCount = ratingCounts.GetValueOrDefault(c.Id, 0),
                AverageRating = averageRatings.GetValueOrDefault(c.Id, 0)
            }).ToList();

            // 3. 计算总评分
            var overallScore = statistics.Any(s => s.RatingCount > 0)
                ? Math.Round(statistics.Where(s => s.RatingCount > 0).Average(s => s.AverageRating), 1)
                : 0.0;

            // 4. 通过 Dapr 调用 CacheService 保存评分
            var requestBody = new
            {
                overallScore = overallScore,
                statistics = System.Text.Json.JsonSerializer.Serialize(statistics)
            };

            await _daprClient.InvokeMethodAsync(
                HttpMethod.Put,
                "cache-service",
                $"api/v1/cache/scores/city/{cityId}",
                requestBody
            );

            _logger.LogInformation("✅ 城市评分已更新到缓存: CityId={CityId}, OverallScore={OverallScore}",
                cityId, overallScore);
        }
        catch (Exception ex)
        {
            // 缓存更新失败不影响主流程,只记录日志
            _logger.LogWarning(ex, "更新城市评分缓存时发生错误: CityId={CityId}", cityId);
        }
    }
}
