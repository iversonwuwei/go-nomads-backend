using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace CityService.Domain.Entities;

/// <summary>
///     GeoNames 城市数据实体 - 存储从 GeoNames.org 导入的完整城市信息
/// </summary>
[Table("geonames_cities")]
public class GeoNamesCity : BaseModel
{
    [PrimaryKey("id")] [Column("id")] public Guid Id { get; set; }

    /// <summary>
    ///     GeoNames ID (唯一标识)
    /// </summary>
    [Column("geoname_id")]
    public long GeonameId { get; set; }

    /// <summary>
    ///     城市名称
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     ASCII 名称
    /// </summary>
    [MaxLength(200)]
    [Column("ascii_name")]
    public string? AsciiName { get; set; }

    /// <summary>
    ///     替代名称(JSON数组)
    /// </summary>
    [Column("alternate_names")]
    public List<string>? AlternateNames { get; set; }

    /// <summary>
    ///     纬度
    /// </summary>
    [Column("latitude")]
    public double? Latitude { get; set; }

    /// <summary>
    ///     经度
    /// </summary>
    [Column("longitude")]
    public double? Longitude { get; set; }

    /// <summary>
    ///     Feature Class (P=城市, A=行政区等)
    /// </summary>
    [MaxLength(10)]
    [Column("feature_class")]
    public string? FeatureClass { get; set; }

    /// <summary>
    ///     Feature Code (PPLA=一级行政区首府, PPLC=首都等)
    /// </summary>
    [MaxLength(10)]
    [Column("feature_code")]
    public string? FeatureCode { get; set; }

    /// <summary>
    ///     国家代码 (ISO 2字母)
    /// </summary>
    [Required]
    [MaxLength(2)]
    [Column("country_code")]
    public string CountryCode { get; set; } = string.Empty;

    /// <summary>
    ///     国家名称
    /// </summary>
    [MaxLength(200)]
    [Column("country_name")]
    public string? CountryName { get; set; }

    /// <summary>
    ///     一级行政区代码
    /// </summary>
    [MaxLength(20)]
    [Column("admin1_code")]
    public string? Admin1Code { get; set; }

    /// <summary>
    ///     一级行政区名称
    /// </summary>
    [MaxLength(200)]
    [Column("admin1_name")]
    public string? Admin1Name { get; set; }

    /// <summary>
    ///     二级行政区代码
    /// </summary>
    [MaxLength(80)]
    [Column("admin2_code")]
    public string? Admin2Code { get; set; }

    /// <summary>
    ///     二级行政区名称
    /// </summary>
    [MaxLength(200)]
    [Column("admin2_name")]
    public string? Admin2Name { get; set; }

    /// <summary>
    ///     三级行政区代码
    /// </summary>
    [MaxLength(20)]
    [Column("admin3_code")]
    public string? Admin3Code { get; set; }

    /// <summary>
    ///     四级行政区代码
    /// </summary>
    [MaxLength(20)]
    [Column("admin4_code")]
    public string? Admin4Code { get; set; }

    /// <summary>
    ///     人口数量
    /// </summary>
    [Column("population")]
    public long? Population { get; set; }

    /// <summary>
    ///     海拔高度(米)
    /// </summary>
    [Column("elevation")]
    public int? Elevation { get; set; }

    /// <summary>
    ///     数字高程模型 (SRTM3 或 GTOPO30)
    /// </summary>
    [Column("dem")]
    public int? Dem { get; set; }

    /// <summary>
    ///     时区ID
    /// </summary>
    [MaxLength(100)]
    [Column("timezone")]
    public string? Timezone { get; set; }

    /// <summary>
    ///     GeoNames 最后修改日期
    /// </summary>
    [Column("modification_date")]
    public DateTime? ModificationDate { get; set; }

    /// <summary>
    ///     是否已同步到 cities 表
    /// </summary>
    [Column("synced_to_cities")]
    public bool SyncedToCities { get; set; } = false;

    /// <summary>
    ///     对应的 cities 表 ID
    /// </summary>
    [Column("city_id")]
    public Guid? CityId { get; set; }

    /// <summary>
    ///     数据导入时间
    /// </summary>
    [Column("imported_at")]
    public DateTime ImportedAt { get; set; }

    /// <summary>
    ///     数据更新时间
    /// </summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    ///     备注信息
    /// </summary>
    [Column("notes")]
    public string? Notes { get; set; }
}