using UserService.Domain.Entities;
using UserService.Domain.Repositories;
using Client = Supabase.Client;

namespace UserService.Infrastructure.Repositories;

/// <summary>
///     法律文档仓储 — Supabase 实现
/// </summary>
public class LegalDocumentRepository : ILegalDocumentRepository
{
    private readonly ILogger<LegalDocumentRepository> _logger;
    private readonly Client _supabaseClient;

    public LegalDocumentRepository(Client supabaseClient, ILogger<LegalDocumentRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<List<LegalDocument>> ListCurrentAsync(string language,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("📚 查询当前法律文档列表: lang={Lang}", language);

            var response = await _supabaseClient
                .From<LegalDocument>()
                .Where(d => d.Language == language)
                .Where(d => d.IsCurrent == true)
                .Order(d => d.EffectiveDate, Postgrest.Constants.Ordering.Descending)
                .Get(cancellationToken: cancellationToken);

            return response.Models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 查询当前法律文档列表失败: lang={Lang}", language);
            return new List<LegalDocument>();
        }
    }

    public async Task<LegalDocument?> GetCurrentAsync(string documentType, string language,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("📄 查询法律文档: type={Type}, lang={Lang}", documentType, language);

            var response = await _supabaseClient
                .From<LegalDocument>()
                .Where(d => d.DocumentType == documentType)
                .Where(d => d.Language == language)
                .Where(d => d.IsCurrent == true)
                .Order(d => d.EffectiveDate, Postgrest.Constants.Ordering.Descending)
                .Single(cancellationToken);

            if (response != null)
            {
                _logger.LogInformation("✅ 找到法律文档: {Title} v{Version}", response.Title, response.Version);
            }
            else
            {
                _logger.LogWarning("⚠️ 未找到法律文档: type={Type}, lang={Lang}", documentType, language);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 查询法律文档失败: type={Type}, lang={Lang}", documentType, language);
            return null;
        }
    }

    public async Task<List<LegalDocument>> ListByTypeAsync(string documentType, string language,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("📚 查询法律文档历史版本: type={Type}, lang={Lang}", documentType, language);

            var response = await _supabaseClient
                .From<LegalDocument>()
                .Where(d => d.DocumentType == documentType)
                .Where(d => d.Language == language)
                .Order(d => d.EffectiveDate, Postgrest.Constants.Ordering.Descending)
                .Get(cancellationToken: cancellationToken);

            return response.Models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 查询法律文档历史版本失败: type={Type}, lang={Lang}", documentType, language);
            return new List<LegalDocument>();
        }
    }

    public async Task<List<LegalDocument>> GetAllAsync(
        string? documentType = null,
        string? language = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = (Supabase.Interfaces.ISupabaseTable<LegalDocument, Supabase.Realtime.RealtimeChannel>)_supabaseClient
                .From<LegalDocument>();

            if (!string.IsNullOrWhiteSpace(documentType))
                query = (Supabase.Interfaces.ISupabaseTable<LegalDocument, Supabase.Realtime.RealtimeChannel>)query
                    .Where(document => document.DocumentType == documentType);

            if (!string.IsNullOrWhiteSpace(language))
                query = (Supabase.Interfaces.ISupabaseTable<LegalDocument, Supabase.Realtime.RealtimeChannel>)query
                    .Where(document => document.Language == language);

            var response = await query
                .Order(document => document.EffectiveDate, Postgrest.Constants.Ordering.Descending)
                .Get(cancellationToken: cancellationToken);

            return response.Models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 查询法律文档后台列表失败");
            return new List<LegalDocument>();
        }
    }

    public async Task<LegalDocument?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _supabaseClient
                .From<LegalDocument>()
                .Where(document => document.Id == id)
                .Single(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 未找到法律文档: {Id}", id);
            return null;
        }
    }

    public async Task<LegalDocument> CreateAsync(LegalDocument document, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<LegalDocument>()
            .Insert(document, cancellationToken: cancellationToken);

        var created = response.Models.FirstOrDefault();
        if (created == null)
            throw new InvalidOperationException("创建法律文档失败");

        return created;
    }

    public async Task<LegalDocument?> UpdateAsync(LegalDocument document, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<LegalDocument>()
            .Where(existing => existing.Id == document.Id)
            .Update(document, cancellationToken: cancellationToken);

        return response.Models.FirstOrDefault();
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _supabaseClient
                .From<LegalDocument>()
                .Where(document => document.Id == id)
                .Delete(cancellationToken: cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除法律文档失败: {Id}", id);
            return false;
        }
    }

    public async Task UnsetCurrentAsync(
        string documentType,
        string language,
        string? exceptId = null,
        CancellationToken cancellationToken = default)
    {
        var currentDocuments = await _supabaseClient
            .From<LegalDocument>()
            .Where(document => document.DocumentType == documentType)
            .Where(document => document.Language == language)
            .Where(document => document.IsCurrent == true)
            .Get(cancellationToken: cancellationToken);

        foreach (var current in currentDocuments.Models.Where(document => document.Id != exceptId))
        {
            current.IsCurrent = false;
            current.UpdatedAt = DateTime.UtcNow;

            await _supabaseClient
                .From<LegalDocument>()
                .Where(document => document.Id == current.Id)
                .Update(current, cancellationToken: cancellationToken);
        }
    }
}
