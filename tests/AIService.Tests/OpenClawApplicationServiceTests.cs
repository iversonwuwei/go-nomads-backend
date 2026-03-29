using System.Net;
using System.Text;
using System.Text.Json;
using AIService.Application.DTOs;
using AIService.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AIService.Tests;

public class OpenClawApplicationServiceTests
{
    [Fact]
    public async Task ResearchTravelPlanAsync_WhenGatewayReturnsJson_ParsesStructuredResponseAndSendsSessionId()
    {
        const string sessionId = "gonomads-travel-research-123";
        var handler = new StubHttpMessageHandler(_ =>
        {
            var response = CreateChatCompletionResponse(
                """
                {"summary":"东京樱花季需要尽早锁定热门时段与交通票券","insights":["上野与目黑川清晨人流更可控","Suica 或 PASMO 交通卡更适合城市内高频移动"],"checks":["确认樱花开放周与出发日期是否错峰","检查热门展望台与主题餐厅预约窗口"]}
                """);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response, Encoding.UTF8, "application/json")
            });
        });

        var service = CreateService(handler);

        var result = await service.ResearchTravelPlanAsync(
            new OpenClawResearchRequest
            {
                CityName = "Tokyo",
                Duration = 5,
                Budget = "medium",
                TravelStyle = "culture",
                PlanningMode = "balanced",
                PlanningObjective = "explore",
                ResearchSignals = ["crowd", "transport"],
                Interests = ["sakura", "coffee"]
            },
            sessionId);

        Assert.Equal(sessionId, result.SessionKey);
        Assert.Equal("东京樱花季需要尽早锁定热门时段与交通票券", result.Summary);
        Assert.Equal(2, result.Insights.Count);
        Assert.Contains("上野与目黑川清晨人流更可控", result.Insights);
        Assert.Equal(2, result.Checks.Count);
        Assert.Contains("确认樱花开放周与出发日期是否错峰", result.Checks);
        Assert.Contains("summary", result.RawResponse);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("http://localhost:8080/v1/chat/completions", handler.LastRequest.RequestUri!.ToString());

        using var payloadDocument = JsonDocument.Parse(handler.LastRequestBody!);
        var root = payloadDocument.RootElement;
        Assert.Equal(sessionId, root.GetProperty("session_id").GetString());
        Assert.Equal("openclaw:main", root.GetProperty("model").GetString());
        Assert.Equal(1200, root.GetProperty("max_tokens").GetInt32());
    }

    [Fact]
    public async Task ResearchTravelPlanAsync_WhenGatewayReturnsNonJson_FallsBackToCollapsedSummary()
    {
        const string sessionId = "gonomads-travel-research-fallback";
        const string rawResponse = "东京适合把浅草和上野放在同一天。\n请优先确认博物馆闭馆日，再安排夜景与餐厅预约。";

        var handler = new StubHttpMessageHandler(_ =>
        {
            var response = CreateChatCompletionResponse(rawResponse);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response, Encoding.UTF8, "application/json")
            });
        });

        var service = CreateService(handler);

        var result = await service.ResearchTravelPlanAsync(
            new OpenClawResearchRequest
            {
                CityName = "Tokyo",
                Duration = 4,
                Budget = "high",
                TravelStyle = "urban",
                PlanningMode = "fast-paced",
                PlanningObjective = "food"
            },
            sessionId);

        Assert.Equal(sessionId, result.SessionKey);
        Assert.Equal(
            "东京适合把浅草和上野放在同一天。 请优先确认博物馆闭馆日，再安排夜景与餐厅预约。",
            result.Summary);
        Assert.Empty(result.Insights);
        Assert.Empty(result.Checks);
        Assert.Equal(rawResponse, result.RawResponse);
    }

    private static OpenClawApplicationService CreateService(StubHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8080")
        };

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(factory => factory.CreateClient("OpenClawClient"))
            .Returns(httpClient);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenClaw:BaseUrl"] = "http://localhost:8080"
            })
            .Build();

        return new OpenClawApplicationService(
            httpClientFactory.Object,
            configuration,
            NullLogger<OpenClawApplicationService>.Instance);
    }

    private static string CreateChatCompletionResponse(string content)
    {
        return JsonSerializer.Serialize(new
        {
            id = "chatcmpl-test",
            @object = "chat.completion",
            created = 1,
            choices = new[]
            {
                new
                {
                    index = 0,
                    message = new
                    {
                        role = "assistant",
                        content
                    },
                    finish_reason = "stop"
                }
            }
        });
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastRequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return await _handler(request);
        }
    }
}