namespace Gateway.DTOs;

/// <summary>
///     首页聚合数据 DTO
/// </summary>
public class HomeFeedDto
{
    /// <summary>
    ///     推荐城市列表
    /// </summary>
    public List<CityDto> Cities { get; set; } = new();

    /// <summary>
    ///     最新活动列表
    /// </summary>
    public List<MeetupDto> Meetups { get; set; } = new();

    /// <summary>
    ///     数据时间戳
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    ///     是否有更多城市数据
    /// </summary>
    public bool HasMoreCities { get; set; }

    /// <summary>
    ///     是否有更多活动数据
    /// </summary>
    public bool HasMoreMeetups { get; set; }
}