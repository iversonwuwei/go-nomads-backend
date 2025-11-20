using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace CityService.Domain.Entities;

/// <summary>
///     城市 Pros & Cons 投票实体
/// </summary>
[Table("city_pros_cons_votes")]
public class CityProsConsVote : BaseModel
{
    [PrimaryKey("id")] 
    public Guid Id { get; set; }

    [Required] 
    [Column("pros_cons_id")] 
    public Guid ProsConsId { get; set; }

    [Required] 
    [Column("voter_user_id")] 
    public Guid VoterUserId { get; set; }

    [Required] 
    [Column("is_upvote")] 
    public bool IsUpvote { get; set; }

    [Column("created_at")] 
    public DateTime CreatedAt { get; set; }
}
