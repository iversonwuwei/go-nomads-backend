using System.ComponentModel.DataAnnotations;
using AIService.Models;
using Postgrest.Attributes;

namespace AIService.Domain.Entities;

[Table("community_question_votes")]
public class CommunityQuestionVote : BaseAIModel
{
    [Required]
    [Column("question_id")]
    public Guid QuestionId { get; set; }

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }
}