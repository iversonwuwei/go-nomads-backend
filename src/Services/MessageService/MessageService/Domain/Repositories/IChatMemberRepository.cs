using MessageService.Domain.Entities;

namespace MessageService.Domain.Repositories;

/// <summary>
///     聊天室成员仓储接口
/// </summary>
public interface IChatMemberRepository
{
    /// <summary>
    ///     获取聊天室成员列表
    /// </summary>
    Task<List<ChatRoomMember>> GetMembersAsync(string roomId, int page = 1, int pageSize = 20);
    
    /// <summary>
    ///     获取在线成员列表
    /// </summary>
    Task<List<ChatRoomMember>> GetOnlineMembersAsync(string roomId);
    
    /// <summary>
    ///     获取成员信息
    /// </summary>
    Task<ChatRoomMember?> GetMemberAsync(string roomId, string userId);
    
    /// <summary>
    ///     用户是否是成员
    /// </summary>
    Task<bool> IsMemberAsync(string roomId, string userId);
    
    /// <summary>
    ///     添加成员
    /// </summary>
    Task<ChatRoomMember> AddMemberAsync(ChatRoomMember member);
    
    /// <summary>
    ///     更新成员
    /// </summary>
    Task<ChatRoomMember> UpdateMemberAsync(ChatRoomMember member);
    
    /// <summary>
    ///     移除成员
    /// </summary>
    Task RemoveMemberAsync(string roomId, string userId);
    
    /// <summary>
    ///     更新最后活跃时间
    /// </summary>
    Task UpdateLastSeenAsync(string roomId, string userId);
    
    /// <summary>
    ///     获取成员数量
    /// </summary>
    Task<int> GetMemberCountAsync(string roomId);
}
