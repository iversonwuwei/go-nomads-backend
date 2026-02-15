using UserService.Domain.Entities;
using UserService.Domain.Repositories;
using Client = Supabase.Client;

namespace UserService.Infrastructure.Repositories;

/// <summary>
///     法律文档仓储 Supabase 实现
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

    public async Task<LegalDocument?> GetCurrentAsync(string documentType, string language, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 获取当前生效法律文档: type={Type}, lang={Lang}", documentType, language);

        try
        {
            var response = await _supabaseClient
                .From<LegalDocument>()
                .Where(d => d.DocumentType == documentType)
                .Where(d => d.Language == language)
                .Where(d => d.IsCurrent == true)
                .Get(cancellationToken);

            var result = response.Models.FirstOrDefault();
            _logger.LogInformation("📄 查询结果: 共 {Count} 条, result={HasResult}", response.Models.Count, result != null);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 查询法律文档异常: type={Type}, lang={Lang}", documentType, language);
            return null;
        }
    }

    public async Task<LegalDocument?> GetByVersionAsync(string documentType, string language, string version, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 获取指定版本法律文档: type={Type}, lang={Lang}, ver={Ver}", documentType, language, version);

        try
        {
            var response = await _supabaseClient
                .From<LegalDocument>()
                .Where(d => d.DocumentType == documentType)
                .Where(d => d.Language == language)
                .Where(d => d.Version == version)
                .Get(cancellationToken);

            var result = response.Models.FirstOrDefault();
            _logger.LogInformation("📄 查询结果: 共 {Count} 条, result={HasResult}", response.Models.Count, result != null);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 查询法律文档异常: type={Type}, lang={Lang}, ver={Ver}", documentType, language, version);
            return null;
        }
    }
}
