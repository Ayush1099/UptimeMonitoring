using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using UptimeMonitoring.Api.Controllers;
using UptimeMonitoring.Application.DTOs;
using UptimeMonitoring.Application.Interfaces;
using UptimeMonitoring.Application.Services;
using Xunit;

namespace UptimeMonitoring.Tests.Controllers;

public class DashboardControllerTests
{
    private readonly Mock<IWebsiteRepository> _mockWebsiteRepository;
    private readonly Mock<IMonitoringResultRepository> _mockResultRepository;
    private readonly DashboardService _service;
    private readonly DashboardController _controller;
    private readonly Guid _testUserId = Guid.NewGuid();

    public DashboardControllerTests()
    {
        _mockWebsiteRepository = new Mock<IWebsiteRepository>();
        _mockResultRepository = new Mock<IMonitoringResultRepository>();
        _service = new DashboardService(_mockWebsiteRepository.Object, _mockResultRepository.Object);
        _controller = new DashboardController(_service);
        SetupAuthenticatedUser();
    }

    private void SetupAuthenticatedUser()
    {
        var claims = new List<Claim> { new Claim("sub", _testUserId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task GetStatus_ReturnsDashboardStatusList()
    {
        var websiteId = Guid.NewGuid();
        var website = new Domain.Entities.Website 
        { 
            Id = websiteId, 
            UserId = _testUserId, 
            Url = "https://example.com", 
            IsActive = true 
        };
        var latestResult = new Domain.Entities.MonitoringResult 
        { 
            Id = Guid.NewGuid(), 
            WebsiteId = websiteId, 
            IsUp = true, 
            ResponseTimeMs = 100, 
            CheckedAt = DateTime.UtcNow 
        };

        _mockWebsiteRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(new List<Domain.Entities.Website> { website });
        _mockResultRepository.Setup(r => r.GetLatestByWebsiteIdAsync(websiteId))
            .ReturnsAsync(latestResult);
        _mockResultRepository.Setup(r => r.GetStatsAsync(websiteId, It.IsAny<DateTime>()))
            .ReturnsAsync((10, 9));

        var result = await _controller.GetStatus();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<List<DashboardWebsiteStatusResponse>>().Subject;
        response.Should().HaveCount(1);
        response[0].Status.Should().Be("UP");
    }

    [Fact]
    public async Task GetStatus_ExtractsUserIdFromClaims()
    {
        _mockWebsiteRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(new List<Domain.Entities.Website>());

        await _controller.GetStatus();

        _mockWebsiteRepository.Verify(r => r.GetByUserIdAsync(_testUserId), Times.Once);
    }

    [Fact]
    public async Task GetStatus_ResponseMapping()
    {
        var websiteId = Guid.NewGuid();
        var website = new Domain.Entities.Website 
        { 
            Id = websiteId, 
            UserId = _testUserId, 
            Url = "https://example.com", 
            IsActive = true 
        };
        var latestResult = new Domain.Entities.MonitoringResult 
        { 
            Id = Guid.NewGuid(), 
            WebsiteId = websiteId, 
            IsUp = false, 
            ResponseTimeMs = 5000, 
            CheckedAt = DateTime.UtcNow 
        };

        _mockWebsiteRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(new List<Domain.Entities.Website> { website });
        _mockResultRepository.Setup(r => r.GetLatestByWebsiteIdAsync(websiteId))
            .ReturnsAsync(latestResult);
        _mockResultRepository.Setup(r => r.GetStatsAsync(websiteId, It.IsAny<DateTime>()))
            .ReturnsAsync((10, 5));

        var result = await _controller.GetStatus();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<List<DashboardWebsiteStatusResponse>>().Subject;
        response[0].WebsiteId.Should().Be(websiteId);
        response[0].Status.Should().Be("DOWN");
        response[0].UptimePercentage.Should().Be(50.0);
    }
}
