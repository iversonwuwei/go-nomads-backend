using CoworkingService.Application.DTOs;
using CoworkingService.Domain.Entities;
using CoworkingService.Domain.Repositories;

namespace CoworkingService.Application.Services;

/// <summary>
///     Coworking 应用服务实现
///     协调领域对象完成业务用例
/// </summary>
public class CoworkingApplicationService : ICoworkingService
{
    private readonly ICoworkingBookingRepository _bookingRepository;
    private readonly ICoworkingRepository _coworkingRepository;
    private readonly ILogger<CoworkingApplicationService> _logger;

    public CoworkingApplicationService(
        ICoworkingRepository coworkingRepository,
        ICoworkingBookingRepository bookingRepository,
        ILogger<CoworkingApplicationService> logger)
    {
        _coworkingRepository = coworkingRepository;
        _bookingRepository = bookingRepository;
        _logger = logger;
    }

    #region Coworking Space 用例

    public async Task<CoworkingSpaceResponse> CreateCoworkingSpaceAsync(CreateCoworkingSpaceRequest request)
    {
        _logger.LogInformation("创建共享办公空间: {Name}", request.Name);

        try
        {
            // 1. 使用领域工厂方法创建实体
            var coworkingSpace = CoworkingSpace.Create(
                request.Name,
                request.Address,
                request.Latitude,
                request.Longitude,
                request.CityId,
                request.Description,
                request.ImageUrl,
                request.Images,
                request.PricePerDay,
                request.PricePerMonth,
                request.PricePerHour,
                request.Currency,
                request.WifiSpeed,
                request.HasMeetingRoom,
                request.HasCoffee,
                request.HasParking,
                request.Has247Access,
                request.Amenities,
                request.Capacity,
                request.Phone,
                request.Email,
                request.Website,
                request.OpeningHours,
                request.CreatedBy);

            // 2. 通过仓储持久化
            var created = await _coworkingRepository.CreateAsync(coworkingSpace);

            _logger.LogInformation("✅ 共享办公空间创建成功: {Id}", created.Id);

            // 3. 转换为 DTO 返回
            return MapToResponse(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建共享办公空间失败: {Name}", request.Name);
            throw;
        }
    }

    public async Task<CoworkingSpaceResponse> GetCoworkingSpaceAsync(Guid id)
    {
        _logger.LogInformation("获取共享办公空间: {Id}", id);

        var coworkingSpace = await _coworkingRepository.GetByIdAsync(id);
        if (coworkingSpace == null) throw new KeyNotFoundException($"未找到 ID 为 {id} 的共享办公空间");

        return MapToResponse(coworkingSpace);
    }

    public async Task<CoworkingSpaceResponse> UpdateCoworkingSpaceAsync(
        Guid id,
        UpdateCoworkingSpaceRequest request)
    {
        _logger.LogInformation("更新共享办公空间: {Id}", id);

        try
        {
            // 1. 获取聚合根
            var coworkingSpace = await _coworkingRepository.GetByIdAsync(id);
            if (coworkingSpace == null) throw new KeyNotFoundException($"未找到 ID 为 {id} 的共享办公空间");

            // 2. 调用领域方法更新
            coworkingSpace.Update(
                request.Name,
                request.Address,
                request.Latitude,
                request.Longitude,
                request.CityId,
                request.Description,
                request.ImageUrl,
                request.Images,
                request.PricePerDay,
                request.PricePerMonth,
                request.PricePerHour,
                request.Currency,
                request.WifiSpeed,
                request.HasMeetingRoom,
                request.HasCoffee,
                request.HasParking,
                request.Has247Access,
                request.Amenities,
                request.Capacity,
                request.Phone,
                request.Email,
                request.Website,
                request.OpeningHours,
                request.UpdatedBy);

            // 3. 持久化更新
            var updated = await _coworkingRepository.UpdateAsync(coworkingSpace);

            _logger.LogInformation("✅ 共享办公空间更新成功: {Id}", id);

            return MapToResponse(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新共享办公空间失败: {Id}", id);
            throw;
        }
    }

    public async Task DeleteCoworkingSpaceAsync(Guid id)
    {
        _logger.LogInformation("删除共享办公空间: {Id}", id);

        try
        {
            var exists = await _coworkingRepository.ExistsAsync(id);
            if (!exists) throw new KeyNotFoundException($"未找到 ID 为 {id} 的共享办公空间");

            await _coworkingRepository.DeleteAsync(id);

            _logger.LogInformation("✅ 共享办公空间删除成功: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除共享办公空间失败: {Id}", id);
            throw;
        }
    }

    public async Task<PaginatedCoworkingSpacesResponse> GetCoworkingSpacesAsync(
        int page = 1,
        int pageSize = 20,
        Guid? cityId = null)
    {
        _logger.LogInformation("获取共享办公空间列表: Page={Page}, PageSize={PageSize}, CityId={CityId}",
            page, pageSize, cityId);

        var (items, totalCount) = await _coworkingRepository.GetListAsync(page, pageSize, cityId);

        return new PaginatedCoworkingSpacesResponse
        {
            Items = items.Select(MapToResponse).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<CoworkingSpaceResponse>> SearchCoworkingSpacesAsync(
        string searchTerm,
        int page = 1,
        int pageSize = 20)
    {
        _logger.LogInformation("搜索共享办公空间: SearchTerm={SearchTerm}", searchTerm);

        var spaces = await _coworkingRepository.SearchAsync(searchTerm, page, pageSize);
        return spaces.Select(MapToResponse).ToList();
    }

    public async Task<List<CoworkingSpaceResponse>> GetTopRatedCoworkingSpacesAsync(int limit = 10)
    {
        _logger.LogInformation("获取评分最高的共享办公空间: Limit={Limit}", limit);

        var spaces = await _coworkingRepository.GetTopRatedAsync(limit);
        return spaces.Select(MapToResponse).ToList();
    }

    public async Task<Dictionary<Guid, int>> GetCoworkingCountByCitiesAsync(List<Guid> cityIds)
    {
        _logger.LogInformation("批量获取城市的 Coworking 空间数量: CityCount={Count}", cityIds.Count);

        if (cityIds == null || !cityIds.Any()) return new Dictionary<Guid, int>();

        var result = await _coworkingRepository.GetCountByCitiesAsync(cityIds);

        _logger.LogInformation("成功获取 {Count} 个城市的 Coworking 统计", result.Count);
        return result;
    }

    #endregion

    #region Booking 用例

    public async Task<CoworkingBookingResponse> CreateBookingAsync(CreateBookingRequest request)
    {
        _logger.LogInformation("创建预订: CoworkingId={CoworkingId}, UserId={UserId}",
            request.CoworkingId, request.UserId);

        try
        {
            // 1. 验证共享办公空间是否存在且可预订
            var coworkingSpace = await _coworkingRepository.GetByIdAsync(request.CoworkingId);
            if (coworkingSpace == null) throw new KeyNotFoundException($"未找到 ID 为 {request.CoworkingId} 的共享办公空间");

            if (!coworkingSpace.CanBook()) throw new InvalidOperationException("该共享办公空间不可预订");

            // 2. 检查预订冲突
            var hasConflict = await _bookingRepository.HasConflictAsync(
                request.CoworkingId,
                request.BookingDate,
                request.StartTime,
                request.EndTime);

            if (hasConflict) throw new InvalidOperationException("该时间段已被预订");

            // 3. 计算价格
            var totalPrice = CalculatePrice(
                coworkingSpace,
                request.BookingType,
                request.StartTime,
                request.EndTime);

            // 4. 使用领域工厂方法创建预订
            var booking = CoworkingBooking.Create(
                request.CoworkingId,
                request.UserId,
                request.BookingDate,
                request.BookingType,
                totalPrice,
                coworkingSpace.Currency,
                request.StartTime,
                request.EndTime,
                request.SpecialRequests);

            // 5. 持久化
            var created = await _bookingRepository.CreateAsync(booking);

            _logger.LogInformation("✅ 预订创建成功: {Id}", created.Id);

            return MapToBookingResponse(created, coworkingSpace);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建预订失败");
            throw;
        }
    }

    public async Task<CoworkingBookingResponse> GetBookingAsync(Guid id)
    {
        _logger.LogInformation("获取预订: {Id}", id);

        var booking = await _bookingRepository.GetByIdAsync(id);
        if (booking == null) throw new KeyNotFoundException($"未找到 ID 为 {id} 的预订");

        var coworkingSpace = await _coworkingRepository.GetByIdAsync(booking.CoworkingId);
        return MapToBookingResponse(booking, coworkingSpace);
    }

    public async Task CancelBookingAsync(Guid id, Guid userId)
    {
        _logger.LogInformation("取消预订: {Id}, UserId={UserId}", id, userId);

        try
        {
            var booking = await _bookingRepository.GetByIdAsync(id);
            if (booking == null) throw new KeyNotFoundException($"未找到 ID 为 {id} 的预订");

            // 验证用户权限
            if (booking.UserId != userId) throw new UnauthorizedAccessException("无权取消此预订");

            // 调用领域方法
            booking.Cancel();

            // 持久化
            await _bookingRepository.UpdateAsync(booking);

            _logger.LogInformation("✅ 预订取消成功: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 取消预订失败: {Id}", id);
            throw;
        }
    }

    public async Task<List<CoworkingBookingResponse>> GetUserBookingsAsync(Guid userId)
    {
        _logger.LogInformation("获取用户预订列表: UserId={UserId}", userId);

        var bookings = await _bookingRepository.GetByUserIdAsync(userId);
        var responses = new List<CoworkingBookingResponse>();

        foreach (var booking in bookings)
        {
            var coworkingSpace = await _coworkingRepository.GetByIdAsync(booking.CoworkingId);
            responses.Add(MapToBookingResponse(booking, coworkingSpace));
        }

        return responses;
    }

    public async Task ConfirmBookingAsync(Guid id)
    {
        _logger.LogInformation("确认预订: {Id}", id);

        try
        {
            var booking = await _bookingRepository.GetByIdAsync(id);
            if (booking == null) throw new KeyNotFoundException($"未找到 ID 为 {id} 的预订");

            booking.Confirm();
            await _bookingRepository.UpdateAsync(booking);

            _logger.LogInformation("✅ 预订确认成功: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 确认预订失败: {Id}", id);
            throw;
        }
    }

    public async Task CompleteBookingAsync(Guid id)
    {
        _logger.LogInformation("完成预订: {Id}", id);

        try
        {
            var booking = await _bookingRepository.GetByIdAsync(id);
            if (booking == null) throw new KeyNotFoundException($"未找到 ID 为 {id} 的预订");

            booking.Complete();
            await _bookingRepository.UpdateAsync(booking);

            _logger.LogInformation("✅ 预订完成成功: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 完成预订失败: {Id}", id);
            throw;
        }
    }

    #endregion

    #region Private Helpers

    /// <summary>
    ///     映射实体到响应 DTO
    /// </summary>
    private CoworkingSpaceResponse MapToResponse(CoworkingSpace space)
    {
        return new CoworkingSpaceResponse
        {
            Id = space.Id,
            Name = space.Name,
            CityId = space.CityId,
            Address = space.Address,
            Description = space.Description,
            ImageUrl = space.ImageUrl,
            Images = space.Images,
            PricePerDay = space.PricePerDay,
            PricePerMonth = space.PricePerMonth,
            PricePerHour = space.PricePerHour,
            Currency = space.Currency,
            Rating = space.Rating,
            ReviewCount = space.ReviewCount,
            WifiSpeed = space.WifiSpeed,
            HasMeetingRoom = space.HasMeetingRoom,
            HasCoffee = space.HasCoffee,
            HasParking = space.HasParking,
            Has247Access = space.Has247Access,
            Amenities = space.Amenities,
            Capacity = space.Capacity,
            Latitude = space.Latitude,
            Longitude = space.Longitude,
            Phone = space.Phone,
            Email = space.Email,
            Website = space.Website,
            OpeningHours = space.OpeningHours,
            IsActive = space.IsActive,
            CreatedAt = space.CreatedAt,
            UpdatedAt = space.UpdatedAt
        };
    }

    /// <summary>
    ///     映射预订实体到响应 DTO
    /// </summary>
    private CoworkingBookingResponse MapToBookingResponse(
        CoworkingBooking booking,
        CoworkingSpace? coworkingSpace)
    {
        return new CoworkingBookingResponse
        {
            Id = booking.Id,
            CoworkingId = booking.CoworkingId,
            UserId = booking.UserId,
            BookingDate = booking.BookingDate,
            StartTime = booking.StartTime,
            EndTime = booking.EndTime,
            BookingType = booking.BookingType,
            TotalPrice = booking.TotalPrice,
            Currency = booking.Currency,
            Status = booking.Status,
            SpecialRequests = booking.SpecialRequests,
            CreatedAt = booking.CreatedAt,
            CoworkingName = coworkingSpace?.Name,
            CoworkingAddress = coworkingSpace?.Address
        };
    }

    /// <summary>
    ///     计算预订价格
    /// </summary>
    private decimal CalculatePrice(
        CoworkingSpace coworkingSpace,
        string bookingType,
        TimeSpan? startTime,
        TimeSpan? endTime)
    {
        return bookingType switch
        {
            "hourly" => CalculateHourlyPrice(coworkingSpace, startTime, endTime),
            "daily" => coworkingSpace.PricePerDay ?? throw new InvalidOperationException("未设置日价格"),
            "monthly" => coworkingSpace.PricePerMonth ?? throw new InvalidOperationException("未设置月价格"),
            _ => throw new ArgumentException("无效的预订类型")
        };
    }

    /// <summary>
    ///     计算小时价格
    /// </summary>
    private decimal CalculateHourlyPrice(
        CoworkingSpace coworkingSpace,
        TimeSpan? startTime,
        TimeSpan? endTime)
    {
        if (!coworkingSpace.PricePerHour.HasValue) throw new InvalidOperationException("未设置时价格");

        if (!startTime.HasValue || !endTime.HasValue) throw new ArgumentException("小时预订必须指定开始和结束时间");

        var hours = (endTime.Value - startTime.Value).TotalHours;
        return (decimal)hours * coworkingSpace.PricePerHour.Value;
    }

    #endregion
}