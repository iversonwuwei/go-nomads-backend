using AccommodationService.Domain.Entities;
using AccommodationService.Domain.Repositories;
using Postgrest;
using Client = Supabase.Client;

namespace AccommodationService.Infrastructure.Repositories;

/// <summary>
///     酒店仓储实现 - 使用 Supabase 客户端
/// </summary>
public class HotelRepository : IHotelRepository
{
    private readonly Client _supabase;
    private readonly ILogger<HotelRepository> _logger;

    public HotelRepository(Client supabase, ILogger<HotelRepository> logger)
    {
        _supabase = supabase;
        _logger = logger;
    }

    public async Task<Hotel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _supabase
                .From<Hotel>()
                .Where(h => h.Id == id)
                .Single();

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hotel by ID: {HotelId}", id);
            return null;
        }
    }

    public async Task<(List<Hotel> Hotels, int TotalCount)> GetListAsync(
        int page = 1,
        int pageSize = 20,
        Guid? cityId = null,
        string? searchQuery = null,
        bool? hasWifi = null,
        bool? hasCoworkingSpace = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var offset = (page - 1) * pageSize;
            List<Hotel> items;

            // 使用分支逻辑构建查询，避免 Postgrest 无法解析的表达式
            if (activeOnly && cityId.HasValue)
            {
                // 同时按活跃状态和城市过滤
                var response = await _supabase
                    .From<Hotel>()
                    .Where(h => h.IsActive == true && h.CityId == cityId.Value)
                    .Order("created_at", Constants.Ordering.Descending)
                    .Range(offset, offset + pageSize - 1)
                    .Get();
                items = response.Models;
            }
            else if (activeOnly)
            {
                // 仅按活跃状态过滤
                var response = await _supabase
                    .From<Hotel>()
                    .Where(h => h.IsActive == true)
                    .Order("created_at", Constants.Ordering.Descending)
                    .Range(offset, offset + pageSize - 1)
                    .Get();
                items = response.Models;
            }
            else if (cityId.HasValue)
            {
                // 仅按城市过滤
                var response = await _supabase
                    .From<Hotel>()
                    .Where(h => h.CityId == cityId.Value)
                    .Order("created_at", Constants.Ordering.Descending)
                    .Range(offset, offset + pageSize - 1)
                    .Get();
                items = response.Models;
            }
            else
            {
                // 无过滤
                var response = await _supabase
                    .From<Hotel>()
                    .Order("created_at", Constants.Ordering.Descending)
                    .Range(offset, offset + pageSize - 1)
                    .Get();
                items = response.Models;
            }

            // 应用额外的内存过滤（搜索、功能筛选、价格等）
            // 这些过滤器在 Supabase/Postgrest 中难以组合，因此在内存中处理
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                items = items.Where(h => 
                    (h.Name?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (h.Description?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (h.Address?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ?? false)
                ).ToList();
            }

            if (hasWifi.HasValue && hasWifi.Value)
            {
                items = items.Where(h => h.HasWifi == true).ToList();
            }

            if (hasCoworkingSpace.HasValue && hasCoworkingSpace.Value)
            {
                items = items.Where(h => h.HasCoworkingSpace == true).ToList();
            }

            if (minPrice.HasValue)
            {
                items = items.Where(h => h.PricePerNight >= minPrice.Value).ToList();
            }

            if (maxPrice.HasValue)
            {
                items = items.Where(h => h.PricePerNight <= maxPrice.Value).ToList();
            }

            // 计算总数（简化处理）
            var totalCount = items.Count;

            return (items, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hotel list");
            throw;
        }
    }

    public async Task<List<Hotel>> GetByCityIdAsync(Guid cityId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _supabase
                .From<Hotel>()
                .Where(h => h.CityId == cityId)
                .Where(h => h.IsActive == true)
                .Order("rating", Constants.Ordering.Descending)
                .Get();

            return response.Models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hotels by city ID: {CityId}", cityId);
            throw;
        }
    }

    public async Task<Hotel> CreateAsync(Hotel hotel, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating hotel: {HotelName} in city: {CityId}", hotel.Name, hotel.CityId);

            var response = await _supabase
                .From<Hotel>()
                .Upsert(hotel);

            var created = response.Models.FirstOrDefault();
            if (created == null)
            {
                throw new InvalidOperationException("Failed to create hotel - no response returned");
            }

            _logger.LogInformation("Successfully created hotel: {HotelId}", created.Id);
            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating hotel: {HotelName}", hotel.Name);
            throw;
        }
    }

    public async Task<Hotel> UpdateAsync(Hotel hotel, CancellationToken cancellationToken = default)
    {
        try
        {
            hotel.UpdatedAt = DateTime.UtcNow;

            var response = await _supabase
                .From<Hotel>()
                .Where(h => h.Id == hotel.Id)
                .Update(hotel);

            var updated = response.Models.FirstOrDefault();
            if (updated == null)
            {
                throw new InvalidOperationException($"Failed to update hotel {hotel.Id}");
            }

            _logger.LogInformation("Successfully updated hotel: {HotelId}", updated.Id);
            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating hotel: {HotelId}", hotel.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            // 软删除
            var hotel = await GetByIdAsync(id, cancellationToken);
            if (hotel == null) return false;

            hotel.IsActive = false;
            hotel.UpdatedAt = DateTime.UtcNow;

            await _supabase
                .From<Hotel>()
                .Where(h => h.Id == id)
                .Update(hotel);

            _logger.LogInformation("Successfully soft-deleted hotel: {HotelId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting hotel: {HotelId}", id);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _supabase
                .From<Hotel>()
                .Where(h => h.Id == id)
                .Count(Constants.CountType.Exact);

            return response > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking hotel existence: {HotelId}", id);
            throw;
        }
    }

    public async Task<List<Hotel>> GetByCreatorAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _supabase
                .From<Hotel>()
                .Where(h => h.CreatedBy == userId)
                .Order("created_at", Constants.Ordering.Descending)
                .Get();

            return response.Models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hotels by creator: {UserId}", userId);
            throw;
        }
    }
}
