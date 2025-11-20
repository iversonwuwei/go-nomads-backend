namespace CacheService.Domain.Entities;

/// <summary>
/// 分数缓存实体 - 代表城市或共享办公空间的评分缓存数据
/// </summary>
public class ScoreCache
{
    /// <summary>
    /// 缓存键
    /// </summary>
    public string Key { get; private set; }

    /// <summary>
    /// 实体类型 (City 或 Coworking)
    /// </summary>
    public ScoreEntityType EntityType { get; private set; }

    /// <summary>
    /// 实体ID (字符串格式,支持 int 和 Guid)
    /// </summary>
    public string EntityId { get; private set; }

    /// <summary>
    /// 总评分
    /// </summary>
    public decimal OverallScore { get; private set; }

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

    private ScoreCache()
    {
        Key = string.Empty;
        EntityId = string.Empty;
    }

    public ScoreCache(ScoreEntityType entityType, string entityId, decimal overallScore, TimeSpan ttl, string? statistics = null)
    {
        EntityType = entityType;
        EntityId = entityId;
        OverallScore = overallScore;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = CreatedAt.Add(ttl);
        Statistics = statistics;
        Key = GenerateKey(entityType, entityId);
    }

    /// <summary>
    /// 更新分数
    /// </summary>
    public void UpdateScore(decimal newScore, TimeSpan ttl, string? statistics = null)
    {
        OverallScore = newScore;
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
    public static string GenerateKey(ScoreEntityType entityType, string entityId)
    {
        return $"{entityType.ToString().ToLower()}:score:{entityId}";
    }

    /// <summary>
    /// 生成统计信息缓存键
    /// </summary>
    public static string GenerateStatsKey(ScoreEntityType entityType, string entityId)
    {
        return $"{entityType.ToString().ToLower()}:score:stats:{entityId}";
    }
}

/// <summary>
/// 评分实体类型
/// </summary>
public enum ScoreEntityType
{
    City,
    Coworking
}
