using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace GoNomads.Shared.Models;

[Table("users")]
public class User : BaseModel
{
    private string _id = Guid.NewGuid().ToString();
    
    [PrimaryKey("id", true)]  // true = 客户端生成主键,false = 数据库生成
    public string Id 
    { 
        get => _id; 
        set => _id = string.IsNullOrWhiteSpace(value) ? Guid.NewGuid().ToString() : value; 
    }
    
    [Required]
    [StringLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [Column("email")]
    public string Email { get; set; } = string.Empty;
    
    [Phone]
    [Column("phone")]
    public string Phone { get; set; } = string.Empty;
    
    /// <summary>
    /// 密码哈希 (使用 BCrypt 或其他哈希算法存储)
    /// </summary>
    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户角色 (如: user, admin)
    /// </summary>
    [Column("role")]
    public string Role { get; set; } = "user";
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}