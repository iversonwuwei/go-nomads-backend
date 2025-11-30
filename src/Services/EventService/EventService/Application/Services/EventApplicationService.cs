using EventService.Application.DTOs;
using EventService.Domain.Entities;
using EventService.Domain.Repositories;
using EventService.Infrastructure.GrpcClients;

namespace EventService.Application.Services;

/// <summary>
///     Event 应用服务实现
/// </summary>
public class EventApplicationService : IEventService
{
    private readonly ICityGrpcClient _cityGrpcClient;
    private readonly IEventRepository _eventRepository;
    private readonly IEventFollowerRepository _followerRepository;
    private readonly ILogger<EventApplicationService> _logger;
    private readonly IEventParticipantRepository _participantRepository;
    private readonly IUserGrpcClient _userGrpcClient;
    private readonly IEventTypeRepository _eventTypeRepository;

    public EventApplicationService(
        IEventRepository eventRepository,
        IEventParticipantRepository participantRepository,
        IEventFollowerRepository followerRepository,
        ICityGrpcClient cityGrpcClient,
        IUserGrpcClient userGrpcClient,
        IEventTypeRepository eventTypeRepository,
        ILogger<EventApplicationService> logger)
    {
        _eventRepository = eventRepository;
        _participantRepository = participantRepository;
        _followerRepository = followerRepository;
        _cityGrpcClient = cityGrpcClient;
        _userGrpcClient = userGrpcClient;
        _eventTypeRepository = eventTypeRepository;
        _logger = logger;
    }

    public async Task<EventResponse> CreateEventAsync(CreateEventRequest request, Guid organizerId)
    {
        _logger.LogInformation("📝 创建新 Event，Organizer: {OrganizerId}", organizerId);

        // 使用领域工厂方法创建实体
        var @event = Event.Create(
            request.Title,
            organizerId,
            request.StartTime,
            request.Description,
            request.CityId,
            request.Location,
            request.Address,
            request.ImageUrl,
            request.Images?.ToArray(),
            request.Category,
            request.EndTime,
            request.MaxParticipants,
            request.LocationType,
            request.MeetingLink,
            request.Latitude,
            request.Longitude,
            request.Tags?.ToArray());

        // 持久化
        var createdEvent = await _eventRepository.CreateAsync(@event);

        var response = await MapToResponseAsync(createdEvent);
        
        // 创建者就是组织者，设置 IsOrganizer = true
        response.IsOrganizer = true;
        response.IsParticipant = false; // 创建者默认未参加，需要手动 RSVP

        return response;
    }

    public async Task<EventResponse> GetEventAsync(Guid id, Guid? userId = null)
    {
        var @event = await _eventRepository.GetByIdAsync(id);
        if (@event == null) throw new KeyNotFoundException($"Event {id} 不存在");

        var response = await MapToResponseAsync(@event);

        // 如果提供了 userId，检查参与状态和组织者身份
        if (userId.HasValue)
        {
            // 暂时不使用 follower 功能,只检查参与状态
            response.IsFollowing = false;
            response.IsParticipant = await _participantRepository.IsParticipantAsync(id, userId.Value);

            // 判断当前用户是否是活动组织者
            response.IsOrganizer = response.OrganizerId == userId.Value;

            _logger.LogInformation("👥 用户 {UserId} 是否参与了活动 {EventId}: {IsParticipant}", userId.Value, id,
                response.IsParticipant);
            _logger.LogInformation("👥 用户 {UserId} 是否是活动 {EventId} 的组织者: {IsOrganizer}", userId.Value, id,
                response.IsOrganizer);
        }

        // 暂时将关注者数量设为 0
        response.FollowerCount = 0;

        // 获取参与者列表
        var participants = await GetParticipantsAsync(id);

        // 🔧 为参与者填充用户信息（通过 gRPC 调用 UserService）
        if (participants.Any())
        {
            var userIds = participants.Select(p => p.UserId).Distinct().ToList();
            _logger.LogInformation("📞 通过 gRPC 获取 {Count} 个参与者的完整用户信息", userIds.Count);

            try
            {
                var users = await _userGrpcClient.GetUsersInfoByIdsAsync(userIds);

                foreach (var participant in participants)
                    if (users.TryGetValue(participant.UserId, out var userInfo))
                        participant.User = userInfo;

                _logger.LogInformation("✅ 成功为 {Count} 个参与者填充用户信息", participants.Count(p => p.User != null));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 获取参与者用户信息失败");
                // 不抛出异常，返回不完整的数据
            }
        }

        response.Participants = participants.ToList();

        // 🔧 修正参与者数量:使用实际参与者列表的长度,确保数据准确
        response.CurrentParticipants = participants.Count;

        // 填充关联数据
        await EnrichEventResponsesWithRelatedDataAsync(new List<EventResponse> { response });

        return response;
    }

    public async Task<EventResponse> UpdateEventAsync(Guid id, UpdateEventRequest request, Guid userId)
    {
        var @event = await _eventRepository.GetByIdAsync(id);
        if (@event == null) throw new KeyNotFoundException($"Event {id} 不存在");

        // 使用领域方法更新（包含权限验证）
        @event.Update(
            userId,
            request.Title,
            request.Description,
            request.CityId,
            request.Location,
            request.Address,
            request.ImageUrl,
            request.Images?.ToArray(),
            request.Category,
            request.StartTime,
            request.EndTime,
            request.MaxParticipants,
            request.Status,
            request.LocationType,
            request.MeetingLink,
            request.Latitude,
            request.Longitude,
            request.Tags?.ToArray());

        var updatedEvent = await _eventRepository.UpdateAsync(@event);

        return await MapToResponseAsync(updatedEvent);
    }

    /// <summary>
    ///     取消活动
    /// </summary>
    public async Task<EventResponse> CancelEventAsync(Guid id, Guid userId)
    {
        var @event = await _eventRepository.GetByIdAsync(id);
        if (@event == null) throw new KeyNotFoundException($"Event {id} 不存在");

        // 验证权限：只有组织者可以取消
        if (@event.OrganizerId != userId) throw new UnauthorizedAccessException("只有组织者可以取消活动");

        // 使用领域方法取消
        @event.Cancel(userId);

        var updatedEvent = await _eventRepository.UpdateAsync(@event);

        _logger.LogInformation("✅ 活动 {EventId} 已被用户 {UserId} 取消", id, userId);

        return await MapToResponseAsync(updatedEvent);
    }

    public async Task<(List<EventResponse> Events, int Total)> GetEventsAsync(
        Guid? cityId = null,
        string? category = null,
        string? status = null,
        int page = 1,
        int pageSize = 20,
        Guid? userId = null)
    {
        var (events, total) = await _eventRepository.GetListAsync(cityId, category, status, page, pageSize);

        // 转换为 DTO（并行处理）
        var responses = await Task.WhenAll(events.Select(e => MapToResponseAsync(e)));
        var responsesList = responses.ToList();

        // 批量获取关联数据
        await EnrichEventResponsesWithRelatedDataAsync(responsesList);

        // 🔧 保持 current_participants 来自 events 表，避免 N+1 查询
        // 如果后续需要校准，可在后台任务中同步 event_participants 表与该字段。

        // 如果有用户ID,批量检查参与状态
        if (userId.HasValue) await EnrichEventParticipationStatusAsync(responsesList, userId.Value);

        return (responsesList, total);
    }

    public async Task<ParticipantResponse> JoinEventAsync(Guid eventId, Guid userId, JoinEventRequest request)
    {
        _logger.LogInformation("👥 用户 {UserId} 申请参加 Event {EventId}", userId, eventId);

        // 检查 Event 是否存在
        var @event = await _eventRepository.GetByIdAsync(eventId);
        if (@event == null) throw new KeyNotFoundException($"Event {eventId} 不存在");

        // 检查是否已有参与记录(包括已取消的)
        var existingParticipant = await _participantRepository.GetAsync(eventId, userId);
        
        // 如果存在已取消的记录,更新状态为registered
        if (existingParticipant != null)
        {
            if (existingParticipant.Status != "cancelled")
            {
                throw new InvalidOperationException("您已经参加了这个 Event");
            }
            
            _logger.LogInformation("♻️ 检测到已取消的参与记录,更新状态为registered");
            existingParticipant.UpdateStatus("registered");
            var updatedParticipant = await _participantRepository.UpdateAsync(existingParticipant);
            
            // 更新参与人数
            @event.AddParticipant();
            await _eventRepository.UpdateAsync(@event);
            
            return MapToParticipantResponse(updatedParticipant);
        }

        // 检查是否可以参加（领域逻辑）
        if (!@event.CanJoin()) throw new InvalidOperationException("Event 已满员或状态不允许参加");

        // 创建新的参与记录
        var participant = EventParticipant.Create(eventId, userId);
        var createdParticipant = await _participantRepository.CreateAsync(participant);

        // 更新参与人数（领域逻辑）
        @event.AddParticipant();
        await _eventRepository.UpdateAsync(@event);

        return MapToParticipantResponse(createdParticipant);
    }

    public async Task LeaveEventAsync(Guid eventId, Guid userId)
    {
        _logger.LogInformation("👋 用户 {UserId} 取消参加 Event {EventId}", userId, eventId);

        // 查找参与记录
        var participant = await _participantRepository.GetAsync(eventId, userId);
        if (participant == null) throw new KeyNotFoundException("您未参加此 Event");

        // 更新参与记录状态为 cancelled（而不是删除）
        participant.UpdateStatus("cancelled");
        await _participantRepository.UpdateAsync(participant);

        // 更新参与人数
        var @event = await _eventRepository.GetByIdAsync(eventId);
        if (@event != null)
        {
            @event.RemoveParticipant();
            await _eventRepository.UpdateAsync(@event);
        }

        _logger.LogInformation("✅ 用户 {UserId} 的参与状态已更新为 cancelled", userId);
    }

    public async Task<FollowerResponse> FollowEventAsync(Guid eventId, Guid userId, FollowEventRequest request)
    {
        _logger.LogInformation("⭐ 用户 {UserId} 关注 Event {EventId}", userId, eventId);

        // 检查 Event 是否存在
        if (!await _eventRepository.ExistsAsync(eventId)) throw new KeyNotFoundException($"Event {eventId} 不存在");

        // 检查是否已关注
        if (await _followerRepository.IsFollowingAsync(eventId, userId))
            throw new InvalidOperationException("您已经关注了这个 Event");

        // 创建关注记录
        var follower = EventFollower.Create(eventId, userId, request.NotificationEnabled);
        var createdFollower = await _followerRepository.CreateAsync(follower);

        return MapToFollowerResponse(createdFollower);
    }

    public async Task UnfollowEventAsync(Guid eventId, Guid userId)
    {
        _logger.LogInformation("💔 用户 {UserId} 取消关注 Event {EventId}", userId, eventId);

        // 查找关注记录
        var follower = await _followerRepository.GetAsync(eventId, userId);
        if (follower == null) throw new KeyNotFoundException("您未关注此 Event");

        // 删除关注记录
        await _followerRepository.DeleteAsync(follower.Id);
    }

    public async Task<List<ParticipantResponse>> GetParticipantsAsync(Guid eventId)
    {
        var participants = await _participantRepository.GetByEventIdAsync(eventId);
        return participants.Select(MapToParticipantResponse).ToList();
    }

    public async Task<List<FollowerResponse>> GetFollowersAsync(Guid eventId)
    {
        var followers = await _followerRepository.GetByEventIdAsync(eventId);
        return followers.Select(MapToFollowerResponse).ToList();
    }

    public async Task<List<EventResponse>> GetUserCreatedEventsAsync(Guid userId)
    {
        var events = await _eventRepository.GetByOrganizerIdAsync(userId);
        var responses = await Task.WhenAll(events.Select(e => MapToResponseAsync(e)));
        return responses.ToList();
    }

    public async Task<int> GetUserCreatedEventsCountAsync(Guid userId)
    {
        var events = await _eventRepository.GetByOrganizerIdAsync(userId);
        return events.Count;
    }

    public async Task<List<EventResponse>> GetUserJoinedEventsAsync(Guid userId)
    {
        var participants = await _participantRepository.GetByUserIdAsync(userId);
        var eventIds = participants.Select(p => p.EventId).ToList();

        var events = new List<Event>();
        foreach (var eventId in eventIds)
        {
            var @event = await _eventRepository.GetByIdAsync(eventId);
            if (@event != null) events.Add(@event);
        }

        var responses = await Task.WhenAll(events.Select(e => MapToResponseAsync(e)));
        return responses.ToList();
    }

    public async Task<List<EventResponse>> GetUserFollowingEventsAsync(Guid userId)
    {
        var followers = await _followerRepository.GetByUserIdAsync(userId);
        var eventIds = followers.Select(f => f.EventId).ToList();

        var events = new List<Event>();
        foreach (var eventId in eventIds)
        {
            var @event = await _eventRepository.GetByIdAsync(eventId);
            if (@event != null) events.Add(@event);
        }

        var responses = await Task.WhenAll(events.Select(e => MapToResponseAsync(e)));
        return responses.ToList();
    }

    /// <summary>
    ///     为事件列表填充关联数据（城市、组织者信息）
    /// </summary>
    private async Task EnrichEventResponsesWithRelatedDataAsync(List<EventResponse> responses)
    {
        _logger.LogInformation("🔍 开始为 {Count} 个事件填充关联数据", responses.Count);

        if (!responses.Any())
        {
            _logger.LogInformation("⚠️ 事件列表为空，跳过关联数据填充");
            return;
        }

        try
        {
            // 收集所有需要查询的 CityId 和 OrganizerId
            var cityIds = responses
                .Where(r => r.CityId.HasValue)
                .Select(r => r.CityId!.Value)
                .Distinct()
                .ToList();

            var organizerIds = responses
                .Select(r => r.OrganizerId)
                .Distinct()
                .ToList();

            _logger.LogInformation("📊 需要查询 {CityCount} 个城市和 {OrganizerCount} 个组织者",
                cityIds.Count, organizerIds.Count);

            // 并行批量获取城市和用户信息
            var getCitiesTask = _cityGrpcClient.GetCitiesByIdsAsync(cityIds);
            var getUsersTask = _userGrpcClient.GetUsersByIdsAsync(organizerIds);

            await Task.WhenAll(getCitiesTask, getUsersTask);

            var cities = await getCitiesTask;
            var users = await getUsersTask;

            _logger.LogInformation("📥 获取到 {CityCount} 个城市和 {UserCount} 个组织者信息",
                cities.Count, users.Count);

            // 填充数据到每个 EventResponse
            foreach (var response in responses)
            {
                // 填充城市信息
                if (response.CityId.HasValue && cities.TryGetValue(response.CityId.Value, out var cityInfo))
                    response.City = cityInfo;

                // 填充组织者信息
                if (users.TryGetValue(response.OrganizerId, out var organizerInfo)) response.Organizer = organizerInfo;
            }

            _logger.LogInformation("✅ 已为 {Count} 个事件填充关联数据（城市: {CityCount}, 组织者: {OrganizerCount}）",
                responses.Count, cities.Count, users.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 填充事件关联数据失败，将返回不完整的数据");
            // 不抛出异常，允许返回不完整的数据
        }
    }

    /// <summary>
    ///     批量填充事件参与状态（优化后的批量查询版本）
    /// </summary>
    private async Task EnrichEventParticipationStatusAsync(List<EventResponse> responses, Guid userId)
    {
        _logger.LogInformation("👥 开始为 {Count} 个事件填充参与状态，用户ID: {UserId}", responses.Count, userId);

        if (!responses.Any()) return;

        try
        {
            // 🚀 性能优化：使用批量查询代替 N+1 循环查询
            var eventIds = responses.Select(r => r.Id).ToList();
            var participatedEventIds = await _participantRepository.GetParticipatedEventIdsAsync(eventIds, userId);

            // 批量填充参与状态和组织者状态
            foreach (var response in responses)
            {
                response.IsParticipant = participatedEventIds.Contains(response.Id);
                response.IsOrganizer = response.OrganizerId == userId;
            }

            var participatedCount = responses.Count(r => r.IsParticipant);
            var organizerCount = responses.Count(r => r.IsOrganizer);
            _logger.LogInformation("✅ 用户参与了 {ParticipatedCount}/{TotalCount} 个活动，组织了 {OrganizerCount} 个活动",
                participatedCount, responses.Count, organizerCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 填充参与状态失败");
        }
    }

    #region Mapping Methods

    private async Task<EventResponse> MapToResponseAsync(Event @event)
    {
        var response = new EventResponse
        {
            Id = @event.Id,
            Title = @event.Title,
            Description = @event.Description,
            OrganizerId = @event.OrganizerId,
            CityId = @event.CityId,
            Location = @event.Location,
            Address = @event.Address,
            ImageUrl = @event.ImageUrl,
            Images = @event.Images?.ToList(),
            Category = @event.Category,
            StartTime = @event.StartTime,
            EndTime = @event.EndTime,
            MaxParticipants = @event.MaxParticipants,
            CurrentParticipants = @event.CurrentParticipants,
            Status = @event.Status,
            LocationType = @event.LocationType,
            MeetingLink = @event.MeetingLink,
            Latitude = @event.Latitude,
            Longitude = @event.Longitude,
            Tags = @event.Tags?.ToList(),
            IsFeatured = @event.IsFeatured,
            CreatedAt = @event.CreatedAt,
            UpdatedAt = @event.UpdatedAt
        };

        // 🔍 根据 category (UUID) 查询 EventType
        if (!string.IsNullOrEmpty(@event.Category) && Guid.TryParse(@event.Category, out var eventTypeId))
        {
            try
            {
                var eventType = await _eventTypeRepository.GetByIdAsync(eventTypeId);
                if (eventType != null)
                {
                    response.EventType = new EventTypeInfo
                    {
                        Id = eventType.Id,
                        Name = eventType.Name,
                        EnName = eventType.EnName,
                        Description = eventType.Description,
                        Icon = eventType.Icon,
                        SortOrder = eventType.SortOrder
                    };
                    _logger.LogInformation("✅ 成功加载 EventType: {EventTypeName} ({EventTypeId})", eventType.Name, eventType.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ 查询 EventType 失败: {EventTypeId}", eventTypeId);
            }
        }

        return response;
    }

    private ParticipantResponse MapToParticipantResponse(EventParticipant participant)
    {
        return new ParticipantResponse
        {
            Id = participant.Id,
            EventId = participant.EventId,
            UserId = participant.UserId,
            Status = participant.Status,
            RegisteredAt = participant.RegisteredAt
        };
    }

    private FollowerResponse MapToFollowerResponse(EventFollower follower)
    {
        return new FollowerResponse
        {
            Id = follower.Id,
            EventId = follower.EventId,
            UserId = follower.UserId,
            FollowedAt = follower.FollowedAt,
            NotificationEnabled = follower.NotificationEnabled
        };
    }

    #endregion

    #region 新增分页方法

    /// <summary>
    ///     获取用户已加入的活动列表(分页)
    /// </summary>
    public async Task<(List<EventResponse> Events, int Total)> GetJoinedEventsAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20)
    {
        // ✅ 优化方案:使用Repository层过滤,避免内存加载全部数据
        
        // 1. 只查询未取消的参与记录
        var participants = await _participantRepository.GetByUserIdWithStatusAsync(userId);
        var activeParticipants = participants
            .Where(p => p.Status != "cancelled")
            .ToList();

        if (!activeParticipants.Any())
        {
            return (new List<EventResponse>(), 0);
        }

        var eventIds = activeParticipants.Select(p => p.EventId).ToList();

        // 2. 使用批量查询,在数据库层过滤status=upcoming并分页
        var (events, total) = await _eventRepository.GetByIdsAsync(
            eventIds,
            status: "upcoming",
            page: page,
            pageSize: pageSize);

        // 3. 转换为 DTO
        var responses = await Task.WhenAll(events.Select(e => MapToResponseAsync(e)));
        var responsesList = responses.ToList();

        // 4. 批量获取关联数据
        await EnrichEventResponsesWithRelatedDataAsync(responsesList);

        // 5. 设置 IsParticipant 为 true(因为都是已加入的活动)
        foreach (var response in responsesList)
        {
            response.IsParticipant = true;
        }

        return (responsesList, total);
    }

    /// <summary>
    ///     获取用户取消参与的活动列表(分页)
    /// </summary>
    public async Task<(List<EventResponse> Events, int Total)> GetCancelledEventsByUserAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20)
    {
        // ✅ 优化方案:使用Repository层过滤,避免N+1查询
        
        // 1. 只查询已取消的参与记录
        var cancelledParticipants = await _participantRepository.GetByUserIdWithStatusAsync(userId, "cancelled");

        if (!cancelledParticipants.Any())
        {
            return (new List<EventResponse>(), 0);
        }

        var eventIds = cancelledParticipants.Select(p => p.EventId).ToList();

        // 2. 使用批量查询并分页
        var (events, total) = await _eventRepository.GetByIdsAsync(
            eventIds,
            status: null,  // 不过滤状态,显示所有已取消参与的活动
            page: page,
            pageSize: pageSize);

        // 3. 转换为 DTO
        var responses = await Task.WhenAll(events.Select(e => MapToResponseAsync(e)));
        var responsesList = responses.ToList();

        // 4. 批量获取关联数据
        await EnrichEventResponsesWithRelatedDataAsync(responsesList);

        return (responsesList, total);
    }

    #endregion
}