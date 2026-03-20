using AIService.Application.DTOs;

namespace AIService.Application.Services;

public interface IOpenClawAutomationService
{
    Task<OpenClawAutomationResponse> ExecuteCommandAsync(OpenClawExecuteRequest request, CancellationToken cancellationToken = default);
    Task<OpenClawAutomationResponse> SetReminderAsync(OpenClawReminderRequest request, CancellationToken cancellationToken = default);
    Task<OpenClawAutomationResponse> SetVisaReminderAsync(OpenClawVisaReminderRequest request, CancellationToken cancellationToken = default);
    Task<OpenClawAutomationResponse> RunAutomationAsync(string scenario, OpenClawAutomationRequest request, CancellationToken cancellationToken = default);
    Task<OpenClawAutomationResponse> OrganizeInvoicesAsync(OpenClawInvoiceOrganizeRequest request, CancellationToken cancellationToken = default);
    Task<OpenClawAutomationResponse> CreateScriptAsync(OpenClawScriptRequest request, CancellationToken cancellationToken = default);
}
