using FluentAssertions;
using Moq;
using UptimeMonitoring.Application.DTOs;
using UptimeMonitoring.Application.Interfaces;
using UptimeMonitoring.Application.Services;
using UptimeMonitoring.Domain.Entities;
using Xunit;

namespace UptimeMonitoring.Tests.Services;

public class DashboardServiceTests
{
    private readonly Mock<IWebsiteRepository> _mockWebsiteRepository;
    private readonly Mock<IMonitoringResultRepository> _mockResultRepository;
    private readonly DashboardService _service;

    public DashboardServiceTests()
    {
        _mockWebsiteRepository = new Mock<IWebsiteRepository>();
        _mockResultRepository = new Mock<IMonitoringResultRepository>();
        _service = new DashboardService(_mockWebsiteRepository.Object, _mockResultRepository.Object);
    }

    [Fact]
    public async Task GetStatusAsync_ActiveWebsiteWithResults_ReturnsUpStatus()
    {
        var userId = Guid.NewGuid();
        var websiteId = Guid.NewGuid();
        var website = new Website 
        { 
            Id = websiteId, 
            UserId = userId, 
            Url = "https://example.com", 
            IsActive = true 
        };
        var latestResult = new MonitoringResult 
        { 
            Id = Guid.NewGuid(), 
            WebsiteId = websiteId, 
            IsUp = true, 
            ResponseTimeMs = 100, 
            CheckedAt = DateTime.UtcNow 
        };

        _mockWebsiteRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Website> { website });
        _mockResultRepository.Setup(r => r.GetLatestByWebsiteIdAsync(websiteId))
            .ReturnsAsync(latestResult);
        _mockResultRepository.Setup(r => r.GetStatsAsync(websiteId, It.IsAny<DateTime>()))
            .ReturnsAsync((10, 9));

        var result = await _service.GetStatusAsync(userId);

        result.Should().HaveCount(1);
        result[0].WebsiteId.Should().Be(websiteId);
        result[0].Url.Should().Be(website.Url);
        result[0].Status.Should().Be("UP");
        result[0].ResponseTimeMs.Should().Be(100);
        result[0].UptimePercentage.Should().Be(90.0);
    }

    [Fact]
    public async Task GetStatusAsync_ActiveWebsiteDown_ReturnsDownStatus()
    {
        var userId = Guid.NewGuid();
        var websiteId = Guid.NewGuid();
        var website = new Website 
        { 
            Id = websiteId, 
            UserId = userId, 
            Url = "https://example.com", 
            IsActive = true 
        };
        var latestResult = new MonitoringResult 
        { 
            Id = Guid.NewGuid(), 
            WebsiteId = websiteId, 
            IsUp = false, 
            ResponseTimeMs = 5000, 
            CheckedAt = DateTime.UtcNow 
        };

        _mockWebsiteRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Website> { website });
        _mockResultRepository.Setup(r => r.GetLatestByWebsiteIdAsync(websiteId))
            .ReturnsAsync(latestResult);
        _mockResultRepository.Setup(r => r.GetStatsAsync(websiteId, It.IsAny<DateTime>()))
            .ReturnsAsync((10, 5));

        var result = await _service.GetStatusAsync(userId);

        result.Should().HaveCount(1);
        result[0].Status.Should().Be("DOWN");
        result[0].UptimePercentage.Should().Be(50.0);
    }

    [Fact]
    public async Task GetStatusAsync_InactiveWebsite_ReturnsPausedStatus()
    {
        var userId = Guid.NewGuid();
        var websiteId = Guid.NewGuid();
        var website = new Website 
        { 
            Id = websiteId, 
            UserId = userId, 
            Url = "https://example.com", 
            IsActive = false 
        };

        _mockWebsiteRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Website> { website });

        var result = await _service.GetStatusAsync(userId);

        result.Should().HaveCount(1);
        result[0].Status.Should().Be("PAUSED");
        result[0].UptimePercentage.Should().BeNull();
        _mockResultRepository.Verify(r => r.GetLatestByWebsiteIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetStatusAsync_NoLatestResult_ReturnsUnknownStatus()
    {
        var userId = Guid.NewGuid();
        var websiteId = Guid.NewGuid();
        var website = new Website 
        { 
            Id = websiteId, 
            UserId = userId, 
            Url = "https://example.com", 
            IsActive = true 
        };

        _mockWebsiteRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Website> { website });
        _mockResultRepository.Setup(r => r.GetLatestByWebsiteIdAsync(websiteId))
            .ReturnsAsync((MonitoringResult?)null);
        _mockResultRepository.Setup(r => r.GetStatsAsync(websiteId, It.IsAny<DateTime>()))
            .ReturnsAsync((0, 0));

        var result = await _service.GetStatusAsync(userId);

        result.Should().HaveCount(1);
        result[0].Status.Should().Be("UNKNOWN");
        result[0].UptimePercentage.Should().BeNull();
    }

    [Fact]
    public async Task GetStatusAsync_MultipleWebsites_ReturnsAllStatuses()
    {
        var userId = Guid.NewGuid();
        var website1Id = Guid.NewGuid();
        var website2Id = Guid.NewGuid();
        var websites = new List<Website>
        {
            new Website { Id = website1Id, UserId = userId, Url = "https://example.com", IsActive = true },
            new Website { Id = website2Id, UserId = userId, Url = "https://test.com", IsActive = false }
        };

        var latestResult1 = new MonitoringResult 
        { 
            Id = Guid.NewGuid(), 
            WebsiteId = website1Id, 
            IsUp = true, 
            ResponseTimeMs = 100, 
            CheckedAt = DateTime.UtcNow 
        };

        _mockWebsiteRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(websites);
        _mockResultRepository.Setup(r => r.GetLatestByWebsiteIdAsync(website1Id))
            .ReturnsAsync(latestResult1);
        _mockResultRepository.Setup(r => r.GetStatsAsync(website1Id, It.IsAny<DateTime>()))
            .ReturnsAsync((20, 18));

        var result = await _service.GetStatusAsync(userId);

        result.Should().HaveCount(2);
        result[0].Status.Should().Be("UP");
        result[1].Status.Should().Be("PAUSED");
    }

    [Fact]
    public async Task GetStatusAsync_UptimeCalculation_RoundsToTwoDecimals()
    {
        var userId = Guid.NewGuid();
        var websiteId = Guid.NewGuid();
        var website = new Website 
        { 
            Id = websiteId, 
            UserId = userId, 
            Url = "https://example.com", 
            IsActive = true 
        };
        var latestResult = new MonitoringResult 
        { 
            Id = Guid.NewGuid(), 
            WebsiteId = websiteId, 
            IsUp = true, 
            ResponseTimeMs = 100, 
            CheckedAt = DateTime.UtcNow 
        };

        _mockWebsiteRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Website> { website });
        _mockResultRepository.Setup(r => r.GetLatestByWebsiteIdAsync(websiteId))
            .ReturnsAsync(latestResult);
        _mockResultRepository.Setup(r => r.GetStatsAsync(websiteId, It.IsAny<DateTime>()))
            .ReturnsAsync((3, 2));

        var result = await _service.GetStatusAsync(userId);

        result[0].UptimePercentage.Should().Be(66.67);
    }

    [Fact]
    public async Task GetStatusAsync_Uses24HourWindow()
    {
        var userId = Guid.NewGuid();
        var websiteId = Guid.NewGuid();
        var website = new Website 
        { 
            Id = websiteId, 
            UserId = userId, 
            Url = "https://example.com", 
            IsActive = true 
        };
        var latestResult = new MonitoringResult 
        { 
            Id = Guid.NewGuid(), 
            WebsiteId = websiteId, 
            IsUp = true, 
            ResponseTimeMs = 100, 
            CheckedAt = DateTime.UtcNow 
        };

        _mockWebsiteRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Website> { website });
        _mockResultRepository.Setup(r => r.GetLatestByWebsiteIdAsync(websiteId))
            .ReturnsAsync(latestResult);
        _mockResultRepository.Setup(r => r.GetStatsAsync(websiteId, It.IsAny<DateTime>()))
            .ReturnsAsync((10, 9));

        await _service.GetStatusAsync(userId);

        _mockResultRepository.Verify(r => r.GetStatsAsync(
            websiteId, 
            It.Is<DateTime>(d => d <= DateTime.UtcNow.AddHours(-24) && d > DateTime.UtcNow.AddHours(-25))), 
            Times.Once);
    }
}
