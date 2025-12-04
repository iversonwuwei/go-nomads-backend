using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

/// <summary>
///     支付交易仓储接口
/// </summary>
public interface IPaymentTransactionRepository
{
    /// <summary>
    ///     根据 ID 获取交易记录
    /// </summary>
    Task<PaymentTransaction?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取订单的所有交易记录
    /// </summary>
    Task<List<PaymentTransaction>> GetByOrderIdAsync(string orderId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     创建交易记录
    /// </summary>
    Task<PaymentTransaction> CreateAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    ///     更新交易记录
    /// </summary>
    Task<PaymentTransaction> UpdateAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default);
}
