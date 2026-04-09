using System.Net;
using System.Text;
using System.Text.Json;
using GoNomads.Shared.Communication;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UserService.Application.Services;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;
using UserService.Infrastructure.Services;
using Xunit;

namespace UserService.Tests;

public class ReportServiceTests
{
    [Fact]
    public async Task GetReportByIdAsync_WhenCityTargetExists_ReturnsResolvedReporterAndTargetDisplay()
    {
        var report = BuildReport(contentType: "city", targetId: "city-1", targetNameSnapshot: "旧城市快照");
        var reportRepository = new Mock<IReportRepository>();
        var userRepository = new Mock<IUserRepository>();
        var cityServiceClient = new Mock<ICityServiceClient>();

        reportRepository
            .Setup(repository => repository.GetByIdAsync(report.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        userRepository
            .Setup(repository => repository.GetByIdAsync(report.ReporterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = report.ReporterId,
                Name = "Walden",
                Email = "walden@gonomads.app"
            });

        cityServiceClient
            .Setup(client => client.GetCityDetailAsync(report.TargetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CityDetailDto
            {
                Id = report.TargetId,
                Name = "Bangkok",
                Country = "Thailand"
            });

        var service = CreateService(reportRepository, userRepository, cityServiceClient, _ => throw new InvalidOperationException("unexpected downstream call"));

        var result = await service.GetReportByIdAsync(report.Id);

        Assert.NotNull(result);
        Assert.Equal("Walden", result!.ReporterDisplayName);
        Assert.Equal("walden@gonomads.app", result.ReporterSummary);
        Assert.Equal("Bangkok", result.TargetDisplayName);
        Assert.Equal("Thailand", result.TargetSummary);
        Assert.Equal("Bangkok", result.TargetName);
        Assert.Equal("Walden", result.ReporterName);
    }

    [Fact]
    public async Task GetReportByIdAsync_WhenEventTargetExists_ReturnsDownstreamSummary()
    {
        var report = BuildReport(contentType: "event", targetId: "meetup-1", targetNameSnapshot: "旧活动快照");
        var reportRepository = new Mock<IReportRepository>();
        var userRepository = new Mock<IUserRepository>();
        var cityServiceClient = new Mock<ICityServiceClient>();

        reportRepository
            .Setup(repository => repository.GetByIdAsync(report.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        userRepository
            .Setup(repository => repository.GetByIdAsync(report.ReporterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var service = CreateService(
            reportRepository,
            userRepository,
            cityServiceClient,
            request =>
            {
                if (request.RequestUri!.AbsoluteUri.Contains("/api/v1/events/meetup-1", StringComparison.Ordinal))
                {
                    return JsonResponse(new
                    {
                        success = true,
                        data = new
                        {
                            title = "Nomad Meetup",
                            organizerName = "Alice"
                        }
                    });
                }

                throw new InvalidOperationException($"Unhandled request: {request.RequestUri}");
            });

        var result = await service.GetReportByIdAsync(report.Id);

        Assert.NotNull(result);
        Assert.Equal("Nomad Meetup", result!.TargetDisplayName);
        Assert.Equal("Alice", result.TargetSummary);
        Assert.Equal(report.ReporterNameSnapshot, result.ReporterDisplayName);
    }

    [Fact]
    public async Task GetReportByIdAsync_WhenDownstreamFails_FallsBackToSnapshots()
    {
        var report = BuildReport(contentType: "innovation", targetId: "project-1", targetNameSnapshot: "创新项目快照");
        var reportRepository = new Mock<IReportRepository>();
        var userRepository = new Mock<IUserRepository>();
        var cityServiceClient = new Mock<ICityServiceClient>();

        reportRepository
            .Setup(repository => repository.GetByIdAsync(report.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        userRepository
            .Setup(repository => repository.GetByIdAsync(report.ReporterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var service = CreateService(
            reportRepository,
            userRepository,
            cityServiceClient,
            _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("downstream unavailable", Encoding.UTF8, "text/plain")
            });

        var result = await service.GetReportByIdAsync(report.Id);

        Assert.NotNull(result);
        Assert.Equal(report.ReporterNameSnapshot, result!.ReporterDisplayName);
        Assert.Equal(report.TargetNameSnapshot, result.TargetDisplayName);
        Assert.Equal(string.Empty, result.TargetSummary);
    }

    private static ReportService CreateService(
        Mock<IReportRepository> reportRepository,
        Mock<IUserRepository> userRepository,
        Mock<ICityServiceClient> cityServiceClient,
        Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(factory => factory.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(new StubHttpMessageHandler(handler)));

        var invocationClient = new ServiceInvocationClient(
            httpClientFactory.Object,
            new ConfigurationBuilder().Build(),
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<ServiceInvocationClient>.Instance);

        return new ReportService(
            reportRepository.Object,
            userRepository.Object,
            cityServiceClient.Object,
            invocationClient,
            NullLogger<ReportService>.Instance);
    }

    private static Report BuildReport(string contentType, string targetId, string targetNameSnapshot)
    {
        return new Report
        {
            Id = "report-1",
            ReporterId = "user-1",
            ReporterNameSnapshot = "举报人快照",
            ContentType = contentType,
            TargetId = targetId,
            TargetNameSnapshot = targetNameSnapshot,
            ReasonId = "spam",
            ReasonLabel = "垃圾内容",
            Status = Report.Statuses.Pending,
            CreatedAt = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)
        };
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