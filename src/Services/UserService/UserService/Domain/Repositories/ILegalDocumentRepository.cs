using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

/// <summary>
///     法律文档仓储接口 — 定义在领域层
/// </summary>
public interface ILegalDocumentRepository
{
    /// <summary>
    ///     获取当前生效的法律文档（按类型+语言）
    /// </summary>
    Task<LegalDocument?> GetCurrentAsync(string documentType, string language, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取指定版本的法律文档
    /// </summary>
    Task<LegalDocument?> GetByVersionAsync(string documentType, string language, string version, CancellationToken cancellationToken = default);
}
