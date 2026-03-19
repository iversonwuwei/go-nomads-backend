using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace GoNomads.Shared.Communication;

public sealed class ServiceInvocationClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Dictionary<string, int> DefaultPorts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["gateway"] = 5080,
        ["product-service"] = 5002,
        ["user-service"] = 5001,
        ["document-service"] = 5003,
        ["city-service"] = 8002,
        ["event-service"] = 8005,
        ["coworking-service"] = 8006,
        ["ai-service"] = 8009,
        ["cache-service"] = 8010,
        ["message-service"] = 5005,
        ["innovation-service"] = 8011,
        ["accommodation-service"] = 8012,
        ["search-service"] = 8015
    };

    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ServiceInvocationClient> _logger;
    private readonly IMemoryCache _memoryCache;

    public ServiceInvocationClient(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IMemoryCache memoryCache,
        ILogger<ServiceInvocationClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public HttpRequestMessage CreateRequest(HttpMethod httpMethod, string serviceName, string path)
    {
        return new HttpRequestMessage(httpMethod, BuildServiceUri(serviceName, path));
    }

    public async Task<TResponse?> InvokeAsync<TResponse>(
        HttpMethod httpMethod,
        string serviceName,
        string path,
        CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(httpMethod, serviceName, path);
        using var response = await InvokeWithResponseAsync(request, cancellationToken);
        return await DeserializeResponseAsync<TResponse>(response, cancellationToken);
    }

    public async Task<TResponse?> InvokeAsync<TResponse>(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        using var response = await InvokeWithResponseAsync(request, cancellationToken);
        return await DeserializeResponseAsync<TResponse>(response, cancellationToken);
    }

    public async Task<TResponse?> InvokeAsync<TRequest, TResponse>(
        HttpMethod httpMethod,
        string serviceName,
        string path,
        TRequest data,
        CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(httpMethod, serviceName, path);
        request.Content = CreateJsonContent(data);
        using var response = await InvokeWithResponseAsync(request, cancellationToken);
        return await DeserializeResponseAsync<TResponse>(response, cancellationToken);
    }

    public async Task InvokeAsync(
        HttpMethod httpMethod,
        string serviceName,
        string path,
        CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(httpMethod, serviceName, path);
        using var _ = await InvokeWithResponseAsync(request, cancellationToken);
    }

    public async Task InvokeAsync<TRequest>(
        HttpMethod httpMethod,
        string serviceName,
        string path,
        TRequest data,
        CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(httpMethod, serviceName, path);
        request.Content = CreateJsonContent(data);
        using var _ = await InvokeWithResponseAsync(request, cancellationToken);
    }

    public async Task<HttpResponseMessage> InvokeWithResponseAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(ServiceInvocationServiceCollectionExtensions.HttpClientName);
        var response = await client.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        var body = response.Content == null
            ? string.Empty
            : await response.Content.ReadAsStringAsync(cancellationToken);
        var statusCode = response.StatusCode;
        var reasonPhrase = response.ReasonPhrase;
        response.Dispose();

        throw new HttpRequestException(
            $"Service invocation failed: {(int)statusCode} {reasonPhrase}. {body}",
            null,
            statusCode);
    }

    public Task<T?> GetCachedStateAsync<T>(string storeName, string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_memoryCache.TryGetValue(GetStateCacheKey(storeName, key), out T? value) ? value : default);
    }

    public Task SaveCachedStateAsync<T>(
        string storeName,
        string key,
        T value,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = new MemoryCacheEntryOptions();
        if (metadata != null && metadata.TryGetValue("ttlInSeconds", out var ttlValue) && int.TryParse(ttlValue, out var ttlSeconds))
        {
            options.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ttlSeconds);
        }

        _memoryCache.Set(GetStateCacheKey(storeName, key), value, options);
        return Task.CompletedTask;
    }

    public string ResolveServiceBaseUrl(string serviceName)
    {
        var pascalName = ToPascalCase(serviceName);
        var configuredUrl = _configuration[$"ServiceUrls:{pascalName}"]
                            ?? _configuration[$"ServiceUrls:{serviceName}"]
                            ?? _configuration[$"Services:{pascalName}:Url"]
                            ?? _configuration[$"Services:{serviceName}:Url"];

        if (!string.IsNullOrWhiteSpace(configuredUrl))
        {
            return configuredUrl;
        }

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST")))
        {
            return $"http://{serviceName}:8080";
        }

        if (string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return $"http://go-nomads-{serviceName}:8080";
        }

        if (DefaultPorts.TryGetValue(serviceName, out var port))
        {
            return $"http://localhost:{port}";
        }

        _logger.LogWarning("No default URL mapping found for service {ServiceName}, falling back to localhost:8080", serviceName);
        return "http://localhost:8080";
    }

    private Uri BuildServiceUri(string serviceName, string path)
    {
        var baseUrl = ResolveServiceBaseUrl(serviceName);
        return new Uri(new Uri(AppendTrailingSlash(baseUrl)), TrimLeadingSlash(path));
    }

    private static StringContent CreateJsonContent<T>(T data)
    {
        return new StringContent(JsonSerializer.Serialize(data, JsonOptions), Encoding.UTF8, "application/json");
    }

    private static string GetStateCacheKey(string storeName, string key)
    {
        return $"{storeName}:{key}";
    }

    private static string ToPascalCase(string serviceName)
    {
        return string.Concat(serviceName
            .Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    }

    private static string AppendTrailingSlash(string value)
    {
        return value.EndsWith('/') ? value : value + "/";
    }

    private static string TrimLeadingSlash(string value)
    {
        return value.TrimStart('/');
    }

    private static async Task<TResponse?> DeserializeResponseAsync<TResponse>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.Content == null)
        {
            return default;
        }

        if (typeof(TResponse) == typeof(string))
        {
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            return (TResponse?)(object?)raw;
        }

        if (typeof(TResponse) == typeof(JsonElement))
        {
            using var document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                cancellationToken: cancellationToken);
            return (TResponse?)(object)document.RootElement.Clone();
        }

        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, cancellationToken);
    }
}

public static class ServiceInvocationServiceCollectionExtensions
{
    internal const string HttpClientName = "ServiceInvocation";

    public static IServiceCollection AddServiceInvocationClient(this IServiceCollection services, TimeSpan? timeout = null)
    {
        services.AddHttpClient(HttpClientName, client =>
        {
            if (timeout.HasValue)
            {
                client.Timeout = timeout.Value;
            }
        });
        services.AddMemoryCache();
        services.TryAddSingleton<ServiceInvocationClient>();
        return services;
    }
}