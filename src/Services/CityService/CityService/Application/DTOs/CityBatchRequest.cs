using System.ComponentModel.DataAnnotations;

namespace CityService.Application.DTOs;

/// <summary>
/// 请求批量查询城市信息的 DTO。
/// </summary>
public class CityBatchRequest
{
    [Required]
    public List<Guid> CityIds { get; set; } = new();
}
