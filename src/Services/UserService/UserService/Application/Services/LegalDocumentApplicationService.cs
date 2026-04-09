using System.Text.Json;
using UserService.Application.DTOs;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Application.Services;

public class LegalDocumentApplicationService : ILegalDocumentService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ILegalDocumentRepository _legalDocumentRepository;
    private readonly ILogger<LegalDocumentApplicationService> _logger;

    public LegalDocumentApplicationService(
        ILegalDocumentRepository legalDocumentRepository,
        ILogger<LegalDocumentApplicationService> logger)
    {
        _legalDocumentRepository = legalDocumentRepository;
        _logger = logger;
    }

    public async Task<(List<AdminLegalDocumentListItemDto> Items, int TotalCount)> GetAdminDocumentsAsync(
        int page = 1,
        int pageSize = 20,
        string? documentType = null,
        string? language = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var allDocuments = await _legalDocumentRepository.GetAllAsync(documentType, language, cancellationToken);

        var query = allDocuments.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToLowerInvariant();
            query = query.Where(document =>
                document.Title.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                document.DocumentType.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                document.Version.Contains(normalized, StringComparison.OrdinalIgnoreCase));
        }

        var filtered = query
            .OrderByDescending(document => document.IsCurrent)
            .ThenByDescending(document => document.EffectiveDate)
            .ThenByDescending(document => document.UpdatedAt)
            .ToList();

        var totalCount = filtered.Count;
        var items = filtered
            .Skip((Math.Max(page, 1) - 1) * Math.Max(pageSize, 1))
            .Take(Math.Max(pageSize, 1))
            .Select(MapToListItemDto)
            .ToList();

        return (items, totalCount);
    }

    public async Task<AdminLegalDocumentDto?> GetAdminDocumentByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var document = await _legalDocumentRepository.GetByIdAsync(id, cancellationToken);
        return document == null ? null : MapToDetailDto(document);
    }

    public async Task<AdminLegalDocumentDto> CreateAdminDocumentAsync(
        CreateAdminLegalDocumentRequest request,
        Guid operatorUserId,
        CancellationToken cancellationToken = default)
    {
        await ValidateVersionUniquenessAsync(
            request.DocumentType,
            request.Language,
            request.Version,
            null,
            cancellationToken);

        if (request.IsCurrent)
        {
            await _legalDocumentRepository.UnsetCurrentAsync(
                request.DocumentType,
                request.Language,
                null,
                cancellationToken);
        }

        var document = new LegalDocument
        {
            DocumentType = request.DocumentType.Trim(),
            Version = request.Version.Trim(),
            Language = request.Language.Trim(),
            Title = request.Title.Trim(),
            EffectiveDate = request.EffectiveDate,
            IsCurrent = request.IsCurrent,
            Sections = Serialize(request.Sections),
            Summary = Serialize(request.Summary),
            SdkList = Serialize(request.SdkList),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _legalDocumentRepository.CreateAsync(document, cancellationToken);
        _logger.LogInformation(
            "📄 创建法律文档成功: {DocumentType} {Language} v{Version} by {OperatorUserId}",
            created.DocumentType,
            created.Language,
            created.Version,
            operatorUserId);

        return MapToDetailDto(created);
    }

    public async Task<AdminLegalDocumentDto?> UpdateAdminDocumentAsync(
        string id,
        UpdateAdminLegalDocumentRequest request,
        Guid operatorUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = await _legalDocumentRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null)
            return null;

        await ValidateVersionUniquenessAsync(
            request.DocumentType,
            request.Language,
            request.Version,
            id,
            cancellationToken);

        if (request.IsCurrent)
        {
            await _legalDocumentRepository.UnsetCurrentAsync(
                request.DocumentType,
                request.Language,
                id,
                cancellationToken);
        }

        existing.DocumentType = request.DocumentType.Trim();
        existing.Version = request.Version.Trim();
        existing.Language = request.Language.Trim();
        existing.Title = request.Title.Trim();
        existing.EffectiveDate = request.EffectiveDate;
        existing.IsCurrent = request.IsCurrent;
        existing.Sections = Serialize(request.Sections);
        existing.Summary = Serialize(request.Summary);
        existing.SdkList = Serialize(request.SdkList);
        existing.UpdatedAt = DateTime.UtcNow;

        var updated = await _legalDocumentRepository.UpdateAsync(existing, cancellationToken);
        if (updated == null)
            return null;

        _logger.LogInformation(
            "📝 更新法律文档成功: {DocumentId} by {OperatorUserId}",
            id,
            operatorUserId);

        return MapToDetailDto(updated);
    }

    public async Task<bool> DeleteAdminDocumentAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var deleted = await _legalDocumentRepository.DeleteAsync(id, cancellationToken);
        if (deleted)
        {
            _logger.LogInformation("🗑️ 删除法律文档成功: {DocumentId}", id);
        }

        return deleted;
    }

    private async Task ValidateVersionUniquenessAsync(
        string documentType,
        string language,
        string version,
        string? existingId,
        CancellationToken cancellationToken)
    {
        var documents = await _legalDocumentRepository.ListByTypeAsync(documentType.Trim(), language.Trim(), cancellationToken);
        var duplicated = documents.Any(document =>
            document.Version.Equals(version.Trim(), StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(document.Id, existingId, StringComparison.OrdinalIgnoreCase));

        if (duplicated)
        {
            throw new InvalidOperationException("同类型、同语言、同版本的法律文档已存在");
        }
    }

    private static AdminLegalDocumentListItemDto MapToListItemDto(LegalDocument document)
    {
        return new AdminLegalDocumentListItemDto
        {
            Id = document.Id,
            DocumentType = document.DocumentType,
            Slug = document.DocumentType,
            Version = document.Version,
            Language = document.Language,
            Title = document.Title,
            EffectiveDate = document.EffectiveDate,
            IsCurrent = document.IsCurrent,
            Status = ResolveStatus(document),
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt
        };
    }

    private static AdminLegalDocumentDto MapToDetailDto(LegalDocument document)
    {
        return new AdminLegalDocumentDto
        {
            Id = document.Id,
            DocumentType = document.DocumentType,
            Slug = document.DocumentType,
            Version = document.Version,
            Language = document.Language,
            Title = document.Title,
            EffectiveDate = document.EffectiveDate,
            IsCurrent = document.IsCurrent,
            Status = ResolveStatus(document),
            Sections = Deserialize<List<LegalSectionDto>>(document.Sections) ?? new List<LegalSectionDto>(),
            Summary = Deserialize<List<LegalSummaryDto>>(document.Summary) ?? new List<LegalSummaryDto>(),
            SdkList = Deserialize<List<SdkInfoDto>>(document.SdkList) ?? new List<SdkInfoDto>(),
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt
        };
    }

    private static string ResolveStatus(LegalDocument document)
    {
        if (document.IsCurrent)
            return "published";

        return document.EffectiveDate > DateTime.UtcNow ? "scheduled" : "archived";
    }

    private static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, JsonOptions);
    }

    private static T? Deserialize<T>(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(value, JsonOptions);
        }
        catch
        {
            return default;
        }
    }
}