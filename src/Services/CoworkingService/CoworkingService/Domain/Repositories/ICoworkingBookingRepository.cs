using CoworkingService.Domain.Entities;

namespace CoworkingService.Domain.Repositories;

/// <summary>
/// CoworkingBooking 仓储接口
/// 定义预订相关的数据访问契约
/// </summary>
public interface ICoworkingBookingRepository
{
    /// <summary>
    /// 创建预订
    /// </summary>
    Task<CoworkingBooking> CreateAsync(CoworkingBooking booking);

    /// <summary>
    /// 根据 ID 获取预订
    /// </summary>
    Task<CoworkingBooking?> GetByIdAsync(Guid id);

    /// <summary>
    /// 更新预订
    /// </summary>
    Task<CoworkingBooking> UpdateAsync(CoworkingBooking booking);

    /// <summary>
    /// 删除预订
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// 根据用户 ID 获取预订列表
    /// </summary>
    Task<List<CoworkingBooking>> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// 根据共享办公空间 ID 获取预订列表
    /// </summary>
    Task<List<CoworkingBooking>> GetByCoworkingIdAsync(Guid coworkingId);

    /// <summary>
    /// 根据状态获取用户的预订列表
    /// </summary>
    Task<List<CoworkingBooking>> GetByStatusAsync(string status, Guid userId);

    /// <summary>
    /// 检查预订冲突
    /// </summary>
    Task<bool> HasConflictAsync(
        Guid coworkingId,
        DateTime bookingDate,
        TimeSpan? startTime,
        TimeSpan? endTime);

    /// <summary>
    /// 检查预订是否存在
    /// </summary>
    Task<bool> ExistsAsync(Guid id);
}
