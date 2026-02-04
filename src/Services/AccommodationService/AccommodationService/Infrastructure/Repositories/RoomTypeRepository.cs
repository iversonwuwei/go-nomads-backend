using AccommodationService.Domain.Entities;
using AccommodationService.Domain.Repositories;
using Client = Supabase.Client;

namespace AccommodationService.Infrastructure.Repositories;

/// <summary>
///     房型仓储实现 - 使用 Supabase 客户端
/// </summary>
public class RoomTypeRepository : IRoomTypeRepository
{
    private readonly Client _supabase;
    private readonly ILogger<RoomTypeRepository> _logger;

    public RoomTypeRepository(Client supabase, ILogger<RoomTypeRepository> logger)
    {
        _supabase = supabase;
        _logger = logger;
    }

    public async Task<RoomType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _supabase
                .From<RoomType>()
                .Where(r => r.Id == id)
                .Single();

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting room type by ID: {RoomTypeId}", id);
            return null;
        }
    }

    public async Task<List<RoomType>> GetByHotelIdAsync(Guid hotelId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _supabase
                .From<RoomType>()
                .Where(r => r.HotelId == hotelId)
                .Order(r => r.PricePerNight, Postgrest.Constants.Ordering.Ascending)
                .Get();

            return response.Models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting room types for hotel: {HotelId}", hotelId);
            return new List<RoomType>();
        }
    }

    public async Task<RoomType> CreateAsync(RoomType roomType, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _supabase
                .From<RoomType>()
                .Insert(roomType);

            var created = response.Models.FirstOrDefault();
            if (created == null)
            {
                throw new Exception("Failed to create room type");
            }

            _logger.LogInformation("Created room type: {RoomTypeId} for hotel: {HotelId}", created.Id, created.HotelId);
            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating room type for hotel: {HotelId}", roomType.HotelId);
            throw;
        }
    }

    public async Task<List<RoomType>> CreateManyAsync(List<RoomType> roomTypes, CancellationToken cancellationToken = default)
    {
        if (roomTypes.Count == 0) return new List<RoomType>();

        try
        {
            var response = await _supabase
                .From<RoomType>()
                .Insert(roomTypes);

            _logger.LogInformation("Created {Count} room types", response.Models.Count);
            return response.Models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating multiple room types");
            throw;
        }
    }

    public async Task<RoomType> UpdateAsync(RoomType roomType, CancellationToken cancellationToken = default)
    {
        try
        {
            roomType.UpdatedAt = DateTime.UtcNow;
            
            var response = await _supabase
                .From<RoomType>()
                .Where(r => r.Id == roomType.Id)
                .Update(roomType);

            var updated = response.Models.FirstOrDefault();
            if (updated == null)
            {
                throw new Exception("Failed to update room type");
            }

            _logger.LogInformation("Updated room type: {RoomTypeId}", updated.Id);
            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating room type: {RoomTypeId}", roomType.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _supabase
                .From<RoomType>()
                .Where(r => r.Id == id)
                .Delete();

            _logger.LogInformation("Deleted room type: {RoomTypeId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting room type: {RoomTypeId}", id);
            return false;
        }
    }

    public async Task<bool> DeleteByHotelIdAsync(Guid hotelId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _supabase
                .From<RoomType>()
                .Where(r => r.HotelId == hotelId)
                .Delete();

            _logger.LogInformation("Deleted all room types for hotel: {HotelId}", hotelId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting room types for hotel: {HotelId}", hotelId);
            return false;
        }
    }
}
