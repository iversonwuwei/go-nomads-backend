using Microsoft.Extensions.Options;
using SearchService.Application.Interfaces;
using SearchService.Domain.Models;
using SearchService.Infrastructure.Configuration;

namespace SearchService.Application.Services;

/// <summary>
/// 搜索服务实现
/// </summary>
public class SearchApplicationService : ISearchService
{
    private readonly IElasticsearchService _elasticsearchService;
    private readonly IndexSettings _indexSettings;
    private readonly ILogger<SearchApplicationService> _logger;

    public SearchApplicationService(
        IElasticsearchService elasticsearchService,
        IOptions<IndexSettings> indexSettings,
        ILogger<SearchApplicationService> logger)
    {
        _elasticsearchService = elasticsearchService;
        _indexSettings = indexSettings.Value;
        _logger = logger;
    }

    public async Task<SearchResult<CitySearchDocument>> SearchCitiesAsync(SearchRequest request)
    {
        _logger.LogDebug("搜索城市: Query={Query}, Page={Page}, PageSize={PageSize}",
            request.Query, request.Page, request.PageSize);

        return await _elasticsearchService.SearchAsync<CitySearchDocument>(
            _indexSettings.CityIndex,
            request
        );
    }

    public async Task<SearchResult<CoworkingSearchDocument>> SearchCoworkingsAsync(SearchRequest request)
    {
        _logger.LogDebug("搜索共享办公空间: Query={Query}, Page={Page}, PageSize={PageSize}",
            request.Query, request.Page, request.PageSize);

        return await _elasticsearchService.SearchAsync<CoworkingSearchDocument>(
            _indexSettings.CoworkingIndex,
            request
        );
    }

    public async Task<UnifiedSearchResult> SearchAllAsync(SearchRequest request)
    {
        _logger.LogDebug("统一搜索: Query={Query}, Type={Type}", request.Query, request.Type);

        var result = new UnifiedSearchResult();
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // 根据类型过滤决定搜索哪些索引
        var searchType = request.Type?.ToLower();

        var tasks = new List<Task>();

        if (string.IsNullOrEmpty(searchType) || searchType == "all" || searchType == "city")
        {
            tasks.Add(Task.Run(async () =>
            {
                result.Cities = await SearchCitiesAsync(request);
            }));
        }

        if (string.IsNullOrEmpty(searchType) || searchType == "all" || searchType == "coworking")
        {
            tasks.Add(Task.Run(async () =>
            {
                result.Coworkings = await SearchCoworkingsAsync(request);
            }));
        }

        await Task.WhenAll(tasks);

        sw.Stop();
        result.TotalTook = sw.ElapsedMilliseconds;

        _logger.LogDebug("统一搜索完成: 城市={CityCount}, 共享办公={CoworkingCount}, 耗时={Took}ms",
            result.Cities.TotalCount, result.Coworkings.TotalCount, result.TotalTook);

        return result;
    }

    public async Task<SuggestResponse> GetSuggestionsAsync(SuggestRequest request)
    {
        _logger.LogDebug("获取搜索建议: Prefix={Prefix}, Type={Type}", request.Prefix, request.Type);

        var result = new SuggestResponse();
        var searchType = request.Type?.ToLower();

        // 根据类型决定从哪些索引获取建议
        if (string.IsNullOrEmpty(searchType) || searchType == "all" || searchType == "city")
        {
            var citySuggestions = await _elasticsearchService.GetSuggestionsAsync(
                _indexSettings.CityIndex,
                request
            );
            result.Suggestions.AddRange(citySuggestions.Suggestions);
        }

        if (string.IsNullOrEmpty(searchType) || searchType == "all" || searchType == "coworking")
        {
            var coworkingSuggestions = await _elasticsearchService.GetSuggestionsAsync(
                _indexSettings.CoworkingIndex,
                request
            );
            result.Suggestions.AddRange(coworkingSuggestions.Suggestions);
        }

        // 按分数排序并限制数量
        result.Suggestions = result.Suggestions
            .OrderByDescending(s => s.Score)
            .Take(request.Size)
            .ToList();

        return result;
    }

    public async Task<CitySearchDocument?> GetCityByIdAsync(Guid id)
    {
        var request = new SearchRequest
        {
            Query = id.ToString(),
            Page = 1,
            PageSize = 1,
            EnableFuzzy = false
        };

        var result = await _elasticsearchService.SearchAsync<CitySearchDocument>(
            _indexSettings.CityIndex,
            request
        );

        return result.Items.FirstOrDefault()?.Document;
    }

    public async Task<CoworkingSearchDocument?> GetCoworkingByIdAsync(Guid id)
    {
        var request = new SearchRequest
        {
            Query = id.ToString(),
            Page = 1,
            PageSize = 1,
            EnableFuzzy = false
        };

        var result = await _elasticsearchService.SearchAsync<CoworkingSearchDocument>(
            _indexSettings.CoworkingIndex,
            request
        );

        return result.Items.FirstOrDefault()?.Document;
    }
}
