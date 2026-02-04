namespace SearchService.Infrastructure.Configuration;

/// <summary>
/// 索引维护任务相关配置
/// </summary>
public class IndexMaintenanceSettings
{
    /// <summary>
    /// 是否启用定期校验/补全任务
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 首次执行前的延迟（分钟），避免服务刚启动时立即全量拉取
    /// </summary>
    public int InitialDelayMinutes { get; set; } = 5;

    /// <summary>
    /// 周期执行间隔（分钟）
    /// </summary>
    public int VerifyIntervalMinutes { get; set; } = 60;
}
