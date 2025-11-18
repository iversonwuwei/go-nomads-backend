using Postgrest.Attributes;
using Postgrest.Models;

namespace CoworkingService.Domain.Entities;

/// <summary>
///     CoworkingBooking 实体 - 共享办公空间预订
/// </summary>
[Table("coworking_bookings")]
public class CoworkingBooking : BaseModel
{
    /// <summary>
    ///     公共无参构造函数 (ORM 需要)
    /// </summary>
    public CoworkingBooking()
    {
    }

    [PrimaryKey("id")] public Guid Id { get; private set; }

    [Column("coworking_id")] public Guid CoworkingId { get; private set; }

    [Column("user_id")] public Guid UserId { get; private set; }

    [Column("booking_date")] public DateTime BookingDate { get; private set; }

    [Column("start_time")] public TimeSpan? StartTime { get; private set; }

    [Column("end_time")] public TimeSpan? EndTime { get; private set; }

    [Column("booking_type")] public string BookingType { get; private set; } = "daily"; // hourly, daily, monthly

    [Column("total_price")] public decimal TotalPrice { get; private set; }

    [Column("currency")] public string Currency { get; private set; } = "USD";

    [Column("status")]
    public string Status { get; private set; } = "pending"; // pending, confirmed, cancelled, completed

    [Column("special_requests")] public string? SpecialRequests { get; private set; }

    [Column("created_at")] public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>
    ///     工厂方法 - 创建新的预订
    /// </summary>
    public static CoworkingBooking Create(
        Guid coworkingId,
        Guid userId,
        DateTime bookingDate,
        string bookingType,
        decimal totalPrice,
        string currency = "USD",
        TimeSpan? startTime = null,
        TimeSpan? endTime = null,
        string? specialRequests = null)
    {
        // 业务规则验证
        if (coworkingId == Guid.Empty)
            throw new ArgumentException("共享办公空间 ID 不能为空", nameof(coworkingId));

        if (userId == Guid.Empty)
            throw new ArgumentException("用户 ID 不能为空", nameof(userId));

        if (bookingDate < DateTime.UtcNow.Date)
            throw new ArgumentException("预订日期不能早于今天", nameof(bookingDate));

        if (totalPrice <= 0)
            throw new ArgumentException("总价必须大于 0", nameof(totalPrice));

        if (!IsValidBookingType(bookingType))
            throw new ArgumentException("无效的预订类型", nameof(bookingType));

        // 时间预订需要开始和结束时间
        if (bookingType == "hourly" && (!startTime.HasValue || !endTime.HasValue))
            throw new ArgumentException("小时预订必须指定开始和结束时间");

        if (startTime.HasValue && endTime.HasValue && startTime >= endTime)
            throw new ArgumentException("开始时间必须早于结束时间");

        return new CoworkingBooking
        {
            Id = Guid.NewGuid(),
            CoworkingId = coworkingId,
            UserId = userId,
            BookingDate = bookingDate,
            StartTime = startTime,
            EndTime = endTime,
            BookingType = bookingType,
            TotalPrice = totalPrice,
            Currency = currency,
            Status = "pending",
            SpecialRequests = specialRequests,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    ///     领域方法 - 确认预订
    /// </summary>
    public void Confirm()
    {
        if (Status != "pending")
            throw new InvalidOperationException($"只有待处理的预订可以确认，当前状态: {Status}");

        Status = "confirmed";
    }

    /// <summary>
    ///     领域方法 - 取消预订
    /// </summary>
    public void Cancel()
    {
        if (Status == "completed")
            throw new InvalidOperationException("已完成的预订不能取消");

        if (Status == "cancelled")
            throw new InvalidOperationException("预订已经被取消");

        Status = "cancelled";
    }

    /// <summary>
    ///     领域方法 - 完成预订
    /// </summary>
    public void Complete()
    {
        if (Status != "confirmed")
            throw new InvalidOperationException($"只有已确认的预订可以完成，当前状态: {Status}");

        Status = "completed";
    }

    /// <summary>
    ///     领域方法 - 更新特殊要求
    /// </summary>
    public void UpdateSpecialRequests(string? specialRequests)
    {
        if (Status != "pending" && Status != "confirmed")
            throw new InvalidOperationException($"当前状态下不能更新特殊要求: {Status}");

        SpecialRequests = specialRequests;
    }

    /// <summary>
    ///     领域查询 - 检查是否可以取消
    /// </summary>
    public bool CanCancel()
    {
        return Status == "pending" || Status == "confirmed";
    }

    /// <summary>
    ///     领域查询 - 检查时间冲突
    /// </summary>
    public bool HasTimeConflict(TimeSpan? otherStart, TimeSpan? otherEnd)
    {
        if (!StartTime.HasValue || !EndTime.HasValue || !otherStart.HasValue || !otherEnd.HasValue)
            return false;

        return StartTime < otherEnd && EndTime > otherStart;
    }

    /// <summary>
    ///     验证预订类型
    /// </summary>
    private static bool IsValidBookingType(string bookingType)
    {
        return bookingType == "hourly" || bookingType == "daily" || bookingType == "monthly";
    }
}