using Postgrest.Attributes;
using Postgrest.Models;

namespace UserService.Domain.Entities;

/// <summary>
///     用户统计数据实体 - 记录用户的游牧生活统计信息
/// </summary>
[Table("user_stats")]
public class UserStats : BaseModel
{
    public UserStats()
    {
        Id = Guid.NewGuid().ToString();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    [PrimaryKey("id", true)]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    ///     访问过的国家数量
    /// </summary>
    [Column("countries_visited")]
    public int CountriesVisited { get; set; } = 0;

    /// <summary>
    ///     居住过的城市数量
    /// </summary>
    [Column("cities_lived")]
    public int CitiesLived { get; set; } = 0;

    /// <summary>
    ///     游牧天数
    /// </summary>
    [Column("days_nomading")]
    public int DaysNomading { get; set; } = 0;

    /// <summary>
    ///     完成的旅行数量
    /// </summary>
    [Column("trips_completed")]
    public int TripsCompleted { get; set; } = 0;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    #region 工厂方法

    /// <summary>
    ///     为新用户创建初始统计数据
    /// </summary>
    public static UserStats CreateForUser(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("用户ID不能为空", nameof(userId));

        return new UserStats
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            CountriesVisited = 0,
            CitiesLived = 0,
            DaysNomading = 0,
            TripsCompleted = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    #endregion

    #region 领域方法

    /// <summary>
    ///     更新统计数据
    /// </summary>
    public void Update(
        int? countriesVisited = null,
        int? citiesLived = null,
        int? daysNomading = null,
        int? tripsCompleted = null)
    {
        if (countriesVisited.HasValue)
            CountriesVisited = Math.Max(0, countriesVisited.Value);
        
        if (citiesLived.HasValue)
            CitiesLived = Math.Max(0, citiesLived.Value);
        
        if (daysNomading.HasValue)
            DaysNomading = Math.Max(0, daysNomading.Value);
        
        if (tripsCompleted.HasValue)
            TripsCompleted = Math.Max(0, tripsCompleted.Value);
        
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     增加访问的国家数量
    /// </summary>
    public void IncrementCountriesVisited(int count = 1)
    {
        CountriesVisited += Math.Max(0, count);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     增加完成的旅行数量
    /// </summary>
    public void IncrementTripsCompleted(int count = 1)
    {
        TripsCompleted += Math.Max(0, count);
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion
}
