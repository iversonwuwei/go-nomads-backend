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
}
