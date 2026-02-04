using AccommodationService.Application.DTOs;
using AccommodationService.Domain.Entities;
using AccommodationService.Domain.Repositories;

namespace AccommodationService.Application.Services;

/// <summary>
///     酒店应用服务实现
/// </summary>
public class HotelApplicationService : IHotelService
{
    private readonly IHotelRepository _hotelRepository;
    private readonly IRoomTypeRepository _roomTypeRepository;
    private readonly ILogger<HotelApplicationService> _logger;

    public HotelApplicationService(
        IHotelRepository hotelRepository,
        IRoomTypeRepository roomTypeRepository,
        ILogger<HotelApplicationService> logger)
    {
        _hotelRepository = hotelRepository;
        _roomTypeRepository = roomTypeRepository;
        _logger = logger;
    }

    public async Task<HotelDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var hotel = await _hotelRepository.GetByIdAsync(id, cancellationToken);
        if (hotel == null) return null;
        
        var dto = MapToDto(hotel);
        dto.RoomTypes = (await _roomTypeRepository.GetByHotelIdAsync(id, cancellationToken))
            .Select(MapRoomTypeToDto).ToList();
        return dto;
    }

    public async Task<HotelListResponse> GetListAsync(HotelQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        var (hotels, totalCount) = await _hotelRepository.GetListAsync(
            parameters.Page,
            parameters.PageSize,
            parameters.CityId,
            parameters.Search,
            parameters.HasWifi,
            parameters.HasCoworkingSpace,
            parameters.MinPrice,
            parameters.MaxPrice,
            activeOnly: true,
            cancellationToken);

        return new HotelListResponse
        {
            Hotels = hotels.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = parameters.Page,
            PageSize = parameters.PageSize
        };
    }

    public async Task<List<HotelDto>> GetByCityIdAsync(Guid cityId, CancellationToken cancellationToken = default)
    {
        var hotels = await _hotelRepository.GetByCityIdAsync(cityId, cancellationToken);
        return hotels.Select(MapToDto).ToList();
    }

    public async Task<HotelDto> CreateAsync(CreateHotelRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating hotel: {HotelName} by user: {UserId}", request.Name, userId);

        // 解析 CityId
        Guid? cityId = null;
        if (!string.IsNullOrEmpty(request.CityId) && Guid.TryParse(request.CityId, out var parsedCityId))
        {
            cityId = parsedCityId;
        }

        // 使用工厂方法创建酒店实体
        var hotel = Hotel.Create(
            name: request.Name,
            address: request.Address,
            latitude: request.Latitude,
            longitude: request.Longitude,
            cityId: cityId,
            cityName: request.CityName,
            country: request.Country,
            description: request.Description,
            images: request.Images,
            pricePerNight: request.PricePerNight,
            currency: request.Currency,
            phone: request.Phone,
            email: request.Email,
            website: request.Website,
            wifiSpeed: request.WifiSpeed,
            hasWifi: request.HasWifi,
            hasWorkDesk: request.HasWorkDesk,
            hasCoworkingSpace: request.HasCoworkingSpace,
            hasAirConditioning: request.HasAirConditioning,
            hasKitchen: request.HasKitchen,
            hasLaundry: request.HasLaundry,
            hasParking: request.HasParking,
            hasPool: request.HasPool,
            hasGym: request.HasGym,
            has24HReception: request.Has24HReception,
            hasLongStayDiscount: request.HasLongStayDiscount,
            longStayDiscountPercent: request.LongStayDiscountPercent,
            isPetFriendly: request.IsPetFriendly,
            createdBy: userId
        );

        var created = await _hotelRepository.CreateAsync(hotel, cancellationToken);

        // 如果有房型，批量创建
        if (request.RoomTypes != null && request.RoomTypes.Count > 0)
        {
            var roomTypes = request.RoomTypes.Select(rt => RoomType.Create(
                hotelId: created.Id,
                name: rt.Name,
                description: rt.Description,
                maxOccupancy: rt.MaxOccupancy,
                size: rt.RoomSize ?? 25,
                bedType: rt.BedType ?? "Double",
                pricePerNight: rt.PricePerNight,
                currency: rt.Currency,
                availableRooms: rt.AvailableRooms,
                amenities: rt.Amenities,
                images: rt.Images
            )).ToList();

            await _roomTypeRepository.CreateManyAsync(roomTypes, cancellationToken);
            _logger.LogInformation("Created {Count} room types for hotel: {HotelId}", roomTypes.Count, created.Id);
        }

        _logger.LogInformation("Successfully created hotel: {HotelId}", created.Id);
        return await GetByIdAsync(created.Id, cancellationToken) ?? MapToDto(created);
    }

    public async Task<HotelDto?> UpdateAsync(Guid id, UpdateHotelRequest request, Guid userId, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        var hotel = await _hotelRepository.GetByIdAsync(id, cancellationToken);
        if (hotel == null)
        {
            _logger.LogWarning("Hotel not found for update: {HotelId}", id);
            return null;
        }

        // 验证权限：只有创建者或管理员可以更新
        if (!isAdmin && hotel.CreatedBy != userId)
        {
            _logger.LogWarning("User {UserId} attempted to update hotel {HotelId} created by {CreatedBy}", 
                userId, id, hotel.CreatedBy);
            throw new UnauthorizedAccessException("You can only update hotels you created");
        }

        // 使用领域方法更新
        hotel.Update(
            name: request.Name,
            address: request.Address,
            description: request.Description,
            latitude: request.Latitude,
            longitude: request.Longitude,
            pricePerNight: request.PricePerNight,
            phone: request.Phone,
            email: request.Email,
            website: request.Website,
            images: request.Images,
            wifiSpeed: request.WifiSpeed,
            hasWifi: request.HasWifi,
            hasWorkDesk: request.HasWorkDesk,
            hasCoworkingSpace: request.HasCoworkingSpace,
            hasAirConditioning: request.HasAirConditioning,
            hasKitchen: request.HasKitchen,
            hasLaundry: request.HasLaundry,
            hasParking: request.HasParking,
            hasPool: request.HasPool,
            hasGym: request.HasGym,
            has24HReception: request.Has24HReception,
            hasLongStayDiscount: request.HasLongStayDiscount,
            longStayDiscountPercent: request.LongStayDiscountPercent,
            isPetFriendly: request.IsPetFriendly,
            updatedBy: userId
        );

        var updated = await _hotelRepository.UpdateAsync(hotel, cancellationToken);
        _logger.LogInformation("Successfully updated hotel: {HotelId}", updated.Id);

        // 处理房型更新
        if (request.RoomTypes != null)
        {
            // 获取现有房型
            var existingRoomTypes = await _roomTypeRepository.GetByHotelIdAsync(updated.Id, cancellationToken);
            var existingRoomTypeIds = existingRoomTypes.Select(rt => rt.Id).ToHashSet();
            var requestRoomTypeIds = request.RoomTypes
                .Where(rt => !string.IsNullOrEmpty(rt.Id) && Guid.TryParse(rt.Id, out _))
                .Select(rt => Guid.Parse(rt.Id!))
                .ToHashSet();

            // 删除不在请求中的房型
            foreach (var existingRoomType in existingRoomTypes)
            {
                if (!requestRoomTypeIds.Contains(existingRoomType.Id))
                {
                    await _roomTypeRepository.DeleteAsync(existingRoomType.Id, cancellationToken);
                    _logger.LogInformation("Deleted room type {RoomTypeId} from hotel {HotelId}", existingRoomType.Id, updated.Id);
                }
            }

            // 添加或更新房型
            foreach (var roomTypeRequest in request.RoomTypes)
            {
                if (!string.IsNullOrEmpty(roomTypeRequest.Id) && Guid.TryParse(roomTypeRequest.Id, out var roomTypeId) && existingRoomTypeIds.Contains(roomTypeId))
                {
                    // 更新现有房型
                    var existingRoomType = existingRoomTypes.First(rt => rt.Id == roomTypeId);
                    existingRoomType.Update(
                        name: roomTypeRequest.Name,
                        description: roomTypeRequest.Description,
                        maxOccupancy: roomTypeRequest.MaxOccupancy,
                        pricePerNight: roomTypeRequest.PricePerNight,
                        amenities: roomTypeRequest.Amenities,
                        images: roomTypeRequest.Images,
                        bedType: roomTypeRequest.BedType,
                        roomSize: roomTypeRequest.RoomSize
                    );
                    await _roomTypeRepository.UpdateAsync(existingRoomType, cancellationToken);
                    _logger.LogInformation("Updated room type {RoomTypeId} for hotel {HotelId}", roomTypeId, updated.Id);
                }
                else
                {
                    // 创建新房型
                    var newRoomType = RoomType.Create(
                        hotelId: updated.Id,
                        name: roomTypeRequest.Name,
                        description: roomTypeRequest.Description,
                        maxOccupancy: roomTypeRequest.MaxOccupancy,
                        size: roomTypeRequest.RoomSize ?? 25,
                        bedType: roomTypeRequest.BedType ?? "Double",
                        pricePerNight: roomTypeRequest.PricePerNight,
                        currency: roomTypeRequest.Currency,
                        availableRooms: roomTypeRequest.AvailableRooms,
                        amenities: roomTypeRequest.Amenities,
                        images: roomTypeRequest.Images
                    );
                    await _roomTypeRepository.CreateAsync(newRoomType, cancellationToken);
                    _logger.LogInformation("Created new room type {RoomTypeName} for hotel {HotelId}", roomTypeRequest.Name, updated.Id);
                }
            }
        }

        return await GetByIdAsync(updated.Id, cancellationToken) ?? MapToDto(updated);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        var hotel = await _hotelRepository.GetByIdAsync(id, cancellationToken);
        if (hotel == null)
        {
            return false;
        }

        // 验证权限：只有创建者或管理员可以删除
        if (!isAdmin && hotel.CreatedBy != userId)
        {
            _logger.LogWarning("User {UserId} attempted to delete hotel {HotelId} created by {CreatedBy}", 
                userId, id, hotel.CreatedBy);
            throw new UnauthorizedAccessException("You can only delete hotels you created");
        }

        return await _hotelRepository.DeleteAsync(id, cancellationToken);
    }

    public async Task<List<HotelDto>> GetMyHotelsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var hotels = await _hotelRepository.GetByCreatorAsync(userId, cancellationToken);
        var result = new List<HotelDto>();
        foreach (var hotel in hotels)
        {
            var dto = MapToDto(hotel);
            dto.RoomTypes = (await _roomTypeRepository.GetByHotelIdAsync(hotel.Id, cancellationToken))
                .Select(MapRoomTypeToDto).ToList();
            result.Add(dto);
        }
        return result;
    }

    // ============================================================
    // 房型相关方法
    // ============================================================

    public async Task<List<RoomTypeDto>> GetRoomTypesAsync(Guid hotelId, CancellationToken cancellationToken = default)
    {
        var roomTypes = await _roomTypeRepository.GetByHotelIdAsync(hotelId, cancellationToken);
        return roomTypes.Select(MapRoomTypeToDto).ToList();
    }

    public async Task<RoomTypeDto?> GetRoomTypeByIdAsync(Guid roomTypeId, CancellationToken cancellationToken = default)
    {
        var roomType = await _roomTypeRepository.GetByIdAsync(roomTypeId, cancellationToken);
        return roomType == null ? null : MapRoomTypeToDto(roomType);
    }

    public async Task<RoomTypeDto> CreateRoomTypeAsync(Guid hotelId, CreateRoomTypeRequest request, CancellationToken cancellationToken = default)
    {
        var roomType = RoomType.Create(
            hotelId: hotelId,
            name: request.Name,
            description: request.Description,
            maxOccupancy: request.MaxOccupancy,
            size: request.RoomSize ?? 25,
            bedType: request.BedType ?? "Double",
            pricePerNight: request.PricePerNight,
            currency: request.Currency,
            availableRooms: request.AvailableRooms,
            amenities: request.Amenities,
            images: request.Images
        );

        var created = await _roomTypeRepository.CreateAsync(roomType, cancellationToken);
        return MapRoomTypeToDto(created);
    }

    public async Task<RoomTypeDto?> UpdateRoomTypeAsync(Guid roomTypeId, UpdateRoomTypeRequest request, CancellationToken cancellationToken = default)
    {
        var roomType = await _roomTypeRepository.GetByIdAsync(roomTypeId, cancellationToken);
        if (roomType == null) return null;

        // 更新字段
        if (request.Name != null) roomType.Name = request.Name;
        if (request.Description != null) roomType.Description = request.Description;
        if (request.MaxOccupancy.HasValue) roomType.MaxOccupancy = request.MaxOccupancy.Value;
        if (request.Size.HasValue) roomType.Size = request.Size.Value;
        if (request.BedType != null) roomType.BedType = request.BedType;
        if (request.PricePerNight.HasValue) roomType.PricePerNight = request.PricePerNight.Value;
        if (request.AvailableRooms.HasValue) roomType.AvailableRooms = request.AvailableRooms.Value;
        if (request.Amenities != null) roomType.Amenities = request.Amenities;
        if (request.Images != null) roomType.Images = request.Images;
        if (request.IsAvailable.HasValue) roomType.IsAvailable = request.IsAvailable.Value;

        var updated = await _roomTypeRepository.UpdateAsync(roomType, cancellationToken);
        return MapRoomTypeToDto(updated);
    }

    public async Task<bool> DeleteRoomTypeAsync(Guid roomTypeId, CancellationToken cancellationToken = default)
    {
        return await _roomTypeRepository.DeleteAsync(roomTypeId, cancellationToken);
    }

    // ============================================================
    // 私有方法
    // ============================================================

    private static HotelDto MapToDto(Hotel hotel)
    {
        return new HotelDto
        {
            Id = hotel.Id,
            Name = hotel.Name,
            Description = hotel.Description,
            Address = hotel.Address,
            CityId = hotel.CityId,
            CityName = hotel.CityName,
            Country = hotel.Country,
            Latitude = hotel.Latitude,
            Longitude = hotel.Longitude,
            Rating = hotel.Rating,
            ReviewCount = hotel.ReviewCount,
            Images = hotel.Images,
            Category = hotel.Category,
            StarRating = hotel.StarRating,
            PricePerNight = hotel.PricePerNight,
            Currency = hotel.Currency,
            IsFeatured = hotel.IsFeatured,
            Phone = hotel.Phone,
            Email = hotel.Email,
            Website = hotel.Website,
            WifiSpeed = hotel.WifiSpeed,
            HasWifi = hotel.HasWifi,
            HasWorkDesk = hotel.HasWorkDesk,
            HasCoworkingSpace = hotel.HasCoworkingSpace,
            HasAirConditioning = hotel.HasAirConditioning,
            HasKitchen = hotel.HasKitchen,
            HasLaundry = hotel.HasLaundry,
            HasParking = hotel.HasParking,
            HasPool = hotel.HasPool,
            HasGym = hotel.HasGym,
            Has24HReception = hotel.Has24HReception,
            HasLongStayDiscount = hotel.HasLongStayDiscount,
            LongStayDiscountPercent = hotel.LongStayDiscountPercent,
            IsPetFriendly = hotel.IsPetFriendly,
            NomadScore = hotel.CalculateNomadScore(),
            CreatedAt = hotel.CreatedAt,
            CreatedBy = hotel.CreatedBy
        };
    }

    private static RoomTypeDto MapRoomTypeToDto(RoomType roomType)
    {
        return new RoomTypeDto
        {
            Id = roomType.Id,
            HotelId = roomType.HotelId,
            Name = roomType.Name,
            Description = roomType.Description,
            MaxOccupancy = roomType.MaxOccupancy,
            Size = roomType.Size,
            BedType = roomType.BedType,
            PricePerNight = roomType.PricePerNight,
            Currency = roomType.Currency,
            AvailableRooms = roomType.AvailableRooms,
            Amenities = roomType.Amenities,
            Images = roomType.Images,
            IsAvailable = roomType.IsAvailable,
            CreatedAt = roomType.CreatedAt
        };
    }
}
