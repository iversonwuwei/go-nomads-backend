namespace Gateway.DTOs;

/// <summary>
///     通用分页响应负载
/// </summary>
public class PaginatedResponse<T>
{
    /// <summary>
    ///     当前页的数据集合
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    ///     数据总条数
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    ///     当前页码
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    ///     每页大小
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    ///     总页数
    /// </summary>
    public int TotalPages => PageSize == 0
        ? 0
        : (int)Math.Ceiling((double)TotalCount / PageSize);
}