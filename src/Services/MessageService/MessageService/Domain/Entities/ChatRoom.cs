namespace MessageService.Domain.Entities;

/// <summary>
///     聊天室实体
/// </summary>
public class ChatRoom
{
    public Guid Id { get; set; }
    
    /// <summary>
    ///     聊天室类型：city, meetup, direct
    /// </summary>
    public string RoomType { get; set; } = "city";
    
    /// <summary>
    ///     关联的 Meetup ID（如果是 meetup 类型）
    /// </summary>
    public Guid? MeetupId { get; set; }
    
    /// <summary>
    ///     聊天室名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    ///     聊天室描述
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    ///     城市名称
    /// </summary>
    public string? City { get; set; }
    
    /// <summary>
    ///     国家名称
    /// </summary>
    public string? Country { get; set; }
    
    /// <summary>
    ///     聊天室头像/图片
    /// </summary>
    public string? ImageUrl { get; set; }
    
    /// <summary>
    ///     创建者 ID
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
    
    /// <summary>
    ///     是否公开
    /// </summary>
    public bool IsPublic { get; set; } = true;
    
    /// <summary>
    ///     成员总数
    /// </summary>
    public int TotalMembers { get; set; }
    
    /// <summary>
    ///     创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    ///     更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    ///     是否已删除
    /// </summary>
    public bool IsDeleted { get; set; }
}
