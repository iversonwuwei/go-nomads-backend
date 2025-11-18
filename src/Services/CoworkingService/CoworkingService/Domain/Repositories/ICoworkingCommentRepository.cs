using CoworkingService.Domain.Entities;

namespace CoworkingService.Domain.Repositories;

/// <summary>
///     Coworking 评论仓储接口
/// </summary>
public interface ICoworkingCommentRepository
{
    Task<CoworkingComment> CreateAsync(CoworkingComment comment);
    Task<CoworkingComment?> GetByIdAsync(Guid id);
    Task<List<CoworkingComment>> GetByCoworkingIdAsync(Guid coworkingId, int page = 1, int pageSize = 20);
    Task<int> GetCountByCoworkingIdAsync(Guid coworkingId);
    Task<CoworkingComment> UpdateAsync(CoworkingComment comment);
    Task DeleteAsync(Guid id);
}
