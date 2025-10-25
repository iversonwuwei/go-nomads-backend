using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace CityService.Domain.Entities;

/// <summary>
/// 省份/州表
/// </summary>
[Table("provinces")]
public class Province : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Column("country_id")]
    public Guid CountryId { get; set; }

    /// <summary>
    /// 省份代码（如：11=北京, 31=上海）
    /// </summary>
    [MaxLength(10)]
    [Column("code")]
    public string? Code { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Reference(typeof(Country), ReferenceAttribute.JoinType.Inner, false)]
    public Country? Country { get; set; }
}
