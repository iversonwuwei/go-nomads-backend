using System.ComponentModel.DataAnnotations;
using EventService.Domain.Enums;
using Postgrest.Attributes;
using Postgrest.Models;

namespace EventService.Domain.Entities;

/// <summary>
///     Event 聚合根 - 领域实体
/// </summary>
[Table("events")]
public class Event : BaseModel
{
    [PrimaryKey("id")] public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")] public string? Description { get; set; }

    [Required] [Column("organizer_id")] public Guid OrganizerId { get; set; }

    [Column("city_id")] public Guid? CityId { get; set; }

    [MaxLength(200)] [Column("location")] public string? Location { get; set; }

    [Column("address")] public string? Address { get; set; }

    [Column("image_url")] public string? ImageUrl { get; set; }

    [Column("images")] public string[]? Images { get; set; }

    [MaxLength(50)] [Column("category")] public string? Category { get; set; }

    [Required] [Column("start_time")] public DateTime StartTime { get; set; }

    [Column("end_time")] public DateTime? EndTime { get; set; }

    [Column("max_participants")] public int? MaxParticipants { get; set; }

    [Column("current_participants")] public int CurrentParticipants { get; set; }

    [MaxLength(20)] [Column("status")] public string Status { get; set; } = "upcoming";

    [MaxLength(20)]
    [Column("location_type")]
    public string LocationType { get; set; } = "physical";

    [Column("meeting_link")] public string? MeetingLink { get; set; }

    [Column("latitude")] public decimal? Latitude { get; set; }

    [Column("longitude")] public decimal? Longitude { get; set; }

    [Column("tags")] public string[]? Tags { get; set; }

    [Column("is_featured")] public bool IsFeatured { get; set; }

    [Column("created_by")] public Guid? CreatedBy { get; set; }

    [Column("updated_by")] public Guid? UpdatedBy { get; set; }

    [Column("created_at")] public DateTime CreatedAt { get; set; }

    [Column("updated_at")] public DateTime UpdatedAt { get; set; }

    // 公共无参构造函数 (Supabase 需要)

    /// <summary>
    ///     创建新的 Event - 工厂方法
    /// </summary>
    public static Event Create(
        string title,
        Guid organizerId,
        DateTime startTime,
        string? description = null,
        Guid? cityId = null,
        string? location = null,
        string? address = null,
        string? imageUrl = null,
        string[]? images = null,
        string? category = null,
        DateTime? endTime = null,
        int? maxParticipants = null,
        string locationType = "physical",
        string? meetingLink = null,
        decimal? latitude = null,
        decimal? longitude = null,
        string[]? tags = null)
    {
        var @event = new Event
        {
            Id = Guid.NewGuid(),
            Title = title ?? throw new ArgumentNullException(nameof(title)),
            OrganizerId = organizerId,
            StartTime = startTime,
            Description = description,
            CityId = cityId,
            Location = location,
            Address = address,
            ImageUrl = imageUrl,
            Images = images,
            Category = category,
            EndTime = endTime,
            MaxParticipants = maxParticipants,
            CurrentParticipants = 0,
            Status = "upcoming",
            LocationType = locationType,
            MeetingLink = meetingLink,
            Latitude = latitude,
            Longitude = longitude,
            Tags = tags,
            IsFeatured = false,
            CreatedBy = organizerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return @event;
    }

    /// <summary>
    ///     更新 Event 信息 - 领域逻辑
    /// </summary>
    public void Update(
        Guid userId,
        string? title = null,
        string? description = null,
        Guid? cityId = null,
        string? location = null,
        string? address = null,
        string? imageUrl = null,
        string[]? images = null,
        string? category = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int? maxParticipants = null,
        string? status = null,
        string? locationType = null,
        string? meetingLink = null,
        decimal? latitude = null,
        decimal? longitude = null,
        string[]? tags = null)
    {
        // 权限验证
        if (OrganizerId != userId) throw new UnauthorizedAccessException("只有创建者可以修改 Event");

        if (!string.IsNullOrEmpty(title)) Title = title;
        if (description != null) Description = description;
        if (cityId.HasValue) CityId = cityId;
        if (!string.IsNullOrEmpty(location)) Location = location;
        if (address != null) Address = address;
        if (imageUrl != null) ImageUrl = imageUrl;
        if (images != null) Images = images;
        if (!string.IsNullOrEmpty(category)) Category = category;
        if (startTime.HasValue) StartTime = startTime.Value;
        if (endTime.HasValue) EndTime = endTime;
        if (maxParticipants.HasValue) MaxParticipants = maxParticipants;
        if (!string.IsNullOrEmpty(status)) Status = status;
        if (!string.IsNullOrEmpty(locationType)) LocationType = locationType;
        if (meetingLink != null) MeetingLink = meetingLink;
        if (latitude.HasValue) Latitude = latitude;
        if (longitude.HasValue) Longitude = longitude;
        if (tags != null) Tags = tags;

        UpdatedBy = userId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     取消活动 - 领域逻辑
    /// </summary>
    public void Cancel(Guid userId)
    {
        if (Status == EventStatus.Cancelled) throw new InvalidOperationException("活动已经被取消");

        if (Status == EventStatus.Completed) throw new InvalidOperationException("已结束的活动不能取消");

        Status = EventStatus.Cancelled;
        UpdatedBy = userId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     标记活动为进行中
    /// </summary>
    public void MarkAsOngoing()
    {
        if (Status != EventStatus.Upcoming) throw new InvalidOperationException($"只有即将开始的活动可以标记为进行中，当前状态: {Status}");

        Status = EventStatus.Ongoing;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     标记活动为已结束
    /// </summary>
    public void MarkAsCompleted()
    {
        if (Status == EventStatus.Cancelled) throw new InvalidOperationException("已取消的活动不能标记为已结束");

        Status = EventStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     根据时间自动更新状态
    /// </summary>
    public void UpdateStatusByTime()
    {
        var now = DateTime.UtcNow;

        // 已取消的活动不自动更新状态
        if (Status == EventStatus.Cancelled) return;

        // 如果活动已经开始但还未结束，标记为进行中
        if (now >= StartTime && EndTime.HasValue && now < EndTime.Value)
        {
            if (Status != EventStatus.Ongoing)
            {
                Status = EventStatus.Ongoing;
                UpdatedAt = DateTime.UtcNow;
            }
        }
        // 如果活动已经结束，标记为已结束
        else if ((EndTime.HasValue && now >= EndTime.Value) ||
                 (!EndTime.HasValue && now >= StartTime.AddHours(3))) // 没有结束时间的活动默认3小时后结束
        {
            if (Status != EventStatus.Completed)
            {
                Status = EventStatus.Completed;
                UpdatedAt = DateTime.UtcNow;
            }
        }
        // 活动还未开始，保持 Upcoming
        else if (now < StartTime && Status != EventStatus.Upcoming)
        {
            Status = EventStatus.Upcoming;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    ///     增加参与者 - 领域逻辑
    /// </summary>
    public void AddParticipant()
    {
        if (MaxParticipants.HasValue && CurrentParticipants >= MaxParticipants.Value)
            throw new InvalidOperationException("Event 已满员");

        CurrentParticipants++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     移除参与者 - 领域逻辑
    /// </summary>
    public void RemoveParticipant()
    {
        // 确保参与者数量不会变成负数
        if (CurrentParticipants > 0)
            CurrentParticipants--;
        else
            // 如果已经是 0,记录警告但不抛出异常
            CurrentParticipants = 0;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     检查是否可以参加
    /// </summary>
    public bool CanJoin()
    {
        if (Status != EventStatus.Upcoming && Status != EventStatus.Ongoing) return false;

        if (MaxParticipants.HasValue && CurrentParticipants >= MaxParticipants.Value) return false;

        return true;
    }
}