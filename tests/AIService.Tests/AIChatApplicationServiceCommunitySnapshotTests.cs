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

public class AIChatApplicationServiceCommunitySnapshotTests
{
    [Fact]
    public async Task GetCommunitySnapshotAsync_WhenDownstreamsAvailable_ReturnsMeetupsAndFieldNotes()
    {
        var userId = Guid.Parse("c8618bcb-4f2a-46d5-bb91-c5b24782d863");
        var planUpdatedAt = new DateTime(2026, 4, 6, 10, 0, 0, DateTimeKind.Utc);
        var meetupCreatedAt = new DateTime(2026, 4, 6, 11, 0, 0, DateTimeKind.Utc);
        var reviewCreatedAt = new DateTime(2026, 4, 6, 12, 0, 0, DateTimeKind.Utc);
        var prosConsCreatedAt = new DateTime(2026, 4, 6, 12, 30, 0, DateTimeKind.Utc);
        var handler = new StubHttpMessageHandler(request =>
        {
            var uri = request.RequestUri!.ToString();

            if (uri == "http://event-service/api/v1/events?status=upcoming&page=1&pageSize=3")
            {
                var responseBody = JsonSerializer.Serialize(new
                {
                    success = true,
                    message = "ok",
                    data = new
                    {
                        items = new[]
                        {
                            new
                            {
                                id = "meetup-1",
                                title = "Friday Cowork Sprint",
                                description = "Deep work with other nomads.",
                                cityId = "bangkok",
                                cityName = "Bangkok",
                                country = "Thailand",
                                venue = "The Work Loft",
                                address = "Ari, Bangkok",
                                startTime = "2026-04-08T09:00:00Z",
                                maxParticipants = 20,
                                participantCount = 12,
                                organizerId = userId.ToString(),
                                organizerName = "Walden",
                                isParticipant = false,
                                status = "upcoming",
                                createdAt = meetupCreatedAt
                            }
                        }
                    }
                });

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
                });
            }

            if (uri == "http://city-service/api/v1/cities/bangkok/user-content/reviews?page=1&pageSize=3")
            {
                var responseBody = JsonSerializer.Serialize(new
                {
                    success = true,
                    message = "ok",
                    data = new
                    {
                        items = new[]
                        {
                            new
                            {
                                id = Guid.NewGuid(),
                                userId = Guid.NewGuid(),
                                username = "Nomad Ada",
                                userAvatar = "https://example.com/avatar.png",
                                cityId = "bangkok",
                                rating = 4,
                                title = "One month in Ari",
                                content = "Quiet mornings, reliable Wi-Fi, easy BTS access.",
                                internetQualityScore = 5,
                                safetyScore = 4,
                                costScore = 4,
                                communityScore = 4,
                                weatherScore = 3,
                                createdAt = reviewCreatedAt,
                                photoUrls = new[] { "https://example.com/photo.png" }
                            }
                        }
                    }
                });

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
                });
            }

            if (uri == "http://city-service/api/v1/cities/bangkok/user-content/pros-cons")
            {
                var responseBody = JsonSerializer.Serialize(new
                {
                    success = true,
                    message = "ok",
                    data = new[]
                    {
                        new
                        {
                            id = Guid.NewGuid(),
                            userId = Guid.NewGuid(),
                            text = "Ari has enough cowork-friendly cafes to keep a full work week moving without changing neighborhoods.",
                            isPro = true,
                            upvotes = 8,
                            downvotes = 1,
                            createdAt = prosConsCreatedAt
                        },
                        new
                        {
                            id = Guid.NewGuid(),
                            userId = Guid.NewGuid(),
                            text = "Late afternoon traffic can make cross-city meetup logistics slower than they look on a map.",
                            isPro = false,
                            upvotes = 3,
                            downvotes = 0,
                            createdAt = prosConsCreatedAt.AddMinutes(-10)
                        }
                    }
                });

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
                });
            }

            throw new InvalidOperationException($"Unexpected request URI: {uri}");
        });

        var service = CreateService(
            new List<AiTravelPlan>
            {
                CreatePlan(userId, "bangkok", "Bangkok", planUpdatedAt)
            },
            handler);

        var result = await service.GetCommunitySnapshotAsync(userId);

        Assert.Equal("Bangkok", result.FocusCity);
        Assert.Equal("Bangkok", result.NextCoordinationCity);
        Assert.Single(result.UpcomingMeetups);
        Assert.Single(result.FieldNotes);
        Assert.Single(result.Questions);
        Assert.Equal(2, result.Recommendations.Count);
        Assert.Equal("Friday Cowork Sprint", result.UpcomingMeetups[0].Title);
        Assert.Equal("One month in Ari", result.FieldNotes[0].Title);
        Assert.Equal("One month in Ari", result.Questions[0].Title);
        Assert.Equal(3, result.Questions[0].Answers.Count);
        Assert.Equal(prosConsCreatedAt, result.LastUpdatedAt);

        Assert.Equal(3, handler.Requests.Count);
        Assert.All(handler.Requests, request =>
        {
            Assert.True(request.Headers.TryGetValues("X-User-Id", out var values));
            Assert.Contains(userId.ToString(), values!);
        });
    }

    [Fact]
    public async Task GetCommunitySnapshotAsync_WhenMeetupsFail_ReturnsFieldNotesOnly()
    {
        var userId = Guid.Parse("e4d8db47-b4f8-4f96-9013-6830c41201cb");
        var reviewCreatedAt = new DateTime(2026, 4, 6, 9, 0, 0, DateTimeKind.Utc);
        var handler = new StubHttpMessageHandler(request =>
        {
            var uri = request.RequestUri!.ToString();

            if (uri == "http://event-service/api/v1/events?status=upcoming&page=1&pageSize=3")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    Content = new StringContent("downstream unavailable", Encoding.UTF8, "text/plain")
                });
            }

            if (uri == "http://city-service/api/v1/cities/lisbon/user-content/reviews?page=1&pageSize=3")
            {
                var responseBody = JsonSerializer.Serialize(new
                {
                    success = true,
                    message = "ok",
                    data = new
                    {
                        items = new[]
                        {
                            new
                            {
                                id = Guid.NewGuid(),
                                userId = Guid.NewGuid(),
                                username = "Nomad Rui",
                                cityId = "lisbon",
                                rating = 5,
                                title = "Spring week in Lisbon",
                                content = "Sunny mornings and strong cafe network.",
                                createdAt = reviewCreatedAt,
                                photoUrls = Array.Empty<string>()
                            }
                        }
                    }
                });

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
                });
            }

            if (uri == "http://city-service/api/v1/cities/lisbon/user-content/pros-cons")
            {
                var responseBody = JsonSerializer.Serialize(new
                {
                    success = true,
                    message = "ok",
                    data = Array.Empty<object>()
                });

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
                });
            }

            throw new InvalidOperationException($"Unexpected request URI: {uri}");
        });

        var service = CreateService(
            new List<AiTravelPlan>
            {
                CreatePlan(userId, "lisbon", "Lisbon", new DateTime(2026, 4, 6, 8, 0, 0, DateTimeKind.Utc))
            },
            handler);

        var result = await service.GetCommunitySnapshotAsync(userId);

        Assert.Equal("Lisbon", result.FocusCity);
        Assert.Empty(result.UpcomingMeetups);
        Assert.Single(result.FieldNotes);
        Assert.Single(result.Questions);
        Assert.Single(result.Recommendations);
        Assert.Equal("Spring week in Lisbon", result.FieldNotes[0].Title);
        Assert.Equal(reviewCreatedAt, result.LastUpdatedAt);
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
                ["ServiceUrls:EventService"] = "http://event-service",
                ["ServiceUrls:CityService"] = "http://city-service"
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

    private static AiTravelPlan CreatePlan(Guid userId, string cityId, string cityName, DateTime updatedAt)
    {
        return new AiTravelPlan
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CityId = cityId,
            CityName = cityName,
            Duration = 30,
            BudgetLevel = "medium",
            TravelStyle = "culture",
            DepartureDate = updatedAt.AddDays(14),
            Status = "published",
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

        public List<HttpRequestMessage> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return _handler(request);
        }
    }
}