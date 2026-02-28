// =============================================================================
// Go Nomads - Aspire AppHost
// 编排所有微服务和基础设施资源
// =============================================================================
// 100% Aspire 编排:
// - 通过 Aspire 管理所有基础设施资源（Redis, RabbitMQ, Elasticsearch）
// - 通过 Aspire 编排所有微服务，自动注入连接字符串和服务发现地址
// - 所有服务间引用通过 .WithReference() 声明，无需手动注入环境变量
// - 替代 docker-compose.yml + docker-compose-infras.yml + 部署脚本
// =============================================================================

var builder = DistributedApplication.CreateBuilder(args);

// =============================================================================
// 外部参数（敏感配置通过 User Secrets 或环境变量注入）
// =============================================================================
var aliyunSmsKeyId = builder.AddParameter("aliyun-sms-access-key-id", secret: true);
var aliyunSmsKeySecret = builder.AddParameter("aliyun-sms-access-key-secret", secret: true);
var qianwenApiKey = builder.AddParameter("qianwen-api-key", secret: true);

// =============================================================================
// 基础设施资源
// =============================================================================

// Redis - 缓存、SignalR Backplane、状态存储
var redis = builder.AddRedis("redis")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

// RabbitMQ - MassTransit 消息队列
var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

// Elasticsearch - 全文搜索
var elasticsearch = builder.AddElasticsearch("elasticsearch")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

// Supabase PostgreSQL - 外部连接（各服务各自配置自己的连接字符串）
// 注意: Supabase 是远程服务，不由 Aspire 编排，通过 appsettings 或环境变量配置

// =============================================================================
// 微服务
// =============================================================================

// --- Message Service (SignalR Hubs, 最大消息消费者) ---
// 注意: 提前定义，因为 UserService、EventService 等需要引用 MessageService 发送通知
var messageService = builder.AddProject<Projects.MessageService>("message-service")
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WaitFor(redis)
    .WaitFor(rabbitmq);

// --- User Service ---
var userService = builder.AddProject<Projects.UserService>("user-service")
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithReference(messageService)
    .WaitFor(redis)
    .WaitFor(rabbitmq)
    .WithEnvironment("AliyunSms__AccessKeyId", aliyunSmsKeyId)
    .WithEnvironment("AliyunSms__AccessKeySecret", aliyunSmsKeySecret);
// 注意: userService 还依赖 cityService, productService, eventService
// 这些引用在下方定义后通过延迟补全方式添加（见文件末尾 "服务间交叉引用补全" 区块）

// --- City Service ---
var cityService = builder.AddProject<Projects.CityService>("city-service")
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithReference(elasticsearch)
    .WithReference(userService)
    .WithReference(messageService)
    .WaitFor(redis)
    .WaitFor(rabbitmq)
    .WaitFor(elasticsearch);

// --- Product Service ---
var productService = builder.AddProject<Projects.ProductService>("product-service")
    .WithReference(redis)
    .WithReference(userService)
    .WaitFor(redis);

// --- Document Service ---
var documentService = builder.AddProject<Projects.DocumentService>("document-service")
    .WithReference(redis)
    .WithReference(productService)
    .WaitFor(redis);

// --- Coworking Service ---
var coworkingService = builder.AddProject<Projects.CoworkingService>("coworking-service")
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithReference(userService)
    .WithReference(cityService)
    .WaitFor(redis)
    .WaitFor(rabbitmq);

// --- Accommodation Service ---
var accommodationService = builder.AddProject<Projects.AccommodationService>("accommodation-service")
    .WithReference(redis)
    .WithReference(userService)
    .WaitFor(redis);

// --- Event Service ---
var eventService = builder.AddProject<Projects.EventService>("event-service")
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithReference(cityService)
    .WithReference(userService)
    .WithReference(messageService)
    .WaitFor(redis)
    .WaitFor(rabbitmq);

// --- Innovation Service ---
var innovationService = builder.AddProject<Projects.InnovationService>("innovation-service")
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithReference(userService)
    .WaitFor(redis)
    .WaitFor(rabbitmq);

// --- AI Service ---
var aiService = builder.AddProject<Projects.AIService>("ai-service")
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithReference(userService)
    .WithReference(cityService)
    .WaitFor(redis)
    .WaitFor(rabbitmq)
    .WithEnvironment("ConnectionStrings__QianWenApiKey", qianwenApiKey);

// --- Search Service ---
var searchService = builder.AddProject<Projects.SearchService>("search-service")
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithReference(elasticsearch)
    .WithReference(cityService)
    .WithReference(coworkingService)
    .WaitFor(redis)
    .WaitFor(rabbitmq)
    .WaitFor(elasticsearch)
    .WaitFor(cityService)
    .WaitFor(coworkingService);

// --- Cache Service ---
var cacheService = builder.AddProject<Projects.CacheService>("cache-service")
    .WithReference(redis)
    .WithReference(cityService)
    .WithReference(coworkingService)
    .WaitFor(redis);

// =============================================================================
// API Gateway (YARP)
// Gateway 保留为独立项目（包含 JWT 认证、限流、请求转换等中间件）
// 路由配置通过 yarp.json + LoadFromConfig 标准方式加载
// 服务发现通过 Microsoft.Extensions.ServiceDiscovery.Yarp 自动解析
// =============================================================================
var gateway = builder.AddProject<Projects.Gateway>("gateway")
    .WithReference(userService)
    .WithReference(cityService)
    .WithReference(productService)
    .WithReference(documentService)
    .WithReference(coworkingService)
    .WithReference(accommodationService)
    .WithReference(eventService)
    .WithReference(innovationService)
    .WithReference(aiService)
    .WithReference(searchService)
    .WithReference(cacheService)
    .WithReference(messageService)
    .WithExternalHttpEndpoints()
    .WaitFor(userService)
    .WaitFor(cityService);

// =============================================================================
// 服务间交叉引用补全
// 以下引用因声明顺序的原因无法在初始定义时添加（前向依赖）
// .WithReference() 仅注入服务发现地址，不创建启动依赖，循环引用是安全的
// =============================================================================

// UserService 依赖 CityService, ProductService, EventService（在 UserService 之后定义）
userService
    .WithReference(cityService)
    .WithReference(productService)
    .WithReference(eventService);

// CityService 依赖 CacheService, CoworkingService, EventService, AIService（在 CityService 之后定义）
cityService
    .WithReference(cacheService)
    .WithReference(coworkingService)
    .WithReference(eventService)
    .WithReference(aiService);

// CoworkingService 依赖 CacheService（在 CoworkingService 之后定义）
coworkingService
    .WithReference(cacheService);

// MessageService 依赖 UserService（在 MessageService 之后定义）
messageService
    .WithReference(userService);

builder.Build().Run();
