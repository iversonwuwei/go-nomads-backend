using UserService.Domain.Entities;
using UserService.Domain.Repositories;
using Client = Supabase.Client;

namespace UserService.Infrastructure.Repositories;

/// <summary>
///     支付交易仓储 Supabase 实现
/// </summary>
public class PaymentTransactionRepository : IPaymentTransactionRepository
{
    private readonly ILogger<PaymentTransactionRepository> _logger;
    private readonly Client _supabaseClient;

    public PaymentTransactionRepository(Client supabaseClient, ILogger<PaymentTransactionRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<PaymentTransaction?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _supabaseClient
                .From<PaymentTransaction>()
                .Where(t => t.Id == id)
                .Single(cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 未找到交易记录: {Id}", id);
            return null;
        }
    }

    public async Task<List<PaymentTransaction>> GetByOrderIdAsync(string orderId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 查询订单交易记录: {OrderId}", orderId);

        var response = await _supabaseClient
            .From<PaymentTransaction>()
            .Where(t => t.OrderId == orderId)
            .Order("created_at", Postgrest.Constants.Ordering.Descending)
            .Get(cancellationToken);

        return response.Models;
    }

    public async Task<PaymentTransaction> CreateAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 创建交易记录: OrderId: {OrderId}, Type: {Type}", 
            transaction.OrderId, transaction.TransactionType);

        var result = await _supabaseClient
            .From<PaymentTransaction>()
            .Insert(transaction, cancellationToken: cancellationToken);

        var created = result.Models.FirstOrDefault();
        if (created == null) throw new InvalidOperationException("创建交易记录失败");

        _logger.LogInformation("✅ 成功创建交易记录: {TransactionId}", created.Id);
        return created;
    }

    public async Task<PaymentTransaction> UpdateAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 更新交易记录: {TransactionId}", transaction.Id);

        transaction.UpdatedAt = DateTime.UtcNow;

        var result = await _supabaseClient
            .From<PaymentTransaction>()
            .Where(t => t.Id == transaction.Id)
            .Update(transaction, cancellationToken: cancellationToken);

        var updated = result.Models.FirstOrDefault();
        if (updated == null) throw new InvalidOperationException("更新交易记录失败");

        return updated;
    }
}
