using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

/// <summary>
///     法律文档仓储接口
/// </summary>
public interface ILegalDocumentRepository
{
    /// <summary>
    ///     获取当前生效的法律文档（按类型 + 语言）
    /// </summary>
    Task<LegalDocument?> GetCurrentAsync(string documentType, string language,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     查询指定类型和语言的全部法律文档版本（按生效时间倒序）
    /// </summary>
    Task<List<LegalDocument>> ListByTypeAsync(string documentType, string language,
        CancellationToken cancellationToken = default);
}
