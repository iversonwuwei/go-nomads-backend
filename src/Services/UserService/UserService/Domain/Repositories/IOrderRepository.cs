using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

/// <summary>
///     订单仓储接口
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    ///     根据 ID 获取订单
    /// </summary>
    Task<Order?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     根据订单号获取订单
    /// </summary>
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);

    /// <summary>
    ///     根据 PayPal 订单 ID 获取订单
    /// </summary>
    Task<Order?> GetByPayPalOrderIdAsync(string paypalOrderId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取用户的订单列表
    /// </summary>
    Task<List<Order>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取用户待支付的订单
    /// </summary>
    Task<List<Order>> GetPendingOrdersByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     创建订单
    /// </summary>
    Task<Order> CreateAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>
    ///     更新订单
    /// </summary>
    Task<Order> UpdateAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取过期的待支付订单
    /// </summary>
    Task<List<Order>> GetExpiredPendingOrdersAsync(CancellationToken cancellationToken = default);
}
