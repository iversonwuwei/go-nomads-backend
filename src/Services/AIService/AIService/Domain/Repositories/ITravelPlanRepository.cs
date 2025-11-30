using AIService.Domain.Entities;

namespace AIService.Domain.Repositories;

/// <summary>
///     旅行计划仓储接口
/// </summary>
public interface ITravelPlanRepository
{
    /// <summary>
    ///     保存旅行计划
    /// </summary>
    Task<AiTravelPlan> SaveAsync(AiTravelPlan plan);

    /// <summary>
    ///     根据 ID 获取旅行计划
    /// </summary>
    Task<AiTravelPlan?> GetByIdAsync(Guid id);

    /// <summary>
    ///     获取用户的所有旅行计划
    /// </summary>
    Task<List<AiTravelPlan>> GetByUserIdAsync(Guid userId, int page = 1, int pageSize = 20);

    /// <summary>
    ///     获取城市的所有公开旅行计划
    /// </summary>
    Task<List<AiTravelPlan>> GetByCityIdAsync(string cityId, int page = 1, int pageSize = 20);

    /// <summary>
    ///     更新旅行计划
    /// </summary>
    Task<AiTravelPlan?> UpdateAsync(AiTravelPlan plan);

    /// <summary>
    ///     删除旅行计划
    /// </summary>
    Task<bool> DeleteAsync(Guid id);
}
