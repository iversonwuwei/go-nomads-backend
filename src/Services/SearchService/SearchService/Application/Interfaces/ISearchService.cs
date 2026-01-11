using SearchService.Domain.Models;

namespace SearchService.Application.Interfaces;

/// <summary>
/// 搜索服务接口
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// 搜索城市
    /// </summary>
    Task<SearchResult<CitySearchDocument>> SearchCitiesAsync(SearchRequest request);

    /// <summary>
    /// 搜索共享办公空间
    /// </summary>
    Task<SearchResult<CoworkingSearchDocument>> SearchCoworkingsAsync(SearchRequest request);

    /// <summary>
    /// 统一搜索（同时搜索城市和共享办公空间）
    /// </summary>
    Task<UnifiedSearchResult> SearchAllAsync(SearchRequest request);

    /// <summary>
    /// 获取搜索建议
    /// </summary>
    Task<SuggestResponse> GetSuggestionsAsync(SuggestRequest request);

    /// <summary>
    /// 按ID获取城市文档
    /// </summary>
    Task<CitySearchDocument?> GetCityByIdAsync(Guid id);

    /// <summary>
    /// 按ID获取共享办公空间文档
    /// </summary>
    Task<CoworkingSearchDocument?> GetCoworkingByIdAsync(Guid id);
}

/// <summary>
/// 统一搜索结果
/// </summary>
public class UnifiedSearchResult
{
    /// <summary>
    /// 城市搜索结果
    /// </summary>
    public SearchResult<CitySearchDocument> Cities { get; set; } = new();

    /// <summary>
    /// 共享办公空间搜索结果
    /// </summary>
    public SearchResult<CoworkingSearchDocument> Coworkings { get; set; } = new();

    /// <summary>
    /// 总搜索耗时（毫秒）
    /// </summary>
    public long TotalTook { get; set; }

    /// <summary>
    /// 总匹配数
    /// </summary>
    public long TotalCount => Cities.TotalCount + Coworkings.TotalCount;
}
