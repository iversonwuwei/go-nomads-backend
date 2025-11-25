using EventService.Application.DTOs;
using EventService.Domain.Entities;
using EventService.Domain.Repositories;

namespace EventService.Application.Services;

/// <summary>
///     聚会类型服务接口
/// </summary>
public interface IEventTypeService
{
    /// <summary>
    ///     获取所有启用的聚会类型
    /// </summary>
    Task<List<EventTypeDto>> GetAllActiveTypesAsync();

    /// <summary>
    ///     获取所有聚会类型（包括禁用的）- 仅管理员
    /// </summary>
    Task<List<EventTypeDto>> GetAllTypesAsync();

    /// <summary>
    ///     根据 ID 获取聚会类型
    /// </summary>
    Task<EventTypeDto?> GetTypeByIdAsync(Guid id);

    /// <summary>
    ///     创建聚会类型
    /// </summary>
    Task<EventTypeDto> CreateTypeAsync(CreateEventTypeRequest request);

    /// <summary>
    ///     更新聚会类型
    /// </summary>
    Task<EventTypeDto> UpdateTypeAsync(Guid id, UpdateEventTypeRequest request);

    /// <summary>
    ///     删除聚会类型
    /// </summary>
    Task DeleteTypeAsync(Guid id);
}

/// <summary>
///     聚会类型服务实现
/// </summary>
public class EventTypeService : IEventTypeService
{
    private readonly IEventTypeRepository _eventTypeRepository;
    private readonly ILogger<EventTypeService> _logger;

    public EventTypeService(IEventTypeRepository eventTypeRepository, ILogger<EventTypeService> logger)
    {
        _eventTypeRepository = eventTypeRepository;
        _logger = logger;
    }

    public async Task<List<EventTypeDto>> GetAllActiveTypesAsync()
    {
        try
        {
            _logger.LogInformation("📋 获取所有启用的聚会类型");

            var eventTypes = await _eventTypeRepository.GetAllActiveAsync();
            var dtos = eventTypes.Select(MapToDto).ToList();

            _logger.LogInformation("✅ 成功获取 {Count} 个聚会类型", dtos.Count);
            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取聚会类型失败");
            throw;
        }
    }

    public async Task<List<EventTypeDto>> GetAllTypesAsync()
    {
        try
        {
            _logger.LogInformation("📋 获取所有聚会类型（包括禁用的）");

            var eventTypes = await _eventTypeRepository.GetAllAsync();
            var dtos = eventTypes.Select(MapToDto).ToList();

            _logger.LogInformation("✅ 成功获取 {Count} 个聚会类型", dtos.Count);
            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取所有聚会类型失败");
            throw;
        }
    }

    public async Task<EventTypeDto?> GetTypeByIdAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("🔍 获取聚会类型: {Id}", id);

            var eventType = await _eventTypeRepository.GetByIdAsync(id);
            if (eventType == null)
            {
                _logger.LogWarning("⚠️ 未找到聚会类型: {Id}", id);
                return null;
            }

            return MapToDto(eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取聚会类型失败: {Id}", id);
            throw;
        }
    }

    public async Task<EventTypeDto> CreateTypeAsync(CreateEventTypeRequest request)
    {
        try
        {
            _logger.LogInformation("➕ 创建聚会类型: {Name} ({EnName})", request.Name, request.EnName);

            // 验证名称唯一性
            if (await _eventTypeRepository.ExistsByNameAsync(request.Name))
                throw new InvalidOperationException($"聚会类型名称 '{request.Name}' 已存在");

            if (await _eventTypeRepository.ExistsByEnNameAsync(request.EnName))
                throw new InvalidOperationException($"聚会类型英文名称 '{request.EnName}' 已存在");

            // 创建实体
            var eventType = EventType.Create(
                request.Name,
                request.EnName,
                request.Description,
                request.Icon,
                request.SortOrder,
                isSystem: false // 用户创建的都不是系统预设
            );

            // 保存到数据库
            var created = await _eventTypeRepository.CreateAsync(eventType);

            _logger.LogInformation("✅ 成功创建聚会类型: {Id} - {Name}", created.Id, created.Name);
            return MapToDto(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建聚会类型失败: {Name}", request.Name);
            throw;
        }
    }

    public async Task<EventTypeDto> UpdateTypeAsync(Guid id, UpdateEventTypeRequest request)
    {
        try
        {
            _logger.LogInformation("📝 更新聚会类型: {Id}", id);

            // 获取现有类型
            var eventType = await _eventTypeRepository.GetByIdAsync(id);
            if (eventType == null)
                throw new InvalidOperationException($"聚会类型不存在: {id}");

            // 验证名称唯一性
            if (request.Name != null && await _eventTypeRepository.ExistsByNameAsync(request.Name, id))
                throw new InvalidOperationException($"聚会类型名称 '{request.Name}' 已存在");

            if (request.EnName != null && await _eventTypeRepository.ExistsByEnNameAsync(request.EnName, id))
                throw new InvalidOperationException($"聚会类型英文名称 '{request.EnName}' 已存在");

            // 更新实体
            eventType.Update(
                request.Name,
                request.EnName,
                request.Description,
                request.Icon,
                request.SortOrder,
                request.IsActive
            );

            // 保存到数据库
            var updated = await _eventTypeRepository.UpdateAsync(eventType);

            _logger.LogInformation("✅ 成功更新聚会类型: {Id}", id);
            return MapToDto(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新聚会类型失败: {Id}", id);
            throw;
        }
    }

    public async Task DeleteTypeAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("🗑️ 删除聚会类型: {Id}", id);

            // 获取现有类型
            var eventType = await _eventTypeRepository.GetByIdAsync(id);
            if (eventType == null)
                throw new InvalidOperationException($"聚会类型不存在: {id}");

            // 检查是否为系统预设类型
            if (eventType.IsSystem)
                throw new InvalidOperationException("系统预设类型不能删除");

            // 软删除：停用类型
            eventType.Deactivate();
            await _eventTypeRepository.UpdateAsync(eventType);

            _logger.LogInformation("✅ 成功删除聚会类型: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除聚会类型失败: {Id}", id);
            throw;
        }
    }

    /// <summary>
    ///     实体转 DTO
    /// </summary>
    private static EventTypeDto MapToDto(EventType eventType)
    {
        return new EventTypeDto
        {
            Id = eventType.Id,
            Name = eventType.Name,
            EnName = eventType.EnName,
            Description = eventType.Description,
            Icon = eventType.Icon,
            SortOrder = eventType.SortOrder,
            IsActive = eventType.IsActive,
            IsSystem = eventType.IsSystem
        };
    }
}
