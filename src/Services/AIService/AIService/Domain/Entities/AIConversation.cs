using System.ComponentModel.DataAnnotations;
using AIService.Models;
using Postgrest.Attributes;

namespace AIService.Domain.Entities;

/// <summary>
///     AI 对话会话聚合根
/// </summary>
[Table("ai_conversations")]
public class AIConversation : BaseAIModel
{
    [Required]
    [MaxLength(200)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Required] [Column("user_id")] public Guid UserId { get; set; }

    [MaxLength(50)]
    [Column("status")]
    public string Status { get; set; } = "active"; // active, archived, deleted

    [MaxLength(100)]
    [Column("model_name")]
    public string ModelName { get; set; } = "qwen-plus";

    [Column("system_prompt")] public string? SystemPrompt { get; set; }

    [Column("total_messages")] public int TotalMessages { get; set; }

    [Column("total_tokens")] public int TotalTokens { get; set; }

    [Column("last_message_at")] public DateTime? LastMessageAt { get; set; }

    // 领域行为方法

    /// <summary>
    ///     工厂方法 - 创建新对话
    /// </summary>
    public static AIConversation Create(Guid userId, string title, string? systemPrompt = null,
        string modelName = "qwen-plus")
    {
        // 业务规则验证
        if (userId == Guid.Empty)
            throw new ArgumentException("用户ID不能为空", nameof(userId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("对话标题不能为空", nameof(title));

        if (title.Length > 200)
            throw new ArgumentException("对话标题不能超过200个字符", nameof(title));

        return new AIConversation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title.Trim(),
            SystemPrompt = systemPrompt?.Trim(),
            ModelName = modelName,
            Status = "active",
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    ///     更新对话标题
    /// </summary>
    public void UpdateTitle(string newTitle)
    {
        if (string.IsNullOrWhiteSpace(newTitle))
            throw new ArgumentException("对话标题不能为空", nameof(newTitle));

        if (newTitle.Length > 200)
            throw new ArgumentException("对话标题不能超过200个字符", nameof(newTitle));

        Title = newTitle.Trim();
        Touch();
    }

    /// <summary>
    ///     添加消息时更新统计
    /// </summary>
    public void AddMessage(int tokenCount)
    {
        TotalMessages++;
        TotalTokens += tokenCount;
        LastMessageAt = DateTime.UtcNow;
        Touch();
    }

    /// <summary>
    ///     归档对话
    /// </summary>
    public void Archive()
    {
        if (Status == "deleted")
            throw new InvalidOperationException("已删除的对话不能归档");

        Status = "archived";
        Touch();
    }

    /// <summary>
    ///     激活对话
    /// </summary>
    public void Activate()
    {
        if (Status == "deleted")
            throw new InvalidOperationException("已删除的对话不能激活");

        Status = "active";
        Touch();
    }

    /// <summary>
    ///     检查是否可以添加消息
    /// </summary>
    public bool CanAddMessage()
    {
        return Status == "active" && !IsDeleted;
    }

    /// <summary>
    ///     软删除重写
    /// </summary>
    public override void Delete()
    {
        Status = "deleted";
        base.Delete();
    }

    // 无参构造函数 (ORM 需要)
}