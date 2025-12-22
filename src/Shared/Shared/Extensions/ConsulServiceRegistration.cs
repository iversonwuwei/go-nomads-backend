using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace GoNomads.Shared.Extensions;

/// <summary>
///     Consul 服务自动注册扩展
/// </summary>
public static class ConsulServiceRegistration
{
    /// <summary>
    ///     注册服务到 Consul（自动从配置读取）
    /// </summary>
    public static async Task RegisterWithConsulAsync(this WebApplication app)
    {
        var configuration = app.Configuration;
        var lifetime = app.Lifetime;
        var logger = app.Logger;

        // 读取配置
        var consulConfig = configuration.GetSection("Consul");

        // 检查是否启用 Consul 注册
        var enabled = consulConfig.GetValue<bool?>("Enabled");
        if (enabled.HasValue && !enabled.Value)
        {
            logger.LogInformation("🔧 Consul 服务注册已禁用，跳过注册");
            return;
        }

        var consulAddress = consulConfig["Address"] ?? "http://localhost:8500";
        var serviceName = consulConfig["ServiceName"] ?? app.Environment.ApplicationName;

        // 获取服务地址和端口（优先使用 Pod IP）
        var serviceAddress = await GetServiceAddressAsync(app, logger);
        var servicePort = GetServicePort(app);

        // 使用固定的 ServiceId：serviceName-podIP:port
        var serviceId = consulConfig["ServiceId"] ?? $"{serviceName}-{serviceAddress}:{servicePort}";

        // 健康检查配置
        var healthCheckPath = consulConfig["HealthCheckPath"] ?? "/health";
        var healthCheckInterval = consulConfig["HealthCheckInterval"] ?? "10s";
        var healthCheckTimeout = consulConfig["HealthCheckTimeout"] ?? "5s";

        // 服务元数据
        var version = consulConfig["ServiceVersion"] ?? "1.0.0";
        const string protocol = "http";

        var registration = new
        {
            ID = serviceId,
            Name = serviceName,
            Address = serviceAddress,
            Port = servicePort,
            Tags = new[] { version, protocol, "api", "microservice", "k8s" },
            Meta = new Dictionary<string, string>
            {
                { "version", version },
                { "protocol", protocol },
                { "metrics_path", "/metrics" },
                { "pod_name", Environment.GetEnvironmentVariable("HOSTNAME") ?? "unknown" }
            },
            Check = new
            {
                HTTP = $"{protocol}://{serviceAddress}:{servicePort}{healthCheckPath}",
                Interval = healthCheckInterval,
                Timeout = healthCheckTimeout,
                DeregisterCriticalServiceAfter = "60s"
            }
        };

        // 先注销可能存在的旧实例（相同 ServiceId），然后注册新实例
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        try
        {
            await httpClient.PutAsync($"{consulAddress}/v1/agent/service/deregister/{serviceId}", null);
            logger.LogDebug("已注销可能存在的旧服务实例: {ServiceId}", serviceId);
        }
        catch
        {
            // 忽略注销失败（可能服务不存在）
        }

        var json = JsonSerializer.Serialize(registration);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        logger.LogInformation("📝 正在注册服务到 Consul: {ServiceName} ({ServiceId}) at {Address}:{Port}",
            serviceName, serviceId, serviceAddress, servicePort);
        logger.LogDebug("📝 Consul 注册请求: {Json}", json);

        try
        {
            var response = await httpClient.PutAsync($"{consulAddress}/v1/agent/service/register", content);
            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("✅ 服务已注册到 Consul: {ServiceName} ({ServiceId}) at {Address}:{Port}",
                    serviceName, serviceId, serviceAddress, servicePort);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                logger.LogError("❌ Consul 注册失败: {StatusCode} - {Error}", response.StatusCode, error);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ 无法连接到 Consul: {ConsulAddress}", consulAddress);
        }

        // 应用关闭时自动注销
        lifetime.ApplicationStopping.Register(async () =>
        {
            try
            {
                using var deregisterClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var deregisterResponse =
                    await deregisterClient.PutAsync($"{consulAddress}/v1/agent/service/deregister/{serviceId}", null);
                if (deregisterResponse.IsSuccessStatusCode)
                    logger.LogInformation("✅ 服务已从 Consul 注销: {ServiceId}", serviceId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "⚠️ 服务注销失败: {ServiceId}", serviceId);
            }
        });
    }

    private static async Task<string> GetServiceAddressAsync(WebApplication app, ILogger logger)
    {
        // 1. 优先从配置读取（允许手动指定）
        var configAddress = app.Configuration["Consul:ServiceAddress"];
        if (!string.IsNullOrEmpty(configAddress))
        {
            logger.LogDebug("使用配置的服务地址: {Address}", configAddress);
            return configAddress;
        }

        // 2. 尝试从 POD_IP 环境变量获取（K8s Downward API）
        var podIp = Environment.GetEnvironmentVariable("POD_IP");
        if (!string.IsNullOrEmpty(podIp))
        {
            logger.LogDebug("使用 POD_IP 环境变量: {Address}", podIp);
            return podIp;
        }

        // 3. 尝试获取本机 IP 地址（适用于 K8s Pod）
        try
        {
            var hostName = Dns.GetHostName();
            var hostEntry = await Dns.GetHostEntryAsync(hostName);
            
            // 优先选择 IPv4 地址
            var ipAddress = hostEntry.AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork 
                                      && !IPAddress.IsLoopback(ip)
                                      && !ip.ToString().StartsWith("127."));
            
            if (ipAddress != null)
            {
                logger.LogDebug("使用 DNS 解析获取的 IP: {Address}", ipAddress);
                return ipAddress.ToString();
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "DNS 解析失败，尝试其他方式获取 IP");
        }

        // 4. 通过连接外部地址获取本机 IP
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            // 连接到一个外部地址（不需要真正建立连接）
            socket.Connect("8.8.8.8", 53);
            var localEndPoint = socket.LocalEndPoint as IPEndPoint;
            if (localEndPoint != null)
            {
                logger.LogDebug("使用 Socket 获取的本机 IP: {Address}", localEndPoint.Address);
                return localEndPoint.Address.ToString();
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Socket 方式获取 IP 失败");
        }

        // 5. 回退到 hostname（非 K8s 环境）
        var hostname = Environment.GetEnvironmentVariable("HOSTNAME")
                       ?? Environment.GetEnvironmentVariable("SERVICE_HOST")
                       ?? "localhost";
        
        logger.LogWarning("无法获取 Pod IP，回退使用 hostname: {Hostname}", hostname);
        return hostname;
    }

    private static int GetServicePort(WebApplication app)
    {
        // 从配置读取
        if (int.TryParse(app.Configuration["Consul:ServicePort"], out var configPort)) return configPort;

        // 从服务器地址获取
        var server = app.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>();

        if (addresses?.Addresses.Any() == true)
        {
            var address = addresses.Addresses.First();
            var uri = new Uri(address);
            return uri.Port;
        }

        // 默认端口
        return 8080;
    }
}