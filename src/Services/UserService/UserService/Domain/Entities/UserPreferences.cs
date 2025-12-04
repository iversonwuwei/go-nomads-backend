using Postgrest.Attributes;
using Postgrest.Models;

namespace UserService.Domain.Entities;

/// <summary>
///     用户偏好设置实体 - 存储用户的个性化设置
/// </summary>
[Table("user_preferences")]
public class UserPreferences : BaseModel
{
    public UserPreferences()
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
    ///     是否启用通知
    /// </summary>
    [Column("notifications_enabled")]
    public bool NotificationsEnabled { get; set; } = true;

    /// <summary>
    ///     旅行历史是否可见
    /// </summary>
    [Column("travel_history_visible")]
    public bool TravelHistoryVisible { get; set; } = true;

    /// <summary>
    ///     个人资料是否公开
    /// </summary>
    [Column("profile_public")]
    public bool ProfilePublic { get; set; } = true;

    /// <summary>
    ///     首选货币 (USD, EUR, GBP, JPY, CNY)
    /// </summary>
    [Column("currency")]
    public string Currency { get; set; } = "USD";

    /// <summary>
    ///     温度单位 (Celsius, Fahrenheit)
    /// </summary>
    [Column("temperature_unit")]
    public string TemperatureUnit { get; set; } = "Celsius";

    /// <summary>
    ///     首选语言 (en, zh)
    /// </summary>
    [Column("language")]
    public string Language { get; set; } = "en";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    #region 工厂方法

    /// <summary>
    ///     为新用户创建默认偏好设置
    /// </summary>
    public static UserPreferences CreateDefault(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("用户ID不能为空", nameof(userId));

        return new UserPreferences
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            NotificationsEnabled = true,
            TravelHistoryVisible = true,
            ProfilePublic = true,
            Currency = "USD",
            TemperatureUnit = "Celsius",
            Language = "en",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    #endregion

    #region 领域方法

    /// <summary>
    ///     更新偏好设置
    /// </summary>
    public void Update(
        bool? notificationsEnabled = null,
        bool? travelHistoryVisible = null,
        bool? profilePublic = null,
        string? currency = null,
        string? temperatureUnit = null,
        string? language = null)
    {
        if (notificationsEnabled.HasValue)
            NotificationsEnabled = notificationsEnabled.Value;

        if (travelHistoryVisible.HasValue)
            TravelHistoryVisible = travelHistoryVisible.Value;

        if (profilePublic.HasValue)
            ProfilePublic = profilePublic.Value;

        if (!string.IsNullOrWhiteSpace(currency))
            Currency = currency;

        if (!string.IsNullOrWhiteSpace(temperatureUnit))
            TemperatureUnit = temperatureUnit;

        if (!string.IsNullOrWhiteSpace(language))
            Language = language;

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     切换通知开关
    /// </summary>
    public void ToggleNotifications()
    {
        NotificationsEnabled = !NotificationsEnabled;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     切换个人资料公开状态
    /// </summary>
    public void ToggleProfilePublic()
    {
        ProfilePublic = !ProfilePublic;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     切换旅行历史可见性
    /// </summary>
    public void ToggleTravelHistoryVisible()
    {
        TravelHistoryVisible = !TravelHistoryVisible;
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion
}
