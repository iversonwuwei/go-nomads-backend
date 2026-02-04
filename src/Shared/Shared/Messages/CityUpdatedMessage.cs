namespace Shared.Messages;

/// <summary>
///     城市信息更新消息
///     当城市名称、国家等信息变更时发布此消息
///     订阅者可以更新自己服务中的冗余数据
/// </summary>
public class CityUpdatedMessage
{
    /// <summary>
    ///     城市 ID
    /// </summary>
    public required string CityId { get; set; }

    /// <summary>
    ///     城市名称（中文）
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     城市名称（英文）
    /// </summary>
    public string? NameEn { get; set; }

    /// <summary>
    ///     城市所属国家
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    ///     国家代码
    /// </summary>
    public string? CountryCode { get; set; }

    /// <summary>
    ///     更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     更新的字段列表（可选，用于增量更新）
    /// </summary>
    public List<string>? UpdatedFields { get; set; }
}
