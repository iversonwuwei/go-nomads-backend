using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dapr.Client;
using GoNomads.Shared.Models;

namespace GoNomads.Shared.Extensions;

/// <summary>
/// 针对 DaprClient 的扩展，统一解包 ApiResponse 响应结构。
/// </summary>
public static class DaprClientExtensions
{
    /// <summary>
    /// 调用下游服务并返回解包后的 ApiResponse 结果。
    /// </summary>
    public static async Task<ApiResponseUnwrapped<TResponse>> InvokeApiAsync<TResponse>(
        this DaprClient client,
        HttpMethod method,
        string appId,
        string methodName,
        CancellationToken cancellationToken = default)
    {
        var envelope = await client.InvokeMethodAsync<ApiResponse<TResponse>>(
            method,
            appId,
            methodName,
            cancellationToken);

        return ResolveEnvelope(envelope);
    }

    /// <summary>
    /// 调用下游服务（携带请求体）并返回解包后的 ApiResponse 结果。
    /// </summary>
    public static async Task<ApiResponseUnwrapped<TResponse>> InvokeApiAsync<TRequest, TResponse>(
        this DaprClient client,
        HttpMethod method,
        string appId,
        string methodName,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        var envelope = await client.InvokeMethodAsync<TRequest, ApiResponse<TResponse>>(
            method,
            appId,
            methodName,
            request,
            cancellationToken);

        return ResolveEnvelope(envelope);
    }

    private static ApiResponseUnwrapped<T> ResolveEnvelope<T>(ApiResponse<T> envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope, nameof(envelope));
        return envelope.Unwrap();
    }
}
