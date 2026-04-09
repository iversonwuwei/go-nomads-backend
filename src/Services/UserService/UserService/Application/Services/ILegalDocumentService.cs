using UserService.Application.DTOs;

namespace UserService.Application.Services;

public interface ILegalDocumentService
{
    Task<(List<AdminLegalDocumentListItemDto> Items, int TotalCount)> GetAdminDocumentsAsync(
        int page = 1,
        int pageSize = 20,
        string? documentType = null,
        string? language = null,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<AdminLegalDocumentDto?> GetAdminDocumentByIdAsync(
        string id,
        CancellationToken cancellationToken = default);

    Task<AdminLegalDocumentDto> CreateAdminDocumentAsync(
        CreateAdminLegalDocumentRequest request,
        Guid operatorUserId,
        CancellationToken cancellationToken = default);

    Task<AdminLegalDocumentDto?> UpdateAdminDocumentAsync(
        string id,
        UpdateAdminLegalDocumentRequest request,
        Guid operatorUserId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAdminDocumentAsync(
        string id,
        CancellationToken cancellationToken = default);
}