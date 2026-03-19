using GoNomads.Shared.Communication;
using GoNomads.Shared.Models;

namespace GoNomads.Shared.Extensions;

public static class ServiceInvocationClientExtensions
{
    public static async Task<ApiResponseUnwrapped<TResponse>> InvokeApiAsync<TResponse>(
        this ServiceInvocationClient client,
        HttpMethod method,
        string serviceName,
        string path,
        CancellationToken cancellationToken = default)
    {
        var envelope = await client.InvokeAsync<ApiResponse<TResponse>>(
            method,
            serviceName,
            path,
            cancellationToken);

        return ResolveEnvelope(envelope);
    }

    public static async Task<ApiResponseUnwrapped<TResponse>> InvokeApiAsync<TRequest, TResponse>(
        this ServiceInvocationClient client,
        HttpMethod method,
        string serviceName,
        string path,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        var envelope = await client.InvokeAsync<TRequest, ApiResponse<TResponse>>(
            method,
            serviceName,
            path,
            request,
            cancellationToken);

        return ResolveEnvelope(envelope);
    }

    private static ApiResponseUnwrapped<T> ResolveEnvelope<T>(ApiResponse<T>? envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        return envelope.Unwrap();
    }
}