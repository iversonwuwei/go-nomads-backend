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
    private readonly ICoworkingVerificationRepository _verificationRepository;
    private readonly ICoworkingCommentRepository _commentRepository;
    private readonly ICoworkingReviewRepository _reviewRepository;
    private readonly ILogger<CoworkingApplicationService> _logger;

    public CoworkingApplicationService(
        ICoworkingRepository coworkingRepository,
        ICoworkingVerificationRepository verificationRepository,
        ICoworkingBookingRepository bookingRepository,
        ICoworkingCommentRepository commentRepository,
        ICoworkingReviewRepository reviewRepository,
        ILogger<CoworkingApplicationService> logger)
    {
        _coworkingRepository = coworkingRepository;
        _verificationRepository = verificationRepository;
        _bookingRepository = bookingRepository;
        _commentRepository = commentRepository;
        _reviewRepository = reviewRepository;
        _logger = logger;
    }

    #region Coworking Space 用例

    public async Task<CoworkingSpaceResponse> CreateCoworkingSpaceAsync(CreateCoworkingSpaceRequest request)
    {
        _logger.LogInformation("创建共享办公空间: {Name}", request.Name);

        try
        {
            // 1. 使用领域工厂方法创建实体
            var desiredStatus = CoworkingVerificationStatus.Unverified;

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
                request.CreatedBy,
                desiredStatus);

            // 2. 通过仓储持久化
            var created = await _coworkingRepository.CreateAsync(coworkingSpace);

            _logger.LogInformation("✅ 共享办公空间创建成功: {Id}", created.Id);

            // 3. 转换为 DTO 返回 (新创建的评分为 0)
            return MapToResponse(created, 0, 0.0, 0);
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

        var votes = await _verificationRepository.GetVerificationCountAsync(id);
        var (averageRating, reviewCount) = await _reviewRepository.GetAverageRatingAsync(id);
        return MapToResponse(coworkingSpace, votes, averageRating, reviewCount);
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

            var votes = await _verificationRepository.GetVerificationCountAsync(id);
            var (averageRating, reviewCount) = await _reviewRepository.GetAverageRatingAsync(id);
            return MapToResponse(updated, votes, averageRating, reviewCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新共享办公空间失败: {Id}", id);
            throw;
        }
    }

    public async Task<CoworkingSpaceResponse> UpdateVerificationStatusAsync(
        Guid id,
        UpdateCoworkingVerificationStatusRequest request)
    {
        _logger.LogInformation("更新 Coworking 认证状态: {Id} -> {Status}", id, request.VerificationStatus);

        if (!CoworkingVerificationStatus.IsValid(request.VerificationStatus))
            throw new ArgumentException("认证状态必须为 verified 或 unverified", nameof(request.VerificationStatus));

        var coworkingSpace = await _coworkingRepository.GetByIdAsync(id)
                               ?? throw new KeyNotFoundException($"未找到 ID 为 {id} 的共享办公空间");

        var votesBeforeUpdate = await _verificationRepository.GetVerificationCountAsync(id);
        if (request.VerificationStatus == CoworkingVerificationStatus.Verified && votesBeforeUpdate <= 3)
            throw new InvalidOperationException("至少需要超过 3 个不同用户的认证才能通过审核");

        coworkingSpace.SetVerificationStatus(request.VerificationStatus, request.UpdatedBy);

        var updated = await _coworkingRepository.UpdateAsync(coworkingSpace);
        var (averageRating, reviewCount) = await _reviewRepository.GetAverageRatingAsync(id);
        return MapToResponse(updated, votesBeforeUpdate, averageRating, reviewCount);
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
        var verificationCounts = await _verificationRepository.GetCountsByCoworkingIdsAsync(items.Select(i => i.Id));
        
        // 批量获取评分和评论数
        var responseTasks = items.Select(async space =>
        {
            var votes = verificationCounts.TryGetValue(space.Id, out var v) ? v : 0;
            var (averageRating, reviewCount) = await _reviewRepository.GetAverageRatingAsync(space.Id);
            return MapToResponse(space, votes, averageRating, reviewCount);
        });
        var responses = await Task.WhenAll(responseTasks);

        return new PaginatedCoworkingSpacesResponse
        {
            Items = responses.ToList(),
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
        var verificationCounts = await _verificationRepository.GetCountsByCoworkingIdsAsync(spaces.Select(s => s.Id));
        
        var responseTasks = spaces.Select(async space =>
        {
            var votes = verificationCounts.TryGetValue(space.Id, out var v) ? v : 0;
            var (averageRating, reviewCount) = await _reviewRepository.GetAverageRatingAsync(space.Id);
            return MapToResponse(space, votes, averageRating, reviewCount);
        });
        return (await Task.WhenAll(responseTasks)).ToList();
    }

    public async Task<List<CoworkingSpaceResponse>> GetTopRatedCoworkingSpacesAsync(int limit = 10)
    {
        _logger.LogInformation("获取评分最高的共享办公空间: Limit={Limit}", limit);

        var spaces = await _coworkingRepository.GetTopRatedAsync(limit);
        var verificationCounts = await _verificationRepository.GetCountsByCoworkingIdsAsync(spaces.Select(s => s.Id));
        
        var responseTasks = spaces.Select(async space =>
        {
            var votes = verificationCounts.TryGetValue(space.Id, out var v) ? v : 0;
            var (averageRating, reviewCount) = await _reviewRepository.GetAverageRatingAsync(space.Id);
            return MapToResponse(space, votes, averageRating, reviewCount);
        });
        return (await Task.WhenAll(responseTasks)).ToList();
    }

    public async Task<CoworkingSpaceResponse> SubmitVerificationAsync(Guid id, Guid userId)
    {
        var coworkingSpace = await _coworkingRepository.GetByIdAsync(id)
                               ?? throw new KeyNotFoundException($"未找到 ID 为 {id} 的共享办公空间");

        if (coworkingSpace.CreatedBy.HasValue && coworkingSpace.CreatedBy.Value == userId)
            throw new InvalidOperationException("创建者不能为自己的 Coworking 认证");

        var alreadyVerified = await _verificationRepository.HasUserVerifiedAsync(id, userId);
        if (alreadyVerified) throw new InvalidOperationException("您已提交过认证");

        await _verificationRepository.AddAsync(CoworkingVerification.Create(id, userId));

        var votes = await _verificationRepository.GetVerificationCountAsync(id);

        if (coworkingSpace.VerificationStatus == CoworkingVerificationStatus.Unverified && votes > 3)
        {
            coworkingSpace.SetVerificationStatus(CoworkingVerificationStatus.Verified, userId);
            coworkingSpace = await _coworkingRepository.UpdateAsync(coworkingSpace);
        }

        var (averageRating, reviewCount) = await _reviewRepository.GetAverageRatingAsync(id);
        return MapToResponse(coworkingSpace, votes, averageRating, reviewCount);
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
    private CoworkingSpaceResponse MapToResponse(CoworkingSpace space, int verificationVotes = 0, double? averageRating = null, int? reviewCount = null)
    {
        return new CoworkingSpaceResponse
        {
            Id = space.Id,
            Name = space.Name,
            CityId = space.CityId,
            CreatedBy = space.CreatedBy,
            Address = space.Address,
            Description = space.Description,
            ImageUrl = space.ImageUrl,
            Images = space.Images,
            PricePerDay = space.PricePerDay,
            PricePerMonth = space.PricePerMonth,
            PricePerHour = space.PricePerHour,
            Currency = space.Currency,
            Rating = (decimal)(averageRating ?? (double)space.Rating),
            ReviewCount = reviewCount ?? space.ReviewCount,
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
            VerificationStatus = space.VerificationStatus,
            VerificationVotes = verificationVotes,
            CreatedAt = space.CreatedAt,
            UpdatedAt = space.UpdatedAt,
            IsOwner = false
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

    #region Comment 用例

    public async Task<CoworkingCommentResponse> CreateCommentAsync(
        Guid coworkingId,
        Guid userId,
        CreateCoworkingCommentRequest request)
    {
        _logger.LogInformation("创建评论: CoworkingId={CoworkingId}, UserId={UserId}", coworkingId, userId);

        try
        {
            // 验证 Coworking 是否存在
            var coworkingSpace = await _coworkingRepository.GetByIdAsync(coworkingId);
            if (coworkingSpace == null)
                throw new KeyNotFoundException($"未找到 ID 为 {coworkingId} 的共享办公空间");

            // 创建评论
            var comment = CoworkingComment.Create(coworkingId, userId, request.Content, request.Rating, request.Images);
            var created = await _commentRepository.CreateAsync(comment);

            _logger.LogInformation("✅ 评论创建成功: {Id}", created.Id);

            return MapToCommentResponse(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建评论失败: CoworkingId={CoworkingId}", coworkingId);
            throw;
        }
    }

    public async Task<List<CoworkingCommentResponse>> GetCommentsAsync(
        Guid coworkingId,
        int page = 1,
        int pageSize = 20)
    {
        _logger.LogInformation("获取评论列表: CoworkingId={CoworkingId}, Page={Page}", coworkingId, page);

        var comments = await _commentRepository.GetByCoworkingIdAsync(coworkingId, page, pageSize);
        return comments.Select(MapToCommentResponse).ToList();
    }

    public async Task<int> GetCommentCountAsync(Guid coworkingId)
    {
        return await _commentRepository.GetCountByCoworkingIdAsync(coworkingId);
    }

    public async Task DeleteCommentAsync(Guid id, Guid userId)
    {
        _logger.LogInformation("删除评论: Id={Id}, UserId={UserId}", id, userId);

        try
        {
            var comment = await _commentRepository.GetByIdAsync(id);
            if (comment == null)
                throw new KeyNotFoundException($"未找到 ID 为 {id} 的评论");

            // 验证用户权限（只能删除自己的评论）
            if (comment.UserId != userId)
                throw new UnauthorizedAccessException("无权删除此评论");

            comment.SoftDelete();
            await _commentRepository.UpdateAsync(comment);

            _logger.LogInformation("✅ 评论删除成功: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除评论失败: {Id}", id);
            throw;
        }
    }

    private CoworkingCommentResponse MapToCommentResponse(CoworkingComment comment)
    {
        return new CoworkingCommentResponse
        {
            Id = comment.Id,
            CoworkingId = comment.CoworkingId,
            UserId = comment.UserId,
            Content = comment.Content,
            Rating = comment.Rating,
            Images = comment.Images,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt
        };
    }

    #endregion
}