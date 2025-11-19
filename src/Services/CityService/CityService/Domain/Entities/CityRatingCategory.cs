using Postgrest.Attributes;
using Postgrest.Models;

namespace CityService.Domain.Entities;

/// <summary>
/// 城市评分项实体
/// </summary>
[Table("city_rating_categories")]
public class CityRatingCategory : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("name_en")]
    public string? NameEn { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("icon")]
    public string? Icon { get; set; }

    [Column("is_default")]
    public bool IsDefault { get; set; }

    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("display_order")]
    public int DisplayOrder { get; set; }

    public static CityRatingCategory Create(
        string name,
        string? nameEn,
        string? description,
        string? icon,
        Guid? createdBy,
        int displayOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("评分项名称不能为空", nameof(name));

        return new CityRatingCategory
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            NameEn = nameEn?.Trim(),
            Description = description?.Trim(),
            Icon = icon?.Trim(),
            CreatedBy = createdBy,
            DisplayOrder = displayOrder,
            IsDefault = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string? nameEn, string? description, string? icon)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("评分项名称不能为空", nameof(name));

        Name = name.Trim();
        NameEn = nameEn?.Trim();
        Description = description?.Trim();
        Icon = icon?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
