namespace EventService.Domain.Enums;

/// <summary>
/// Event 状态枚举
/// </summary>
public static class EventStatus
{
    /// <summary>
    /// 即将开始 - 活动还未开始
    /// </summary>
    public const string Upcoming = "upcoming";
    
    /// <summary>
    /// 进行中 - 活动正在进行
    /// </summary>
    public const string Ongoing = "ongoing";
    
    /// <summary>
    /// 已结束 - 活动已经结束
    /// </summary>
    public const string Completed = "completed";
    
    /// <summary>
    /// 已取消 - 活动被组织者取消
    /// </summary>
    public const string Cancelled = "cancelled";

    /// <summary>
    /// 所有有效状态
    /// </summary>
    public static readonly string[] AllStatuses = 
    {
        Upcoming,
        Ongoing,
        Completed,
        Cancelled
    };

    /// <summary>
    /// 验证状态是否有效
    /// </summary>
    public static bool IsValid(string status)
    {
        return AllStatuses.Contains(status);
    }

    /// <summary>
    /// 获取状态的显示名称
    /// </summary>
    public static string GetDisplayName(string status)
    {
        return status switch
        {
            Upcoming => "即将开始",
            Ongoing => "进行中",
            Completed => "已结束",
            Cancelled => "已取消",
            _ => "未知状态"
        };
    }
}
