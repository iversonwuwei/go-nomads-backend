namespace AIService.Application.DTOs;

/// <summary>
///     Community 首屏聚合快照
/// </summary>
public class CommunitySnapshotResponse
{
    public string FocusCity { get; set; } = string.Empty;
    public string? NextCoordinationCity { get; set; }
    public List<CommunityMeetupResponse> UpcomingMeetups { get; set; } = new();
    public List<CommunityFieldNoteResponse> FieldNotes { get; set; } = new();
    public List<CommunityQuestionResponse> Questions { get; set; } = new();
    public List<CommunityRecommendationResponse> Recommendations { get; set; } = new();
    public DateTime? LastUpdatedAt { get; set; }
}

/// <summary>
///     Community 首屏使用的 Meetup 摘要
/// </summary>
public class CommunityMeetupResponse
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CityId { get; set; } = string.Empty;
    public string CityName { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Venue { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int MaxParticipants { get; set; }
    public int ParticipantCount { get; set; }
    public string OrganizerId { get; set; } = string.Empty;
    public string OrganizerName { get; set; } = string.Empty;
    public string? OrganizerAvatar { get; set; }
    public bool IsJoined { get; set; }
    public bool IsParticipant { get; set; }
    public string Status { get; set; } = "upcoming";
    public DateTime CreatedAt { get; set; }
}

/// <summary>
///     Community 首屏使用的 Field Note 摘要
/// </summary>
public class CommunityFieldNoteResponse
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public double OverallRating { get; set; }
    public Dictionary<string, double> Ratings { get; set; } = new();
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Photos { get; set; } = new();
    public List<string> Pros { get; set; } = new();
    public List<string> Cons { get; set; } = new();
    public int Likes { get; set; }
    public int Comments { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsLiked { get; set; }
}

/// <summary>
///     Community 首屏使用的问题线程摘要
/// </summary>
public class CommunityQuestionResponse
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public string City { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public int Upvotes { get; set; }
    public int AnswerCount { get; set; }
    public bool HasAcceptedAnswer { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsUpvoted { get; set; }
    public List<CommunityAnswerResponse> Answers { get; set; } = new();
}

/// <summary>
///     Community 首屏问题详情内的答案摘要
/// </summary>
public class CommunityAnswerResponse
{
    public string Id { get; set; } = string.Empty;
    public string QuestionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public string Content { get; set; } = string.Empty;
    public int Upvotes { get; set; }
    public bool IsAccepted { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsUpvoted { get; set; }
}

/// <summary>
///     Community 聚合口附带的推荐摘要
/// </summary>
public class CommunityRecommendationResponse
{
    public string Id { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public string? PriceRange { get; set; }
    public string? Address { get; set; }
    public List<string> Photos { get; set; } = new();
    public string? Website { get; set; }
    public List<string> Tags { get; set; } = new();
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
}