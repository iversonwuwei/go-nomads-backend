using System.ComponentModel.DataAnnotations;
using AIService.Models;
using Postgrest.Attributes;

namespace AIService.Domain.Entities;

[Table("community_questions")]
public class CommunityQuestion : BaseAIModel
{
    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("city")]
    public string City { get; set; } = string.Empty;

    [Required]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("tags_json")]
    public string TagsJson { get; set; } = "[]";

    [Column("upvotes")]
    public int Upvotes { get; set; }

    [Column("answer_count")]
    public int AnswerCount { get; set; }

    [Column("accepted_answer_id")]
    public Guid? AcceptedAnswerId { get; set; }
}