namespace SearchService.Domain.Models;

/// <summary>
/// 统一搜索结果
/// </summary>
public class SearchResult<T> where T : class
{
    /// <summary>
    /// 搜索结果列表
    /// </summary>
    public List<SearchResultItem<T>> Items { get; set; } = new();

    /// <summary>
    /// 总匹配数
    /// </summary>
    public long TotalCount { get; set; }

    /// <summary>
    /// 搜索耗时（毫秒）
    /// </summary>
    public long Took { get; set; }

    /// <summary>
    /// 当前页码
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// 每页大小
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// 是否有更多数据
    /// </summary>
    public bool HasMore => Page < TotalPages;

    /// <summary>
    /// 搜索建议
    /// </summary>
    public List<string>? Suggestions { get; set; }
}

/// <summary>
/// 单个搜索结果项
/// </summary>
public class SearchResultItem<T> where T : class
{
    /// <summary>
    /// 文档内容
    /// </summary>
    public T Document { get; set; } = null!;

    /// <summary>
    /// 相关性得分
    /// </summary>
    public double? Score { get; set; }

    /// <summary>
    /// 高亮内容
    /// </summary>
    public Dictionary<string, List<string>>? Highlights { get; set; }
}

/// <summary>
/// 统一搜索请求
/// </summary>
public class SearchRequest
{
    /// <summary>
    /// 搜索关键词
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// 页码（从1开始）
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// 每页大小
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// 搜索类型过滤 (city, coworking, all)
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// 国家过滤
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// 城市ID过滤
    /// </summary>
    public Guid? CityId { get; set; }

    /// <summary>
    /// 最低评分过滤
    /// </summary>
    public decimal? MinRating { get; set; }

    /// <summary>
    /// 排序字段
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// 排序方向 (asc/desc)
    /// </summary>
    public string SortOrder { get; set; } = "desc";

    /// <summary>
    /// 地理位置搜索 - 纬度
    /// </summary>
    public double? Latitude { get; set; }

    /// <summary>
    /// 地理位置搜索 - 经度
    /// </summary>
    public double? Longitude { get; set; }

    /// <summary>
    /// 地理位置搜索 - 半径（公里）
    /// </summary>
    public double? RadiusKm { get; set; }

    /// <summary>
    /// 是否启用模糊搜索
    /// </summary>
    public bool EnableFuzzy { get; set; } = true;

    /// <summary>
    /// 是否返回高亮
    /// </summary>
    public bool EnableHighlight { get; set; } = true;
}

/// <summary>
/// 搜索建议请求
/// </summary>
public class SuggestRequest
{
    /// <summary>
    /// 输入前缀
    /// </summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>
    /// 建议类型 (city, coworking, all)
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// 返回数量
    /// </summary>
    public int Size { get; set; } = 10;
}

/// <summary>
/// 搜索建议响应
/// </summary>
public class SuggestResponse
{
    /// <summary>
    /// 建议列表
    /// </summary>
    public List<SuggestItem> Suggestions { get; set; } = new();
}

/// <summary>
/// 单个建议项
/// </summary>
public class SuggestItem
{
    /// <summary>
    /// 建议文本
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 文档ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 文档类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 得分
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// 额外信息
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}
