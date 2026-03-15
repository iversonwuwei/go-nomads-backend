using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace AccommodationService.Application.DTOs;

/// <summary>
///     创建酒店请求DTO
/// </summary>
public class CreateHotelRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    public string Address { get; set; } = string.Empty;

    public string? CityId { get; set; }
    
    public string? CityName { get; set; }
    
    public string? Country { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(200)]
    [EmailAddress]
    public string? Email { get; set; }

    [Url]
    public string? Website { get; set; }

    [Range(0, 100000)]
    public decimal PricePerNight { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; } = "USD";

    // 数字游民功能
    public int? WifiSpeed { get; set; }
    public bool HasWifi { get; set; }
    public bool HasWorkDesk { get; set; }
    public bool HasCoworkingSpace { get; set; }
    public bool HasAirConditioning { get; set; }
    public bool HasKitchen { get; set; }
    public bool HasLaundry { get; set; }
    public bool HasParking { get; set; }
    public bool HasPool { get; set; }
    public bool HasGym { get; set; }
    public bool Has24HReception { get; set; }
    public bool HasLongStayDiscount { get; set; }
    public bool IsPetFriendly { get; set; }

    [Range(0, 100)]
    public decimal? LongStayDiscountPercent { get; set; }

    public string[]? Images { get; set; }

    /// <summary>
    ///     房型列表（创建酒店时可选）
    /// </summary>
    public List<CreateRoomTypeRequest>? RoomTypes { get; set; }
}

/// <summary>
///     更新酒店请求DTO
/// </summary>
public class UpdateHotelRequest
{
    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public string? Address { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(200)]
    [EmailAddress]
    public string? Email { get; set; }

    [Url]
    public string? Website { get; set; }

    [Range(0, 100000)]
    public decimal? PricePerNight { get; set; }

    // 数字游民功能
    public int? WifiSpeed { get; set; }
    public bool? HasWifi { get; set; }
    public bool? HasWorkDesk { get; set; }
    public bool? HasCoworkingSpace { get; set; }
    public bool? HasAirConditioning { get; set; }
    public bool? HasKitchen { get; set; }
    public bool? HasLaundry { get; set; }
    public bool? HasParking { get; set; }
    public bool? HasPool { get; set; }
    public bool? HasGym { get; set; }
    public bool? Has24HReception { get; set; }
    public bool? HasLongStayDiscount { get; set; }
    public bool? IsPetFriendly { get; set; }

    [Range(0, 100)]
    public decimal? LongStayDiscountPercent { get; set; }

    public string[]? Images { get; set; }

    /// <summary>
    ///     房型列表（更新酒店时可选）
    /// </summary>
    public List<CreateRoomTypeRequest>? RoomTypes { get; set; }
}

/// <summary>
///     酒店响应DTO
/// </summary>
public class HotelDto
{
    [Description("酒店唯一标识。社区酒店为 GUID 字符串；第三方酒店当前使用 booking_{id} 格式。")]
    public string Id { get; set; } = string.Empty;

    [Description("酒店来源。community 表示平台/用户贡献酒店，booking 表示 Booking Demand 第三方酒店。")]
    public string Source { get; set; } = "community";

    [Description("外部数据状态。internal 表示内部酒店；live 表示第三方数据获取成功；unavailable 表示第三方数据当前不可用。")]
    public string ExternalStatus { get; set; } = "internal";

    [Description("酒店名称。")]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Address { get; set; } = string.Empty;
    public Guid? CityId { get; set; }
    public string? CityName { get; set; }
    public string? Country { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public decimal Rating { get; set; }
    public int ReviewCount { get; set; }
    public string[]? Images { get; set; }
    public string Category { get; set; } = "mid-range";
    public int? StarRating { get; set; }
    public decimal PricePerNight { get; set; }
    public string Currency { get; set; } = "USD";
    public bool IsFeatured { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }

    // 数字游民功能
    public int? WifiSpeed { get; set; }
    public bool HasWifi { get; set; }
    public bool HasWorkDesk { get; set; }
    public bool HasCoworkingSpace { get; set; }
    public bool HasAirConditioning { get; set; }
    public bool HasKitchen { get; set; }
    public bool HasLaundry { get; set; }
    public bool HasParking { get; set; }
    public bool HasPool { get; set; }
    public bool HasGym { get; set; }
    public bool Has24HReception { get; set; }
    public bool HasLongStayDiscount { get; set; }
    public decimal? LongStayDiscountPercent { get; set; }
    public bool IsPetFriendly { get; set; }

    // 计算字段
    public int NomadScore { get; set; }

    // 房型列表
    public List<RoomTypeDto> RoomTypes { get; set; } = new();

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
}

/// <summary>
///     酒店列表响应DTO
/// </summary>
public class HotelListResponse
{
    [Description("酒店列表数据，可能是社区酒店、第三方酒店，或两者的融合结果。")]
    public List<HotelDto> Hotels { get; set; } = new();

    [Description("第三方酒店数据状态。not_requested 表示未尝试调用第三方；disabled 表示未启用第三方；live 表示第三方可用；unavailable 表示第三方失败且已自动回退。")]
    public string ExternalDataStatus { get; set; } = "not_requested";

    [Description("是否发生了部分降级。true 表示本次原本应查询第三方，但第三方失败，所以当前仅返回了社区数据或部分数据。")]
    public bool PartialExternalData { get; set; }

    [Description("第三方数据状态的附加说明，可直接给客户端或日志使用。")]
    public string? ExternalDataMessage { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

/// <summary>
///     酒店查询参数
/// </summary>
public class HotelQueryParameters
{
    [Description("分页页码，从 1 开始。")]
    public int Page { get; set; } = 1;

    [Description("每页条数。")]
    public int PageSize { get; set; } = 20;

    [Description("城市 ID。提供后可用于补全第三方查询所需的城市上下文。")]
    public Guid? CityId { get; set; }

    [Description("城市名称。用于第三方酒店搜索。")]
    public string? CityName { get; set; }

    [Description("国家名称或国家代码。用于第三方酒店搜索。")]
    public string? CountryName { get; set; }

    [Description("城市纬度。用于第三方酒店搜索。")]
    public double? Latitude { get; set; }

    [Description("城市经度。用于第三方酒店搜索。")]
    public double? Longitude { get; set; }

    [Description("入住日期。用于第三方实时价格和库存查询。")]
    public DateTime? CheckInDate { get; set; }

    [Description("入住晚数。用于第三方实时价格和库存查询。")]
    public int? StayNights { get; set; }

    [Description("成人数量。用于第三方实时价格和库存查询。")]
    public int? AdultCount { get; set; }

    [Description("房间数量。用于第三方实时价格和库存查询。")]
    public int? RoomCount { get; set; }

    [Description("关键词搜索，可用于名称、地址等模糊匹配。")]
    public string? Search { get; set; }
    public bool? HasWifi { get; set; }
    public bool? HasCoworkingSpace { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
}
