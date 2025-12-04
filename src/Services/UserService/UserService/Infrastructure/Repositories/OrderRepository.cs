using UserService.Domain.Entities;
using UserService.Domain.Repositories;
using Client = Supabase.Client;

namespace UserService.Infrastructure.Repositories;

/// <summary>
///     订单仓储 Supabase 实现
/// </summary>
public class OrderRepository : IOrderRepository
{
    private readonly ILogger<OrderRepository> _logger;
    private readonly Client _supabaseClient;

    public OrderRepository(Client supabaseClient, ILogger<OrderRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<Order?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _supabaseClient
                .From<Order>()
                .Where(o => o.Id == id)
                .Single(cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 未找到订单: {Id}", id);
            return null;
        }
    }

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _supabaseClient
                .From<Order>()
                .Where(o => o.OrderNumber == orderNumber)
                .Single(cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 未找到订单: {OrderNumber}", orderNumber);
            return null;
        }
    }

    public async Task<Order?> GetByPayPalOrderIdAsync(string paypalOrderId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _supabaseClient
                .From<Order>()
                .Filter("paypal_order_id", Postgrest.Constants.Operator.Equals, paypalOrderId)
                .Single(cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 未找到 PayPal 订单: {PayPalOrderId}", paypalOrderId);
            return null;
        }
    }

    public async Task<List<Order>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 查询用户订单: {UserId}, Page: {Page}", userId, page);

        var offset = (page - 1) * pageSize;

        var response = await _supabaseClient
            .From<Order>()
            .Where(o => o.UserId == userId)
            .Order("created_at", Postgrest.Constants.Ordering.Descending)
            .Range(offset, offset + pageSize - 1)
            .Get(cancellationToken);

        return response.Models;
    }

    public async Task<List<Order>> GetPendingOrdersByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 查询用户待支付订单: {UserId}", userId);

        var response = await _supabaseClient
            .From<Order>()
            .Where(o => o.UserId == userId)
            .Where(o => o.Status == "pending")
            .Order("created_at", Postgrest.Constants.Ordering.Descending)
            .Get(cancellationToken);

        return response.Models;
    }

    public async Task<Order> CreateAsync(Order order, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 创建订单: {OrderNumber}, UserId: {UserId}", order.OrderNumber, order.UserId);

        var result = await _supabaseClient
            .From<Order>()
            .Insert(order, cancellationToken: cancellationToken);

        var created = result.Models.FirstOrDefault();
        if (created == null) throw new InvalidOperationException("创建订单失败");

        _logger.LogInformation("✅ 成功创建订单: {OrderId}", created.Id);
        return created;
    }

    public async Task<Order> UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 更新订单: {OrderId}", order.Id);

        order.UpdatedAt = DateTime.UtcNow;

        var result = await _supabaseClient
            .From<Order>()
            .Where(o => o.Id == order.Id)
            .Update(order, cancellationToken: cancellationToken);

        var updated = result.Models.FirstOrDefault();
        if (updated == null) throw new InvalidOperationException("更新订单失败");

        return updated;
    }

    public async Task<List<Order>> GetExpiredPendingOrdersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 查询过期的待支付订单");

        var now = DateTime.UtcNow;

        var response = await _supabaseClient
            .From<Order>()
            .Where(o => o.Status == "pending")
            .Filter("expired_at", Postgrest.Constants.Operator.LessThan, now.ToString("o"))
            .Get(cancellationToken);

        return response.Models;
    }
}
