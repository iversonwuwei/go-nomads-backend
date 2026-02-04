using System.Text.Json;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Mapping;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport;
using Microsoft.Extensions.Options;
using SearchService.Application.Interfaces;
using SearchService.Domain.Models;
using SearchService.Infrastructure.Configuration;

// 使用别名解决命名冲突
using DomainSearchRequest = SearchService.Domain.Models.SearchRequest;
using DomainSuggestRequest = SearchService.Domain.Models.SuggestRequest;
using AppIndexStats = SearchService.Application.Interfaces.IndexStats;
using EsIndexSettings = Elastic.Clients.Elasticsearch.IndexManagement.IndexSettings;

namespace SearchService.Infrastructure.Services;

/// <summary>
/// Elasticsearch服务实现
/// </summary>
public class ElasticsearchService : IElasticsearchService
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticsearchService> _logger;
    private readonly ElasticsearchSettings _settings;
    private readonly Infrastructure.Configuration.IndexSettings _indexSettings;

    public ElasticsearchService(
        IOptions<ElasticsearchSettings> settings,
        IOptions<Infrastructure.Configuration.IndexSettings> indexSettings,
        ILogger<ElasticsearchService> logger)
    {
        _settings = settings.Value;
        _indexSettings = indexSettings.Value;
        _logger = logger;

        var clientSettings = new ElasticsearchClientSettings(new Uri(_settings.Url))
            .DefaultIndex(_settings.DefaultIndex);

        if (!string.IsNullOrEmpty(_settings.Username) && !string.IsNullOrEmpty(_settings.Password))
        {
            clientSettings.Authentication(new BasicAuthentication(_settings.Username, _settings.Password));
        }

        if (_settings.EnableDebugMode)
        {
            clientSettings.EnableDebugMode();
        }

        _client = new ElasticsearchClient(clientSettings);
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            var response = await _client.PingAsync();
            return response.IsValidResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Elasticsearch 健康检查失败");
            return false;
        }
    }

    public async Task<bool> CreateIndexIfNotExistsAsync<T>(string indexName) where T : class
    {
        try
        {
            var existsResponse = await _client.Indices.ExistsAsync(indexName);
            if (existsResponse.Exists)
            {
                _logger.LogInformation("索引 {IndexName} 已存在", indexName);
                return true;
            }

            // 根据类型创建不同的索引映射
            CreateIndexResponse createResponse;

            if (typeof(T) == typeof(CitySearchDocument))
            {
                createResponse = await CreateCityIndexAsync(indexName);
            }
            else if (typeof(T) == typeof(CoworkingSearchDocument))
            {
                createResponse = await CreateCoworkingIndexAsync(indexName);
            }
            else
            {
                // 默认创建简单索引
                createResponse = await _client.Indices.CreateAsync(indexName);
            }

            if (createResponse.IsValidResponse)
            {
                _logger.LogInformation("成功创建索引 {IndexName}", indexName);
                return true;
            }

            _logger.LogError("创建索引 {IndexName} 失败: {Reason}", indexName, createResponse.DebugInformation);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建索引 {IndexName} 时发生异常", indexName);
            return false;
        }
    }

    private async Task<CreateIndexResponse> CreateCityIndexAsync(string indexName)
    {
        var request = new CreateIndexRequest(indexName)
        {
            Settings = BuildCommonSettings(),
            Mappings = new TypeMapping
            {
                Properties = new Properties
                {
                    { "id", new KeywordProperty() },
                    { "name", CreateTextWithKeyword() },
                    { "nameEn", CreateTextWithKeyword() },
                    { "country", CreateTextWithKeyword() },
                    { "countryId", new KeywordProperty() },
                    { "provinceId", new KeywordProperty() },
                    { "region", CreateTextWithKeyword() },
                    { "description", new TextProperty { Analyzer = "search_text" } },
                    { "latitude", new DoubleNumberProperty() },
                    { "longitude", new DoubleNumberProperty() },
                    { "location", new GeoPointProperty() },
                    { "population", new LongNumberProperty() },
                    { "climate", CreateTextWithKeyword() },
                    { "timeZone", CreateTextWithKeyword() },
                    { "currency", CreateTextWithKeyword() },
                    { "imageUrl", new KeywordProperty { IgnoreAbove = 512 } },
                    { "portraitImageUrl", new KeywordProperty { IgnoreAbove = 512 } },
                    { "overallScore", new DoubleNumberProperty() },
                    { "internetQualityScore", new DoubleNumberProperty() },
                    { "safetyScore", new DoubleNumberProperty() },
                    { "costScore", new DoubleNumberProperty() },
                    { "communityScore", new DoubleNumberProperty() },
                    { "weatherScore", new DoubleNumberProperty() },
                    { "tags", new KeywordProperty() },
                    { "isActive", new BooleanProperty() },
                    { "createdAt", new DateProperty() },
                    { "updatedAt", new DateProperty() },
                    { "averageCost", new DoubleNumberProperty() },
                    { "userCount", new IntegerNumberProperty() },
                    { "moderatorId", new KeywordProperty() },
                    { "moderatorName", CreateTextWithKeyword() },
                    { "moderatorCount", new IntegerNumberProperty() },
                    { "coworkingCount", new IntegerNumberProperty() },
                    { "meetupCount", new IntegerNumberProperty() },
                    { "reviewCount", new IntegerNumberProperty() },
                    { "suggest", new CompletionProperty { Analyzer = "edge_ngram", SearchAnalyzer = "search_text" } },
                    { "documentType", new KeywordProperty() }
                }
            }
        };

        return await _client.Indices.CreateAsync(request);
    }

    private async Task<CreateIndexResponse> CreateCoworkingIndexAsync(string indexName)
    {
        var request = new CreateIndexRequest(indexName)
        {
            Settings = BuildCommonSettings(),
            Mappings = new TypeMapping
            {
                Properties = new Properties
                {
                    { "id", new KeywordProperty() },
                    { "name", CreateTextWithKeyword() },
                    { "cityId", new KeywordProperty() },
                    { "cityName", CreateTextWithKeyword() },
                    { "countryName", CreateTextWithKeyword() },
                    { "address", CreateTextWithKeyword() },
                    { "description", new TextProperty { Analyzer = "search_text" } },
                    { "imageUrl", new KeywordProperty { IgnoreAbove = 512 } },
                    { "pricePerDay", new DoubleNumberProperty() },
                    { "pricePerMonth", new DoubleNumberProperty() },
                    { "pricePerHour", new DoubleNumberProperty() },
                    { "currency", CreateTextWithKeyword() },
                    { "rating", new DoubleNumberProperty() },
                    { "reviewCount", new IntegerNumberProperty() },
                    { "wifiSpeed", new DoubleNumberProperty() },
                    { "desks", new IntegerNumberProperty() },
                    { "meetingRooms", new IntegerNumberProperty() },
                    { "hasMeetingRoom", new BooleanProperty() },
                    { "hasCoffee", new BooleanProperty() },
                    { "hasParking", new BooleanProperty() },
                    { "has247Access", new BooleanProperty() },
                    { "amenities", new KeywordProperty() },
                    { "capacity", new IntegerNumberProperty() },
                    { "latitude", new DoubleNumberProperty() },
                    { "longitude", new DoubleNumberProperty() },
                    { "location", new GeoPointProperty() },
                    { "phone", new KeywordProperty { IgnoreAbove = 128 } },
                    { "email", new KeywordProperty { IgnoreAbove = 256 } },
                    { "website", new KeywordProperty { IgnoreAbove = 256 } },
                    { "openingHours", new TextProperty { Analyzer = "search_text" } },
                    { "isActive", new BooleanProperty() },
                    { "verificationStatus", new KeywordProperty() },
                    { "createdAt", new DateProperty() },
                    { "updatedAt", new DateProperty() },
                    { "suggest", new CompletionProperty { Analyzer = "edge_ngram", SearchAnalyzer = "search_text" } },
                    { "documentType", new KeywordProperty() }
                }
            }
        };

        return await _client.Indices.CreateAsync(request);
    }

    private EsIndexSettings BuildCommonSettings()
    {
        return new EsIndexSettings
        {
            NumberOfShards = _indexSettings.NumberOfShards,
            NumberOfReplicas = _indexSettings.NumberOfReplicas
        };
    }

    private static TextProperty CreateTextWithKeyword()
    {
        return new TextProperty
        {
            Analyzer = "search_text",
            Fields = new Properties
            {
                { "keyword", new KeywordProperty { IgnoreAbove = 256 } }
            }
        };
    }

    public async Task<bool> DeleteIndexAsync(string indexName)
    {
        try
        {
            var response = await _client.Indices.DeleteAsync(indexName);

            if (response.IsValidResponse)
            {
                _logger.LogInformation("成功删除索引 {IndexName}", indexName);
                return true;
            }

            _logger.LogWarning("删除索引 {IndexName} 失败: {Reason}", indexName, response.DebugInformation);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除索引 {IndexName} 时发生异常", indexName);
            return false;
        }
    }

    public async Task<bool> IndexDocumentAsync<T>(string indexName, string id, T document) where T : class
    {
        try
        {
            var response = await _client.IndexAsync(document, idx => idx
                .Index(indexName)
                .Id(id)
            );

            if (response.IsValidResponse)
            {
                _logger.LogDebug("成功索引文档 {Id} 到 {IndexName}", id, indexName);
                return true;
            }

            _logger.LogWarning("索引文档 {Id} 到 {IndexName} 失败: {Reason}", id, indexName, response.DebugInformation);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "索引文档 {Id} 到 {IndexName} 时发生异常", id, indexName);
            return false;
        }
    }

    public async Task<int> BulkIndexAsync<T>(string indexName, IEnumerable<T> documents, Func<T, string> idSelector) where T : class
    {
        try
        {
            var docList = documents.ToList();
            if (docList.Count == 0) return 0;

            var bulkResponse = await _client.BulkAsync(b => b
                .Index(indexName)
                .IndexMany(docList, (descriptor, doc) => descriptor.Id(idSelector(doc)))
            );

            if (bulkResponse.IsValidResponse)
            {
                var successCount = docList.Count - (bulkResponse.ItemsWithErrors?.Count() ?? 0);
                _logger.LogInformation("批量索引到 {IndexName} 完成: 成功 {SuccessCount}/{Total}", 
                    indexName, successCount, docList.Count);
                return successCount;
            }

            _logger.LogWarning("批量索引到 {IndexName} 部分失败: {Reason}", indexName, bulkResponse.DebugInformation);
            return docList.Count - (bulkResponse.ItemsWithErrors?.Count() ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量索引到 {IndexName} 时发生异常", indexName);
            return 0;
        }
    }

    public async Task<bool> DeleteDocumentAsync(string indexName, string id)
    {
        try
        {
            var response = await _client.DeleteAsync<object>(indexName, id);

            if (response.IsValidResponse)
            {
                _logger.LogDebug("成功删除文档 {Id} 从 {IndexName}", id, indexName);
                return true;
            }

            _logger.LogWarning("删除文档 {Id} 从 {IndexName} 失败: {Reason}", id, indexName, response.DebugInformation);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除文档 {Id} 从 {IndexName} 时发生异常", id, indexName);
            return false;
        }
    }

    public async Task<SearchResult<T>> SearchAsync<T>(string indexName, DomainSearchRequest request) where T : class
    {
        var result = new SearchResult<T>
        {
            Page = request.Page,
            PageSize = request.PageSize
        };

        try
        {
            var from = (request.Page - 1) * request.PageSize;
            
            SearchResponse<T> searchResponse;
            
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                searchResponse = await _client.SearchAsync<T>(s => s
                    .Index(indexName)
                    .From(from)
                    .Size(request.PageSize)
                    .Query(q => q.MatchAll(new MatchAllQuery()))
                );
            }
            else
            {
                searchResponse = await _client.SearchAsync<T>(s => s
                    .Index(indexName)
                    .From(from)
                    .Size(request.PageSize)
                    .Query(q => q
                        .MultiMatch(mm => mm
                            .Query(request.Query)
                            .Fields(new[] { "name^3", "nameEn^3", "nameCn^3", "description", "country", "address", "cityName" })
                            .Fuzziness(new Fuzziness("AUTO"))
                        )
                    )
                );
            }

            if (searchResponse.IsValidResponse)
            {
                result.TotalCount = searchResponse.Total;
                result.Took = searchResponse.Took;
                result.Items = searchResponse.Hits.Select(hit => new SearchResultItem<T>
                {
                    Document = hit.Source!,
                    Score = hit.Score,
                    Highlights = hit.Highlight?.ToDictionary(
                        h => h.Key,
                        h => h.Value.ToList()
                    )
                }).ToList();
            }
            else
            {
                _logger.LogWarning("搜索 {IndexName} 失败: {Reason}", indexName, searchResponse.DebugInformation);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索 {IndexName} 时发生异常", indexName);
        }

        return result;
    }

    public async Task<SearchResult<object>> MultiSearchAsync(string[] indexNames, DomainSearchRequest request)
    {
        var result = new SearchResult<object>
        {
            Page = request.Page,
            PageSize = request.PageSize
        };

        try
        {
            var from = (request.Page - 1) * request.PageSize;
            var indices = string.Join(",", indexNames);

            SearchResponse<object> searchResponse;

            if (string.IsNullOrWhiteSpace(request.Query))
            {
                searchResponse = await _client.SearchAsync<object>(s => s
                    .Index(indices)
                    .From(from)
                    .Size(request.PageSize)
                    .Query(q => q.MatchAll(new MatchAllQuery()))
                );
            }
            else
            {
                searchResponse = await _client.SearchAsync<object>(s => s
                    .Index(indices)
                    .From(from)
                    .Size(request.PageSize)
                    .Query(q => q
                        .MultiMatch(mm => mm
                            .Query(request.Query)
                            .Fields(new[] { "name^3", "nameEn^3", "nameCn^3", "description", "country", "address", "cityName" })
                            .Fuzziness(new Fuzziness("AUTO"))
                        )
                    )
                );
            }

            if (searchResponse.IsValidResponse)
            {
                result.TotalCount = searchResponse.Total;
                result.Took = searchResponse.Took;
                result.Items = searchResponse.Hits.Select(hit => new SearchResultItem<object>
                {
                    Document = hit.Source!,
                    Score = hit.Score,
                    Highlights = hit.Highlight?.ToDictionary(
                        h => h.Key,
                        h => h.Value.ToList()
                    )
                }).ToList();
            }
            else
            {
                _logger.LogWarning("多索引搜索失败: {Reason}", searchResponse.DebugInformation);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "多索引搜索时发生异常");
        }

        return result;
    }

    public async Task<SuggestResponse> GetSuggestionsAsync(string indexName, DomainSuggestRequest request)
    {
        var result = new SuggestResponse();

        try
        {
            // 使用前缀查询获取建议
            var searchResponse = await _client.SearchAsync<object>(s => s
                .Index(indexName)
                .Size(request.Size)
                .Query(q => q
                    .Bool(b => b
                        .Should(
                            sh => sh.Prefix(p => p.Field(new Field("name")).Value(request.Prefix)),
                            sh => sh.Prefix(p => p.Field(new Field("nameEn")).Value(request.Prefix))
                        )
                    )
                )
            );

            if (searchResponse.IsValidResponse)
            {
                result.Suggestions = searchResponse.Hits
                    .Select(hit =>
                    {
                        if (hit.Source is JsonElement doc)
                        {
                            var text = string.Empty;
                            var id = string.Empty;
                            var type = string.Empty;

                            if (doc.TryGetProperty("name", out var nameProp))
                            {
                                text = nameProp.GetString() ?? string.Empty;
                            }
                            if (doc.TryGetProperty("id", out var idProp))
                            {
                                id = idProp.GetString() ?? string.Empty;
                            }
                            if (doc.TryGetProperty("documentType", out var typeProp))
                            {
                                type = typeProp.GetString() ?? string.Empty;
                            }

                            return new SuggestItem
                            {
                                Text = text,
                                Id = id,
                                Type = type,
                                Score = hit.Score ?? 0
                            };
                        }
                        return null;
                    })
                    .Where(s => s != null && !string.IsNullOrEmpty(s.Text))
                    .Cast<SuggestItem>()
                    .Take(request.Size)
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取搜索建议时发生异常");
        }

        return result;
    }

    public async Task<AppIndexStats?> GetIndexStatsAsync(string indexName)
    {
        try
        {
            var indices = Indices.Parse(indexName);
            var response = await _client.Indices.StatsAsync(new IndicesStatsRequest(indices));

            if (response.IsValidResponse && response.Indices != null && response.Indices.TryGetValue(indexName, out var indexStats))
            {
                var totalDocs = indexStats.Primaries?.Docs?.Count ?? 0;
                var sizeBytes = indexStats.Primaries?.Store?.SizeInBytes ?? 0;

                return new AppIndexStats
                {
                    IndexName = indexName,
                    DocumentCount = totalDocs,
                    SizeInBytes = sizeBytes,
                    SizeFormatted = FormatBytes(sizeBytes)
                };
            }

            _logger.LogWarning("获取索引 {IndexName} 统计信息失败", indexName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取索引 {IndexName} 统计信息时发生异常", indexName);
            return null;
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
