using System.ComponentModel.DataAnnotations;

namespace AIService.Application.DTOs;

/// <summary>
/// 创建对话请求
/// </summary>
public class CreateConversationRequest
{
    [Required(ErrorMessage = "对话标题不能为空")]
    [StringLength(200, ErrorMessage = "对话标题不能超过200个字符")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "系统提示不能超过1000个字符")]
    public string? SystemPrompt { get; set; }

    [StringLength(50, ErrorMessage = "模型名称不能超过50个字符")]
    public string ModelName { get; set; } = "qwen-plus";
}

/// <summary>
/// 发送消息请求
/// </summary>
public class SendMessageRequest
{
    [Required(ErrorMessage = "消息内容不能为空")]
    [StringLength(50000, ErrorMessage = "消息内容不能超过50000个字符")]
    public string Content { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "模型名称不能超过50个字符")]
    public string? ModelName { get; set; }

    /// <summary>
    /// 是否流式响应
    /// </summary>
    public bool Stream { get; set; } = false;

    /// <summary>
    /// 温度参数 (0.0-2.0)
    /// </summary>
    [Range(0.0, 2.0, ErrorMessage = "温度参数必须在0.0-2.0之间")]
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// 最大输出token数
    /// </summary>
    [Range(1, 8000, ErrorMessage = "最大输出token数必须在1-8000之间")]
    public int MaxTokens { get; set; } = 2000;
}

/// <summary>
/// 更新对话请求
/// </summary>
public class UpdateConversationRequest
{
    [StringLength(200, ErrorMessage = "对话标题不能超过200个字符")]
    public string? Title { get; set; }

    [StringLength(1000, ErrorMessage = "系统提示不能超过1000个字符")]
    public string? SystemPrompt { get; set; }
}

/// <summary>
/// 对话查询请求
/// </summary>
public class GetConversationsRequest
{
    public string? Status { get; set; } // active, archived, all

    [Range(1, 100, ErrorMessage = "页码必须在1-100之间")]
    public int Page { get; set; } = 1;

    [Range(1, 50, ErrorMessage = "每页数量必须在1-50之间")]
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// 消息查询请求
/// </summary>
public class GetMessagesRequest
{
    [Range(1, 100, ErrorMessage = "页码必须在1-100之间")]
    public int Page { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "每页数量必须在1-100之间")]
    public int PageSize { get; set; } = 50;

    public bool IncludeSystem { get; set; } = false;
}