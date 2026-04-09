using System.Net;
using System.Text;
using System.Text.Json;
using GoNomads.Shared.Communication;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UserService.Application.DTOs;
using UserService.Application.Services;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;
using Xunit;

namespace UserService.Tests;

public class ProfileSnapshotServiceTests
{
    [Fact]
    public async Task GetCurrentAsync_WhenDownstreamAvailable_ReturnsAggregatedSnapshotAndPropagatesUserHeader()
    {
        var userId = "2d9e7ce0-bb95-4d74-9d0f-1ed86a2ce111";
        var observedHeaders = new List<string?>();

        var service = CreateService(
            userId,
            handler: request =>
            {
                observedHeaders.Add(request.Headers.TryGetValues("X-User-Id", out var values)
                    ? values.SingleOrDefault()
                    : null);

                if (request.RequestUri!.AbsoluteUri.Contains("/user-favorite-cities/ids", StringComparison.Ordinal))
                {
                    return JsonResponse(new List<string> { "7b9b2c31-8c39-4f63-94fb-55b2f9be96fd", "other-city" });
                }

                if (request.RequestUri.AbsoluteUri.Contains("/ai/chat/travel-plans", StringComparison.Ordinal))
                {
                    return JsonResponse(new
                    {
                        success = true,
                        message = "ok",
                        data = new[]
                        {
                            new
                            {
                                id = Guid.Parse("f40d46db-f7e9-429d-9813-1a25a234f6d7"),
                                cityId = "7b9b2c31-8c39-4f63-94fb-55b2f9be96fd",
                                cityName = "Bangkok",
                                cityImage = "image.png",
                                duration = 30,
                                budgetLevel = "medium",
                                travelStyle = "culture",
                                status = "planning",
                                departureDate = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                                createdAt = new DateTime(2026, 4, 6, 12, 0, 0, DateTimeKind.Utc)
                            }
                        }
                    });
                }

                if (request.RequestUri.AbsoluteUri.Contains("/api/v1/cities/7b9b2c31-8c39-4f63-94fb-55b2f9be96fd", StringComparison.Ordinal))
                {
                    return JsonResponse(new
                    {
                        success = true,
                        message = "ok",
                        data = new
                        {
                            id = Guid.Parse("7b9b2c31-8c39-4f63-94fb-55b2f9be96fd"),
                            name = "Bangkok",
                            country = "Thailand",
                            timeZone = "Asia/Bangkok"
                        }
                    });
                }

                if (request.RequestUri.AbsoluteUri.Contains("/created/count", StringComparison.Ordinal))
                {
                    return JsonResponse(2);
                }

                if (request.RequestUri.AbsoluteUri.Contains("/joined/count", StringComparison.Ordinal))
                {
                    return JsonResponse(5);
                }

                throw new InvalidOperationException($"Unhandled request: {request.RequestUri}");
            });

        var result = await service.GetCurrentAsync(userId);

        Assert.NotNull(result);
        Assert.Equal(userId, result!.User.Id);
        Assert.Equal(2, result.FavoriteCityIds.Count);
        Assert.Equal("Bangkok", result.LatestTravelPlan!.CityName);
        Assert.Equal("Asia/Bangkok", result.NextDestinationCity!.Timezone);
        Assert.Equal(2, result.NomadStats.MeetupsCreated);
        Assert.Equal(5, result.NomadStats.MeetupsJoined);
        Assert.Equal(2, result.NomadStats.FavoriteCitiesCount);
        Assert.Equal(new DateTime(2026, 4, 6, 12, 0, 0, DateTimeKind.Utc), result.LastUpdatedAt);
        Assert.Contains(userId, observedHeaders);
    }

    [Fact]
    public async Task GetCurrentAsync_WhenAiServiceFails_ReturnsPartialSuccessWithoutLatestPlanOrCity()
    {
        var userId = "2d9e7ce0-bb95-4d74-9d0f-1ed86a2ce111";

        var service = CreateService(
            userId,
            handler: request =>
            {
                if (request.RequestUri!.AbsoluteUri.Contains("/user-favorite-cities/ids", StringComparison.Ordinal))
                {
                    return JsonResponse(new List<string> { "7b9b2c31-8c39-4f63-94fb-55b2f9be96fd" });
                }

                if (request.RequestUri.AbsoluteUri.Contains("/ai/chat/travel-plans", StringComparison.Ordinal))
                {
                    return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                    {
                        Content = new StringContent("ai unavailable", Encoding.UTF8, "text/plain")
                    };
                }

                if (request.RequestUri.AbsoluteUri.Contains("/created/count", StringComparison.Ordinal) ||
                    request.RequestUri.AbsoluteUri.Contains("/joined/count", StringComparison.Ordinal))
                {
                    return JsonResponse(0);
                }

                throw new InvalidOperationException($"Unhandled request: {request.RequestUri}");
            });

        var result = await service.GetCurrentAsync(userId);

        Assert.NotNull(result);
        Assert.Equal(userId, result!.User.Id);
        Assert.Single(result.FavoriteCityIds);
        Assert.Null(result.LatestTravelPlan);
        Assert.Null(result.NextDestinationCity);
        Assert.Equal(1, result.NomadStats.FavoriteCitiesCount);
    }

    private static ProfileSnapshotService CreateService(string userId, Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var userService = new Mock<IUserService>();
        var userStatsRepository = new Mock<IUserStatsRepository>();
        var travelHistoryService = new Mock<ITravelHistoryService>();
        var httpClientFactory = new Mock<IHttpClientFactory>();

        userService
            .Setup(service => service.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserDto
            {
                Id = userId,
                Name = "Walden",
                UpdatedAt = new DateTime(2026, 4, 6, 10, 0, 0, DateTimeKind.Utc),
                Membership = new UserMembershipDto
                {
                    Level = 1,
                    LevelName = "basic",
                    IsActive = true,
                    AiUsageThisMonth = 4,
                    AiUsageLimit = 30,
                    RemainingDays = 180,
                    CanUseAI = true
                }
            });

        userStatsRepository
            .Setup(repository => repository.GetOrCreateAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserStats
            {
                Id = "stats-1",
                UserId = userId,
                CountriesVisited = 3,
                CitiesLived = 2,
                DaysNomading = 120,
                TripsCompleted = 4,
                CreatedAt = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 4, 6, 9, 0, 0, DateTimeKind.Utc)
            });

        travelHistoryService
            .Setup(service => service.GetUserTravelStatsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TravelHistoryStats
            {
                CountriesVisited = 6,
                CitiesVisited = 4,
                TotalDays = 280,
                ConfirmedTrips = 9
            });

        httpClientFactory
            .Setup(factory => factory.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(new StubHttpMessageHandler(handler)));

        var configuration = new ConfigurationBuilder().Build();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var invocationClient = new ServiceInvocationClient(
            httpClientFactory.Object,
            configuration,
            memoryCache,
            NullLogger<ServiceInvocationClient>.Instance);

        return new ProfileSnapshotService(
            userService.Object,
            userStatsRepository.Object,
            travelHistoryService.Object,
            invocationClient,
            NullLogger<ProfileSnapshotService>.Instance);
    }

    private static HttpResponseMessage JsonResponse<T>(T payload)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }
}