using CityService.Application.DTOs;
using CityService.Application.Services;
using CityService.Domain.Entities;
using CityService.Domain.Repositories;
using GoNomads.Shared.Communication;
using GoNomads.Shared.Models;
using GoNomads.Shared.Middleware;
using GoNomads.Shared.Services;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Messages;
using System.Security.Claims;

namespace CityService.API.Controllers;

[ApiController]
[Route("api/v1/cities/{cityId}/ratings")]
[Produces("application/json")]
public class CityRatingsController : ControllerBase
{
    private readonly ICityRatingCategoryRepository _categoryRepository;
    private readonly ICityRatingRepository _ratingRepository;
    private readonly ICityRepository _cityRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CityRatingsController> _logger;
    private readonly RatingCategorySeeder _ratingSeeder;
    private readonly ServiceInvocationClient _serviceInvocationClient;

    public CityRatingsController(
        ICityRatingCategoryRepository categoryRepository,
        ICityRatingRepository ratingRepository,
        ICityRepository cityRepository,
        ICurrentUserService currentUser,
        ServiceInvocationClient serviceInvocationClient,
        IPublishEndpoint publishEndpoint,
        ILogger<CityRatingsController> logger,
        RatingCategorySeeder ratingSeeder)
    {
        _categoryRepository = categoryRepository;
        _ratingRepository = ratingRepository;
        _cityRepository = cityRepository;
        _currentUser = currentUser;
        _serviceInvocationClient = serviceInvocationClient;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
        _ratingSeeder = ratingSeeder;
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

            var currentUserId = _currentUser.TryGetUserId();
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

            // 🔧 如果城市有评分数据,确保缓存已初始化
            if (allCityRatings.Any())
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await EnsureCityScoreCacheInitializedAsync(cityId, statistics);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "后台初始化城市评分缓存失败: CityId={CityId}", cityId);
                    }
                });
            }

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
    /// 获取城市评分统计信息 (供 CacheService 调用)
    /// GET /api/v1/cities/{cityId}/ratings/statistics
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(CityRatingStatisticsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CityRatingStatisticsResponse>> GetRatingStatistics(
        [FromRoute] string cityId)
    {
        try
        {
            // 尝试解析为 Guid,如果失败则作为字符串 ID 处理
            Guid cityGuid;
            if (!Guid.TryParse(cityId, out cityGuid))
            {
                // 如果不是 Guid,可能是城市的字符串标识符,需要先查询城市
                // 这里暂时返回空统计,因为我们需要 Guid 来查询评分
                return Ok(new CityRatingStatisticsResponse
                {
                    Statistics = new List<CategoryStatistics>()
                });
            }

            var categories = await _categoryRepository.GetAllActiveAsync();
            var averageRatings = await _ratingRepository.GetCityAverageRatingsAsync(cityGuid);
            var allCityRatings = await _ratingRepository.GetCityRatingsAsync(cityGuid);

            var ratingCounts = allCityRatings
                .GroupBy(r => r.CategoryId)
                .ToDictionary(g => g.Key, g => g.Count());

            var statistics = categories.Select(c => new CategoryStatistics
            {
                CategoryId = c.Id,
                CategoryName = c.Name,
                CategoryNameEn = c.NameEn,
                RatingCount = ratingCounts.GetValueOrDefault(c.Id, 0),
                AverageRating = averageRatings.GetValueOrDefault(c.Id, 0)
            }).ToList();

            return Ok(new CityRatingStatisticsResponse
            {
                Statistics = statistics
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取城市评分统计失败: CityId={CityId}", cityId);
            return StatusCode(500, new { error = "获取评分统计失败" });
        }
    }

    /// <summary>
    /// 批量获取多个城市的评分统计信息 (供 CacheService 调用，优化 N+1 查询)
    /// POST /api/v1/cities/ratings/statistics/batch
    /// </summary>
    [HttpPost("/api/v1/cities/ratings/statistics/batch")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(BatchCityRatingStatisticsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BatchCityRatingStatisticsResponse>> GetRatingStatisticsBatch(
        [FromBody] List<string> cityIds)
    {
        try
        {
            if (cityIds == null || cityIds.Count == 0)
            {
                return Ok(new BatchCityRatingStatisticsResponse
                {
                    CityStatistics = new Dictionary<string, CityRatingStatisticsResponse>()
                });
            }

            _logger.LogInformation("🔍 批量获取城市评分统计: {Count} 个城市", cityIds.Count);

            // 解析 Guid
            var validCityIds = cityIds
                .Where(id => Guid.TryParse(id, out _))
                .Select(id => Guid.Parse(id))
                .ToList();

            if (validCityIds.Count == 0)
            {
                return Ok(new BatchCityRatingStatisticsResponse
                {
                    CityStatistics = new Dictionary<string, CityRatingStatisticsResponse>()
                });
            }

            // 批量获取数据（一次数据库查询）
            var categories = await _categoryRepository.GetAllActiveAsync();
            var allRatings = await _ratingRepository.GetCityRatingsBatchAsync(validCityIds);

            // 按城市分组计算
            var cityRatingsGrouped = allRatings.GroupBy(r => r.CityId);
            var result = new Dictionary<string, CityRatingStatisticsResponse>();

            foreach (var cityGroup in cityRatingsGrouped)
            {
                var cityId = cityGroup.Key.ToString();
                var ratingCounts = cityGroup
                    .GroupBy(r => r.CategoryId)
                    .ToDictionary(g => g.Key, g => g.Count());

                var averageRatings = cityGroup
                    .GroupBy(r => r.CategoryId)
                    .ToDictionary(
                        g => g.Key,
                        g => Math.Round(g.Average(r => r.Rating), 1)
                    );

                var statistics = categories.Select(c => new CategoryStatistics
                {
                    CategoryId = c.Id,
                    CategoryName = c.Name,
                    CategoryNameEn = c.NameEn,
                    RatingCount = ratingCounts.GetValueOrDefault(c.Id, 0),
                    AverageRating = averageRatings.GetValueOrDefault(c.Id, 0)
                }).ToList();

                result[cityId] = new CityRatingStatisticsResponse
                {
                    Statistics = statistics
                };
            }

            // 为没有评分的城市返回空统计
            foreach (var cityId in validCityIds)
            {
                var cityIdStr = cityId.ToString();
                if (!result.ContainsKey(cityIdStr))
                {
                    result[cityIdStr] = new CityRatingStatisticsResponse
                    {
                        Statistics = categories.Select(c => new CategoryStatistics
                        {
                            CategoryId = c.Id,
                            CategoryName = c.Name,
                            CategoryNameEn = c.NameEn,
                            RatingCount = 0,
                            AverageRating = 0
                        }).ToList()
                    };
                }
            }

            _logger.LogInformation("✅ 批量获取城市评分统计完成: {Count} 个城市", result.Count);

            return Ok(new BatchCityRatingStatisticsResponse
            {
                CityStatistics = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量获取城市评分统计失败");
            return StatusCode(500, new { error = "批量获取评分统计失败" });
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
            var userId = _currentUser.TryGetUserId();
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
            var userId = _currentUser.TryGetUserId();
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
            if (!_currentUser.HasAdminOrModeratorPrivileges())
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
            if (!_currentUser.HasAdminOrModeratorPrivileges())
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

            // 4. 计算评价人数（去重后的用户数）
            var reviewCount = allCityRatings.Select(r => r.UserId).Distinct().Count();

            var requestBody = new
            {
                overallScore = overallScore,
                statistics = System.Text.Json.JsonSerializer.Serialize(statistics)
            };

            await _serviceInvocationClient.InvokeAsync(
                HttpMethod.Put,
                "cache-service",
                $"api/v1/cache/scores/city/{cityId}",
                requestBody
            );

            _logger.LogInformation("✅ 城市评分已更新到缓存: CityId={CityId}, OverallScore={OverallScore}",
                cityId, overallScore);

            // 6. 获取城市信息并发布 SignalR 通知
            await PublishCityRatingUpdatedAsync(cityId, overallScore, reviewCount);
        }
        catch (Exception ex)
        {
            // 缓存更新失败不影响主流程,只记录日志
            _logger.LogWarning(ex, "更新城市评分缓存时发生错误: CityId={CityId}", cityId);
        }
    }

    /// <summary>
    /// 发布城市评分更新消息到 MessageService (通过 SignalR 广播给客户端)
    /// </summary>
    private async Task PublishCityRatingUpdatedAsync(Guid cityId, double overallScore, int reviewCount)
    {
        try
        {
            // 获取城市信息
            var city = await _cityRepository.GetByIdAsync(cityId);
            
            var message = new CityRatingUpdatedMessage
            {
                CityId = cityId.ToString(),
                CityName = city?.Name,
                CityNameEn = city?.NameEn,
                OverallScore = overallScore,
                ReviewCount = reviewCount,
                UserId = _currentUser.TryGetUserId()?.ToString(),
                UpdatedAt = DateTime.UtcNow
            };

            await _publishEndpoint.Publish(message);

            _logger.LogInformation(
                "📡 城市评分更新消息已发布: CityId={CityId}, Score={Score}, ReviewCount={ReviewCount}",
                cityId, overallScore, reviewCount);
        }
        catch (Exception ex)
        {
            // SignalR 通知失败不影响主流程
            _logger.LogWarning(ex, "发布城市评分更新消息失败: CityId={CityId}", cityId);
        }
    }

    /// <summary>
    /// 确保城市评分缓存已初始化 (如果有评分但缓存未初始化,则初始化)
    /// </summary>
    private async Task EnsureCityScoreCacheInitializedAsync(Guid cityId, List<CityRatingStatisticsDto> statistics)
    {
        try
        {
            // 检查缓存是否存在
            var cacheCheckResponse = await _serviceInvocationClient.InvokeAsync<ScoreCacheCheckResponse>(
                HttpMethod.Get,
                "cache-service",
                $"api/v1/cache/scores/city/{cityId}"
            );

            // 如果缓存不存在或评分为0,但实际有评分数据,则初始化缓存
            if ((cacheCheckResponse == null || cacheCheckResponse.OverallScore == 0) && 
                statistics.Any(s => s.RatingCount > 0))
            {
                _logger.LogInformation("🔧 检测到城市有评分但缓存未初始化,开始初始化缓存: CityId={CityId}", cityId);
                
                var overallScore = Math.Round(
                    statistics.Where(s => s.RatingCount > 0).Average(s => s.AverageRating), 1);

                var requestBody = new
                {
                    overallScore = overallScore,
                    statistics = System.Text.Json.JsonSerializer.Serialize(statistics)
                };

                await _serviceInvocationClient.InvokeAsync(
                    HttpMethod.Put,
                    "cache-service",
                    $"api/v1/cache/scores/city/{cityId}",
                    requestBody
                );

                _logger.LogInformation("✅ 城市评分缓存初始化完成: CityId={CityId}, OverallScore={OverallScore}",
                    cityId, overallScore);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "确保城市评分缓存初始化时发生错误: CityId={CityId}", cityId);
        }
    }

    /// <summary>
    /// 评分缓存检查响应模型
    /// </summary>
    private class ScoreCacheCheckResponse
    {
        public string EntityId { get; set; } = string.Empty;
        public decimal OverallScore { get; set; }
        public bool FromCache { get; set; }
    }

    /// <summary>
    /// 城市评分统计响应 (供 CacheService 调用)
    /// </summary>
    public class CityRatingStatisticsResponse
    {
        public List<CategoryStatistics> Statistics { get; set; } = new();
    }

    /// <summary>
    /// 批量城市评分统计响应 (供 CacheService 调用，优化 N+1 查询)
    /// </summary>
    public class BatchCityRatingStatisticsResponse
    {
        /// <summary>
        /// Key: CityId (string), Value: 该城市的评分统计
        /// </summary>
        public Dictionary<string, CityRatingStatisticsResponse> CityStatistics { get; set; } = new();
    }

    /// <summary>
    /// 分类统计信息
    /// </summary>
    public class CategoryStatistics
    {
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? CategoryNameEn { get; set; }
        public int RatingCount { get; set; }
        public double AverageRating { get; set; }
    }

    /// <summary>
    /// 初始化默认评分项（管理员功能）
    /// </summary>
    [HttpPost("categories/initialize")]
    [ProducesResponseType(typeof(ApiResponse<RatingCategorySeedResult>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<RatingCategorySeedResult>>> InitializeDefaultCategories()
    {
        try
        {
            _logger.LogInformation("开始初始化默认评分项...");
            var result = await _ratingSeeder.SeedDefaultCategoriesAsync();

            return Ok(new ApiResponse<RatingCategorySeedResult>
            {
                Success = result.Success,
                Data = result,
                Message = result.Message ?? "初始化完成"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化默认评分项失败");
            return StatusCode(500, new ApiResponse<RatingCategorySeedResult>
            {
                Success = false,
                Message = "初始化默认评分项失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }
}
