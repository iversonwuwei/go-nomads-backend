using System.ComponentModel.DataAnnotations;
using AIService.Models;
using Postgrest.Attributes;

namespace AIService.Domain.Entities;

[Table("community_answers")]
public class CommunityAnswer : BaseAIModel
{
    [Required]
    [Column("question_id")]
    public Guid QuestionId { get; set; }

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("upvotes")]
    public int Upvotes { get; set; }

    [Column("is_accepted")]
    public bool IsAccepted { get; set; }
}