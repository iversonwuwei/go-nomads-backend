using CoworkingService.Domain.Entities;
using CoworkingService.Domain.Repositories;
using Supabase;

namespace CoworkingService.Infrastructure.Repositories;

/// <summary>
/// CoworkingBooking 仓储实现 - Supabase
/// 实现预订相关的数据访问
/// </summary>
public class CoworkingBookingRepository : ICoworkingBookingRepository
{
    private readonly Client _supabaseClient;
    private readonly ILogger<CoworkingBookingRepository> _logger;

    public CoworkingBookingRepository(Client supabaseClient, ILogger<CoworkingBookingRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<CoworkingBooking> CreateAsync(CoworkingBooking booking)
    {
        try
        {
            var response = await _supabaseClient
                .From<CoworkingBooking>()
                .Insert(booking);

            var created = response.Models.FirstOrDefault();
            if (created == null)
            {
                throw new InvalidOperationException("创建预订失败");
            }

            _logger.LogInformation("✅ Supabase 创建预订成功: {Id}", created.Id);
            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Supabase 创建预订失败");
            throw;
        }
    }

    public async Task<CoworkingBooking?> GetByIdAsync(Guid id)
    {
        try
        {
            var response = await _supabaseClient
                .From<CoworkingBooking>()
                .Where(x => x.Id == id)
                .Single();

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取预订失败: {Id}", id);
            return null;
        }
    }

    public async Task<CoworkingBooking> UpdateAsync(CoworkingBooking booking)
    {
        try
        {
            var response = await _supabaseClient
                .From<CoworkingBooking>()
                .Update(booking);

            var updated = response.Models.FirstOrDefault();
            if (updated == null)
            {
                throw new InvalidOperationException("更新预订失败");
            }

            _logger.LogInformation("✅ Supabase 更新预订成功: {Id}", updated.Id);
            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Supabase 更新预订失败: {Id}", booking.Id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        try
        {
            await _supabaseClient
                .From<CoworkingBooking>()
                .Filter("id", Postgrest.Constants.Operator.Equals, id.ToString())
                .Delete();

            _logger.LogInformation("✅ Supabase 删除预订成功: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Supabase 删除预订失败: {Id}", id);
            throw;
        }
    }

    public async Task<List<CoworkingBooking>> GetByUserIdAsync(Guid userId)
    {
        try
        {
            var response = await _supabaseClient
                .From<CoworkingBooking>()
                .Where(x => x.UserId == userId)
                .Order(x => x.BookingDate, Postgrest.Constants.Ordering.Descending)
                .Get();

            return response.Models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 按用户获取预订失败: {UserId}", userId);
            throw;
        }
    }

    public async Task<List<CoworkingBooking>> GetByCoworkingIdAsync(Guid coworkingId)
    {
        try
        {
            var response = await _supabaseClient
                .From<CoworkingBooking>()
                .Where(x => x.CoworkingId == coworkingId)
                .Order(x => x.BookingDate, Postgrest.Constants.Ordering.Descending)
                .Get();

            return response.Models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 按共享办公空间获取预订失败: {CoworkingId}", coworkingId);
            throw;
        }
    }

    public async Task<List<CoworkingBooking>> GetByStatusAsync(string status, Guid userId)
    {
        try
        {
            var response = await _supabaseClient
                .From<CoworkingBooking>()
                .Where(x => x.UserId == userId && x.Status == status)
                .Order(x => x.BookingDate, Postgrest.Constants.Ordering.Descending)
                .Get();

            return response.Models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 按状态获取预订失败: {Status}, {UserId}", status, userId);
            throw;
        }
    }

    public async Task<bool> HasConflictAsync(
        Guid coworkingId,
        DateTime bookingDate,
        TimeSpan? startTime,
        TimeSpan? endTime)
    {
        try
        {
            var response = await _supabaseClient
                .From<CoworkingBooking>()
                .Where(x => x.CoworkingId == coworkingId &&
                           x.BookingDate == bookingDate &&
                           x.Status != "cancelled")
                .Get();

            if (!response.Models.Any())
                return false;

            // 如果是小时预订，检查时间冲突
            if (startTime.HasValue && endTime.HasValue)
            {
                foreach (var booking in response.Models)
                {
                    if (booking.StartTime.HasValue && booking.EndTime.HasValue)
                    {
                        // 检查时间重叠
                        if (startTime < booking.EndTime && endTime > booking.StartTime)
                            return true;
                    }
                }
            }
            else
            {
                // 全天预订，只要有预订就冲突
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 检查预订冲突失败");
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        try
        {
            var booking = await GetByIdAsync(id);
            return booking != null;
        }
        catch
        {
            return false;
        }
    }
}
