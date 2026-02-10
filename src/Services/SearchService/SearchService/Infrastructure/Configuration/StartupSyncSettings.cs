namespace SearchService.Infrastructure.Configuration;

/// <summary>
/// 启动时自动同步配置
/// </summary>
public class StartupSyncSettings
{
    /// <summary>
    /// 是否启用启动时自动同步
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 是否强制同步（无论索引是否有数据都执行全量同步）
    /// </summary>
    public bool ForceSync { get; set; } = false;

    /// <summary>
    /// 等待 Elasticsearch 就绪的最大重试次数
    /// </summary>
    public int MaxRetries { get; set; } = 30;

    /// <summary>
    /// 等待 Elasticsearch 就绪的重试间隔（秒）
    /// </summary>
    public int RetryIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// 等待依赖服务就绪的时间（秒），给上游服务（CityService, CoworkingService）启动时间
    /// </summary>
    public int WaitForServicesSeconds { get; set; } = 10;

    /// <summary>
    /// 同步执行的最大重试次数
    /// </summary>
    public int MaxSyncRetries { get; set; } = 3;

    /// <summary>
    /// 索引文档最小阈值。文档数低于此阈值时触发自动同步
    /// </summary>
    public int MinDocumentThreshold { get; set; } = 1;
}
