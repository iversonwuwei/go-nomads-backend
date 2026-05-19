var builder = DistributedApplication.CreateBuilder(args);

// ── Infrastructure (Aspire-managed containers) ──────────────────────────────
var redis = builder.AddRedis("redis")
	.WithLifetime(ContainerLifetime.Persistent);

var rabbitmqUsername = builder.AddParameter("rabbitmq-username");
var rabbitmqPassword = builder.AddParameter("rabbitmq-password", secret: true);
var rabbitmq = builder.AddRabbitMQ("rabbitmq", rabbitmqUsername, rabbitmqPassword)
	.WithManagementPlugin()
	.WithLifetime(ContainerLifetime.Persistent);

var elasticsearch = builder.AddElasticsearch("elasticsearch")
	.WithLifetime(ContainerLifetime.Persistent);

// ── Helper: inject RabbitMQ env vars for MassTransit services ────────────────
// These services read RabbitMQ__Host / Username / Password (not ConnectionStrings)
void AddRabbitMqEnv<T>(IResourceBuilder<T> svc) where T : IResourceWithEnvironment
{
	svc.WithEnvironment(ctx =>
	{
		ctx.EnvironmentVariables["RabbitMQ__Host"] = rabbitmq.Resource.ConnectionStringExpression;
		ctx.EnvironmentVariables["RabbitMQ__Port"] = "5672";
		ctx.EnvironmentVariables["RabbitMQ__Username"] = rabbitmqUsername.Resource;
		ctx.EnvironmentVariables["RabbitMQ__Password"] = rabbitmqPassword.Resource;
	});
}

// ── Services ─────────────────────────────────────────────────────────────────
var userService = builder.AddProject<Projects.UserService>("user-service")
	.WithHttpEndpoint(port: 5001, targetPort: 5001)
	.WithReference(rabbitmq);
AddRabbitMqEnv(userService);

var productService = builder.AddProject<Projects.ProductService>("product-service")
	.WithHttpEndpoint(port: 5002, targetPort: 5002);

var documentService = builder.AddProject<Projects.DocumentService>("document-service")
	.WithHttpEndpoint(port: 5003, targetPort: 5003)
	.WithReference(redis);

var cityService = builder.AddProject<Projects.CityService>("city-service")
	.WithHttpEndpoint(port: 5202, targetPort: 5202)
	.WithReference(redis)
	.WithReference(rabbitmq);
AddRabbitMqEnv(cityService);

var eventService = builder.AddProject<Projects.EventService>("event-service")
	.WithHttpEndpoint(port: 5205, targetPort: 5205)
	.WithReference(rabbitmq);
AddRabbitMqEnv(eventService);

var coworkingService = builder.AddProject<Projects.CoworkingService>("coworking-service")
	.WithHttpEndpoint(port: 5203, targetPort: 5203)
	.WithReference(rabbitmq);
AddRabbitMqEnv(coworkingService);

var aiService = builder.AddProject<Projects.AIService>("ai-service")
	.WithHttpEndpoint(port: 5209, targetPort: 5209)
	.WithReference(redis)
	.WithReference(rabbitmq)
	.WithEnvironment(ctx =>
	{
		// AIService uses non-standard config keys
		ctx.EnvironmentVariables["Redis__ConnectionString"] = ReferenceExpression.Create(
			$"{redis.Resource.ConnectionStringExpression},abortConnect=false");
		ctx.EnvironmentVariables["RabbitMQ__HostName"] = rabbitmq.Resource.ConnectionStringExpression;
		ctx.EnvironmentVariables["RabbitMQ__Port"] = "5672";
		ctx.EnvironmentVariables["RabbitMQ__UserName"] = rabbitmqUsername.Resource;
		ctx.EnvironmentVariables["RabbitMQ__Password"] = rabbitmqPassword.Resource;
	});

var messageService = builder.AddProject<Projects.MessageService>("message-service")
	.WithHttpEndpoint(port: 5005, targetPort: 5005)
	.WithReference(redis)
	.WithReference(rabbitmq);
AddRabbitMqEnv(messageService);

var cacheService = builder.AddProject<Projects.CacheService>("cache-service")
	.WithHttpEndpoint(port: 5210, targetPort: 5210)
	.WithReference(redis);

var innovationService = builder.AddProject<Projects.InnovationService>("innovation-service")
	.WithHttpEndpoint(port: 5206, targetPort: 5206)
	.WithReference(rabbitmq);
AddRabbitMqEnv(innovationService);

var accommodationService = builder.AddProject<Projects.AccommodationService>("accommodation-service")
	.WithHttpEndpoint(port: 5204, targetPort: 5204);

var configService = builder.AddProject<Projects.ConfigService>("config-service")
	.WithHttpEndpoint(port: 5213, targetPort: 5213);

var searchService = builder.AddProject<Projects.SearchService>("search-service")
	.WithHttpEndpoint(port: 5215, targetPort: 5215)
	.WithReference(rabbitmq)
	.WithReference(elasticsearch)
	.WithEnvironment(ctx =>
	{
		// SearchService reads Elasticsearch__Url, not ConnectionStrings
		ctx.EnvironmentVariables["Elasticsearch__Url"] = elasticsearch.Resource.ConnectionStringExpression;
		ctx.EnvironmentVariables["RabbitMQ__Host"] = rabbitmq.Resource.ConnectionStringExpression;
		ctx.EnvironmentVariables["RabbitMQ__Port"] = "5672";
		ctx.EnvironmentVariables["RabbitMQ__Username"] = rabbitmqUsername.Resource;
		ctx.EnvironmentVariables["RabbitMQ__Password"] = rabbitmqPassword.Resource;
		ctx.EnvironmentVariables["ServiceUrls__CityService"] = cityService.GetEndpoint("http");
		ctx.EnvironmentVariables["ServiceUrls__CoworkingService"] = coworkingService.GetEndpoint("http");
	});

// Gateway — routes to all services
var gateway = builder.AddProject<Projects.Gateway>("gateway")
	.WithHttpEndpoint(port: 5080, targetPort: 5000)
	.WithReference(redis)
	.WithEnvironment(ctx =>
	{
		ctx.EnvironmentVariables["ServiceUrls__UserService"] = userService.GetEndpoint("http");
		ctx.EnvironmentVariables["ServiceUrls__CityService"] = cityService.GetEndpoint("http");
		ctx.EnvironmentVariables["ServiceUrls__CoworkingService"] = coworkingService.GetEndpoint("http");
		ctx.EnvironmentVariables["ServiceUrls__EventService"] = eventService.GetEndpoint("http");
		ctx.EnvironmentVariables["ServiceUrls__AIService"] = aiService.GetEndpoint("http");
		ctx.EnvironmentVariables["ServiceUrls__CacheService"] = cacheService.GetEndpoint("http");
		ctx.EnvironmentVariables["ServiceUrls__MessageService"] = messageService.GetEndpoint("http");
		ctx.EnvironmentVariables["ServiceUrls__InnovationService"] = innovationService.GetEndpoint("http");
		ctx.EnvironmentVariables["ServiceUrls__SearchService"] = searchService.GetEndpoint("http");
		ctx.EnvironmentVariables["ServiceUrls__AccommodationService"] = accommodationService.GetEndpoint("http");
		ctx.EnvironmentVariables["ServiceUrls__ProductService"] = productService.GetEndpoint("http");
		ctx.EnvironmentVariables["ServiceUrls__ConfigService"] = configService.GetEndpoint("http");
	});

// DocumentService needs inter-service URLs (defined after gateway)
documentService.WithEnvironment(ctx =>
{
	ctx.EnvironmentVariables["Services__Gateway__Url"] = gateway.GetEndpoint("http");
	ctx.EnvironmentVariables["Services__Gateway__OpenApiUrl"] =
		ReferenceExpression.Create($"{gateway.GetEndpoint("http")}/openapi/v1.json");
	ctx.EnvironmentVariables["Services__ProductService__Url"] = productService.GetEndpoint("http");
	ctx.EnvironmentVariables["Services__ProductService__OpenApiUrl"] =
		ReferenceExpression.Create($"{productService.GetEndpoint("http")}/openapi/v1.json");
	ctx.EnvironmentVariables["Services__UserService__Url"] = userService.GetEndpoint("http");
	ctx.EnvironmentVariables["Services__UserService__OpenApiUrl"] =
		ReferenceExpression.Create($"{userService.GetEndpoint("http")}/openapi/v1.json");
});

builder.Build().Run();
