using CoworkingService.Domain.Entities;
using CoworkingService.Domain.Repositories;
using Postgrest;
using Client = Supabase.Client;

namespace CoworkingService.Infrastructure.Repositories;

/// <summary>
///     Coworking 评论仓储实现 - Supabase
/// </summary>
public class CoworkingCommentRepository : ICoworkingCommentRepository
{
    private readonly ILogger<CoworkingCommentRepository> _logger;
    private readonly Client _supabaseClient;

    public CoworkingCommentRepository(Client supabaseClient, ILogger<CoworkingCommentRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<CoworkingComment> CreateAsync(CoworkingComment comment)
    {
        try
        {
            var response = await _supabaseClient
                .From<CoworkingComment>()
                .Insert(comment);

            var created = response.Models.FirstOrDefault();
            if (created == null)
                throw new InvalidOperationException("创建评论失败");

            _logger.LogInformation("✅ 评论创建成功: {Id}", created.Id);
            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建评论失败");
            throw;
        }
    }

    public async Task<CoworkingComment?> GetByIdAsync(Guid id)
    {
        try
        {
            var response = await _supabaseClient
                .From<CoworkingComment>()
                .Filter("id", Constants.Operator.Equals, id.ToString())
                .Filter("is_active", Constants.Operator.Equals, "true")
                .Single();

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取评论失败: {Id}", id);
            return null;
        }
    }

    public async Task<List<CoworkingComment>> GetByCoworkingIdAsync(Guid coworkingId, int page = 1, int pageSize = 20)
    {
        try
        {
            var offset = (page - 1) * pageSize;

            var response = await _supabaseClient
                .From<CoworkingComment>()
                .Filter("coworking_id", Constants.Operator.Equals, coworkingId.ToString())
                .Filter("is_active", Constants.Operator.Equals, "true")
                .Order(x => x.CreatedAt, Constants.Ordering.Descending)
                .Range(offset, offset + pageSize - 1)
                .Get();

            return response?.Models ?? new List<CoworkingComment>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取评论列表失败: CoworkingId={CoworkingId}", coworkingId);
            throw;
        }
    }

    public async Task<int> GetCountByCoworkingIdAsync(Guid coworkingId)
    {
        try
        {
            var response = await _supabaseClient
                .From<CoworkingComment>()
                .Filter("coworking_id", Constants.Operator.Equals, coworkingId.ToString())
                .Filter("is_active", Constants.Operator.Equals, "true")
                .Get();

            return response?.Models?.Count ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取评论数量失败: CoworkingId={CoworkingId}", coworkingId);
            return 0;
        }
    }

    public async Task<CoworkingComment> UpdateAsync(CoworkingComment comment)
    {
        try
        {
            var response = await _supabaseClient
                .From<CoworkingComment>()
                .Update(comment);

            var updated = response.Models.FirstOrDefault();
            if (updated == null)
                throw new InvalidOperationException("更新评论失败");

            _logger.LogInformation("✅ 评论更新成功: {Id}", updated.Id);
            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新评论失败: {Id}", comment.Id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        try
        {
            await _supabaseClient
                .From<CoworkingComment>()
                .Filter("id", Constants.Operator.Equals, id.ToString())
                .Delete();

            _logger.LogInformation("✅ 评论删除成功: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除评论失败: {Id}", id);
            throw;
        }
    }
}
