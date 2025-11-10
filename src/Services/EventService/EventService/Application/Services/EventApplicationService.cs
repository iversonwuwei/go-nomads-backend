using EventService.Application.DTOs;
using EventService.Domain.Entities;
using EventService.Domain.Repositories;
using EventService.Infrastructure.GrpcClients;

namespace EventService.Application.Services;

/// <summary>
/// Event 应用服务实现
/// </summary>
public class EventApplicationService : IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly IEventParticipantRepository _participantRepository;
    private readonly IEventFollowerRepository _followerRepository;
    private readonly ICityGrpcClient _cityGrpcClient;
    private readonly IUserGrpcClient _userGrpcClient;
    private readonly ILogger<EventApplicationService> _logger;

    public EventApplicationService(
        IEventRepository eventRepository,
        IEventParticipantRepository participantRepository,
        IEventFollowerRepository followerRepository,
        ICityGrpcClient cityGrpcClient,
        IUserGrpcClient userGrpcClient,
        ILogger<EventApplicationService> logger)
    {
        _eventRepository = eventRepository;
        _participantRepository = participantRepository;
        _followerRepository = followerRepository;
        _cityGrpcClient = cityGrpcClient;
        _userGrpcClient = userGrpcClient;
        _logger = logger;
    }

    public async Task<EventResponse> CreateEventAsync(CreateEventRequest request, Guid organizerId)
    {
        _logger.LogInformation("📝 创建新 Event，Organizer: {OrganizerId}", organizerId);

        // 使用领域工厂方法创建实体
        var @event = Event.Create(
            title: request.Title,
            organizerId: organizerId,
            startTime: request.StartTime,
            description: request.Description,
            cityId: request.CityId,
            location: request.Location,
            address: request.Address,
            imageUrl: request.ImageUrl,
            images: request.Images?.ToArray(),
            category: request.Category,
            endTime: request.EndTime,
            maxParticipants: request.MaxParticipants,
            locationType: request.LocationType,
            meetingLink: request.MeetingLink,
            latitude: request.Latitude,
            longitude: request.Longitude,
            tags: request.Tags?.ToArray());

        // 持久化
        var createdEvent = await _eventRepository.CreateAsync(@event);

        return MapToResponse(createdEvent);
    }

    public async Task<EventResponse> GetEventAsync(Guid id, Guid? userId = null)
    {
        var @event = await _eventRepository.GetByIdAsync(id);
        if (@event == null)
        {
            throw new KeyNotFoundException($"Event {id} 不存在");
        }

        var response = MapToResponse(@event);

        // 如果提供了 userId，检查参与状态和组织者身份
        if (userId.HasValue)
        {
            // 暂时不使用 follower 功能,只检查参与状态
            response.IsFollowing = false;
            response.IsParticipant = await _participantRepository.IsParticipantAsync(id, userId.Value);
            
            // 判断当前用户是否是活动组织者
            response.IsOrganizer = response.OrganizerId == userId.Value;
            
            _logger.LogInformation("👥 用户 {UserId} 是否参与了活动 {EventId}: {IsParticipant}", userId.Value, id, response.IsParticipant);
            _logger.LogInformation("👥 用户 {UserId} 是否是活动 {EventId} 的组织者: {IsOrganizer}", userId.Value, id, response.IsOrganizer);
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
                {
                    if (users.TryGetValue(participant.UserId, out var userInfo))
                    {
                        participant.User = userInfo;
                    }
                }
                
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
        if (@event == null)
        {
            throw new KeyNotFoundException($"Event {id} 不存在");
        }

        // 使用领域方法更新（包含权限验证）
        @event.Update(
            userId: userId,
            title: request.Title,
            description: request.Description,
            cityId: request.CityId,
            location: request.Location,
            address: request.Address,
            imageUrl: request.ImageUrl,
            images: request.Images?.ToArray(),
            category: request.Category,
            startTime: request.StartTime,
            endTime: request.EndTime,
            maxParticipants: request.MaxParticipants,
            status: request.Status,
            locationType: request.LocationType,
            meetingLink: request.MeetingLink,
            latitude: request.Latitude,
            longitude: request.Longitude,
            tags: request.Tags?.ToArray());

        var updatedEvent = await _eventRepository.UpdateAsync(@event);

        return MapToResponse(updatedEvent);
    }

    /// <summary>
    /// 取消活动
    /// </summary>
    public async Task<EventResponse> CancelEventAsync(Guid id, Guid userId)
    {
        var @event = await _eventRepository.GetByIdAsync(id);
        if (@event == null)
        {
            throw new KeyNotFoundException($"Event {id} 不存在");
        }

        // 验证权限：只有组织者可以取消
        if (@event.OrganizerId != userId)
        {
            throw new UnauthorizedAccessException("只有组织者可以取消活动");
        }

        // 使用领域方法取消
        @event.Cancel(userId);

        var updatedEvent = await _eventRepository.UpdateAsync(@event);

        _logger.LogInformation("✅ 活动 {EventId} 已被用户 {UserId} 取消", id, userId);

        return MapToResponse(updatedEvent);
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

        // 转换为 DTO
        var responses = events.Select(MapToResponse).ToList();

        // 批量获取关联数据
        await EnrichEventResponsesWithRelatedDataAsync(responses);

        // 🔧 修正参与者数量:批量查询每个事件的实际参与者数量
        foreach (var response in responses)
        {
            var participantCount = await _participantRepository.CountByEventIdAsync(response.Id);
            response.CurrentParticipants = participantCount;
        }

        // 如果有用户ID,批量检查参与状态
        if (userId.HasValue)
        {
            await EnrichEventParticipationStatusAsync(responses, userId.Value);
        }

        return (responses, total);
    }

    /// <summary>
    /// 为事件列表填充关联数据（城市、组织者信息）
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
                {
                    response.City = cityInfo;
                }

                // 填充组织者信息
                if (users.TryGetValue(response.OrganizerId, out var organizerInfo))
                {
                    response.Organizer = organizerInfo;
                }
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
    /// 批量填充事件参与状态
    /// </summary>
    private async Task EnrichEventParticipationStatusAsync(List<EventResponse> responses, Guid userId)
    {
        _logger.LogInformation("👥 开始为 {Count} 个事件填充参与状态，用户ID: {UserId}", responses.Count, userId);

        if (!responses.Any())
        {
            return;
        }

        try
        {
            // 批量检查用户是否参与了这些活动和是否是组织者
            foreach (var response in responses)
            {
                response.IsParticipant = await _participantRepository.IsParticipantAsync(response.Id, userId);
                response.IsOrganizer = response.OrganizerId == userId;
                _logger.LogInformation("👥 用户 {UserId} 是否参与了活动 {EventId}: {IsParticipant}", userId, response.Id, response.IsParticipant);
                _logger.LogInformation("👥 用户 {UserId} 是否是活动 {EventId} 的组织者: {IsOrganizer}", userId, response.Id, response.IsOrganizer);
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

    public async Task<ParticipantResponse> JoinEventAsync(Guid eventId, Guid userId, JoinEventRequest request)
    {
        _logger.LogInformation("👥 用户 {UserId} 申请参加 Event {EventId}", userId, eventId);

        // 检查 Event 是否存在
        var @event = await _eventRepository.GetByIdAsync(eventId);
        if (@event == null)
        {
            throw new KeyNotFoundException($"Event {eventId} 不存在");
        }

        // 检查是否已参加
        if (await _participantRepository.IsParticipantAsync(eventId, userId))
        {
            throw new InvalidOperationException("您已经参加了这个 Event");
        }

        // 检查是否可以参加（领域逻辑）
        if (!@event.CanJoin())
        {
            throw new InvalidOperationException("Event 已满员或状态不允许参加");
        }

        // 创建参与记录
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
        if (participant == null)
        {
            throw new KeyNotFoundException("您未参加此 Event");
        }

        // 删除参与记录
        await _participantRepository.DeleteAsync(participant.Id);

        // 更新参与人数
        var @event = await _eventRepository.GetByIdAsync(eventId);
        if (@event != null)
        {
            @event.RemoveParticipant();
            await _eventRepository.UpdateAsync(@event);
        }
    }

    public async Task<FollowerResponse> FollowEventAsync(Guid eventId, Guid userId, FollowEventRequest request)
    {
        _logger.LogInformation("⭐ 用户 {UserId} 关注 Event {EventId}", userId, eventId);

        // 检查 Event 是否存在
        if (!await _eventRepository.ExistsAsync(eventId))
        {
            throw new KeyNotFoundException($"Event {eventId} 不存在");
        }

        // 检查是否已关注
        if (await _followerRepository.IsFollowingAsync(eventId, userId))
        {
            throw new InvalidOperationException("您已经关注了这个 Event");
        }

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
        if (follower == null)
        {
            throw new KeyNotFoundException("您未关注此 Event");
        }

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
        return events.Select(MapToResponse).ToList();
    }

    public async Task<List<EventResponse>> GetUserJoinedEventsAsync(Guid userId)
    {
        var participants = await _participantRepository.GetByUserIdAsync(userId);
        var eventIds = participants.Select(p => p.EventId).ToList();

        var events = new List<Event>();
        foreach (var eventId in eventIds)
        {
            var @event = await _eventRepository.GetByIdAsync(eventId);
            if (@event != null)
            {
                events.Add(@event);
            }
        }

        return events.Select(MapToResponse).ToList();
    }

    public async Task<List<EventResponse>> GetUserFollowingEventsAsync(Guid userId)
    {
        var followers = await _followerRepository.GetByUserIdAsync(userId);
        var eventIds = followers.Select(f => f.EventId).ToList();

        var events = new List<Event>();
        foreach (var eventId in eventIds)
        {
            var @event = await _eventRepository.GetByIdAsync(eventId);
            if (@event != null)
            {
                events.Add(@event);
            }
        }

        return events.Select(MapToResponse).ToList();
    }

    #region Mapping Methods

    private EventResponse MapToResponse(Event @event)
    {
        return new EventResponse
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
}
