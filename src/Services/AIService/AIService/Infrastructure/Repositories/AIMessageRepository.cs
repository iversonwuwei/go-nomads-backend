using AIService.Domain.Entities;
using AIService.Domain.Repositories;
using Postgrest;
using Client = Supabase.Client;

namespace AIService.Infrastructure.Repositories;

/// <summary>
///     AI 消息仓储实现 (Supabase)
/// </summary>
public class AIMessageRepository : IAIMessageRepository
{
    private readonly ILogger<AIMessageRepository> _logger;
    private readonly Client _supabaseClient;

    public AIMessageRepository(Client supabaseClient, ILogger<AIMessageRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<AIMessage> CreateAsync(AIMessage message)
    {
        try
        {
            var response = await _supabaseClient
                .From<AIMessage>()
                .Insert(message);

            var created = response.Models.FirstOrDefault();
            if (created == null) throw new InvalidOperationException("创建消息失败");

            _logger.LogInformation("✅ 成功创建消息，ID: {MessageId}, 角色: {Role}", created.Id, created.Role);
            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建消息失败");
            throw;
        }
    }

    public async Task<List<AIMessage>> CreateBatchAsync(List<AIMessage> messages)
    {
        try
        {
            var response = await _supabaseClient
                .From<AIMessage>()
                .Insert(messages);

            var created = response.Models ?? new List<AIMessage>();

            _logger.LogInformation("✅ 成功批量创建消息，数量: {Count}", created.Count);
            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 批量创建消息失败");
            throw;
        }
    }

    public async Task<AIMessage?> GetByIdAsync(Guid id)
    {
        try
        {
            var response = await _supabaseClient
                .From<AIMessage>()
                .Where(m => m.Id == id && m.DeletedAt == null)
                .Single();

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取消息失败，ID: {MessageId}", id);
            return null;
        }
    }

    public async Task<List<AIMessage>> GetByConversationIdAsync(
        Guid conversationId,
        int page = 1,
        int pageSize = 50,
        bool includeSystem = false)
    {
        try
        {
            var query = _supabaseClient
                .From<AIMessage>()
                .Where(m => m.ConversationId == conversationId && m.DeletedAt == null);

            // 是否包含系统消息
            if (!includeSystem) query = query.Where(m => m.Role != "system");

            // 分页
            var offset = (page - 1) * pageSize;
            query = query
                .Order(m => m.CreatedAt, Constants.Ordering.Ascending)
                .Range(offset, offset + pageSize - 1);

            var response = await query.Get();
            return response.Models ?? new List<AIMessage>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取对话消息失败，对话ID: {ConversationId}", conversationId);
            throw;
        }
    }

    public async Task<AIMessage?> GetLatestMessageAsync(Guid conversationId)
    {
        try
        {
            var response = await _supabaseClient
                .From<AIMessage>()
                .Where(m => m.ConversationId == conversationId && m.DeletedAt == null)
                .Order(m => m.CreatedAt, Constants.Ordering.Descending)
                .Limit(1)
                .Single();

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取最新消息失败，对话ID: {ConversationId}", conversationId);
            return null;
        }
    }

    public async Task<List<AIMessage>> GetContextMessagesAsync(Guid conversationId, int maxMessages = 20)
    {
        try
        {
            var response = await _supabaseClient
                .From<AIMessage>()
                .Where(m => m.ConversationId == conversationId && m.DeletedAt == null)
                .Order(m => m.CreatedAt, Constants.Ordering.Descending)
                .Limit(maxMessages)
                .Get();

            // 返回按时间正序排列的消息
            var messages = response.Models ?? new List<AIMessage>();
            return messages.OrderBy(m => m.CreatedAt).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取上下文消息失败，对话ID: {ConversationId}", conversationId);
            throw;
        }
    }

    public async Task<AIMessage> UpdateAsync(AIMessage message)
    {
        try
        {
            message.Touch();

            var response = await _supabaseClient
                .From<AIMessage>()
                .Where(m => m.Id == message.Id)
                .Update(message);

            var updated = response.Models.FirstOrDefault();
            if (updated == null) throw new InvalidOperationException("更新消息失败");

            _logger.LogInformation("✅ 成功更新消息，ID: {MessageId}", message.Id);
            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新消息失败，ID: {MessageId}", message.Id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        try
        {
            var message = await GetByIdAsync(id);
            if (message != null)
            {
                message.Delete();
                await UpdateAsync(message);
            }

            _logger.LogInformation("✅ 成功删除消息，ID: {MessageId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除消息失败，ID: {MessageId}", id);
            throw;
        }
    }

    public async Task DeleteByConversationIdAsync(Guid conversationId)
    {
        try
        {
            var messages = await _supabaseClient
                .From<AIMessage>()
                .Where(m => m.ConversationId == conversationId && m.DeletedAt == null)
                .Get();

            if (messages.Models != null && messages.Models.Any())
            {
                foreach (var message in messages.Models) message.Delete();

                await _supabaseClient
                    .From<AIMessage>()
                    .Upsert(messages.Models);
            }

            _logger.LogInformation("✅ 成功删除对话的所有消息，对话ID: {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除对话消息失败，对话ID: {ConversationId}", conversationId);
            throw;
        }
    }

    public async Task<(int TotalMessages, int TotalTokens)> GetConversationStatsAsync(Guid conversationId)
    {
        try
        {
            var response = await _supabaseClient
                .From<AIMessage>()
                .Where(m => m.ConversationId == conversationId && m.DeletedAt == null)
                .Get();

            var messages = response.Models ?? new List<AIMessage>();
            var totalMessages = messages.Count;
            var totalTokens = messages.Sum(m => m.TokenCount);

            return (totalMessages, totalTokens);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取对话统计失败，对话ID: {ConversationId}", conversationId);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        try
        {
            var message = await GetByIdAsync(id);
            return message != null;
        }
        catch
        {
            return false;
        }
    }
}