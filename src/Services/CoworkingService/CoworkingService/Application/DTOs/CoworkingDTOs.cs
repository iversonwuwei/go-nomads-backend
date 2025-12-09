using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CoworkingService.Domain.Entities;

namespace CoworkingService.Application.DTOs;

/// <summary>
///     验证资格检查响应 DTO
/// </summary>
public class VerificationEligibilityResponse
{
    /// <summary>
    ///     是否可以验证
    /// </summary>
    public bool CanVerify { get; set; }

    /// <summary>
    ///     不能验证的原因
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    ///     原因代码: ALREADY_VERIFIED, IS_CREATOR, ALREADY_VOTED, SPACE_VERIFIED
    /// </summary>
    public string? ReasonCode { get; set; }

    /// <summary>
    ///     该 Coworking 空间是否已经是已验证状态
    /// </summary>
    public bool IsSpaceVerified { get; set; }

    /// <summary>
    ///     当前投票数
    /// </summary>
    public int CurrentVotes { get; set; }
}

/// <summary>
///     Coworking 空间响应 DTO
/// </summary>
public class CoworkingSpaceResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? CityId { get; set; }
    public Guid? CreatedBy { get; set; }
    public string? CreatorName { get; set; }
    public string Address { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string[]? Images { get; set; }
    public decimal? PricePerDay { get; set; }
    public decimal? PricePerMonth { get; set; }
    public decimal? PricePerHour { get; set; }
    public decimal? PricePerWeek { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal Rating { get; set; }
    public int ReviewCount { get; set; }
    public decimal? WifiSpeed { get; set; }
    public int? Desks { get; set; }
    public int? MeetingRooms { get; set; }
    public bool HasMeetingRoom { get; set; }
    public bool HasCoffee { get; set; }
    public bool HasParking { get; set; }
    public bool Has247Access { get; set; }
    public string[]? Amenities { get; set; }
    public int? Capacity { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? OpeningHours { get; set; }
    public bool IsActive { get; set; }
    public string VerificationStatus { get; set; } = CoworkingVerificationStatus.Unverified;
    public int VerificationVotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsOwner { get; set; }
}

/// <summary>
///     创建共享办公空间请求 DTO
/// </summary>
public class CreateCoworkingSpaceRequest
{
    [Required(ErrorMessage = "名称不能为空")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public Guid? CityId { get; set; }

    [Required(ErrorMessage = "地址不能为空")] public string Address { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string[]? Images { get; set; }

    [Range(0, 10000, ErrorMessage = "日价格必须在 0-10000 之间")]
    public decimal? PricePerDay { get; set; }

    [Range(0, 50000, ErrorMessage = "月价格必须在 0-50000 之间")]
    public decimal? PricePerMonth { get; set; }

    [Range(0, 500, ErrorMessage = "时价格必须在 0-500 之间")]
    public decimal? PricePerHour { get; set; }

    [Range(0, 5000, ErrorMessage = "周价格必须在 0-5000 之间")]
    public decimal? PricePerWeek { get; set; }

    [MaxLength(10)] public string Currency { get; set; } = "USD";

    [Range(0, 1000, ErrorMessage = "Wifi 速度必须在 0-1000 之间")]
    public decimal? WifiSpeed { get; set; }

    [Range(0, 500, ErrorMessage = "桌位数量必须在 0-500 之间")]
    public int? Desks { get; set; }

    [Range(0, 100, ErrorMessage = "会议室数量必须在 0-100 之间")]
    public int? MeetingRooms { get; set; }

    public bool HasMeetingRoom { get; set; }
    public bool HasCoffee { get; set; }
    public bool HasParking { get; set; }
    public bool Has247Access { get; set; }
    public string[]? Amenities { get; set; }

    [Range(1, 1000, ErrorMessage = "容量必须在 1-1000 之间")]
    public int? Capacity { get; set; }

    [Range(-90, 90, ErrorMessage = "纬度必须在 -90 到 90 之间")]
    public decimal? Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "经度必须在 -180 到 180 之间")]
    public decimal? Longitude { get; set; }

    // 移除 [Phone] 验证以支持各种电话号码格式(中国手机/固话等)
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "无效的电子邮件地址")]
    public string? Email { get; set; }

    [Url(ErrorMessage = "无效的网站 URL")] public string? Website { get; set; }

    public string? OpeningHours { get; set; }

    public Guid? CreatedBy { get; set; }

    [JsonIgnore]
    public string VerificationStatus { get; set; } = CoworkingVerificationStatus.Unverified;
}

/// <summary>
///     更新共享办公空间请求 DTO
/// </summary>
public class UpdateCoworkingSpaceRequest
{
    [Required(ErrorMessage = "名称不能为空")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public Guid? CityId { get; set; }

    [Required(ErrorMessage = "地址不能为空")] public string Address { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string[]? Images { get; set; }

    [Range(0, 10000)] public decimal? PricePerDay { get; set; }

    [Range(0, 50000)] public decimal? PricePerMonth { get; set; }

    [Range(0, 500)] public decimal? PricePerHour { get; set; }

    [Range(0, 5000)] public decimal? PricePerWeek { get; set; }

    [MaxLength(10)] public string Currency { get; set; } = "USD";

    [Range(0, 1000)] public decimal? WifiSpeed { get; set; }

    [Range(0, 500)] public int? Desks { get; set; }

    [Range(0, 100)] public int? MeetingRooms { get; set; }

    public bool HasMeetingRoom { get; set; }
    public bool HasCoffee { get; set; }
    public bool HasParking { get; set; }
    public bool Has247Access { get; set; }
    public string[]? Amenities { get; set; }

    [Range(1, 1000)] public int? Capacity { get; set; }

    [Range(-90, 90)] public decimal? Latitude { get; set; }

    [Range(-180, 180)] public decimal? Longitude { get; set; }

    // 移除 [Phone] 验证以支持各种电话号码格式(中国手机/固话等)
    public string? Phone { get; set; }

    [EmailAddress] public string? Email { get; set; }

    [Url] public string? Website { get; set; }

    public string? OpeningHours { get; set; }

    public Guid? UpdatedBy { get; set; }
}

/// <summary>
///     更新 Coworking 认证状态请求 DTO
/// </summary>
public class UpdateCoworkingVerificationStatusRequest
{
    [Required(ErrorMessage = "状态不能为空")]
    [RegularExpression("^(verified|unverified)$", ErrorMessage = "状态必须是 verified 或 unverified")]
    public string VerificationStatus { get; set; } = CoworkingVerificationStatus.Unverified;

    [JsonIgnore]
    public Guid? UpdatedBy { get; set; }
}

/// <summary>
///     分页的共享办公空间列表响应
/// </summary>
public class PaginatedCoworkingSpacesResponse
{
    public List<CoworkingSpaceResponse> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

/// <summary>
///     Coworking 预订响应 DTO
/// </summary>
public class CoworkingBookingResponse
{
    public Guid Id { get; set; }
    public Guid CoworkingId { get; set; }
    public Guid UserId { get; set; }
    public DateTime BookingDate { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public string BookingType { get; set; } = "daily";
    public decimal TotalPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = "pending";
    public string? SpecialRequests { get; set; }
    public DateTime CreatedAt { get; set; }

    // 附加信息
    public string? CoworkingName { get; set; }
    public string? CoworkingAddress { get; set; }
}

/// <summary>
///     创建预订请求 DTO
/// </summary>
public class CreateBookingRequest
{
    [Required(ErrorMessage = "共享办公空间 ID 不能为空")]
    public Guid CoworkingId { get; set; }

    [Required(ErrorMessage = "用户 ID 不能为空")]
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "预订日期不能为空")] public DateTime BookingDate { get; set; }

    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }

    [Required(ErrorMessage = "预订类型不能为空")]
    [RegularExpression("^(hourly|daily|monthly)$", ErrorMessage = "预订类型必须是 hourly、daily 或 monthly")]
    public string BookingType { get; set; } = "daily";

    public string? SpecialRequests { get; set; }
}

/// <summary>
///     Coworking 评论响应 DTO
/// </summary>
public class CoworkingCommentResponse
{
    public Guid Id { get; set; }
    public Guid CoworkingId { get; set; }
    public Guid UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public int? Rating { get; set; }
    public List<string>? Images { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
///     创建评论请求 DTO
/// </summary>
public class CreateCoworkingCommentRequest
{
    [Required(ErrorMessage = "评论内容不能为空")]
    [MaxLength(1000, ErrorMessage = "评论内容不能超过1000字")]
    public string Content { get; set; } = string.Empty;

    [Range(0, 5, ErrorMessage = "评分必须在 0-5 之间")]
    public int? Rating { get; set; }

    public List<string>? Images { get; set; }
}

/// <summary>
///     更新评论请求 DTO
/// </summary>
public class UpdateCoworkingCommentRequest
{
    [Required(ErrorMessage = "评论内容不能为空")]
    [MaxLength(1000, ErrorMessage = "评论内容不能超过1000字")]
    public string Content { get; set; } = string.Empty;

    [Range(0, 5, ErrorMessage = "评分必须在 0-5 之间")]
    public int? Rating { get; set; }

    public List<string>? Images { get; set; }
}