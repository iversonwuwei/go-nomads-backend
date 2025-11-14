using Postgrest.Attributes;
using Postgrest.Models;
using ColumnAttribute = Postgrest.Attributes.ColumnAttribute;
using TableAttribute = Postgrest.Attributes.TableAttribute;

namespace CityService.Domain.Entities;

/// <summary>
/// 城市版主关联实体 - 支持多对多关系
/// 一个城市可以有多个版主，一个用户可以是多个城市的版主
/// </summary>
[Table("city_moderators")]
public class CityModerator : BaseModel
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [PrimaryKey("id")]
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// 城市ID
    /// </summary>
    [Column("city_id")]
    public Guid CityId { get; set; }

    /// <summary>
    /// 版主用户ID
    /// </summary>
    [Column("user_id")]
    public Guid UserId { get; set; }

    #region 权限控制

    /// <summary>
    /// 是否可以编辑城市基本信息
    /// </summary>
    [Column("can_edit_city")]
    public bool CanEditCity { get; set; } = true;

    /// <summary>
    /// 是否可以管理联合办公空间
    /// </summary>
    [Column("can_manage_coworks")]
    public bool CanManageCoworks { get; set; } = true;

    /// <summary>
    /// 是否可以管理生活成本
    /// </summary>
    [Column("can_manage_costs")]
    public bool CanManageCosts { get; set; } = true;

    /// <summary>
    /// 是否可以管理签证信息
    /// </summary>
    [Column("can_manage_visas")]
    public bool CanManageVisas { get; set; } = true;

    /// <summary>
    /// 是否可以管理城市聊天室
    /// </summary>
    [Column("can_moderate_chats")]
    public bool CanModerateChats { get; set; } = true;

    #endregion

    #region 指定信息

    /// <summary>
    /// 指定该版主的管理员用户ID
    /// </summary>
    [Column("assigned_by")]
    public Guid? AssignedBy { get; set; }

    /// <summary>
    /// 指定时间
    /// </summary>
    [Column("assigned_at")]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    #endregion

    #region 状态

    /// <summary>
    /// 版主是否激活
    /// </summary>
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 备注信息
    /// </summary>
    [Column("notes")]
    public string? Notes { get; set; }

    #endregion

    #region 时间戳

    /// <summary>
    /// 创建时间
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    #endregion
}
