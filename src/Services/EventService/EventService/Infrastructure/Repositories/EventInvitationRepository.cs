using EventService.Domain.Entities;
using EventService.Domain.Repositories;
using Client = Supabase.Client;

namespace EventService.Infrastructure.Repositories;

/// <summary>
///     活动邀请仓储实现 - Supabase
/// </summary>
public class EventInvitationRepository : IEventInvitationRepository
{
    private readonly ILogger<EventInvitationRepository> _logger;
    private readonly Client _supabaseClient;

    public EventInvitationRepository(Client supabaseClient, ILogger<EventInvitationRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<EventInvitation> CreateAsync(EventInvitation invitation)
    {
        try
        {
            var result = await _supabaseClient
                .From<EventInvitation>()
                .Insert(invitation);

            var created = result.Models.FirstOrDefault();
            if (created == null) throw new InvalidOperationException("创建邀请失败");

            _logger.LogInformation("✅ 邀请创建成功，EventId: {EventId}, InviterId: {InviterId}, InviteeId: {InviteeId}",
                invitation.EventId, invitation.InviterId, invitation.InviteeId);
            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建邀请失败");
            throw;
        }
    }

    public async Task<EventInvitation?> GetByIdAsync(Guid id)
    {
        try
        {
            var result = await _supabaseClient
                .From<EventInvitation>()
                .Where(i => i.Id == id)
                .Get();

            return result.Models.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取邀请失败，ID: {Id}", id);
            throw;
        }
    }

    public async Task<EventInvitation> UpdateAsync(EventInvitation invitation)
    {
        try
        {
            var result = await _supabaseClient
                .From<EventInvitation>()
                .Where(i => i.Id == invitation.Id)
                .Update(invitation);

            var updated = result.Models.FirstOrDefault();
            if (updated == null) throw new InvalidOperationException("更新邀请失败");

            _logger.LogInformation("✅ 邀请更新成功，ID: {Id}, Status: {Status}",
                invitation.Id, invitation.Status);
            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新邀请失败，ID: {Id}", invitation.Id);
            throw;
        }
    }

    public async Task<List<EventInvitation>> GetByEventIdAsync(Guid eventId)
    {
        try
        {
            var result = await _supabaseClient
                .From<EventInvitation>()
                .Where(i => i.EventId == eventId)
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Get();

            return result.Models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取活动邀请列表失败，EventId: {EventId}", eventId);
            throw;
        }
    }

    public async Task<List<EventInvitation>> GetReceivedInvitationsAsync(Guid inviteeId, string? status = null)
    {
        try
        {
            var query = _supabaseClient
                .From<EventInvitation>()
                .Where(i => i.InviteeId == inviteeId);

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(i => i.Status == status);
            }

            var result = await query
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Get();

            return result.Models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取用户收到的邀请失败，InviteeId: {InviteeId}", inviteeId);
            throw;
        }
    }

    public async Task<List<EventInvitation>> GetSentInvitationsAsync(Guid inviterId, string? status = null)
    {
        try
        {
            var query = _supabaseClient
                .From<EventInvitation>()
                .Where(i => i.InviterId == inviterId);

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(i => i.Status == status);
            }

            var result = await query
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Get();

            return result.Models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取用户发出的邀请失败，InviterId: {InviterId}", inviterId);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid eventId, Guid inviteeId)
    {
        try
        {
            var result = await _supabaseClient
                .From<EventInvitation>()
                .Where(i => i.EventId == eventId)
                .Where(i => i.InviteeId == inviteeId)
                .Where(i => i.Status == "pending")
                .Get();

            return result.Models.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 检查邀请是否存在失败");
            throw;
        }
    }

    public async Task<EventInvitation?> GetPendingInvitationAsync(Guid eventId, Guid inviteeId)
    {
        try
        {
            var result = await _supabaseClient
                .From<EventInvitation>()
                .Where(i => i.EventId == eventId)
                .Where(i => i.InviteeId == inviteeId)
                .Where(i => i.Status == "pending")
                .Get();

            return result.Models.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取待处理邀请失败");
            throw;
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        try
        {
            await _supabaseClient
                .From<EventInvitation>()
                .Where(i => i.Id == id)
                .Delete();

            _logger.LogInformation("✅ 邀请删除成功，ID: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除邀请失败，ID: {Id}", id);
            throw;
        }
    }
}
