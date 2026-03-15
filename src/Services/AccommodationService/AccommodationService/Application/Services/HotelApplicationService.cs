using AccommodationService.Application.DTOs;
using AccommodationService.Domain.Entities;
using AccommodationService.Domain.Repositories;
using AccommodationService.Services;

namespace AccommodationService.Application.Services;

/// <summary>
///     酒店应用服务实现
/// </summary>
public class HotelApplicationService : IHotelService
{
    private const int MaxExternalPageSize = 100;

    private readonly IBookingDemandClient _bookingDemandClient;
    private readonly ICityServiceClient _cityServiceClient;
    private readonly IHotelRepository _hotelRepository;
    private readonly IRoomTypeRepository _roomTypeRepository;
    private readonly ILogger<HotelApplicationService> _logger;

    public HotelApplicationService(
        IHotelRepository hotelRepository,
        IRoomTypeRepository roomTypeRepository,
        ICityServiceClient cityServiceClient,
        IBookingDemandClient bookingDemandClient,
        ILogger<HotelApplicationService> logger)
    {
        _hotelRepository = hotelRepository;
        _roomTypeRepository = roomTypeRepository;
        _cityServiceClient = cityServiceClient;
        _bookingDemandClient = bookingDemandClient;
        _logger = logger;
    }

    public async Task<HotelDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        if (IsExternalHotelId(id))
        {
            try
            {
                return await _bookingDemandClient.GetHotelDetailsAsync(id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load external hotel detail for {HotelId}. Returning null detail.", id);
                return null;
            }
        }

        if (!Guid.TryParse(id, out var hotelId)) return null;

        var hotel = await _hotelRepository.GetByIdAsync(hotelId, cancellationToken);
        if (hotel == null) return null;

        var dto = MapToDto(hotel);
        dto.RoomTypes = (await _roomTypeRepository.GetByHotelIdAsync(hotelId, cancellationToken))
            .Select(MapRoomTypeToDto).ToList();
        return dto;
    }

    public async Task<HotelListResponse> GetListAsync(HotelQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        var shouldMergeExternal = ShouldMergeExternalHotels(parameters);
        if (!shouldMergeExternal)
        {
            var (pagedHotels, totalCount) = await _hotelRepository.GetListAsync(
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
                Hotels = pagedHotels.Select(MapToDto).ToList(),
                ExternalDataStatus = "not_requested",
                PartialExternalData = false,
                TotalCount = totalCount,
                Page = parameters.Page,
                PageSize = parameters.PageSize
            };
        }

        var fetchSize = Math.Min(Math.Max(parameters.Page * parameters.PageSize, parameters.PageSize), MaxExternalPageSize);

        var externalTask = SearchExternalHotelsAsync(parameters, fetchSize, cancellationToken);
        var internalTask = _hotelRepository.GetListAsync(
            1,
            fetchSize,
            parameters.CityId,
            parameters.Search,
            parameters.HasWifi,
            parameters.HasCoworkingSpace,
            parameters.MinPrice,
            parameters.MaxPrice,
            activeOnly: true,
            cancellationToken);

        await Task.WhenAll(externalTask, internalTask);

        var externalResult = await externalTask;
        var (internalHotels, internalTotalCount) = await internalTask;
        var mergedHotels = MergeHotels(
            internalHotels.Select(MapToDto),
            externalResult.Response?.Hotels ?? Enumerable.Empty<HotelDto>());

        return new HotelListResponse
        {
            Hotels = mergedHotels
                .Skip((parameters.Page - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .ToList(),
            ExternalDataStatus = externalResult.Status,
            PartialExternalData = externalResult.PartialExternalData,
            ExternalDataMessage = externalResult.Message,
            TotalCount = mergedHotels.Count,
            Page = parameters.Page,
            PageSize = parameters.PageSize
        };
    }

    public async Task<List<HotelDto>> GetByCityIdAsync(Guid cityId, CancellationToken cancellationToken = default)
    {
        var hotels = await _hotelRepository.GetByCityIdAsync(cityId, cancellationToken);
        var internalHotels = hotels.Select(MapToDto);

        if (!_bookingDemandClient.IsConfigured)
        {
            return internalHotels.ToList();
        }

        var cityInfo = await _cityServiceClient.GetCityInfoAsync(cityId.ToString(), cancellationToken);
        if (cityInfo == null)
        {
            return internalHotels.ToList();
        }

        List<HotelDto> externalHotels;
        try
        {
            externalHotels = await _bookingDemandClient.SearchHotelsAsync(new BookingHotelSearchRequest
            {
                CityName = cityInfo.NameEn ?? cityInfo.Name,
                CountryName = cityInfo.Country,
                Latitude = cityInfo.Latitude,
                Longitude = cityInfo.Longitude,
                Rows = 50
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load external hotels for city {CityId}. Falling back to internal hotels only.", cityId);
            externalHotels = new List<HotelDto>();
        }

        return MergeHotels(internalHotels, externalHotels).ToList();
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
        return await GetByIdAsync(created.Id.ToString(), cancellationToken) ?? MapToDto(created);
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

        return await GetByIdAsync(updated.Id.ToString(), cancellationToken) ?? MapToDto(updated);
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
            Id = hotel.Id.ToString(),
            Source = "community",
            ExternalStatus = "internal",
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

    private async Task<ExternalHotelFetchResult> SearchExternalHotelsAsync(
        HotelQueryParameters parameters,
        int rows,
        CancellationToken cancellationToken)
    {
        if (!_bookingDemandClient.IsConfigured)
        {
            return new ExternalHotelFetchResult(
                null,
                "disabled",
                "Booking Demand integration is disabled. Returned community hotels only.",
                false);
        }

        var cityName = parameters.CityName;
        var countryName = parameters.CountryName;
        var latitude = parameters.Latitude;
        var longitude = parameters.Longitude;

        if (parameters.CityId.HasValue &&
            (string.IsNullOrWhiteSpace(cityName) || !latitude.HasValue || !longitude.HasValue))
        {
            var cityInfo = await _cityServiceClient.GetCityInfoAsync(parameters.CityId.Value.ToString(), cancellationToken);
            if (cityInfo != null)
            {
                cityName ??= cityInfo.NameEn ?? cityInfo.Name;
                countryName ??= cityInfo.Country;
                latitude ??= cityInfo.Latitude;
                longitude ??= cityInfo.Longitude;
            }
        }

        if (string.IsNullOrWhiteSpace(cityName) && (!latitude.HasValue || !longitude.HasValue))
        {
            return new ExternalHotelFetchResult(
                null,
                "not_requested",
                "External hotel search skipped because location context was incomplete.",
                false);
        }

        List<HotelDto> hotels;
        try
        {
            hotels = await _bookingDemandClient.SearchHotelsAsync(new BookingHotelSearchRequest
            {
                CityName = cityName,
                CountryName = countryName,
                Latitude = latitude,
                Longitude = longitude,
                CheckInDate = parameters.CheckInDate,
                StayNights = parameters.StayNights,
                AdultCount = parameters.AdultCount,
                RoomCount = parameters.RoomCount,
                Search = parameters.Search,
                Rows = rows
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load external hotels for query. Falling back to internal hotels only.");
            return new ExternalHotelFetchResult(
                new HotelListResponse
                {
                    Hotels = new List<HotelDto>(),
                    ExternalDataStatus = "unavailable",
                    PartialExternalData = true,
                    ExternalDataMessage = "Booking Demand request failed. Returned community hotels only.",
                    TotalCount = 0,
                    Page = parameters.Page,
                    PageSize = parameters.PageSize
                },
                "unavailable",
                "Booking Demand request failed. Returned community hotels only.",
                true);
        }

        if (hotels.Count == 0)
        {
            return new ExternalHotelFetchResult(
                new HotelListResponse
                {
                    Hotels = new List<HotelDto>(),
                    ExternalDataStatus = "live",
                    PartialExternalData = false,
                    TotalCount = 0,
                    Page = parameters.Page,
                    PageSize = parameters.PageSize
                },
                "live",
                null,
                false);
        }

        return new ExternalHotelFetchResult(
            new HotelListResponse
            {
                Hotels = hotels,
                ExternalDataStatus = "live",
                PartialExternalData = false,
                TotalCount = hotels.Count,
                Page = parameters.Page,
                PageSize = parameters.PageSize
            },
            "live",
            null,
            false);
    }

    private static bool ShouldMergeExternalHotels(HotelQueryParameters parameters)
    {
        return parameters.CityId.HasValue ||
               !string.IsNullOrWhiteSpace(parameters.CityName) ||
               (parameters.Latitude.HasValue && parameters.Longitude.HasValue);
    }

    private static List<HotelDto> MergeHotels(IEnumerable<HotelDto> internalHotels, IEnumerable<HotelDto> externalHotels)
    {
        var merged = internalHotels
            .Concat(externalHotels)
            .GroupBy(BuildHotelIdentityKey)
            .Select(group => group
                .OrderByDescending(h => h.Source.Equals("community", StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(h => h.IsFeatured)
                .ThenByDescending(h => h.Rating)
                .ThenByDescending(h => h.ReviewCount)
                .First())
            .OrderByDescending(h => h.Source.Equals("community", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(h => h.IsFeatured)
            .ThenByDescending(h => h.Rating)
            .ThenByDescending(h => h.ReviewCount)
            .ThenBy(h => h.PricePerNight)
            .ToList();

        return merged;
    }

    private static string BuildHotelIdentityKey(HotelDto hotel)
    {
        var normalizedName = NormalizeKeyPart(hotel.Name);
        var normalizedCity = NormalizeKeyPart(hotel.CityName);
        var normalizedAddress = NormalizeKeyPart(hotel.Address);

        return string.Join("|", normalizedName, normalizedCity, normalizedAddress);
    }

    private static string NormalizeKeyPart(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value
            .Trim()
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());
    }

    private static bool IsExternalHotelId(string id)
    {
        return id.StartsWith("booking_", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record ExternalHotelFetchResult(
        HotelListResponse? Response,
        string Status,
        string? Message,
        bool PartialExternalData);

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
