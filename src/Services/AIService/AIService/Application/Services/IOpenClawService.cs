namespace AIService.Application.Services;

/// <summary>
///     OpenClaw 自动化服务接口
/// </summary>
public interface IOpenClawService
{
    /// <summary>
    ///     发送用户指令到 OpenClaw 执行
    /// </summary>
    Task<string> ExecuteCommandAsync(string userCommand, string? sessionId = null);

    /// <summary>
    ///     设置定时提醒任务
    /// </summary>
    Task<string> SetReminderAsync(string reminderText, DateTime triggerTime);

    /// <summary>
    ///     设置签证到期提醒（自动多次提醒）
    /// </summary>
    Task<string> SetVisaReminderAsync(string country, DateTime expiryDate);

    /// <summary>
    ///     执行预设自动化场景
    /// </summary>
    Task<string> RunAutomationAsync(string scenarioName, Dictionary<string, string> parameters);
}
