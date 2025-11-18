using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace GoNomads.Shared.Models;

[Table("roles")]
public class Role : BaseModel
{
    [PrimaryKey("id")] [Column("id")] public string Id { get; set; } = string.Empty;

    [Required] [Column("name")] public string Name { get; set; } = string.Empty;

    [Column("description")] public string? Description { get; set; }

    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Predefined role IDs as constants
    public static class RoleIds
    {
        public const string User = "role_user";
        public const string Admin = "role_admin";
    }

    // Predefined role names as constants
    public static class RoleNames
    {
        public const string User = "user";
        public const string Admin = "admin";
    }
}