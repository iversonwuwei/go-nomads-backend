using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace CityService.Domain.Entities;

/// <summary>
///     用户上传的城市费用实体
/// </summary>
[Table("user_city_expenses")]
public class UserCityExpense : BaseModel
{
    [PrimaryKey("id")] public Guid Id { get; set; }

    [Required] [Column("user_id")] public Guid UserId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("city_id")]
    public string CityId { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("category")]
    public string Category { get; set; } = string.Empty;

    [Required] [Column("amount")] public decimal Amount { get; set; }

    [Required]
    [MaxLength(10)]
    [Column("currency")]
    public string Currency { get; set; } = "USD";

    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    [Column("date")] public DateTime Date { get; set; }

    [Column("created_at")] public DateTime CreatedAt { get; set; }

    [Column("updated_at")] public DateTime? UpdatedAt { get; set; }
}