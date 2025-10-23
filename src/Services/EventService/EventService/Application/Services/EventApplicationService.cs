using EventService.Application.DTOs;
using EventService.Domain.Entities;
using EventService.Domain.Repositories;

namespace EventService.Application.Services;

/// <summary>
/// Event 应用服务实现
/// </summary>
public class EventApplicationService : IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly IEventParticipantRepository _participantRepository;
    private readonly IEventFollowerRepository _followerRepository;
    private readonly ILogger<EventApplicationService> _logger;

    public EventApplicationService(
        IEventRepository eventRepository,
        IEventParticipantRepository participantRepository,
        IEventFollowerRepository followerRepository,
        ILogger<EventApplicationService> logger)
    {
        _eventRepository = eventRepository;
        _participantRepository = participantRepository;
        _followerRepository = followerRepository;
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
            price: request.Price,
            currency: request.Currency,
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

        // 如果提供了 userId，检查关注和参与状态
        if (userId.HasValue)
        {
            response.IsFollowing = await _followerRepository.IsFollowingAsync(id, userId.Value);
            response.IsParticipant = await _participantRepository.IsParticipantAsync(id, userId.Value);
        }

        response.FollowerCount = await _followerRepository.GetFollowerCountAsync(id);

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
            price: request.Price,
            currency: request.Currency,
            status: request.Status,
            locationType: request.LocationType,
            meetingLink: request.MeetingLink,
            latitude: request.Latitude,
            longitude: request.Longitude,
            tags: request.Tags?.ToArray());

        var updatedEvent = await _eventRepository.UpdateAsync(@event);

        return MapToResponse(updatedEvent);
    }

    public async Task<(List<EventResponse> Events, int Total)> GetEventsAsync(
        Guid? cityId = null,
        string? category = null,
        string? status = null,
        int page = 1,
        int pageSize = 20)
    {
        var (events, total) = await _eventRepository.GetListAsync(cityId, category, status, page, pageSize);

        var responses = events.Select(MapToResponse).ToList();

        return (responses, total);
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
        var participant = EventParticipant.Create(eventId, userId, request.PaymentStatus);
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
            Price = @event.Price,
            Currency = @event.Currency,
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
            PaymentStatus = participant.PaymentStatus,
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
