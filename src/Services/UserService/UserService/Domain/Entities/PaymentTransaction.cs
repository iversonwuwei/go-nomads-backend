using Postgrest.Attributes;
using Postgrest.Models;

namespace UserService.Domain.Entities;

/// <summary>
///     交易类型枚举
/// </summary>
public enum TransactionType
{
    Payment,
    Refund,
    Chargeback
}

/// <summary>
///     交易状态枚举
/// </summary>
public enum TransactionStatus
{
    Pending,
    Completed,
    Failed
}

/// <summary>
///     支付交易实体 - DDD 领域实体
/// </summary>
[Table("payment_transactions")]
public class PaymentTransaction : BaseModel
{
    public PaymentTransaction()
    {
        Id = Guid.NewGuid().ToString();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    [PrimaryKey("id", true)]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Column("order_id")]
    public string OrderId { get; set; } = string.Empty;

    [Column("transaction_type")]
    public string TransactionType { get; set; } = "payment";

    [Column("status")]
    public string Status { get; set; } = "pending";

    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("currency")]
    public string Currency { get; set; } = "USD";

    [Column("paypal_transaction_id")]
    public string? PayPalTransactionId { get; set; }

    [Column("paypal_capture_id")]
    public string? PayPalCaptureId { get; set; }

    [Column("payment_method")]
    public string PaymentMethod { get; set; } = "paypal";

    [Column("raw_response")]
    public string? RawResponse { get; set; }

    [Column("error_code")]
    public string? ErrorCode { get; set; }

    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    #region 工厂方法

    public static PaymentTransaction CreatePayment(string orderId, decimal amount, string currency = "USD")
    {
        return new PaymentTransaction
        {
            OrderId = orderId,
            TransactionType = "payment",
            Amount = amount,
            Currency = currency
        };
    }

    public static PaymentTransaction CreateRefund(string orderId, decimal amount, string currency = "USD")
    {
        return new PaymentTransaction
        {
            OrderId = orderId,
            TransactionType = "refund",
            Amount = amount,
            Currency = currency
        };
    }

    #endregion

    #region 领域方法

    public void MarkAsCompleted(string? transactionId = null, string? captureId = null, string? rawResponse = null)
    {
        Status = "completed";
        PayPalTransactionId = transactionId;
        PayPalCaptureId = captureId;
        RawResponse = rawResponse;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string errorCode, string errorMessage, string? rawResponse = null)
    {
        Status = "failed";
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        RawResponse = rawResponse;
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion
}
