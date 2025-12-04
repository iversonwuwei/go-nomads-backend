namespace UserService.Application.DTOs;

/// <summary>
///     用户偏好设置 DTO
/// </summary>
public class UserPreferencesDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public bool NotificationsEnabled { get; set; } = true;
    public bool TravelHistoryVisible { get; set; } = true;
    public bool ProfilePublic { get; set; } = true;
    public string Currency { get; set; } = "USD";
    public string TemperatureUnit { get; set; } = "Celsius";
    public string Language { get; set; } = "en";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
///     更新用户偏好设置请求
/// </summary>
public class UpdateUserPreferencesRequest
{
    public bool? NotificationsEnabled { get; set; }
    public bool? TravelHistoryVisible { get; set; }
    public bool? ProfilePublic { get; set; }
    public string? Currency { get; set; }
    public string? TemperatureUnit { get; set; }
    public string? Language { get; set; }
}
