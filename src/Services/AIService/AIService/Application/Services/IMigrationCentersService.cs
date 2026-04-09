using AIService.Application.DTOs;

namespace AIService.Application.Services;

public interface IMigrationCentersService
{
    Task<MigrationWorkspaceResponse> GetMigrationWorkspaceAsync(Guid userId, int page = 1, int pageSize = 20);

    Task<MigrationWorkspaceResponse> SaveMigrationWorkspacePlanAsync(
        Guid userId,
        Guid planId,
        UpdateMigrationWorkspacePlanRequest request);

    Task<BudgetCenterResponse> GetBudgetCenterAsync(Guid userId);

    Task<BudgetCenterResponse> SaveBudgetPlanAsync(Guid userId, Guid planId, SaveBudgetPlanRequest request);

    Task<VisaCenterResponse> GetVisaCenterAsync(Guid userId);

    Task<VisaCenterResponse> SaveVisaProfileAsync(Guid userId, Guid planId, SaveVisaProfileRequest request);
}