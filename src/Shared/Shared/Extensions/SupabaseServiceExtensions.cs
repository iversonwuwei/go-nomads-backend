using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Configuration;
using Supabase;

namespace Shared.Extensions;

/// <summary>
/// Supabase 服务扩展
/// </summary>
public static class SupabaseServiceExtensions
{
    /// <summary>
    /// 添加 Supabase 客户端到服务容器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <param name="configSection">配置节名称，默认为 "Supabase"</param>
    /// <returns></returns>
    public static IServiceCollection AddSupabase(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSection = "Supabase")
    {
        // 注册配置
        services.Configure<SupabaseSettings>(configuration.GetSection(configSection));

        // 验证配置
        var settings = configuration.GetSection(configSection).Get<SupabaseSettings>();
        if (settings == null || !settings.IsValid())
        {
            throw new InvalidOperationException(
                settings?.GetValidationError() ?? "Supabase configuration is missing");
        }

        // 注册 Supabase 客户端（单例模式）
        services.AddSingleton<Client>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<Client>>();
            var options = provider.GetRequiredService<IOptions<SupabaseSettings>>().Value;

            logger.LogInformation("Initializing Supabase client with URL: {Url}", options.Url);

            var supabaseOptions = new SupabaseOptions
            {
                AutoConnectRealtime = options.AutoConnectRealtime,
                AutoRefreshToken = options.AutoRefreshToken
            };

            var client = new Client(options.Url, options.Key, supabaseOptions);
            
            // 初始化客户端
            try
            {
                client.InitializeAsync().Wait();
                logger.LogInformation("Supabase client initialized successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize Supabase client");
                throw;
            }

            return client;
        });

        return services;
    }

    /// <summary>
    /// 添加 Supabase 客户端到服务容器（使用自定义配置）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureSettings">配置委托</param>
    /// <returns></returns>
    public static IServiceCollection AddSupabase(
        this IServiceCollection services,
        Action<SupabaseSettings> configureSettings)
    {
        var settings = new SupabaseSettings();
        configureSettings(settings);

        if (!settings.IsValid())
        {
            throw new InvalidOperationException(settings.GetValidationError());
        }

        // 注册配置
        services.Configure(configureSettings);

        // 注册 Supabase 客户端
        services.AddSingleton<Client>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<Client>>();
            logger.LogInformation("Initializing Supabase client with URL: {Url}", settings.Url);

            var supabaseOptions = new SupabaseOptions
            {
                AutoConnectRealtime = settings.AutoConnectRealtime,
                AutoRefreshToken = settings.AutoRefreshToken
            };

            var client = new Client(settings.Url, settings.Key, supabaseOptions);
            
            try
            {
                client.InitializeAsync().Wait();
                logger.LogInformation("Supabase client initialized successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize Supabase client");
                throw;
            }

            return client;
        });

        return services;
    }

    /// <summary>
    /// 从环境变量添加 Supabase 客户端
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="urlEnvVar">URL 环境变量名，默认为 "Supabase__Url"</param>
    /// <param name="keyEnvVar">Key 环境变量名，默认为 "Supabase__Key"</param>
    /// <returns></returns>
    public static IServiceCollection AddSupabaseFromEnvironment(
        this IServiceCollection services,
        string urlEnvVar = "Supabase__Url",
        string keyEnvVar = "Supabase__Key")
    {
        var url = Environment.GetEnvironmentVariable(urlEnvVar);
        var key = Environment.GetEnvironmentVariable(keyEnvVar);

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException($"Environment variable '{urlEnvVar}' is not set");
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException($"Environment variable '{keyEnvVar}' is not set");
        }

        return services.AddSupabase(settings =>
        {
            settings.Url = url;
            settings.Key = key;
        });
    }
}
