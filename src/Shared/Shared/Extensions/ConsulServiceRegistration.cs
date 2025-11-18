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

        // 获取服务地址和端口
        var serviceAddress = await GetServiceAddressAsync(app);
        var servicePort = GetServicePort(app);

        // 使用固定的 ServiceId：serviceName-hostname:port
        // 这样同一个服务在同一个主机/端口上重启时会复用同一个 ID
        var hostname = serviceAddress.Replace("http://", "").Replace("https://", "").Split(':')[0];
        var serviceId = consulConfig["ServiceId"] ?? $"{serviceName}-{hostname}:{servicePort}";

        // 健康检查配置
        var healthCheckPath = consulConfig["HealthCheckPath"] ?? "/health";
        var healthCheckInterval = consulConfig["HealthCheckInterval"] ?? "10s";
        var healthCheckTimeout = consulConfig["HealthCheckTimeout"] ?? "5s";

        // 服务元数据
        var version = consulConfig["ServiceVersion"] ?? "1.0.0";
        var protocol = serviceAddress.StartsWith("https") ? "https" : "http";

        var registration = new
        {
            ID = serviceId,
            Name = serviceName,
            Address = serviceAddress.Replace("http://", "").Replace("https://", "").Split(':')[0],
            Port = servicePort,
            Tags = new[] { version, protocol, "api", "microservice" },
            Meta = new Dictionary<string, string>
            {
                { "version", version },
                { "protocol", protocol },
                { "metrics_path", "/metrics" }
            },
            Check = new
            {
                HTTP =
                    $"{protocol}://{serviceAddress.Replace("http://", "").Replace("https://", "").Split(':')[0]}:{servicePort}{healthCheckPath}",
                Interval = healthCheckInterval,
                Timeout = healthCheckTimeout,
                DeregisterCriticalServiceAfter = "30s"
            }
        };

        // 先注销可能存在的旧实例（相同 ServiceId），然后注册新实例
        using var httpClient = new HttpClient();
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
                logger.LogError("❌ Consul 注册失败: {Error}", error);
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
                var deregisterResponse =
                    await httpClient.PutAsync($"{consulAddress}/v1/agent/service/deregister/{serviceId}", null);
                if (deregisterResponse.IsSuccessStatusCode)
                    logger.LogInformation("✅ 服务已从 Consul 注销: {ServiceId}", serviceId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "⚠️ 服务注销失败: {ServiceId}", serviceId);
            }
        });
    }

    private static async Task<string> GetServiceAddressAsync(WebApplication app)
    {
        // 从配置读取
        var configAddress = app.Configuration["Consul:ServiceAddress"];
        if (!string.IsNullOrEmpty(configAddress)) return configAddress;

        // 从环境变量读取（容器环境）
        var hostname = Environment.GetEnvironmentVariable("HOSTNAME")
                       ?? Environment.GetEnvironmentVariable("SERVICE_HOST")
                       ?? "localhost";

        // 如果是容器环境，使用容器主机名
        if (hostname != "localhost" && !hostname.StartsWith("192.168") && !hostname.StartsWith("127.")) return hostname;

        // 从服务器地址获取
        await Task.Delay(100); // 等待服务器启动
        var server = app.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>();

        if (addresses?.Addresses.Any() == true)
        {
            var address = addresses.Addresses.First();
            var uri = new Uri(address);
            return uri.Host == "localhost" || uri.Host == "0.0.0.0" || uri.Host == "[::]"
                ? hostname
                : uri.Host;
        }

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