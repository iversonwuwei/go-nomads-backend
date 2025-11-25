namespace EventService.Application.DTOs;

/// <summary>
///     聚会类型响应 DTO
/// </summary>
public class EventTypeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EnName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystem { get; set; }
}

/// <summary>
///     创建聚会类型请求 DTO
/// </summary>
public class CreateEventTypeRequest
{
    public string Name { get; set; } = string.Empty;
    public string EnName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public int SortOrder { get; set; } = 0;
}

/// <summary>
///     更新聚会类型请求 DTO
/// </summary>
public class UpdateEventTypeRequest
{
    public string? Name { get; set; }
    public string? EnName { get; set; }
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
}
