using MessageService.Domain.Entities;

namespace MessageService.Domain.Repositories;

/// <summary>
///     聊天消息仓储接口
/// </summary>
public interface IChatMessageRepository
{
    /// <summary>
    ///     获取聊天室消息列表
    /// </summary>
    Task<List<ChatRoomMessage>> GetMessagesAsync(string roomId, int page = 1, int pageSize = 50);
    
    /// <summary>
    ///     根据 ID 获取消息
    /// </summary>
    Task<ChatRoomMessage?> GetByIdAsync(Guid messageId);
    
    /// <summary>
    ///     保存消息
    /// </summary>
    Task<ChatRoomMessage> SaveAsync(ChatRoomMessage message);
    
    /// <summary>
    ///     删除消息（软删除）
    /// </summary>
    Task<bool> DeleteAsync(Guid messageId, string userId);
    
    /// <summary>
    ///     获取消息数量
    /// </summary>
    Task<int> GetMessageCountAsync(string roomId);
    
    /// <summary>
    ///     搜索消息
    /// </summary>
    Task<List<ChatRoomMessage>> SearchMessagesAsync(string roomId, string keyword, int page = 1, int pageSize = 20);
}
