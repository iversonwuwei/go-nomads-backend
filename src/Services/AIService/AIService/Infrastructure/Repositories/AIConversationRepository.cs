using AIService.Domain.Entities;
using AIService.Domain.Repositories;
using Supabase;

namespace AIService.Infrastructure.Repositories;

/// <summary>
/// AI 对话仓储实现 (Supabase)
/// </summary>
public class AIConversationRepository : IAIConversationRepository
{
    private readonly Client _supabaseClient;
    private readonly ILogger<AIConversationRepository> _logger;

    public AIConversationRepository(Client supabaseClient, ILogger<AIConversationRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<AIConversation> CreateAsync(AIConversation conversation)
    {
        try
        {
            var response = await _supabaseClient
                .From<AIConversation>()
                .Insert(conversation);

            var created = response.Models.FirstOrDefault();
            if (created == null)
            {
                throw new InvalidOperationException("创建对话失败");
            }

            _logger.LogInformation("✅ 成功创建对话，ID: {ConversationId}", created.Id);
            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建对话失败");
            throw;
        }
    }

    public async Task<AIConversation?> GetByIdAsync(Guid id)
    {
        try
        {
            var response = await _supabaseClient
                .From<AIConversation>()
                .Where(c => c.Id == id && c.DeletedAt == null)
                .Single();

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取对话失败，ID: {ConversationId}", id);
            return null;
        }
    }

    public async Task<(List<AIConversation> Conversations, int Total)> GetByUserIdAsync(
        Guid userId, 
        string? status = null, 
        int page = 1, 
        int pageSize = 20)
    {
        try
        {
            var query = _supabaseClient
                .From<AIConversation>()
                .Where(c => c.UserId == userId && c.DeletedAt == null);

            // 状态过滤
            if (!string.IsNullOrEmpty(status) && status != "all")
            {
                query = query.Where(c => c.Status == status);
            }

            // 分页
            var offset = (page - 1) * pageSize;
            query = query
                .Order(c => c.LastMessageAt, Postgrest.Constants.Ordering.Descending)
                .Range(offset, offset + pageSize - 1);

            var response = await query.Get();
            var conversations = response.Models ?? new List<AIConversation>();

            // 获取总数（简化实现，实际应该用 count 查询）
            var totalQuery = _supabaseClient
                .From<AIConversation>()
                .Where(c => c.UserId == userId && c.DeletedAt == null);

            if (!string.IsNullOrEmpty(status) && status != "all")
            {
                totalQuery = totalQuery.Where(c => c.Status == status);
            }

            var totalResponse = await totalQuery.Get();
            var total = totalResponse.Models?.Count ?? 0;

            return (conversations, total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取用户对话列表失败，用户ID: {UserId}", userId);
            throw;
        }
    }

    public async Task<AIConversation> UpdateAsync(AIConversation conversation)
    {
        try
        {
            conversation.Touch();

            var response = await _supabaseClient
                .From<AIConversation>()
                .Where(c => c.Id == conversation.Id)
                .Update(conversation);

            var updated = response.Models.FirstOrDefault();
            if (updated == null)
            {
                throw new InvalidOperationException("更新对话失败");
            }

            _logger.LogInformation("✅ 成功更新对话，ID: {ConversationId}", conversation.Id);
            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新对话失败，ID: {ConversationId}", conversation.Id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        try
        {
            var conversation = await GetByIdAsync(id);
            if (conversation != null)
            {
                conversation.Delete();
                await UpdateAsync(conversation);
            }

            _logger.LogInformation("✅ 成功删除对话，ID: {ConversationId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除对话失败，ID: {ConversationId}", id);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        try
        {
            var conversation = await GetByIdAsync(id);
            return conversation != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> HasPermissionAsync(Guid conversationId, Guid userId)
    {
        try
        {
            var response = await _supabaseClient
                .From<AIConversation>()
                .Where(c => c.Id == conversationId && c.UserId == userId && c.DeletedAt == null)
                .Single();

            return response != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<(int TotalConversations, int ActiveConversations, int TotalMessages)> GetUserStatsAsync(Guid userId)
    {
        try
        {
            // 获取总对话数
            var totalResponse = await _supabaseClient
                .From<AIConversation>()
                .Where(c => c.UserId == userId && c.DeletedAt == null)
                .Get();

            var totalConversations = totalResponse.Models?.Count ?? 0;

            // 获取活跃对话数
            var activeResponse = await _supabaseClient
                .From<AIConversation>()
                .Where(c => c.UserId == userId && c.Status == "active" && c.DeletedAt == null)
                .Get();

            var activeConversations = activeResponse.Models?.Count ?? 0;

            // 获取总消息数（简化实现）
            var totalMessages = totalResponse.Models?.Sum(c => c.TotalMessages) ?? 0;

            return (totalConversations, activeConversations, totalMessages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取用户统计失败，用户ID: {UserId}", userId);
            throw;
        }
    }
}