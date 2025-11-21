namespace CacheService.Domain.Entities;

/// <summary>
/// 费用缓存实体 - 代表城市的平均费用缓存数据
/// </summary>
public class CostCache
{
    /// <summary>
    /// 缓存键
    /// </summary>
    public string Key { get; private set; }

    /// <summary>
    /// 实体类型 (目前只支持 City)
    /// </summary>
    public CostEntityType EntityType { get; private set; }

    /// <summary>
    /// 实体ID (字符串格式,支持 int 和 Guid)
    /// </summary>
    public string EntityId { get; private set; }

    /// <summary>
    /// 平均费用
    /// </summary>
    public decimal AverageCost { get; private set; }

    /// <summary>
    /// 缓存创建时间
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// 缓存过期时间
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// 额外的统计信息(JSON格式)
    /// </summary>
    public string? Statistics { get; private set; }

    private CostCache()
    {
        Key = string.Empty;
        EntityId = string.Empty;
    }

    public CostCache(CostEntityType entityType, string entityId, decimal averageCost, TimeSpan ttl, string? statistics = null)
    {
        EntityType = entityType;
        EntityId = entityId;
        AverageCost = averageCost;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = CreatedAt.Add(ttl);
        Statistics = statistics;
        Key = GenerateKey(entityType, entityId);
    }

    /// <summary>
    /// 更新费用
    /// </summary>
    public void UpdateCost(decimal newCost, TimeSpan ttl, string? statistics = null)
    {
        AverageCost = newCost;
        Statistics = statistics;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = CreatedAt.Add(ttl);
    }

    /// <summary>
    /// 检查缓存是否过期
    /// </summary>
    public bool IsExpired() => DateTime.UtcNow >= ExpiresAt;

    /// <summary>
    /// 生成缓存键
    /// </summary>
    public static string GenerateKey(CostEntityType entityType, string entityId)
    {
        return $"{entityType.ToString().ToLower()}:cost:{entityId}";
    }
}

/// <summary>
/// 费用实体类型
/// </summary>
public enum CostEntityType
{
    City
}
