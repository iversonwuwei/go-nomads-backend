using System.ComponentModel.DataAnnotations;
using AIService.Models;
using Postgrest.Attributes;

namespace AIService.Domain.Entities;

[Table("community_answer_votes")]
public class CommunityAnswerVote : BaseAIModel
{
    [Required]
    [Column("answer_id")]
    public Guid AnswerId { get; set; }

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }
}