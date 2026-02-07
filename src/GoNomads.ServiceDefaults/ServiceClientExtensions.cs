// =============================================================================
// Go Nomads - 服务间通信 HttpClient 注册扩展
// 统一管理微服务间 typed/named HttpClient 的注册，
// BaseAddress 由 Aspire ServiceDiscovery 自动解析。
// =============================================================================

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 服务间通信 HttpClient 注册扩展方法。
/// <para>
/// 所有 Go Nomads 微服务使用 <c>http://{serviceName}</c> 作为 BaseAddress，
/// 由 Aspire ServiceDiscovery 在运行时解析为实际地址。
/// </para>
/// </summary>
public static class ServiceClientExtensions
{
    /// <summary>
    /// 注册面向服务间通信的 <b>类型化 HttpClient</b>。
    /// <para>
    /// 等效于:
    /// <code>
    /// services.AddHttpClient&lt;TInterface, TImplementation&gt;(c =&gt;
    ///     c.BaseAddress = new Uri("http://service-name"));
    /// </code>
    /// </para>
    /// </summary>
    /// <typeparam name="TInterface">服务客户端接口</typeparam>
    /// <typeparam name="TImplementation">服务客户端实现</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="serviceName">目标服务名称，如 "user-service"、"cache-service"</param>
    /// <returns><see cref="IHttpClientBuilder"/>，可用于进一步配置</returns>
    public static IHttpClientBuilder AddServiceClient<TInterface, TImplementation>(
        this IServiceCollection services,
        string serviceName)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        return services.AddHttpClient<TInterface, TImplementation>(c =>
        {
            c.BaseAddress = new Uri($"http://{serviceName}");
        });
    }

    /// <summary>
    /// 注册面向服务间通信的 <b>命名 HttpClient</b>。
    /// <para>
    /// 通过 <c>IHttpClientFactory.CreateClient("service-name")</c> 使用。
    /// </para>
    /// <para>
    /// 等效于:
    /// <code>
    /// services.AddHttpClient("service-name", c =&gt;
    ///     c.BaseAddress = new Uri("http://service-name"));
    /// </code>
    /// </para>
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="serviceName">目标服务名称，同时作为 HttpClient 名称和 BaseAddress 主机名</param>
    /// <returns><see cref="IHttpClientBuilder"/>，可用于进一步配置</returns>
    public static IHttpClientBuilder AddServiceClient(
        this IServiceCollection services,
        string serviceName)
    {
        return services.AddHttpClient(serviceName, c =>
        {
            c.BaseAddress = new Uri($"http://{serviceName}");
        });
    }
}
