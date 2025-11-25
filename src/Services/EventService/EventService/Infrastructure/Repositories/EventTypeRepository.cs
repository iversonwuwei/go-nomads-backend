using EventService.Domain.Entities;
using EventService.Domain.Repositories;
using Postgrest;
using Client = Supabase.Client;
using Constants = Postgrest.Constants;

namespace EventService.Infrastructure.Repositories;

/// <summary>
///     聚会类型仓储实现 - Supabase
/// </summary>
public class EventTypeRepository : IEventTypeRepository
{
    private readonly ILogger<EventTypeRepository> _logger;
    private readonly Client _supabaseClient;

    public EventTypeRepository(Client supabaseClient, ILogger<EventTypeRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    /// <summary>
    ///     获取所有启用的聚会类型
    /// </summary>
    public async Task<List<EventType>> GetAllActiveAsync()
    {
        try
        {
            _logger.LogInformation("📋 获取所有启用的聚会类型");

            var response = await _supabaseClient
                .From<EventType>()
                .Where(t => t.IsActive == true)
                .Order("sort_order", Constants.Ordering.Ascending)
                .Get();

            var eventTypes = response.Models;
            _logger.LogInformation("✅ 成功获取 {Count} 个启用的聚会类型", eventTypes.Count);

            return eventTypes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取启用的聚会类型失败");
            throw;
        }
    }

    /// <summary>
    ///     获取所有聚会类型（包括禁用的）
    /// </summary>
    public async Task<List<EventType>> GetAllAsync()
    {
        try
        {
            _logger.LogInformation("📋 获取所有聚会类型");

            var response = await _supabaseClient
                .From<EventType>()
                .Order("sort_order", Constants.Ordering.Ascending)
                .Get();

            var eventTypes = response.Models;
            _logger.LogInformation("✅ 成功获取 {Count} 个聚会类型", eventTypes.Count);

            return eventTypes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取所有聚会类型失败");
            throw;
        }
    }

    /// <summary>
    ///     根据 ID 获取聚会类型
    /// </summary>
    public async Task<EventType?> GetByIdAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("🔍 根据 ID 获取聚会类型: {Id}", id);

            var response = await _supabaseClient
                .From<EventType>()
                .Where(t => t.Id == id)
                .Single();

            if (response == null)
            {
                _logger.LogWarning("⚠️ 未找到聚会类型: {Id}", id);
                return null;
            }

            _logger.LogInformation("✅ 成功获取聚会类型: {Name}", response.Name);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 根据 ID 获取聚会类型失败: {Id}", id);
            throw;
        }
    }

    /// <summary>
    ///     根据英文名称获取聚会类型
    /// </summary>
    public async Task<EventType?> GetByEnNameAsync(string enName)
    {
        try
        {
            _logger.LogInformation("🔍 根据英文名称获取聚会类型: {EnName}", enName);

            var response = await _supabaseClient
                .From<EventType>()
                .Where(t => t.EnName == enName && t.IsActive == true)
                .Single();

            if (response == null)
            {
                _logger.LogWarning("⚠️ 未找到聚会类型: {EnName}", enName);
                return null;
            }

            _logger.LogInformation("✅ 成功获取聚会类型: {Name}", response.Name);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 根据英文名称获取聚会类型失败: {EnName}", enName);
            throw;
        }
    }

    /// <summary>
    ///     创建聚会类型
    /// </summary>
    public async Task<EventType> CreateAsync(EventType eventType)
    {
        try
        {
            _logger.LogInformation("➕ 创建聚会类型: {Name} ({EnName})", eventType.Name, eventType.EnName);

            var insertResult = await _supabaseClient
                .From<EventType>()
                .Insert(eventType, new QueryOptions { Returning = QueryOptions.ReturnType.Representation });

            var createdType = insertResult.Models.FirstOrDefault();

            if (createdType == null || createdType.Id == Guid.Empty)
            {
                _logger.LogWarning("⚠️ Insert 未返回数据，尝试查询最新记录");

                var queryResult = await _supabaseClient
                    .From<EventType>()
                    .Where(t => t.Name == eventType.Name)
                    .Order("created_at", Constants.Ordering.Descending)
                    .Limit(1)
                    .Get();

                createdType = queryResult.Models.FirstOrDefault();
            }

            if (createdType == null)
                throw new InvalidOperationException("创建聚会类型后无法获取数据");

            _logger.LogInformation("✅ 成功创建聚会类型: {Id} - {Name}", createdType.Id, createdType.Name);
            return createdType;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建聚会类型失败: {Name}", eventType.Name);
            throw;
        }
    }

    /// <summary>
    ///     更新聚会类型
    /// </summary>
    public async Task<EventType> UpdateAsync(EventType eventType)
    {
        try
        {
            _logger.LogInformation("📝 更新聚会类型: {Id} - {Name}", eventType.Id, eventType.Name);

            await _supabaseClient
                .From<EventType>()
                .Where(t => t.Id == eventType.Id)
                .Update(eventType);

            // 更新后重新获取
            var updated = await GetByIdAsync(eventType.Id);
            if (updated == null)
                throw new InvalidOperationException("更新后无法获取聚会类型数据");

            _logger.LogInformation("✅ 成功更新聚会类型: {Id}", eventType.Id);
            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新聚会类型失败: {Id}", eventType.Id);
            throw;
        }
    }

    /// <summary>
    ///     删除聚会类型
    /// </summary>
    public async Task DeleteAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("🗑️ 删除聚会类型: {Id}", id);

            await _supabaseClient
                .From<EventType>()
                .Where(t => t.Id == id)
                .Delete();

            _logger.LogInformation("✅ 成功删除聚会类型: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除聚会类型失败: {Id}", id);
            throw;
        }
    }

    /// <summary>
    ///     检查名称是否存在
    /// </summary>
    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null)
    {
        try
        {
            var query = _supabaseClient
                .From<EventType>()
                .Where(t => t.Name == name && t.IsActive == true);

            if (excludeId.HasValue)
                query = query.Where(t => t.Id != excludeId.Value);

            var response = await query.Get();
            return response.Models.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 检查名称是否存在失败: {Name}", name);
            throw;
        }
    }

    /// <summary>
    ///     检查英文名称是否存在
    /// </summary>
    public async Task<bool> ExistsByEnNameAsync(string enName, Guid? excludeId = null)
    {
        try
        {
            var query = _supabaseClient
                .From<EventType>()
                .Where(t => t.EnName == enName && t.IsActive == true);

            if (excludeId.HasValue)
                query = query.Where(t => t.Id != excludeId.Value);

            var response = await query.Get();
            return response.Models.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 检查英文名称是否存在失败: {EnName}", enName);
            throw;
        }
    }
}
