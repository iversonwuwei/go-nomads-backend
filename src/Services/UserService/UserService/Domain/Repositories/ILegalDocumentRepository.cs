using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

/// <summary>
///     法律文档仓储接口
/// </summary>
public interface ILegalDocumentRepository
{
    /// <summary>
    ///     获取指定语言当前生效的法律文档列表
    /// </summary>
    Task<List<LegalDocument>> ListCurrentAsync(string language, CancellationToken cancellationToken = default);

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

    /// <summary>
    ///     查询后台法律文档列表
    /// </summary>
    Task<List<LegalDocument>> GetAllAsync(
        string? documentType = null,
        string? language = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     根据 ID 获取法律文档
    /// </summary>
    Task<LegalDocument?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     创建法律文档
    /// </summary>
    Task<LegalDocument> CreateAsync(LegalDocument document, CancellationToken cancellationToken = default);

    /// <summary>
    ///     更新法律文档
    /// </summary>
    Task<LegalDocument?> UpdateAsync(LegalDocument document, CancellationToken cancellationToken = default);

    /// <summary>
    ///     删除法律文档
    /// </summary>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     将同类型同语言的其它 current 版本下线
    /// </summary>
    Task UnsetCurrentAsync(
        string documentType,
        string language,
        string? exceptId = null,
        CancellationToken cancellationToken = default);
}
