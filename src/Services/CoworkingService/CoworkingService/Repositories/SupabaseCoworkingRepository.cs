using CoworkingService.Models;
using Microsoft.Extensions.Logging;
using Shared.Repositories;
using Supabase;

namespace CoworkingService.Repositories;

/// <summary>
/// Coworking 空间仓储 - 使用 Supabase
/// </summary>
public class SupabaseCoworkingRepository : SupabaseRepositoryBase<CoworkingSpace>
{
    public SupabaseCoworkingRepository(Client supabaseClient, ILogger<SupabaseCoworkingRepository> logger) 
        : base(supabaseClient, logger)
    {
    }

    /// <summary>
    /// 按城市ID获取共享办公空间列表
    /// </summary>
    public async Task<List<CoworkingSpace>> GetByCityIdAsync(Guid cityId)
    {
        var response = await SupabaseClient
            .From<CoworkingSpace>()
            .Where(x => x.CityId == cityId && x.IsActive)
            .Order(x => x.Rating, Postgrest.Constants.Ordering.Descending)
            .Get();

        return response.Models;
    }

    /// <summary>
    /// 搜索共享办公空间（按名称或地址�?
    /// </summary>
    public async Task<List<CoworkingSpace>> SearchAsync(string searchTerm, int page = 1, int pageSize = 20)
    {
        // 获取所有活跃的共享办公空间
        var response = await SupabaseClient
            .From<CoworkingSpace>()
            .Where(x => x.IsActive)
            .Get();

        var spaces = response.Models.AsEnumerable();

        // 客户端过滤
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            spaces = spaces.Where(s => 
                (s.Name != null && s.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                (s.Address != null && s.Address.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
        }

        // 应用分页
        spaces = spaces
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        return spaces.ToList();
    }

    /// <summary>
    /// 按价格范围获取共享办公空�?
    /// </summary>
    public async Task<List<CoworkingSpace>> GetByPriceRangeAsync(decimal? minPrice, decimal? maxPrice, string priceType = "day")
    {
        var query = SupabaseClient
            .From<CoworkingSpace>()
            .Where(x => x.IsActive);

        if (minPrice.HasValue && maxPrice.HasValue)
        {
            if (priceType == "day")
                query = query.Where(x => x.PricePerDay >= minPrice && x.PricePerDay <= maxPrice);
            else if (priceType == "hour")
                query = query.Where(x => x.PricePerHour >= minPrice && x.PricePerHour <= maxPrice);
            else if (priceType == "month")
                query = query.Where(x => x.PricePerMonth >= minPrice && x.PricePerMonth <= maxPrice);
        }

        var response = await query.Get();
        return response.Models;
    }

    /// <summary>
    /// 获取评分最高的共享办公空间
    /// </summary>
    public async Task<List<CoworkingSpace>> GetTopRatedAsync(int limit = 10)
    {
        var response = await SupabaseClient
            .From<CoworkingSpace>()
            .Where(x => x.IsActive)
            .Order(x => x.Rating, Postgrest.Constants.Ordering.Descending)
            .Limit(limit)
            .Get();

        return response.Models;
    }
}

/// <summary>
/// Coworking 预订仓储 - 使用 Supabase
/// </summary>
public class SupabaseCoworkingBookingRepository : SupabaseRepositoryBase<CoworkingBooking>
{
    public SupabaseCoworkingBookingRepository(Client supabaseClient, ILogger<SupabaseCoworkingBookingRepository> logger) 
        : base(supabaseClient, logger)
    {
    }

    /// <summary>
    /// 按用户ID获取预订列表
    /// </summary>
    public async Task<List<CoworkingBooking>> GetByUserIdAsync(Guid userId)
    {
        var response = await SupabaseClient
            .From<CoworkingBooking>()
            .Where(x => x.UserId == userId)
            .Order(x => x.BookingDate, Postgrest.Constants.Ordering.Descending)
            .Get();

        return response.Models;
    }

    /// <summary>
    /// 按共享办公空间ID获取预订列表
    /// </summary>
    public async Task<List<CoworkingBooking>> GetByCoworkingIdAsync(Guid coworkingId)
    {
        var response = await SupabaseClient
            .From<CoworkingBooking>()
            .Where(x => x.CoworkingId == coworkingId)
            .Order(x => x.BookingDate, Postgrest.Constants.Ordering.Descending)
            .Get();

        return response.Models;
    }

    /// <summary>
    /// 按状态获取预订列�?
    /// </summary>
    public async Task<List<CoworkingBooking>> GetByStatusAsync(string status, Guid userId)
    {
        var response = await SupabaseClient
            .From<CoworkingBooking>()
            .Where(x => x.UserId == userId && x.Status == status)
            .Order(x => x.BookingDate, Postgrest.Constants.Ordering.Descending)
            .Get();

        return response.Models;
    }

    /// <summary>
    /// 检查预订冲�?
    /// </summary>
    public async Task<bool> HasConflictAsync(Guid coworkingId, DateTime bookingDate, TimeSpan? startTime, TimeSpan? endTime)
    {
        var response = await SupabaseClient
            .From<CoworkingBooking>()
            .Where(x => x.CoworkingId == coworkingId && 
                       x.BookingDate == bookingDate && 
                       x.Status != "cancelled")
            .Get();

        if (!response.Models.Any())
            return false;

        // 如果是小时预订，检查时间冲�?
        if (startTime.HasValue && endTime.HasValue)
        {
            foreach (var booking in response.Models)
            {
                if (booking.StartTime.HasValue && booking.EndTime.HasValue)
                {
                    // 检查时间重�?
                    if (startTime < booking.EndTime && endTime > booking.StartTime)
                        return true;
                }
            }
        }
        else
        {
            // 全天预订，只要有预订就冲�?
            return true;
        }

        return false;
    }
}
