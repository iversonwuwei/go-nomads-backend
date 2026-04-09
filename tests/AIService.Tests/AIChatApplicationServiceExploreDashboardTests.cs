using System.Net;
using System.Text;
using System.Text.Json;
using AIService.Application.DTOs;
using AIService.Application.Services;
using AIService.Domain.Entities;
using AIService.Domain.Repositories;
using GoNomads.Shared.Communication;
using MassTransit;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;
using Xunit;

namespace AIService.Tests;

public class AIChatApplicationServiceExploreDashboardTests
{
    [Fact]
    public async Task GetExploreDashboardAsync_WhenInboxSummaryAvailable_ReturnsAggregatedDataAndPropagatesUserHeader()
    {
        var userId = Guid.Parse("8e60c56f-f640-417a-8c8f-f46f1e290f77");
        var latestNotificationAt = new DateTime(2026, 4, 6, 12, 0, 0, DateTimeKind.Utc);
        var handler = new StubHttpMessageHandler(request =>
        {
            var responseBody = JsonSerializer.Serialize(new
            {
                success = true,
                message = "ok",
                data = new
                {
                    unreadNotifications = 4,
                    totalNotifications = 9,
                    actionRequiredCount = 2,
                    latestNotificationAt,
                    recentNotifications = new[]
                    {
                        new
                        {
                            id = "notif-1",
                            userId = userId.ToString(),
                            title = "Review visa reminder",
                            message = "Visa follow-up is due.",
                            type = "reminder",
                            relatedId = "visa-1",
                            metadata = new { source = "test" },
                            isRead = false,
                            createdAt = latestNotificationAt,
                            readAt = (DateTime?)null
                        }
                    }
                }
            });

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            });
        });

        var plans = new List<AiTravelPlan>
        {
            CreatePlan(
                userId,
                cityId: "bangkok",
                cityName: "Bangkok",
                status: "published",
                budgetLevel: "medium",
                travelStyle: "culture",
                departureDate: new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc),
                updatedAt: new DateTime(2026, 4, 6, 10, 0, 0, DateTimeKind.Utc)),
            CreatePlan(
                userId,
                cityId: "chiang-mai",
                cityName: "Chiang Mai",
                status: "draft",
                budgetLevel: "low",
                travelStyle: "relaxation",
                departureDate: new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc),
                updatedAt: new DateTime(2026, 4, 5, 8, 0, 0, DateTimeKind.Utc))
        };

        var service = CreateService(plans, handler);

        var result = await service.GetExploreDashboardAsync(userId);

        Assert.NotNull(result.MigrationWorkspace);
        Assert.Equal(2, result.MigrationWorkspace!.TotalPlans);
        Assert.Equal(2, result.MigrationWorkspace.ActivePlans);
        Assert.Equal(1, result.MigrationWorkspace.DraftPlans);

        Assert.NotNull(result.BudgetCenter);
        Assert.Equal(2, result.BudgetCenter!.ActivePlanCount);

        Assert.NotNull(result.VisaCenter);
        Assert.Equal(2, result.VisaCenter!.ActiveProfileCount);

        Assert.NotNull(result.InboxSummary);
        Assert.Equal(4, result.InboxSummary!.UnreadNotifications);
        Assert.Single(result.InboxSummary.RecentNotifications);
        Assert.Equal(latestNotificationAt, result.LastUpdatedAt);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal(
            "http://message-service/api/v1/inbox/summary?recentLimit=5",
            handler.LastRequest.RequestUri!.ToString());
        Assert.True(handler.LastRequest.Headers.TryGetValues("X-User-Id", out var values));
        Assert.Contains(userId.ToString(), values!);
    }

    [Fact]
    public async Task GetExploreDashboardAsync_WhenInboxSummaryFails_ReturnsPartialSuccessWithNullInboxSummary()
    {
        var userId = Guid.Parse("26743d22-6097-4c80-8f73-9ff9fa1c956d");
        var handler = new StubHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("downstream unavailable", Encoding.UTF8, "text/plain")
            }));

        var plans = new List<AiTravelPlan>
        {
            CreatePlan(
                userId,
                cityId: "lisbon",
                cityName: "Lisbon",
                status: "published",
                budgetLevel: "high",
                travelStyle: "nightlife",
                departureDate: new DateTime(2026, 4, 18, 0, 0, 0, DateTimeKind.Utc),
                updatedAt: new DateTime(2026, 4, 6, 9, 30, 0, DateTimeKind.Utc))
        };

        var service = CreateService(plans, handler);

        var result = await service.GetExploreDashboardAsync(userId);

        Assert.NotNull(result.MigrationWorkspace);
        Assert.Equal(1, result.MigrationWorkspace!.TotalPlans);
        Assert.NotNull(result.BudgetCenter);
        Assert.NotNull(result.VisaCenter);
        Assert.Null(result.InboxSummary);
        Assert.Equal(new DateTime(2026, 4, 6, 9, 30, 0, DateTimeKind.Utc), result.LastUpdatedAt);
    }

    private static AIChatApplicationService CreateService(
        List<AiTravelPlan> plans,
        StubHttpMessageHandler handler)
    {
        var travelPlanRepository = new Mock<ITravelPlanRepository>();
        travelPlanRepository
            .Setup(repository => repository.GetByUserIdAsync(It.IsAny<Guid>(), 1, 20))
            .ReturnsAsync(plans);

        var httpClient = new HttpClient(handler);
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(factory => factory.CreateClient("ServiceInvocation"))
            .Returns(httpClient);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServiceUrls:MessageService"] = "http://message-service"
            })
            .Build();

        var serviceInvocationClient = new ServiceInvocationClient(
            httpClientFactory.Object,
            configuration,
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<ServiceInvocationClient>.Instance);

        return new AIChatApplicationService(
            Mock.Of<IAIConversationRepository>(),
            Mock.Of<IAIMessageRepository>(),
            travelPlanRepository.Object,
            CreateKernel(),
            NullLogger<AIChatApplicationService>.Instance,
            configuration,
            Mock.Of<IPublishEndpoint>(),
            serviceInvocationClient);
    }

    private static Kernel CreateKernel()
    {
        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton(Mock.Of<IChatCompletionService>());
        return builder.Build();
    }

    private static AiTravelPlan CreatePlan(
        Guid userId,
        string cityId,
        string cityName,
        string status,
        string budgetLevel,
        string travelStyle,
        DateTime departureDate,
        DateTime updatedAt)
    {
        return new AiTravelPlan
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CityId = cityId,
            CityName = cityName,
            Duration = 30,
            BudgetLevel = budgetLevel,
            TravelStyle = travelStyle,
            DepartureDate = departureDate,
            Status = status,
            PlanData = "{}",
            CreatedAt = updatedAt.AddHours(-2),
            UpdatedAt = updatedAt
        };
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return _handler(request);
        }
    }
}