using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace CityService.Models;

/// <summary>
/// 国家表
/// </summary>
[Table("countries")]
public class Country : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("name_zh")]
    public string NameZh { get; set; } = string.Empty;

    /// <summary>
    /// ISO 3166-1 alpha-2 (CN, US, JP, etc.)
    /// </summary>
    [Required]
    [MaxLength(2)]
    [Column("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// ISO 3166-1 alpha-3 (CHN, USA, JPN, etc.)
    /// </summary>
    [MaxLength(3)]
    [Column("code_alpha3")]
    public string? CodeAlpha3 { get; set; }

    [MaxLength(50)]
    [Column("continent")]
    public string? Continent { get; set; }

    [MaxLength(500)]
    [Column("flag_url")]
    public string? FlagUrl { get; set; }

    [Column("calling_code")]
    public string? CallingCode { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
