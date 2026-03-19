using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

const string localhost = "127.0.0.1";
var redisHost = Environment.GetEnvironmentVariable("GO_NOMADS_REDIS_HOST") ?? localhost;
var redisPort = Environment.GetEnvironmentVariable("GO_NOMADS_REDIS_PORT") ?? "6379";
var rabbitMqHost = Environment.GetEnvironmentVariable("GO_NOMADS_RABBITMQ_HOST") ?? localhost;
var rabbitMqPort = Environment.GetEnvironmentVariable("GO_NOMADS_RABBITMQ_PORT") ?? "5672";
var rabbitMqUsername = Environment.GetEnvironmentVariable("GO_NOMADS_RABBITMQ_USERNAME") ?? "walden";
var rabbitMqPassword = Environment.GetEnvironmentVariable("GO_NOMADS_RABBITMQ_PASSWORD") ?? "walden";
var elasticsearchHost = Environment.GetEnvironmentVariable("GO_NOMADS_ELASTICSEARCH_HOST") ?? localhost;
var elasticsearchPort = Environment.GetEnvironmentVariable("GO_NOMADS_ELASTICSEARCH_PORT") ?? "9200";

var gateway = builder.AddProject<Projects.Gateway>("gateway")
	.WithEnvironment("ASPNETCORE_URLS", $"http://{localhost}:5080")
	.WithEnvironment("ServiceUrls__UserService", $"http://{localhost}:5001")
	.WithEnvironment("ServiceUrls__CityService", $"http://{localhost}:8002")
	.WithEnvironment("ServiceUrls__CoworkingService", $"http://{localhost}:8006")
	.WithEnvironment("ServiceUrls__EventService", $"http://{localhost}:8005")
	.WithEnvironment("ServiceUrls__AIService", $"http://{localhost}:8009")
	.WithEnvironment("ServiceUrls__CacheService", $"http://{localhost}:8010")
	.WithEnvironment("ServiceUrls__MessageService", $"http://{localhost}:5005")
	.WithEnvironment("ServiceUrls__InnovationService", $"http://{localhost}:8011")
	.WithEnvironment("ServiceUrls__SearchService", $"http://{localhost}:8015")
	.WithEnvironment("ServiceUrls__AccommodationService", $"http://{localhost}:8012")
	.WithEnvironment("ServiceUrls__ProductService", $"http://{localhost}:5002")
	.WithEnvironment(context =>
	{
		context.EnvironmentVariables["ConnectionStrings__Redis"] = $"{redisHost}:{redisPort}";
	});

builder.AddProject<Projects.UserService>("user-service")
	.WithEnvironment("ASPNETCORE_URLS", $"http://{localhost}:5001")
	.WithEnvironment(context =>
	{
		context.EnvironmentVariables["RabbitMQ__Host"] = rabbitMqHost;
		context.EnvironmentVariables["RabbitMQ__Port"] = rabbitMqPort;
		context.EnvironmentVariables["RabbitMQ__Username"] = rabbitMqUsername;
		context.EnvironmentVariables["RabbitMQ__Password"] = rabbitMqPassword;
	});

builder.AddProject<Projects.ProductService>("product-service")
	.WithEnvironment("ASPNETCORE_URLS", $"http://{localhost}:5002");

builder.AddProject<Projects.DocumentService>("document-service")
	.WithEnvironment("ASPNETCORE_URLS", $"http://{localhost}:5003")
	.WithEnvironment("Services__Gateway__Url", $"http://{localhost}:5080")
	.WithEnvironment("Services__Gateway__OpenApiUrl", $"http://{localhost}:5080/openapi/v1.json")
	.WithEnvironment("Services__ProductService__Url", $"http://{localhost}:5002")
	.WithEnvironment("Services__ProductService__OpenApiUrl", $"http://{localhost}:5002/openapi/v1.json")
	.WithEnvironment("Services__UserService__Url", $"http://{localhost}:5001")
	.WithEnvironment("Services__UserService__OpenApiUrl", $"http://{localhost}:5001/openapi/v1.json")
	.WithEnvironment(context =>
	{
		context.EnvironmentVariables["ConnectionStrings__Redis"] = $"{redisHost}:{redisPort}";
	});

builder.AddProject<Projects.CityService>("city-service")
	.WithEnvironment("ASPNETCORE_URLS", $"http://{localhost}:8002")
	.WithEnvironment(context =>
	{
		context.EnvironmentVariables["ConnectionStrings__Redis"] = $"{redisHost}:{redisPort}";
		context.EnvironmentVariables["RabbitMQ__Host"] = rabbitMqHost;
		context.EnvironmentVariables["RabbitMQ__Port"] = rabbitMqPort;
		context.EnvironmentVariables["RabbitMQ__Username"] = rabbitMqUsername;
		context.EnvironmentVariables["RabbitMQ__Password"] = rabbitMqPassword;
	});

builder.AddProject<Projects.EventService>("event-service")
	.WithEnvironment("ASPNETCORE_URLS", $"http://{localhost}:8005")
	.WithEnvironment(context =>
	{
		context.EnvironmentVariables["RabbitMQ__Host"] = rabbitMqHost;
		context.EnvironmentVariables["RabbitMQ__Port"] = rabbitMqPort;
		context.EnvironmentVariables["RabbitMQ__Username"] = rabbitMqUsername;
		context.EnvironmentVariables["RabbitMQ__Password"] = rabbitMqPassword;
	});

builder.AddProject<Projects.CoworkingService>("coworking-service")
	.WithEnvironment("ASPNETCORE_URLS", $"http://{localhost}:8006")
	.WithEnvironment(context =>
	{
		context.EnvironmentVariables["RabbitMQ__Host"] = rabbitMqHost;
		context.EnvironmentVariables["RabbitMQ__Port"] = rabbitMqPort;
		context.EnvironmentVariables["RabbitMQ__Username"] = rabbitMqUsername;
		context.EnvironmentVariables["RabbitMQ__Password"] = rabbitMqPassword;
	});

builder.AddProject<Projects.AIService>("ai-service")
	.WithEnvironment("ASPNETCORE_URLS", $"http://{localhost}:8009")
	.WithEnvironment(context =>
	{
		context.EnvironmentVariables["RabbitMQ__HostName"] = rabbitMqHost;
		context.EnvironmentVariables["RabbitMQ__Port"] = rabbitMqPort;
		context.EnvironmentVariables["RabbitMQ__UserName"] = rabbitMqUsername;
		context.EnvironmentVariables["RabbitMQ__Password"] = rabbitMqPassword;
		context.EnvironmentVariables["Redis__ConnectionString"] = $"{redisHost}:{redisPort},abortConnect=false";
	});

builder.AddProject<Projects.MessageService>("message-service")
	.WithEnvironment("ASPNETCORE_URLS", $"http://{localhost}:5005")
	.WithEnvironment(context =>
	{
		context.EnvironmentVariables["ConnectionStrings__Redis"] = $"{redisHost}:{redisPort}";
		context.EnvironmentVariables["RabbitMQ__Host"] = rabbitMqHost;
		context.EnvironmentVariables["RabbitMQ__Port"] = rabbitMqPort;
		context.EnvironmentVariables["RabbitMQ__Username"] = rabbitMqUsername;
		context.EnvironmentVariables["RabbitMQ__Password"] = rabbitMqPassword;
	});

builder.AddProject<Projects.CacheService>("cache-service")
	.WithEnvironment("ASPNETCORE_URLS", $"http://{localhost}:8010")
	.WithEnvironment(context =>
	{
		context.EnvironmentVariables["ConnectionStrings__Redis"] = $"{redisHost}:{redisPort}";
	});

builder.AddProject<Projects.InnovationService>("innovation-service")
	.WithEnvironment("ASPNETCORE_URLS", $"http://{localhost}:8011")
	.WithEnvironment(context =>
	{
		context.EnvironmentVariables["RabbitMQ__Host"] = rabbitMqHost;
		context.EnvironmentVariables["RabbitMQ__Port"] = rabbitMqPort;
		context.EnvironmentVariables["RabbitMQ__Username"] = rabbitMqUsername;
		context.EnvironmentVariables["RabbitMQ__Password"] = rabbitMqPassword;
	});

builder.AddProject<Projects.AccommodationService>("accommodation-service")
	.WithEnvironment("ASPNETCORE_URLS", $"http://{localhost}:8012");

builder.AddProject<Projects.SearchService>("search-service")
	.WithEnvironment("ASPNETCORE_URLS", $"http://{localhost}:8015")
	.WithEnvironment("ServiceUrls__CityService", $"http://{localhost}:8002")
	.WithEnvironment("ServiceUrls__CoworkingService", $"http://{localhost}:8006")
	.WithEnvironment(context =>
	{
		context.EnvironmentVariables["RabbitMQ__Host"] = rabbitMqHost;
		context.EnvironmentVariables["RabbitMQ__Port"] = rabbitMqPort;
		context.EnvironmentVariables["RabbitMQ__Username"] = rabbitMqUsername;
		context.EnvironmentVariables["RabbitMQ__Password"] = rabbitMqPassword;
		context.EnvironmentVariables["Elasticsearch__Url"] = $"http://{elasticsearchHost}:{elasticsearchPort}";
	});

builder.Build().Run();
