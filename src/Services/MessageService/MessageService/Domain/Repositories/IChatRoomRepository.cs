using MessageService.Domain.Entities;

namespace MessageService.Domain.Repositories;

/// <summary>
///     聊天室仓储接口
/// </summary>
public interface IChatRoomRepository
{
    // ==================== 聊天室管理 ====================
    
    /// <summary>
    ///     获取所有公开聊天室
    /// </summary>
    Task<List<ChatRoom>> GetPublicRoomsAsync(int page = 1, int pageSize = 20);
    
    /// <summary>
    ///     根据城市获取聊天室
    /// </summary>
    Task<List<ChatRoom>> GetRoomsByCityAsync(string city, string country);
    
    /// <summary>
    ///     根据 Meetup ID 获取聊天室
    /// </summary>
    Task<ChatRoom?> GetRoomByMeetupIdAsync(Guid meetupId);
    
    /// <summary>
    ///     根据 ID 获取聊天室
    /// </summary>
    Task<ChatRoom?> GetByIdAsync(string roomId);
    
    /// <summary>
    ///     创建聊天室
    /// </summary>
    Task<ChatRoom> CreateAsync(ChatRoom room);
    
    /// <summary>
    ///     更新聊天室
    /// </summary>
    Task<ChatRoom> UpdateAsync(ChatRoom room);
    
    /// <summary>
    ///     删除聊天室（软删除）
    /// </summary>
    Task DeleteAsync(string roomId);
    
    /// <summary>
    ///     获取用户加入的聊天室列表
    /// </summary>
    Task<List<ChatRoom>> GetUserRoomsAsync(string userId, int page = 1, int pageSize = 20);
    
    /// <summary>
    ///     根据私聊标识获取聊天室
    /// </summary>
    /// <param name="directChatKey">私聊唯一标识 (格式: direct_{userId1}_{userId2})</param>
    Task<ChatRoom?> GetRoomByDirectChatKeyAsync(string directChatKey);
}
