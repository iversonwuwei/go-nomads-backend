using Postgrest.Attributes;
using Postgrest.Models;

namespace UserService.Domain.Entities;

/// <summary>
///     订单类型枚举
/// </summary>
public enum OrderType
{
    MembershipUpgrade,
    MembershipRenew,
    ModeratorDeposit
}

/// <summary>
///     订单状态枚举
/// </summary>
public enum OrderStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Refunded,
    Cancelled
}

/// <summary>
///     订单实体 - DDD 领域实体
/// </summary>
[Table("orders")]
public class Order : BaseModel
{
    public Order()
    {
        Id = Guid.NewGuid().ToString();
        OrderNumber = GenerateOrderNumber();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        // 订单默认 30 分钟过期
        ExpiredAt = DateTime.UtcNow.AddMinutes(30);
    }

    [PrimaryKey("id", true)]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Column("order_number")]
    public string OrderNumber { get; set; } = string.Empty;

    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("order_type")]
    public string OrderType { get; set; } = "membership_upgrade";

    [Column("status")]
    public string Status { get; set; } = "pending";

    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("total_amount")]
    public decimal TotalAmount { get; set; }

    [Column("currency")]
    public string Currency { get; set; } = "USD";

    [Column("membership_level")]
    public int? MembershipLevel { get; set; }

    [Column("duration_days")]
    public int? DurationDays { get; set; }

    [Column("paypal_order_id")]
    public string? PayPalOrderId { get; set; }

    [Column("paypal_capture_id")]
    public string? PayPalCaptureId { get; set; }

    [Column("paypal_payer_id")]
    public string? PayPalPayerId { get; set; }

    [Column("paypal_payer_email")]
    public string? PayPalPayerEmail { get; set; }

    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Column("metadata")]
    public string? Metadata { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("expired_at")]
    public DateTime? ExpiredAt { get; set; }

    #region 计算属性

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public OrderStatus OrderStatus => Enum.TryParse<OrderStatus>(Status, true, out var status)
        ? status
        : Entities.OrderStatus.Pending;

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public bool IsExpired => ExpiredAt.HasValue && ExpiredAt.Value < DateTime.UtcNow;

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public bool IsPending => OrderStatus == Entities.OrderStatus.Pending;

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public bool IsCompleted => OrderStatus == Entities.OrderStatus.Completed;

    #endregion

    #region 工厂方法

    public static Order CreateMembershipUpgrade(
        string userId,
        int membershipLevel,
        decimal amount,
        int durationDays = 365)
    {
        return new Order
        {
            UserId = userId,
            OrderType = "membership_upgrade",
            MembershipLevel = membershipLevel,
            Amount = amount,
            TotalAmount = amount,
            DurationDays = durationDays
        };
    }

    public static Order CreateMembershipRenew(
        string userId,
        int membershipLevel,
        decimal amount,
        int durationDays = 365)
    {
        return new Order
        {
            UserId = userId,
            OrderType = "membership_renew",
            MembershipLevel = membershipLevel,
            Amount = amount,
            TotalAmount = amount,
            DurationDays = durationDays
        };
    }

    public static Order CreateModeratorDeposit(string userId, decimal amount)
    {
        return new Order
        {
            UserId = userId,
            OrderType = "moderator_deposit",
            Amount = amount,
            TotalAmount = amount
        };
    }

    #endregion

    #region 领域方法

    public void SetPayPalOrderId(string paypalOrderId)
    {
        PayPalOrderId = paypalOrderId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsProcessing()
    {
        Status = "processing";
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsCompleted(string captureId, string? payerId = null, string? payerEmail = null)
    {
        Status = "completed";
        PayPalCaptureId = captureId;
        PayPalPayerId = payerId;
        PayPalPayerEmail = payerEmail;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = "failed";
        ErrorMessage = errorMessage;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsCancelled()
    {
        Status = "cancelled";
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsRefunded()
    {
        Status = "refunded";
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion

    #region 私有方法

    private static string GenerateOrderNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = new Random().Next(1000, 9999);
        return $"ORD{timestamp}{random}";
    }

    #endregion
}
