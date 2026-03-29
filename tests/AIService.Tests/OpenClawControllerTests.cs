using AIService.API.Controllers;
using AIService.Application.DTOs;
using AIService.Application.Services;
using GoNomads.Shared.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AIService.Tests;

public class OpenClawControllerTests
{
    private static readonly Guid UserId = Guid.Parse("12345678-1234-1234-1234-1234567890ab");

    [Fact]
    public async Task Research_WhenUserIsMissing_ReturnsUnauthorized()
    {
        var openClawService = new Mock<IOpenClawService>();
        var controller = CreateController(openClawService);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await controller.Research(new OpenClawResearchRequest
        {
            CityName = "Tokyo",
            Duration = 5
        });

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var payload = Assert.IsType<ApiResponse<OpenClawResearchResponse>>(unauthorized.Value);
        Assert.False(payload.Success);
        Assert.Equal("用户未认证", payload.Message);
        openClawService.Verify(service => service.ResearchTravelPlanAsync(It.IsAny<OpenClawResearchRequest>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task Research_WhenCityNameIsEmpty_ReturnsBadRequest()
    {
        var openClawService = new Mock<IOpenClawService>();
        var controller = CreateController(openClawService);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = CreateHttpContext(UserId)
        };

        var result = await controller.Research(new OpenClawResearchRequest
        {
            CityName = "  ",
            Duration = 5
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var payload = Assert.IsType<ApiResponse<OpenClawResearchResponse>>(badRequest.Value);
        Assert.False(payload.Success);
        Assert.Equal("目的地不能为空", payload.Message);
        openClawService.Verify(service => service.ResearchTravelPlanAsync(It.IsAny<OpenClawResearchRequest>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task Research_WhenDurationIsInvalid_ReturnsBadRequest()
    {
        var openClawService = new Mock<IOpenClawService>();
        var controller = CreateController(openClawService);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = CreateHttpContext(UserId)
        };

        var result = await controller.Research(new OpenClawResearchRequest
        {
            CityName = "Tokyo",
            Duration = 0
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var payload = Assert.IsType<ApiResponse<OpenClawResearchResponse>>(badRequest.Value);
        Assert.False(payload.Success);
        Assert.Equal("行程天数必须大于 0", payload.Message);
        openClawService.Verify(service => service.ResearchTravelPlanAsync(It.IsAny<OpenClawResearchRequest>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task Research_WhenRequestIsValid_UsesDerivedSessionKeyAndReturnsSuccess()
    {
        var openClawService = new Mock<IOpenClawService>();
        var controller = CreateController(openClawService);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = CreateHttpContext(UserId)
        };

        var request = new OpenClawResearchRequest
        {
            CityName = "Tokyo",
            Duration = 5,
            PlanningMode = "balanced",
            PlanningObjective = "explore",
            SessionId = "client-session",
            Interests = ["coffee"]
        };

        OpenClawResearchRequest? forwardedRequest = null;
        string? forwardedSessionId = null;

        openClawService
            .Setup(service => service.ResearchTravelPlanAsync(It.IsAny<OpenClawResearchRequest>(), It.IsAny<string?>()))
            .Callback<OpenClawResearchRequest, string?>((dto, sessionId) =>
            {
                forwardedRequest = dto;
                forwardedSessionId = sessionId;
            })
            .ReturnsAsync(new OpenClawResearchResponse
            {
                SessionKey = "server-session",
                Summary = "summary",
                Insights = ["insight"],
                Checks = ["check"],
                RawResponse = "raw"
            });

        var result = await controller.Research(request);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<ApiResponse<OpenClawResearchResponse>>(ok.Value);

        Assert.True(payload.Success);
        Assert.Equal("研究完成", payload.Message);
        Assert.NotNull(payload.Data);
        Assert.Equal("summary", payload.Data!.Summary);

        Assert.Same(request, forwardedRequest);
        var expectedSessionId = OpenClawSessionKeyFactory.BuildUserScopedSessionKey(
            UserId,
            request.SessionId,
            "travel-research-Tokyo-balanced-explore");
        Assert.Equal(expectedSessionId, forwardedSessionId);
        Assert.NotEqual(request.SessionId, forwardedSessionId);
    }

    private static OpenClawController CreateController(Mock<IOpenClawService> openClawService)
    {
        return new OpenClawController(
            openClawService.Object,
            NullLogger<OpenClawController>.Instance);
    }

    private static HttpContext CreateHttpContext(Guid userId)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-User-Id"] = userId.ToString();
        return context;
    }
}