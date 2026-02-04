using SearchService.Domain.Models;

namespace SearchService.Application.Interfaces;

/// <summary>
/// 索引同步服务接口
/// </summary>
public interface IIndexSyncService
{
    /// <summary>
    /// 同步所有城市数据到Elasticsearch
    /// </summary>
    Task<SyncResult> SyncAllCitiesAsync();

    /// <summary>
    /// 同步单个城市数据
    /// </summary>
    Task<bool> SyncCityAsync(Guid cityId);

    /// <summary>
    /// 删除城市索引
    /// </summary>
    Task<bool> DeleteCityAsync(Guid cityId);

    /// <summary>
    /// 同步所有共享办公空间数据到Elasticsearch
    /// </summary>
    Task<SyncResult> SyncAllCoworkingsAsync();

    /// <summary>
    /// 同步单个共享办公空间数据
    /// </summary>
    Task<bool> SyncCoworkingAsync(Guid coworkingId);

    /// <summary>
    /// 删除共享办公空间索引
    /// </summary>
    Task<bool> DeleteCoworkingAsync(Guid coworkingId);

    /// <summary>
    /// 同步所有数据
    /// </summary>
    Task<SyncResult> SyncAllAsync();

    /// <summary>
    /// 重建所有索引
    /// </summary>
    Task<SyncResult> RebuildAllIndexesAsync();
}

/// <summary>
/// 同步结果
/// </summary>
public class SyncResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 成功同步的文档数
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// 失败的文档数
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 同步耗时（毫秒）
    /// </summary>
    public long ElapsedMilliseconds { get; set; }

    /// <summary>
    /// 详细信息
    /// </summary>
    public Dictionary<string, object>? Details { get; set; }
}
