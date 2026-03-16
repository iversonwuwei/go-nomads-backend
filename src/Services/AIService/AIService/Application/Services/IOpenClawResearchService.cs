using AIService.Application.DTOs;

namespace AIService.Application.Services;

public interface IOpenClawResearchService
{
    bool IsConfigured { get; }
    Task<OpenClawResearchResponse?> ResearchTravelPlanAsync(OpenClawResearchRequest request, CancellationToken cancellationToken = default);
}