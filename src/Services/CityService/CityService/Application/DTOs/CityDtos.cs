using System.ComponentModel.DataAnnotations;

namespace CityService.Application.DTOs;

/// <summary>
/// 基础 DTO - 包含当前用户上下文信息
/// </summary>
public abstract class BaseDtoWithUserContext
{
    /// <summary>
    /// 当前登录用户是否为管理员
    /// </summary>
    public bool IsCurrentUserAdmin { get; set; }

    /// <summary>
    /// 设置当前用户上下文
    /// </summary>
    public virtual void SetUserContext(Guid? currentUserId, string? currentUserRole)
    {
        IsCurrentUserAdmin = currentUserRole?.ToLower() == "admin";
    }
}

public class CityDto : BaseDtoWithUserContext
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string Country { get; set; } = string.Empty;
    public string? Region { get; set; }
    public string? Description { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? Population { get; set; }
    public string? Climate { get; set; }
    public string? TimeZone { get; set; }
    public string? Currency { get; set; }
    public string? ImageUrl { get; set; }
    public decimal? AverageCostOfLiving { get; set; }
    public decimal? OverallScore { get; set; }
    public decimal? InternetQualityScore { get; set; }
    public decimal? SafetyScore { get; set; }
    public decimal? CostScore { get; set; }
    public decimal? CommunityScore { get; set; }
    public decimal? WeatherScore { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public WeatherDto? Weather { get; set; }
    public int MeetupCount { get; set; }
    public int CoworkingCount { get; set; }
    
    /// <summary>
    /// 当前用户是否已收藏该城市
    /// 注意: 此字段需要在查询时根据当前用户动态填充
    /// </summary>
    public bool IsFavorite { get; set; }

    /// <summary>
    /// 城市版主ID
    /// </summary>
    public Guid? ModeratorId { get; set; }

    /// <summary>
    /// 城市版主信息
    /// </summary>
    public ModeratorDto? Moderator { get; set; }

    /// <summary>
    /// 当前登录用户是否为该城市的版主
    /// </summary>
    public bool IsCurrentUserModerator { get; set; }

    /// <summary>
    /// 重写基类方法，设置当前用户在此城市的权限上下文
    /// </summary>
    public override void SetUserContext(Guid? currentUserId, string? currentUserRole)
    {
        base.SetUserContext(currentUserId, currentUserRole);

        // 判断当前用户是否为该城市的版主
        if (currentUserId.HasValue && ModeratorId.HasValue)
        {
            IsCurrentUserModerator = currentUserId.Value == ModeratorId.Value;
        }
    }
}

/// <summary>
/// 版主信息DTO
/// </summary>
public class ModeratorDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Avatar { get; set; }
}

/// <summary>
/// 申请成为城市版主DTO
/// </summary>
public class ApplyModeratorDto
{
    [Required]
    public Guid CityId { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }
}

/// <summary>
/// 指定城市版主DTO (仅管理员)
/// </summary>
public class AssignModeratorDto
{
    [Required]
    public Guid CityId { get; set; }

    [Required]
    public Guid UserId { get; set; }
}

public class CreateCityDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? NameEn { get; set; }

    [Required]
    [MaxLength(100)]
    public string Country { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Region { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? Population { get; set; }

    [MaxLength(50)]
    public string? Climate { get; set; }

    [MaxLength(50)]
    public string? TimeZone { get; set; }

    [MaxLength(10)]
    public string? Currency { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public decimal? AverageCostOfLiving { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class UpdateCityDto
{
    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(100)]
    public string? NameEn { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    [MaxLength(100)]
    public string? Region { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? Population { get; set; }

    [MaxLength(50)]
    public string? Climate { get; set; }

    [MaxLength(50)]
    public string? TimeZone { get; set; }

    [MaxLength(10)]
    public string? Currency { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public decimal? AverageCostOfLiving { get; set; }
    public List<string>? Tags { get; set; }
    public bool? IsActive { get; set; }

    /// <summary>
    /// 城市版主ID - 仅管理员可设置
    /// </summary>
    public Guid? ModeratorId { get; set; }
}

public class CitySearchDto
{
    public string? Name { get; set; }
    public string? Country { get; set; }
    public string? Region { get; set; }
    public decimal? MinCostOfLiving { get; set; }
    public decimal? MaxCostOfLiving { get; set; }
    public decimal? MinScore { get; set; }
    public List<string>? Tags { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class CityStatisticsDto
{
    public Guid CityId { get; set; }
    public string CityName { get; set; } = string.Empty;
    public int TotalCoworkingSpaces { get; set; }
    public int TotalAccommodations { get; set; }
    public int TotalEvents { get; set; }
    public int TotalNomads { get; set; }
    public decimal AverageRating { get; set; }
}

/// <summary>
/// 城市版主详细信息DTO（支持多版主）
/// </summary>
public class CityModeratorDto
{
    public Guid Id { get; set; }
    public Guid CityId { get; set; }
    public Guid UserId { get; set; }

    /// <summary>
    /// 用户信息
    /// </summary>
    public ModeratorUserDto User { get; set; } = null!;

    /// <summary>
    /// 权限设置
    /// </summary>
    public bool CanEditCity { get; set; }
    public bool CanManageCoworks { get; set; }
    public bool CanManageCosts { get; set; }
    public bool CanManageVisas { get; set; }
    public bool CanModerateChats { get; set; }

    /// <summary>
    /// 指定信息
    /// </summary>
    public Guid? AssignedBy { get; set; }
    public DateTime AssignedAt { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public bool IsActive { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 版主用户信息DTO
/// </summary>
public class ModeratorUserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string Role { get; set; } = string.Empty;
}

/// <summary>
/// 添加城市版主请求DTO
/// </summary>
public class AddCityModeratorDto
{
    [Required]
    public Guid CityId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// 权限设置（可选，默认全部为 true）
    /// </summary>
    public bool CanEditCity { get; set; } = true;
    public bool CanManageCoworks { get; set; } = true;
    public bool CanManageCosts { get; set; } = true;
    public bool CanManageVisas { get; set; } = true;
    public bool CanModerateChats { get; set; } = true;

    /// <summary>
    /// 备注信息
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }
}

/// <summary>
/// 更新城市版主权限请求DTO
/// </summary>
public class UpdateCityModeratorDto
{
    public bool? CanEditCity { get; set; }
    public bool? CanManageCoworks { get; set; }
    public bool? CanManageCosts { get; set; }
    public bool? CanManageVisas { get; set; }
    public bool? CanModerateChats { get; set; }
    public bool? IsActive { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
