using SearchService.Domain.Models;

namespace SearchService.Application.Interfaces;

/// <summary>
/// Elasticsearch客户端服务接口
/// </summary>
public interface IElasticsearchService
{
    /// <summary>
    /// 检查Elasticsearch连接状态
    /// </summary>
    Task<bool> IsHealthyAsync();

    /// <summary>
    /// 创建索引（如果不存在）
    /// </summary>
    Task<bool> CreateIndexIfNotExistsAsync<T>(string indexName) where T : class;

    /// <summary>
    /// 删除索引
    /// </summary>
    Task<bool> DeleteIndexAsync(string indexName);

    /// <summary>
    /// 索引单个文档
    /// </summary>
    Task<bool> IndexDocumentAsync<T>(string indexName, string id, T document) where T : class;

    /// <summary>
    /// 批量索引文档
    /// </summary>
    Task<int> BulkIndexAsync<T>(string indexName, IEnumerable<T> documents, Func<T, string> idSelector) where T : class;

    /// <summary>
    /// 删除文档
    /// </summary>
    Task<bool> DeleteDocumentAsync(string indexName, string id);

    /// <summary>
    /// 搜索文档
    /// </summary>
    Task<SearchResult<T>> SearchAsync<T>(string indexName, SearchRequest request) where T : class;

    /// <summary>
    /// 多索引搜索
    /// </summary>
    Task<SearchResult<object>> MultiSearchAsync(string[] indexNames, SearchRequest request);

    /// <summary>
    /// 获取搜索建议
    /// </summary>
    Task<SuggestResponse> GetSuggestionsAsync(string indexName, SuggestRequest request);

    /// <summary>
    /// 获取索引统计信息
    /// </summary>
    Task<IndexStats?> GetIndexStatsAsync(string indexName);
}

/// <summary>
/// 索引统计信息
/// </summary>
public class IndexStats
{
    public string IndexName { get; set; } = string.Empty;
    public long DocumentCount { get; set; }
    public long SizeInBytes { get; set; }
    public string SizeFormatted { get; set; } = string.Empty;
}
