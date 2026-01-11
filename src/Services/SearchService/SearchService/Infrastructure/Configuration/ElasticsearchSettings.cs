namespace SearchService.Infrastructure.Configuration;

/// <summary>
/// Elasticsearch配置
/// </summary>
public class ElasticsearchSettings
{
    /// <summary>
    /// Elasticsearch URL
    /// </summary>
    public string Url { get; set; } = "http://localhost:9200";

    /// <summary>
    /// 默认索引名称
    /// </summary>
    public string DefaultIndex { get; set; } = "gonomads";

    /// <summary>
    /// 用户名（可选）
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// 密码（可选）
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// 是否启用调试模式
    /// </summary>
    public bool EnableDebugMode { get; set; } = false;
}

/// <summary>
/// 索引配置
/// </summary>
public class IndexSettings
{
    /// <summary>
    /// 城市索引名称
    /// </summary>
    public string CityIndex { get; set; } = "cities";

    /// <summary>
    /// 共享办公空间索引名称
    /// </summary>
    public string CoworkingIndex { get; set; } = "coworking_spaces";

    /// <summary>
    /// 分片数
    /// </summary>
    public int NumberOfShards { get; set; } = 1;

    /// <summary>
    /// 副本数
    /// </summary>
    public int NumberOfReplicas { get; set; } = 0;
}
