namespace CityService.DTOs;

/// <summary>
/// 用户收藏城市DTO
/// </summary>
public class UserFavoriteCityDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string CityId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 添加收藏请求DTO
/// </summary>
public class AddFavoriteCityRequest
{
    public string CityId { get; set; } = string.Empty;
}

/// <summary>
/// 检查收藏状态响应DTO
/// </summary>
public class CheckFavoriteStatusResponse
{
    public bool IsFavorited { get; set; }
}

/// <summary>
/// 收藏城市列表响应DTO
/// </summary>
public class FavoriteCitiesResponse
{
    public List<UserFavoriteCityDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
