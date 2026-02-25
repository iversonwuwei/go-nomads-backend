using InnovationService.DTOs;
using InnovationService.Models;
using InnovationService.Services;
using Postgrest;
using Client = Supabase.Client;

namespace InnovationService.Repositories;

/// <summary>
///     创新项目仓储接口
/// </summary>
public interface IInnovationRepository
{
    Task<PagedResponse<InnovationListItem>> GetAllAsync(int page, int pageSize, string? category = null, string? stage = null, string? search = null, Guid? currentUserId = null);
    Task<InnovationResponse?> GetByIdAsync(Guid id, Guid? currentUserId = null);
    Task<Innovation> CreateAsync(Innovation innovation);
    Task<Innovation?> UpdateAsync(Guid id, UpdateInnovationRequest request, Guid userId);
    Task<bool> DeleteAsync(Guid id, Guid userId);
    Task<bool> AdminDeleteAsync(Guid id, Guid? deletedBy = null);
    Task<List<InnovationListItem>> GetByUserIdAsync(Guid userId, int page, int pageSize);
    Task<List<InnovationListItem>> GetFeaturedAsync(int limit);
    Task<List<InnovationListItem>> GetPopularAsync(int limit);
    Task<bool> ToggleLikeAsync(Guid innovationId, Guid userId);
    Task<bool> IncrementViewCountAsync(Guid innovationId);
    Task<List<InnovationTeamMember>> GetTeamMembersAsync(Guid innovationId);
    Task<InnovationTeamMember> AddTeamMemberAsync(InnovationTeamMember member);
    
    /// <summary>
    ///     批量添加团队成员（优化性能，避免多次插入）
    /// </summary>
    Task<List<InnovationTeamMember>> AddTeamMembersBatchAsync(List<InnovationTeamMember> members);
    
    Task<bool> RemoveTeamMemberAsync(Guid innovationId, Guid memberId, Guid userId);
    Task<List<CommentResponse>> GetCommentsAsync(Guid innovationId, int page, int pageSize);
    Task<InnovationComment> AddCommentAsync(InnovationComment comment);
    Task<bool> DeleteCommentAsync(Guid commentId, Guid userId);

    /// <summary>
    ///     更新创建者信息（冗余字段）
    /// </summary>
    /// <param name="creatorId">创建者ID</param>
    /// <param name="name">新的名称</param>
    /// <param name="avatarUrl">新的头像URL</param>
    /// <returns>更新的记录数</returns>
    Task<int> UpdateCreatorInfoAsync(Guid creatorId, string? name, string? avatarUrl);

    /// <summary>
    ///     更新评论用户信息（冗余字段）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="name">新的名称</param>
    /// <param name="avatarUrl">新的头像URL</param>
    /// <returns>更新的记录数</returns>
    Task<int> UpdateCommentUserInfoAsync(Guid userId, string? name, string? avatarUrl);
}

/// <summary>
///     创新项目仓储实现
/// </summary>
public class InnovationRepository : IInnovationRepository
{
    private readonly Client _supabase;
    private readonly ILogger<InnovationRepository> _logger;
    private readonly IUserServiceClient _userServiceClient;

    /// <summary>
    ///     列表查询所需的列（避免 Select("*") 拉取大文本字段如 description/problem/solution 等）
    /// </summary>
    private const string ListSelectColumns = "id,title,elevator_pitch,product_type,key_features,category,stage,image_url,creator_id,creator_name,creator_avatar,team_size,like_count,view_count,comment_count,is_featured,is_public,is_deleted,created_at";

    public InnovationRepository(
        Client supabase, 
        ILogger<InnovationRepository> logger, 
        IUserServiceClient userServiceClient)
    {
        _supabase = supabase;
        _logger = logger;
        _userServiceClient = userServiceClient;
    }

    public async Task<PagedResponse<InnovationListItem>> GetAllAsync(int page, int pageSize, string? category = null, string? stage = null, string? search = null, Guid? currentUserId = null)
    {
        try
        {
            var offset = (page - 1) * pageSize;

            // 构建基础查询的辅助方法
            Postgrest.Table<Innovation> BuildBaseQuery()
            {
                var q = _supabase.From<Innovation>()
                    .Select(ListSelectColumns)
                    .Filter("is_public", Postgrest.Constants.Operator.Is, "true")
                    .Filter("is_deleted", Postgrest.Constants.Operator.Equals, "false");

                if (!string.IsNullOrEmpty(category))
                    q = q.Filter("category", Postgrest.Constants.Operator.Equals, category);

                if (!string.IsNullOrEmpty(stage))
                    q = q.Filter("stage", Postgrest.Constants.Operator.Equals, stage);

                if (!string.IsNullOrEmpty(search))
                    q = q.Filter("title", Postgrest.Constants.Operator.ILike, $"%{search}%");

                return q;
            }

            // 并行执行 Count 和 Get 查询，减少总延迟
            var countTask = BuildBaseQuery().Count(Postgrest.Constants.CountType.Exact);
            var dataTask = BuildBaseQuery()
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Range(offset, offset + pageSize - 1)
                .Get();

            await Task.WhenAll(countTask, dataTask);

            var total = await countTask;
            var result = await dataTask;

            var items = result.Models.Select(i => new InnovationListItem
            {
                Id = i.Id,
                Title = i.Title,
                ElevatorPitch = i.ElevatorPitch,
                ProductType = i.ProductType,
                KeyFeatures = i.KeyFeatures,
                Category = i.Category,
                Stage = i.Stage,
                ImageUrl = i.ImageUrl,
                CreatorId = i.CreatorId,
                // 优先使用冗余字段
                CreatorName = i.CreatorName,
                CreatorAvatar = i.CreatorAvatar,
                TeamSize = i.TeamSize,
                LikeCount = i.LikeCount,
                ViewCount = i.ViewCount,
                CommentCount = i.CommentCount,
                IsFeatured = i.IsFeatured,
                CreatedAt = i.CreatedAt
            }).ToList();

            // 并行执行 Enrich 操作（创建者信息 + 点赞状态）
            var enrichCreatorTask = EnrichCreatorInfoAsync(items);
            var enrichLikeTask = (currentUserId.HasValue && items.Count > 0)
                ? EnrichLikeStatusAsync(items, currentUserId.Value)
                : Task.CompletedTask;
            await Task.WhenAll(enrichCreatorTask, enrichLikeTask);

            return new PagedResponse<InnovationListItem>
            {
                Items = items,
                Total = total,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取创新项目列表失败");
            throw;
        }
    }

    public async Task<InnovationResponse?> GetByIdAsync(Guid id, Guid? currentUserId = null)
    {
        try
        {
            var result = await _supabase.From<Innovation>()
                .Select("*")
                .Filter("id", Postgrest.Constants.Operator.Equals, id.ToString())
                .Filter("is_deleted", Postgrest.Constants.Operator.NotEqual, "true")
                .Single();

            if (result == null) return null;

            var response = new InnovationResponse
            {
                Id = result.Id,
                Title = result.Title,
                Description = result.Description,
                ElevatorPitch = result.ElevatorPitch,
                Problem = result.Problem,
                Solution = result.Solution,
                TargetAudience = result.TargetAudience,
                ProductType = result.ProductType,
                KeyFeatures = result.KeyFeatures,
                CompetitiveAdvantage = result.CompetitiveAdvantage,
                BusinessModel = result.BusinessModel,
                MarketOpportunity = result.MarketOpportunity,
                Ask = result.Ask,
                CreatorId = result.CreatorId,
                // 优先使用冗余字段
                CreatorName = result.CreatorName,
                CreatorAvatar = result.CreatorAvatar,
                Category = result.Category,
                Stage = result.Stage,
                Tags = result.Tags,
                ImageUrl = result.ImageUrl,
                Images = result.Images,
                VideoUrl = result.VideoUrl,
                DemoUrl = result.DemoUrl,
                GithubUrl = result.GithubUrl,
                WebsiteUrl = result.WebsiteUrl,
                TeamSize = result.TeamSize,
                LookingFor = result.LookingFor,
                SkillsNeeded = result.SkillsNeeded,
                LikeCount = result.LikeCount,
                ViewCount = result.ViewCount,
                CommentCount = result.CommentCount,
                IsFeatured = result.IsFeatured,
                IsPublic = result.IsPublic,
                CreatedAt = result.CreatedAt,
                UpdatedAt = result.UpdatedAt
            };

            // 并行执行：获取团队成员 + 查询点赞状态（两者无依赖关系）
            var teamTask = GetTeamMembersAsync(id);
            var likeTask = currentUserId.HasValue
                ? _supabase.From<InnovationLike>()
                    .Select("id")
                    .Filter("innovation_id", Postgrest.Constants.Operator.Equals, id.ToString())
                    .Filter("user_id", Postgrest.Constants.Operator.Equals, currentUserId.Value.ToString())
                    .Get()
                : Task.FromResult<Postgrest.Responses.ModeledResponse<InnovationLike>>(null!);

            await Task.WhenAll(teamTask, likeTask);

            var teamMembers = await teamTask;

            // 批量获取需要补充信息的用户 ID（避免 N+1 查询）
            var userIdsToFetch = teamMembers
                .Where(m => string.IsNullOrEmpty(m.Name) && m.UserId.HasValue)
                .Select(m => m.UserId!.Value)
                .Distinct()
                .ToList();
            
            // 如果创建者信息也需要查询，一起批量获取
            if (string.IsNullOrEmpty(response.CreatorName) && !userIdsToFetch.Contains(result.CreatorId))
            {
                userIdsToFetch.Add(result.CreatorId);
            }
            
            // 批量获取用户信息
            Dictionary<Guid, UserInfoDto> userInfoMap = new();
            if (userIdsToFetch.Count > 0)
            {
                try
                {
                    userInfoMap = await _userServiceClient.GetUsersInfoBatchAsync(userIdsToFetch);
                    _logger.LogDebug("✅ 批量获取 {Count} 个用户信息用于详情页", userInfoMap.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ 批量获取用户信息失败");
                }
            }
            
            // 填充创建者信息
            if (string.IsNullOrEmpty(response.CreatorName) && userInfoMap.TryGetValue(result.CreatorId, out var creatorInfo))
            {
                response.CreatorName = creatorInfo.Name;
                response.CreatorAvatar = creatorInfo.AvatarUrl;
            }
            
            // 构建团队成员响应
            var teamResponses = teamMembers.Select(m =>
            {
                var teamMember = new TeamMemberResponse
                {
                    Id = m.Id,
                    UserId = m.UserId,
                    Name = m.Name,
                    Role = m.Role,
                    Description = m.Description,
                    AvatarUrl = m.AvatarUrl,
                    IsFounder = m.IsFounder
                };
                
                // 使用批量获取的用户信息填充
                if (string.IsNullOrEmpty(teamMember.Name) && m.UserId.HasValue && userInfoMap.TryGetValue(m.UserId.Value, out var userInfo))
                {
                    teamMember.Name = userInfo.Name ?? string.Empty;
                    teamMember.AvatarUrl = string.IsNullOrEmpty(teamMember.AvatarUrl) ? userInfo.AvatarUrl : teamMember.AvatarUrl;
                }
                
                return teamMember;
            }).ToList();
            
            response.Team = teamResponses;

            // 使用并行获取的点赞结果
            if (currentUserId.HasValue)
            {
                var likeResult = await likeTask;
                response.IsLiked = likeResult?.Models?.Any() == true;
                
                // 检查当前用户是否可以编辑（创建者可以编辑）
                response.CanEdit = response.CreatorId == currentUserId.Value;
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取创新项目详情失败: {Id}", id);
            throw;
        }
    }

    public async Task<Innovation> CreateAsync(Innovation innovation)
    {
        try
        {
            var result = await _supabase.From<Innovation>().Insert(innovation);
            return result.Models.First();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建创新项目失败");
            throw;
        }
    }

    public async Task<Innovation?> UpdateAsync(Guid id, UpdateInnovationRequest request, Guid userId)
    {
        try
        {
            // 先检查项目是否属于当前用户
            var existing = await _supabase.From<Innovation>()
                .Select("*")
                .Filter("id", Postgrest.Constants.Operator.Equals, id.ToString())
                .Single();

            if (existing == null || existing.CreatorId != userId)
                return null;

            // 更新字段
            if (request.Title != null) existing.Title = request.Title;
            if (request.Description != null) existing.Description = request.Description;
            if (request.ElevatorPitch != null) existing.ElevatorPitch = request.ElevatorPitch;
            if (request.Problem != null) existing.Problem = request.Problem;
            if (request.Solution != null) existing.Solution = request.Solution;
            if (request.TargetAudience != null) existing.TargetAudience = request.TargetAudience;
            if (request.ProductType != null) existing.ProductType = request.ProductType;
            if (request.KeyFeatures != null) existing.KeyFeatures = request.KeyFeatures;
            if (request.CompetitiveAdvantage != null) existing.CompetitiveAdvantage = request.CompetitiveAdvantage;
            if (request.BusinessModel != null) existing.BusinessModel = request.BusinessModel;
            if (request.MarketOpportunity != null) existing.MarketOpportunity = request.MarketOpportunity;
            if (request.Ask != null) existing.Ask = request.Ask;
            if (request.Category != null) existing.Category = request.Category;
            if (request.Stage != null) existing.Stage = request.Stage;
            if (request.Tags != null) existing.Tags = request.Tags;
            if (request.ImageUrl != null) existing.ImageUrl = request.ImageUrl;
            if (request.Images != null) existing.Images = request.Images;
            if (request.VideoUrl != null) existing.VideoUrl = request.VideoUrl;
            if (request.DemoUrl != null) existing.DemoUrl = request.DemoUrl;
            if (request.GithubUrl != null) existing.GithubUrl = request.GithubUrl;
            if (request.WebsiteUrl != null) existing.WebsiteUrl = request.WebsiteUrl;
            if (request.LookingFor != null) existing.LookingFor = request.LookingFor;
            if (request.SkillsNeeded != null) existing.SkillsNeeded = request.SkillsNeeded;
            if (request.IsPublic.HasValue) existing.IsPublic = request.IsPublic.Value;

            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = userId;

            var result = await _supabase.From<Innovation>()
                .Update(existing);

            // 处理团队成员更新
            if (request.Team != null)
            {
                await UpdateTeamMembersAsync(id, request.Team);
            }

            return result.Models.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新创新项目失败: {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// 更新团队成员（先删除再插入）
    /// </summary>
    private async Task UpdateTeamMembersAsync(Guid innovationId, List<TeamMemberRequest> teamMembers)
    {
        try
        {
            // 先删除现有团队成员
            await _supabase.From<InnovationTeamMember>()
                .Filter("innovation_id", Postgrest.Constants.Operator.Equals, innovationId.ToString())
                .Delete();

            // 插入新团队成员
            if (teamMembers.Any())
            {
                var members = teamMembers.Select(m => new InnovationTeamMember
                {
                    Id = Guid.NewGuid(),
                    InnovationId = innovationId,
                    UserId = m.UserId,
                    Name = m.Name,
                    Role = m.Role,
                    Description = m.Description,
                    AvatarUrl = m.AvatarUrl,
                    IsFounder = m.IsFounder,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                await _supabase.From<InnovationTeamMember>().Insert(members);
                _logger.LogInformation("✅ 更新团队成员成功: {Count} 人", members.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新团队成员失败: InnovationId={Id}", innovationId);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            // 先检查项目是否属于当前用户
            var existing = await _supabase.From<Innovation>()
                .Select("*")
                .Filter("id", Postgrest.Constants.Operator.Equals, id.ToString())
                .Filter("is_deleted", Postgrest.Constants.Operator.NotEqual, "true")
                .Single();

            if (existing == null || existing.CreatorId != userId)
                return false;

            // 逻辑删除
            existing.MarkAsDeleted(userId);
            
            await _supabase.From<Innovation>()
                .Filter("id", Postgrest.Constants.Operator.Equals, id.ToString())
                .Set(x => x.IsDeleted, true)
                .Set(x => x.DeletedAt, existing.DeletedAt)
                .Set(x => x.DeletedBy, userId)
                .Set(x => x.UpdatedAt, existing.UpdatedAt)
                .Set(x => x.UpdatedBy, userId)
                .Update();

            _logger.LogInformation("✅ 创新项目逻辑删除成功: {Id}, DeletedBy: {UserId}", id, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除创新项目失败: {Id}", id);
            throw;
        }
    }

    /// <summary>
    ///     管理员删除创新项目（逻辑删除，无权限检查）
    /// </summary>
    public async Task<bool> AdminDeleteAsync(Guid id, Guid? deletedBy = null)
    {
        try
        {
            // 先检查项目是否存在
            var existing = await _supabase.From<Innovation>()
                .Select("*")
                .Filter("id", Postgrest.Constants.Operator.Equals, id.ToString())
                .Filter("is_deleted", Postgrest.Constants.Operator.NotEqual, "true")
                .Single();

            if (existing == null)
                return false;

            // 逻辑删除
            existing.MarkAsDeleted(deletedBy);
            
            await _supabase.From<Innovation>()
                .Filter("id", Postgrest.Constants.Operator.Equals, id.ToString())
                .Set(x => x.IsDeleted, true)
                .Set(x => x.DeletedAt, existing.DeletedAt)
                .Set(x => x.DeletedBy, deletedBy)
                .Set(x => x.UpdatedAt, existing.UpdatedAt)
                .Set(x => x.UpdatedBy, deletedBy)
                .Update();

            _logger.LogInformation("✅ 管理员逻辑删除创新项目成功: {Id}, DeletedBy: {DeletedBy}", id, deletedBy);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "管理员删除创新项目失败: {Id}", id);
            throw;
        }
    }

    public async Task<List<InnovationListItem>> GetByUserIdAsync(Guid userId, int page, int pageSize)
    {
        try
        {
            var offset = (page - 1) * pageSize;

            var result = await _supabase.From<Innovation>()
                .Select(ListSelectColumns)
                .Filter("creator_id", Postgrest.Constants.Operator.Equals, userId.ToString())
                .Filter("is_deleted", Postgrest.Constants.Operator.NotEqual, "true")
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Range(offset, offset + pageSize - 1)
                .Get();

            return result.Models.Select(i => new InnovationListItem
            {
                Id = i.Id,
                Title = i.Title,
                ElevatorPitch = i.ElevatorPitch,
                ProductType = i.ProductType,
                KeyFeatures = i.KeyFeatures,
                Category = i.Category,
                Stage = i.Stage,
                ImageUrl = i.ImageUrl,
                CreatorId = i.CreatorId,
                TeamSize = i.TeamSize,
                LikeCount = i.LikeCount,
                ViewCount = i.ViewCount,
                CommentCount = i.CommentCount,
                IsFeatured = i.IsFeatured,
                CreatedAt = i.CreatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户创新项目列表失败: {UserId}", userId);
            throw;
        }
    }

    public async Task<List<InnovationListItem>> GetFeaturedAsync(int limit)
    {
        try
        {
            var result = await _supabase.From<Innovation>()
                .Select(ListSelectColumns)
                .Filter("is_featured", Postgrest.Constants.Operator.Equals, "true")
                .Filter("is_public", Postgrest.Constants.Operator.Equals, "true")
                .Filter("is_deleted", Postgrest.Constants.Operator.NotEqual, "true")
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Limit(limit)
                .Get();

            var items = result.Models.Select(i => new InnovationListItem
            {
                Id = i.Id,
                Title = i.Title,
                ElevatorPitch = i.ElevatorPitch,
                ProductType = i.ProductType,
                KeyFeatures = i.KeyFeatures,
                Category = i.Category,
                Stage = i.Stage,
                ImageUrl = i.ImageUrl,
                CreatorId = i.CreatorId,
                TeamSize = i.TeamSize,
                LikeCount = i.LikeCount,
                ViewCount = i.ViewCount,
                CommentCount = i.CommentCount,
                IsFeatured = i.IsFeatured,
                CreatedAt = i.CreatedAt
            }).ToList();

            await EnrichCreatorInfoAsync(items);

            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取精选创新项目失败");
            throw;
        }
    }

    public async Task<List<InnovationListItem>> GetPopularAsync(int limit)
    {
        try
        {
            var result = await _supabase.From<Innovation>()
                .Select(ListSelectColumns)
                .Filter("is_public", Postgrest.Constants.Operator.Equals, "true")
                .Filter("is_deleted", Postgrest.Constants.Operator.NotEqual, "true")
                .Order("like_count", Postgrest.Constants.Ordering.Descending)
                .Limit(limit)
                .Get();

            var items = result.Models.Select(i => new InnovationListItem
            {
                Id = i.Id,
                Title = i.Title,
                ElevatorPitch = i.ElevatorPitch,
                ProductType = i.ProductType,
                KeyFeatures = i.KeyFeatures,
                Category = i.Category,
                Stage = i.Stage,
                ImageUrl = i.ImageUrl,
                CreatorId = i.CreatorId,
                TeamSize = i.TeamSize,
                LikeCount = i.LikeCount,
                ViewCount = i.ViewCount,
                CommentCount = i.CommentCount,
                IsFeatured = i.IsFeatured,
                CreatedAt = i.CreatedAt
            }).ToList();

            await EnrichCreatorInfoAsync(items);

            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取热门创新项目失败");
            throw;
        }
    }

    public async Task<bool> ToggleLikeAsync(Guid innovationId, Guid userId)
    {
        try
        {
            // 检查是否已点赞
            var existingLike = await _supabase.From<InnovationLike>()
                .Select("*")
                .Filter("innovation_id", Postgrest.Constants.Operator.Equals, innovationId.ToString())
                .Filter("user_id", Postgrest.Constants.Operator.Equals, userId.ToString())
                .Get();

            if (existingLike.Models.Any())
            {
                // 取消点赞
                await _supabase.From<InnovationLike>()
                    .Filter("innovation_id", Postgrest.Constants.Operator.Equals, innovationId.ToString())
                    .Filter("user_id", Postgrest.Constants.Operator.Equals, userId.ToString())
                    .Delete();

                // 更新计数（使用 RPC 调用更安全）
                await UpdateLikeCountAsync(innovationId, -1);

                return false; // 返回 false 表示已取消点赞
            }
            else
            {
                // 添加点赞
                await _supabase.From<InnovationLike>().Insert(new InnovationLike
                {
                    Id = Guid.NewGuid(),
                    InnovationId = innovationId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                });

                await UpdateLikeCountAsync(innovationId, 1);

                return true; // 返回 true 表示已点赞
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换点赞状态失败: InnovationId={InnovationId}, UserId={UserId}", innovationId, userId);
            throw;
        }
    }

    public async Task<bool> IncrementViewCountAsync(Guid innovationId)
    {
        try
        {
            // 使用 Supabase RPC 调用进行原子递增操作，避免并发问题和额外的读取查询
            await _supabase.Rpc("increment_innovation_view_count", new { p_innovation_id = innovationId });
            return true;
        }
        catch (Exception ex)
        {
            // 如果 RPC 函数不存在，回退到原有逻辑
            _logger.LogWarning(ex, "RPC increment_innovation_view_count 调用失败，回退到标准更新: {InnovationId}", innovationId);
            try
            {
                var existing = await _supabase.From<Innovation>()
                    .Select("view_count")
                    .Filter("id", Postgrest.Constants.Operator.Equals, innovationId.ToString())
                    .Single();

                if (existing == null) return false;

                await _supabase.From<Innovation>()
                    .Filter("id", Postgrest.Constants.Operator.Equals, innovationId.ToString())
                    .Set(x => x.ViewCount, existing.ViewCount + 1)
                    .Update();

                return true;
            }
            catch (Exception innerEx)
            {
                _logger.LogError(innerEx, "增加浏览次数失败: {InnovationId}", innovationId);
                return false;
            }
        }
    }

    public async Task<List<InnovationTeamMember>> GetTeamMembersAsync(Guid innovationId)
    {
        try
        {
            var result = await _supabase.From<InnovationTeamMember>()
                .Select("*")
                .Filter("innovation_id", Postgrest.Constants.Operator.Equals, innovationId.ToString())
                .Order("is_founder", Postgrest.Constants.Ordering.Descending)
                .Order("joined_at", Postgrest.Constants.Ordering.Ascending)
                .Get();

            return result.Models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取团队成员失败: {InnovationId}", innovationId);
            throw;
        }
    }

    public async Task<InnovationTeamMember> AddTeamMemberAsync(InnovationTeamMember member)
    {
        try
        {
            var result = await _supabase.From<InnovationTeamMember>().Insert(member);
            
            // 更新团队人数
            await UpdateTeamSizeAsync(member.InnovationId);

            return result.Models.First();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加团队成员失败");
            throw;
        }
    }

    /// <summary>
    ///     批量添加团队成员（优化性能，使用单次批量插入）
    /// </summary>
    public async Task<List<InnovationTeamMember>> AddTeamMembersBatchAsync(List<InnovationTeamMember> members)
    {
        if (members.Count == 0) return new List<InnovationTeamMember>();
        
        try
        {
            // 使用单次批量插入
            var result = await _supabase.From<InnovationTeamMember>().Insert(members);
            
            // 获取所有不同的 InnovationId 并批量更新团队人数
            var innovationIds = members.Select(m => m.InnovationId).Distinct().ToList();
            foreach (var innovationId in innovationIds)
            {
                await UpdateTeamSizeAsync(innovationId);
            }
            
            _logger.LogInformation("✅ 批量添加 {Count} 个团队成员成功", members.Count);
            return result.Models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量添加团队成员失败");
            throw;
        }
    }

    public async Task<bool> RemoveTeamMemberAsync(Guid innovationId, Guid memberId, Guid userId)
    {
        try
        {
            // 检查项目所有权
            var innovation = await _supabase.From<Innovation>()
                .Select("creator_id")
                .Filter("id", Postgrest.Constants.Operator.Equals, innovationId.ToString())
                .Single();

            if (innovation == null || innovation.CreatorId != userId)
                return false;

            await _supabase.From<InnovationTeamMember>()
                .Filter("id", Postgrest.Constants.Operator.Equals, memberId.ToString())
                .Delete();

            await UpdateTeamSizeAsync(innovationId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "移除团队成员失败: InnovationId={InnovationId}, MemberId={MemberId}", innovationId, memberId);
            throw;
        }
    }

    public async Task<List<CommentResponse>> GetCommentsAsync(Guid innovationId, int page, int pageSize)
    {
        try
        {
            var offset = (page - 1) * pageSize;

            var result = await _supabase.From<InnovationComment>()
                .Select("*")
                .Filter("innovation_id", Postgrest.Constants.Operator.Equals, innovationId.ToString())
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Range(offset, offset + pageSize - 1)
                .Get();

            var comments = result.Models.Select(c => new CommentResponse
            {
                Id = c.Id,
                InnovationId = c.InnovationId,
                UserId = c.UserId,
                Content = c.Content,
                ParentId = c.ParentId,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            }).ToList();
            
            // 批量获取用户信息
            await EnrichCommentUserInfoAsync(comments);
            
            return comments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取评论失败: {InnovationId}", innovationId);
            throw;
        }
    }

    public async Task<InnovationComment> AddCommentAsync(InnovationComment comment)
    {
        try
        {
            var result = await _supabase.From<InnovationComment>().Insert(comment);

            // 更新评论计数
            await UpdateCommentCountAsync(comment.InnovationId, 1);

            return result.Models.First();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加评论失败");
            throw;
        }
    }

    public async Task<bool> DeleteCommentAsync(Guid commentId, Guid userId)
    {
        try
        {
            var comment = await _supabase.From<InnovationComment>()
                .Select("*")
                .Filter("id", Postgrest.Constants.Operator.Equals, commentId.ToString())
                .Single();

            if (comment == null || comment.UserId != userId)
                return false;

            await _supabase.From<InnovationComment>()
                .Filter("id", Postgrest.Constants.Operator.Equals, commentId.ToString())
                .Delete();

            await UpdateCommentCountAsync(comment.InnovationId, -1);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除评论失败: {CommentId}", commentId);
            throw;
        }
    }

    #region Private Methods

    /// <summary>
    ///     批量填充创建者信息
    ///     优先使用冗余字段，仅对冗余字段为空的项目通过 UserServiceClient 补充查询
    /// </summary>
    private async Task EnrichCreatorInfoAsync(List<InnovationListItem> items)
    {
        if (items.Count == 0) return;

        // 筛选出冗余字段为空的项目（需要从 UserService 获取）
        var itemsNeedEnrich = items
            .Where(i => string.IsNullOrEmpty(i.CreatorName))
            .ToList();

        if (itemsNeedEnrich.Count == 0)
        {
            _logger.LogInformation("✅ 所有 {Count} 个项目已有冗余字段，无需调用 UserService", items.Count);
            return;
        }

        _logger.LogInformation("📊 {TotalCount} 个项目中有 {NeedEnrichCount} 个需要从 UserService 获取用户信息",
            items.Count, itemsNeedEnrich.Count);

        try
        {
            // 收集需要查询的 CreatorId
            var creatorIds = itemsNeedEnrich.Select(i => i.CreatorId).Distinct().ToList();

            _logger.LogInformation("🔄 通过 UserServiceClient 批量获取 {Count} 个用户信息", creatorIds.Count);

            // 通过 UserServiceClient 批量获取用户信息
            var userMap = await _userServiceClient.GetUsersInfoBatchAsync(creatorIds);

            // 填充创建者信息
            foreach (var item in itemsNeedEnrich)
            {
                if (userMap.TryGetValue(item.CreatorId, out var user))
                {
                    item.CreatorName = user.Name;
                    item.CreatorAvatar = user.AvatarUrl;
                }
            }

            _logger.LogInformation("✅ 成功获取 {Count} 个用户信息", userMap.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 获取用户信息失败，跳过填充创建者信息");
            // 不抛出异常，允许 API 正常返回（只是没有创建者详细信息）
        }
    }

    /// <summary>
    ///     批量填充点赞状态
    /// </summary>
    private async Task EnrichLikeStatusAsync(List<InnovationListItem> items, Guid userId)
    {
        if (items.Count == 0) return;
        
        try
        {
            var innovationIds = items.Select(i => i.Id).ToList();

            // 查询当前用户对这些项目的点赞记录，使用 In 过滤减少返回数据量
            var likeResult = await _supabase.From<InnovationLike>()
                .Select("innovation_id")
                .Filter("user_id", Postgrest.Constants.Operator.Equals, userId.ToString())
                .Filter("innovation_id", Postgrest.Constants.Operator.In, innovationIds.Select(id => id.ToString()).ToList())
                .Get();

            var likedIds = likeResult.Models
                .Select(l => l.InnovationId)
                .ToHashSet();

            // 填充点赞状态和编辑权限
            foreach (var item in items)
            {
                item.IsLiked = likedIds.Contains(item.Id);
                // 检查当前用户是否可以编辑（创建者可以编辑）
                item.CanEdit = item.CreatorId == userId;
            }

            _logger.LogDebug("✅ 成功获取 {Count} 个项目的点赞状态和编辑权限，用户点赞了 {LikedCount} 个", items.Count, likedIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 获取点赞状态失败");
        }
    }

    /// <summary>
    ///     获取单个用户的基本信息（用于详情页）
    /// </summary>
    private async Task<UserInfoDto?> GetUserBasicInfoAsync(Guid userId)
    {
        try
        {
            return await _userServiceClient.GetUserInfoAsync(userId.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 获取用户 {UserId} 信息失败", userId);
            return null;
        }
    }

    private async Task UpdateLikeCountAsync(Guid innovationId, int delta)
    {
        try
        {
            // 使用 Supabase RPC 调用进行原子递增/递减操作
            await _supabase.Rpc("update_innovation_like_count", new { p_innovation_id = innovationId, p_delta = delta });
        }
        catch (Exception ex)
        {
            // 如果 RPC 函数不存在，回退到原有逻辑
            _logger.LogWarning(ex, "RPC update_innovation_like_count 调用失败，回退到标准更新: {InnovationId}", innovationId);
            try
            {
                var existing = await _supabase.From<Innovation>()
                    .Select("like_count")
                    .Filter("id", Postgrest.Constants.Operator.Equals, innovationId.ToString())
                    .Single();

                if (existing != null)
                {
                    var newCount = Math.Max(0, existing.LikeCount + delta);
                    await _supabase.From<Innovation>()
                        .Filter("id", Postgrest.Constants.Operator.Equals, innovationId.ToString())
                        .Set(x => x.LikeCount, newCount)
                        .Update();
                }
            }
            catch (Exception innerEx)
            {
                _logger.LogWarning(innerEx, "更新点赞计数失败: {InnovationId}", innovationId);
            }
        }
    }

    private async Task UpdateCommentCountAsync(Guid innovationId, int delta)
    {
        try
        {
            // 使用 Supabase RPC 调用进行原子递增/递减操作
            await _supabase.Rpc("update_innovation_comment_count", new { p_innovation_id = innovationId, p_delta = delta });
        }
        catch (Exception ex)
        {
            // 如果 RPC 函数不存在，回退到原有逻辑
            _logger.LogWarning(ex, "RPC update_innovation_comment_count 调用失败，回退到标准更新: {InnovationId}", innovationId);
            try
            {
                var existing = await _supabase.From<Innovation>()
                    .Select("comment_count")
                    .Filter("id", Postgrest.Constants.Operator.Equals, innovationId.ToString())
                    .Single();

                if (existing != null)
                {
                    var newCount = Math.Max(0, existing.CommentCount + delta);
                    await _supabase.From<Innovation>()
                        .Filter("id", Postgrest.Constants.Operator.Equals, innovationId.ToString())
                        .Set(x => x.CommentCount, newCount)
                        .Update();
                }
            }
            catch (Exception innerEx)
            {
                _logger.LogWarning(innerEx, "更新评论计数失败: {InnovationId}", innovationId);
            }
        }
    }

    private async Task UpdateTeamSizeAsync(Guid innovationId)
    {
        try
        {
            var countResult = await _supabase.From<InnovationTeamMember>()
                .Select("*")
                .Filter("innovation_id", Postgrest.Constants.Operator.Equals, innovationId.ToString())
                .Count(Postgrest.Constants.CountType.Exact);

            var teamSize = countResult;

            await _supabase.From<Innovation>()
                .Filter("id", Postgrest.Constants.Operator.Equals, innovationId.ToString())
                .Set(x => x.TeamSize, teamSize)
                .Update();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "更新团队人数失败: {InnovationId}", innovationId);
        }
    }

    /// <summary>
    ///     批量填充评论用户信息
    /// </summary>
    private async Task EnrichCommentUserInfoAsync(List<CommentResponse> comments)
    {
        if (comments.Count == 0) return;

        try
        {
            // 收集所有不重复的 UserId
            var userIds = comments.Select(c => c.UserId).Distinct().ToList();

            _logger.LogDebug("🔄 通过 UserServiceClient 批量获取 {Count} 个评论用户信息", userIds.Count);

            // 通过 UserServiceClient 批量获取用户信息
            var userMap = await _userServiceClient.GetUsersInfoBatchAsync(userIds);

            // 填充用户信息
            foreach (var comment in comments)
            {
                if (userMap.TryGetValue(comment.UserId, out var user))
                {
                    comment.UserName = user.Name;
                    comment.UserAvatar = user.AvatarUrl;
                }
            }

            _logger.LogDebug("✅ 成功获取 {Count} 个评论用户信息", userMap.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 获取评论用户信息失败，跳过填充用户信息");
            // 不抛出异常，允许 API 正常返回（只是没有用户详细信息）
        }
    }

    #endregion

    #region 冗余字段更新方法

    /// <summary>
    ///     更新创建者信息（冗余字段）
    ///     当收到 UserUpdatedMessage 时调用此方法
    /// </summary>
    public async Task<int> UpdateCreatorInfoAsync(Guid creatorId, string? name, string? avatarUrl)
    {
        try
        {
            _logger.LogInformation("🔄 开始更新创建者 {CreatorId} 的冗余字段: Name={Name}", creatorId, name);

            // 查询该创建者的所有创新项目
            var result = await _supabase.From<Innovation>()
                .Select("id")
                .Filter("creator_id", Postgrest.Constants.Operator.Equals, creatorId.ToString())
                .Filter("is_deleted", Postgrest.Constants.Operator.NotEqual, "true")
                .Get();

            var count = result.Models.Count;
            if (count == 0)
            {
                _logger.LogInformation("📝 未找到创建者 {CreatorId} 的创新项目", creatorId);
                return 0;
            }

            // 更新所有记录的冗余字段
            await _supabase.From<Innovation>()
                .Filter("creator_id", Postgrest.Constants.Operator.Equals, creatorId.ToString())
                .Filter("is_deleted", Postgrest.Constants.Operator.NotEqual, "true")
                .Set(x => x.CreatorName, name)
                .Set(x => x.CreatorAvatar, avatarUrl)
                .Set(x => x.UpdatedAt, DateTime.UtcNow)
                .Update();

            _logger.LogInformation("✅ 已更新 {Count} 个创新项目的创建者信息", count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新创建者信息失败: CreatorId={CreatorId}", creatorId);
            throw;
        }
    }

    /// <summary>
    ///     更新评论用户信息（冗余字段）
    ///     当收到 UserUpdatedMessage 时调用此方法
    /// </summary>
    public async Task<int> UpdateCommentUserInfoAsync(Guid userId, string? name, string? avatarUrl)
    {
        try
        {
            _logger.LogInformation("🔄 开始更新用户 {UserId} 的评论冗余字段: Name={Name}", userId, name);

            // 查询该用户的所有评论
            var result = await _supabase.From<InnovationComment>()
                .Select("id")
                .Filter("user_id", Postgrest.Constants.Operator.Equals, userId.ToString())
                .Get();

            var count = result.Models.Count;
            if (count == 0)
            {
                _logger.LogInformation("📝 未找到用户 {UserId} 的评论", userId);
                return 0;
            }

            // 更新所有记录的冗余字段
            await _supabase.From<InnovationComment>()
                .Filter("user_id", Postgrest.Constants.Operator.Equals, userId.ToString())
                .Set(x => x.UserName, name)
                .Set(x => x.UserAvatar, avatarUrl)
                .Set(x => x.UpdatedAt, DateTime.UtcNow)
                .Update();

            _logger.LogInformation("✅ 已更新 {Count} 条评论的用户信息", count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新评论用户信息失败: UserId={UserId}", userId);
            throw;
        }
    }

    #endregion
}
