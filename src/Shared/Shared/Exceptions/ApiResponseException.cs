namespace GoNomads.Shared.Exceptions;

/// <summary>
///     表示下游服务返回的标准化 ApiResponse 失败时抛出的异常。
/// </summary>
public class ApiResponseException : Exception
{
    public ApiResponseException(
        string message,
        IEnumerable<string>? errors = null,
        string? source = null) : base(message)
    {
        SourceService = source;
        Errors = errors?.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()).ToList() ?? new List<string>();
    }

    /// <summary>
    ///     产生该异常的下游服务标识。
    /// </summary>
    public string? SourceService { get; }

    /// <summary>
    ///     下游返回的错误明细（如果有）。
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    public override string ToString()
    {
        var baseMessage = base.ToString();
        if (Errors.Count == 0) return baseMessage;

        return $"{baseMessage}{Environment.NewLine}Errors: {string.Join(", ", Errors)}";
    }
}