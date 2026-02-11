namespace UserService.Application.DTOs;

/// <summary>
///     用户统计数据 DTO
/// </summary>
public class UserStatsDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    ///     访问过的国家数量
    /// </summary>
    public int CountriesVisited { get; set; }
    
    /// <summary>
    ///     居住过的城市数量
    /// </summary>
    public int CitiesLived { get; set; }
    
    /// <summary>
    ///     游牧天数
    /// </summary>
    public int DaysNomading { get; set; }
    
    /// <summary>
    ///     用户创建的 Meetup 数量（从 EventService 获取）
    /// </summary>
    public int MeetupsCreated { get; set; }
    
    /// <summary>
    ///     用户参加的未结束 Meetup 数量（从 EventService 获取）
    /// </summary>
    public int MeetupsJoined { get; set; }
    
    /// <summary>
    ///     用户正在进行的 Meetup 数量：已加入 + 已创建（去重，仅 upcoming/ongoing）
    /// </summary>
    public int ActiveMeetups { get; set; }
    
    /// <summary>
    ///     完成的旅行数量
    /// </summary>
    public int TripsCompleted { get; set; }
    
    /// <summary>
    ///     收藏的城市数量（从 CityService 获取）
    /// </summary>
    public int FavoriteCitiesCount { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
///     更新用户统计数据请求 DTO
/// </summary>
public class UpdateUserStatsRequest
{
    public int? CountriesVisited { get; set; }
    public int? CitiesLived { get; set; }
    public int? DaysNomading { get; set; }
    public int? TripsCompleted { get; set; }
}
