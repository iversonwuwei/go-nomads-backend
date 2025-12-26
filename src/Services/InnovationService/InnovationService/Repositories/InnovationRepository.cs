using InnovationService.DTOs;
using InnovationService.Models;
using InnovationService.Services;
using Supabase;

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
    Task<List<InnovationListItem>> GetByUserIdAsync(Guid userId, int page, int pageSize);
    Task<List<InnovationListItem>> GetFeaturedAsync(int limit);
    Task<List<InnovationListItem>> GetPopularAsync(int limit);
    Task<bool> ToggleLikeAsync(Guid innovationId, Guid userId);
    Task<bool> IncrementViewCountAsync(Guid innovationId);
    Task<List<InnovationTeamMember>> GetTeamMembersAsync(Guid innovationId);
    Task<InnovationTeamMember> AddTeamMemberAsync(InnovationTeamMember member);
    Task<bool> RemoveTeamMemberAsync(Guid innovationId, Guid memberId, Guid userId);
    Task<List<CommentResponse>> GetCommentsAsync(Guid innovationId, int page, int pageSize);
    Task<InnovationComment> AddCommentAsync(InnovationComment comment);
    Task<bool> DeleteCommentAsync(Guid commentId, Guid userId);
}

/// <summary>
///     创新项目仓储实现
/// </summary>
public class InnovationRepository : IInnovationRepository
{
    private readonly Client _supabase;
    private readonly ILogger<InnovationRepository> _logger;
    private readonly IUserServiceClient _userServiceClient;

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

            // 构建基础查询条件
            var baseQuery = _supabase.From<Innovation>()
                .Select("*")
                .Filter("is_public", Postgrest.Constants.Operator.Is, "true");

            if (!string.IsNullOrEmpty(category))
                baseQuery = baseQuery.Filter("category", Postgrest.Constants.Operator.Equals, category);

            if (!string.IsNullOrEmpty(stage))
                baseQuery = baseQuery.Filter("stage", Postgrest.Constants.Operator.Equals, stage);

            if (!string.IsNullOrEmpty(search))
                baseQuery = baseQuery.Filter("title", Postgrest.Constants.Operator.ILike, $"%{search}%");

            // 先获取总数
            var countResult = await baseQuery.Count(Postgrest.Constants.CountType.Exact);
            var total = countResult;

            // 重新构建查询获取数据
            var dataQuery = _supabase.From<Innovation>()
                .Select("*")
                .Filter("is_public", Postgrest.Constants.Operator.Is, "true")
                .Order("created_at", Postgrest.Constants.Ordering.Descending);

            if (!string.IsNullOrEmpty(category))
                dataQuery = dataQuery.Filter("category", Postgrest.Constants.Operator.Equals, category);

            if (!string.IsNullOrEmpty(stage))
                dataQuery = dataQuery.Filter("stage", Postgrest.Constants.Operator.Equals, stage);

            if (!string.IsNullOrEmpty(search))
                dataQuery = dataQuery.Filter("title", Postgrest.Constants.Operator.ILike, $"%{search}%");

            var result = await dataQuery.Range(offset, offset + pageSize - 1).Get();

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

            // 获取创建者信息
            await EnrichCreatorInfoAsync(items);

            // 填充当前用户的点赞状态
            if (currentUserId.HasValue && items.Count > 0)
            {
                await EnrichLikeStatusAsync(items, currentUserId.Value);
            }

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

            // 获取创建者信息
            var creatorInfo = await GetUserBasicInfoAsync(result.CreatorId);
            if (creatorInfo != null)
            {
                response.CreatorName = creatorInfo.Name;
                response.CreatorAvatar = creatorInfo.AvatarUrl;
            }

            // 获取团队成员
            response.Team = (await GetTeamMembersAsync(id)).Select(m => new TeamMemberResponse
            {
                Id = m.Id,
                UserId = m.UserId,
                Name = m.Name,
                Role = m.Role,
                Description = m.Description,
                AvatarUrl = m.AvatarUrl,
                IsFounder = m.IsFounder
            }).ToList();

            // 检查当前用户是否点赞
            if (currentUserId.HasValue)
            {
                var likeResult = await _supabase.From<InnovationLike>()
                    .Select("id")
                    .Filter("innovation_id", Postgrest.Constants.Operator.Equals, id.ToString())
                    .Filter("user_id", Postgrest.Constants.Operator.Equals, currentUserId.Value.ToString())
                    .Get();

                response.IsLiked = likeResult.Models.Any();
                
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
                .Select("creator_id")
                .Filter("id", Postgrest.Constants.Operator.Equals, id.ToString())
                .Single();

            if (existing == null || existing.CreatorId != userId)
                return false;

            await _supabase.From<Innovation>()
                .Filter("id", Postgrest.Constants.Operator.Equals, id.ToString())
                .Delete();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除创新项目失败: {Id}", id);
            throw;
        }
    }

    public async Task<List<InnovationListItem>> GetByUserIdAsync(Guid userId, int page, int pageSize)
    {
        try
        {
            var offset = (page - 1) * pageSize;

            var result = await _supabase.From<Innovation>()
                .Select("*")
                .Filter("creator_id", Postgrest.Constants.Operator.Equals, userId.ToString())
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
                .Select("*")
                .Filter("is_featured", Postgrest.Constants.Operator.Equals, "true")
                .Filter("is_public", Postgrest.Constants.Operator.Equals, "true")
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
                .Select("*")
                .Filter("is_public", Postgrest.Constants.Operator.Equals, "true")
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
            var existing = await _supabase.From<Innovation>()
                .Select("view_count")
                .Filter("id", Postgrest.Constants.Operator.Equals, innovationId.ToString())
                .Single();

            if (existing == null) return false;

            existing.ViewCount += 1;

            await _supabase.From<Innovation>()
                .Filter("id", Postgrest.Constants.Operator.Equals, innovationId.ToString())
                .Set(x => x.ViewCount, existing.ViewCount)
                .Update();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "增加浏览次数失败: {InnovationId}", innovationId);
            return false;
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

            return result.Models.Select(c => new CommentResponse
            {
                Id = c.Id,
                InnovationId = c.InnovationId,
                UserId = c.UserId,
                Content = c.Content,
                ParentId = c.ParentId,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            }).ToList();
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

    private async Task EnrichCreatorInfoAsync(List<InnovationListItem> items)
    {
        if (items.Count == 0) return;

        try
        {
            // 收集所有不重复的 CreatorId
            var creatorIds = items.Select(i => i.CreatorId).Distinct().ToList();

            _logger.LogInformation("🔄 通过 UserServiceClient 批量获取 {Count} 个用户信息", creatorIds.Count);

            // 通过 UserServiceClient 批量获取用户信息
            var userMap = await _userServiceClient.GetUsersInfoBatchAsync(creatorIds);

            // 填充创建者信息
            foreach (var item in items)
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
        try
        {
            var innovationIds = items.Select(i => i.Id).ToList();

            // 查询当前用户对这些项目的点赞记录
            var likeResult = await _supabase.From<InnovationLike>()
                .Select("innovation_id")
                .Filter("user_id", Postgrest.Constants.Operator.Equals, userId.ToString())
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

            _logger.LogInformation("✅ 成功获取 {Count} 个项目的点赞状态和编辑权限", items.Count);
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
            var existing = await _supabase.From<Innovation>()
                .Select("like_count")
                .Filter("id", Postgrest.Constants.Operator.Equals, innovationId.ToString())
                .Single();

            if (existing != null)
            {
                existing.LikeCount = Math.Max(0, existing.LikeCount + delta);

                await _supabase.From<Innovation>()
                    .Filter("id", Postgrest.Constants.Operator.Equals, innovationId.ToString())
                    .Set(x => x.LikeCount, existing.LikeCount)
                    .Update();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "更新点赞计数失败: {InnovationId}", innovationId);
        }
    }

    private async Task UpdateCommentCountAsync(Guid innovationId, int delta)
    {
        try
        {
            var existing = await _supabase.From<Innovation>()
                .Select("comment_count")
                .Filter("id", Postgrest.Constants.Operator.Equals, innovationId.ToString())
                .Single();

            if (existing != null)
            {
                existing.CommentCount = Math.Max(0, existing.CommentCount + delta);

                await _supabase.From<Innovation>()
                    .Filter("id", Postgrest.Constants.Operator.Equals, innovationId.ToString())
                    .Set(x => x.CommentCount, existing.CommentCount)
                    .Update();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "更新评论计数失败: {InnovationId}", innovationId);
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

    #endregion
}
