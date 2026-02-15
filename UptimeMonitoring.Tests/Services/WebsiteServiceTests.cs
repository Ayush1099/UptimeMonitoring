using FluentAssertions;
using Moq;
using UptimeMonitoring.Application.Common;
using UptimeMonitoring.Application.Interfaces;
using UptimeMonitoring.Application.Services;
using UptimeMonitoring.Domain.Entities;
using Xunit;

namespace UptimeMonitoring.Tests.Services;

public class WebsiteServiceTests
{
    private readonly Mock<IWebsiteRepository> _mockRepository;
    private readonly Mock<IAlertStateStore> _mockAlertStateStore;
    private readonly WebsiteService _service;

    public WebsiteServiceTests()
    {
        _mockRepository = new Mock<IWebsiteRepository>();
        _mockAlertStateStore = new Mock<IAlertStateStore>();
        _service = new WebsiteService(_mockRepository.Object, _mockAlertStateStore.Object);
    }

    [Fact]
    public async Task AddWebsiteAsync_ValidInput_ReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        var url = "https://example.com";
        var intervalMinutes = 5;

        _mockRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Website>());

        var result = await _service.AddWebsiteAsync(userId, url, intervalMinutes);

        result.IsSuccess.Should().BeTrue();
        _mockRepository.Verify(r => r.AddAsync(It.Is<Website>(w => 
            w.UserId == userId && 
            w.Url == url && 
            w.CheckIntervalMinutes == intervalMinutes &&
            w.IsActive == true)), Times.Once);
    }

    [Fact]
    public async Task AddWebsiteAsync_EmptyUrl_ReturnsValidationError()
    {
        var userId = Guid.NewGuid();
        var result = await _service.AddWebsiteAsync(userId, "", 5);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
        result.Error.Message.Should().Contain("URL is required");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Website>()), Times.Never);
    }

    [Fact]
    public async Task AddWebsiteAsync_NullUrl_ReturnsValidationError()
    {
        var userId = Guid.NewGuid();
        var result = await _service.AddWebsiteAsync(userId, null!, 5);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Website>()), Times.Never);
    }

    [Fact]
    public async Task AddWebsiteAsync_InvalidUrlFormat_ReturnsValidationError()
    {
        var userId = Guid.NewGuid();
        var result = await _service.AddWebsiteAsync(userId, "not-a-valid-url", 5);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
        result.Error.Message.Should().Contain("valid HTTP or HTTPS URL");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Website>()), Times.Never);
    }

    [Fact]
    public async Task AddWebsiteAsync_InvalidIntervalTooLow_ReturnsValidationError()
    {
        var userId = Guid.NewGuid();
        var result = await _service.AddWebsiteAsync(userId, "https://example.com", 0);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
        result.Error.Message.Should().Contain("between 1 and 1440 minutes");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Website>()), Times.Never);
    }

    [Fact]
    public async Task AddWebsiteAsync_InvalidIntervalTooHigh_ReturnsValidationError()
    {
        var userId = Guid.NewGuid();
        var result = await _service.AddWebsiteAsync(userId, "https://example.com", 1441);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
        result.Error.Message.Should().Contain("between 1 and 1440 minutes");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Website>()), Times.Never);
    }

    [Fact]
    public async Task AddWebsiteAsync_DuplicateUrl_ReturnsConflictError()
    {
        var userId = Guid.NewGuid();
        var url = "https://example.com";
        var existingWebsite = new Website { Id = Guid.NewGuid(), UserId = userId, Url = url };

        _mockRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Website> { existingWebsite });

        var result = await _service.AddWebsiteAsync(userId, url, 5);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Conflict");
        result.Error.Message.Should().Contain("already exists");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Website>()), Times.Never);
    }

    [Fact]
    public async Task AddWebsiteAsync_DuplicateUrlCaseInsensitive_ReturnsConflictError()
    {
        var userId = Guid.NewGuid();
        var existingWebsite = new Website { Id = Guid.NewGuid(), UserId = userId, Url = "https://EXAMPLE.com" };

        _mockRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Website> { existingWebsite });

        var result = await _service.AddWebsiteAsync(userId, "https://example.com", 5);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Conflict");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Website>()), Times.Never);
    }

    [Fact]
    public async Task GetUserWebsitesAsync_ReturnsWebsites()
    {
        var userId = Guid.NewGuid();
        var websites = new List<Website>
        {
            new Website { Id = Guid.NewGuid(), UserId = userId, Url = "https://example.com" },
            new Website { Id = Guid.NewGuid(), UserId = userId, Url = "https://test.com" }
        };

        _mockRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(websites);

        var result = await _service.GetUserWebsitesAsync(userId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().BeEquivalentTo(websites);
    }

    [Fact]
    public async Task GetUserWebsitesAsync_NoWebsites_ReturnsEmptyList()
    {
        var userId = Guid.NewGuid();

        _mockRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<Website>());

        var result = await _service.GetUserWebsitesAsync(userId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteWebsiteAsync_ValidOwnership_ReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        var websiteId = Guid.NewGuid();
        var website = new Website { Id = websiteId, UserId = userId, Url = "https://example.com" };

        _mockRepository.Setup(r => r.GetByIdAsync(websiteId))
            .ReturnsAsync(website);

        var result = await _service.DeleteWebsiteAsync(userId, websiteId);

        result.IsSuccess.Should().BeTrue();
        _mockRepository.Verify(r => r.DeleteAsync(website), Times.Once);
        _mockAlertStateStore.Verify(s => s.DeleteStateAsync(websiteId), Times.Once);
    }

    [Fact]
    public async Task DeleteWebsiteAsync_WebsiteNotFound_ReturnsNotFoundError()
    {
        var userId = Guid.NewGuid();
        var websiteId = Guid.NewGuid();

        _mockRepository.Setup(r => r.GetByIdAsync(websiteId))
            .ReturnsAsync((Website?)null);

        var result = await _service.DeleteWebsiteAsync(userId, websiteId);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
        result.Error.Message.Should().Contain("not found");
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<Website>()), Times.Never);
    }

    [Fact]
    public async Task DeleteWebsiteAsync_DifferentUser_ReturnsUnauthorizedError()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var websiteId = Guid.NewGuid();
        var website = new Website { Id = websiteId, UserId = otherUserId, Url = "https://example.com" };

        _mockRepository.Setup(r => r.GetByIdAsync(websiteId))
            .ReturnsAsync(website);

        var result = await _service.DeleteWebsiteAsync(userId, websiteId);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Unauthorized");
        result.Error.Message.Should().Contain("not authorized");
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<Website>()), Times.Never);
    }

    [Fact]
    public async Task PauseAsync_ValidInput_ReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        var url = "https://example.com";
        var website = new Website { Id = Guid.NewGuid(), UserId = userId, Url = url, IsActive = true };

        _mockRepository.Setup(r => r.GetByUserIdAndUrlAsync(userId, url))
            .ReturnsAsync(website);

        var result = await _service.PauseAsync(userId, url);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsActive.Should().BeFalse();
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<Website>(w => w.IsActive == false)), Times.Once);
    }

    [Fact]
    public async Task PauseAsync_EmptyUrl_ReturnsValidationError()
    {
        var userId = Guid.NewGuid();
        var result = await _service.PauseAsync(userId, "");

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Website>()), Times.Never);
    }

    [Fact]
    public async Task PauseAsync_WebsiteNotFound_ReturnsNotFoundError()
    {
        var userId = Guid.NewGuid();
        var url = "https://example.com";

        _mockRepository.Setup(r => r.GetByUserIdAndUrlAsync(userId, url))
            .ReturnsAsync((Website?)null);

        var result = await _service.PauseAsync(userId, url);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Website>()), Times.Never);
    }

    [Fact]
    public async Task ResumeAsync_ValidInput_ReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        var url = "https://example.com";
        var website = new Website { Id = Guid.NewGuid(), UserId = userId, Url = url, IsActive = false };

        _mockRepository.Setup(r => r.GetByUserIdAndUrlAsync(userId, url))
            .ReturnsAsync(website);

        var result = await _service.ResumeAsync(userId, url);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsActive.Should().BeTrue();
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<Website>(w => w.IsActive == true)), Times.Once);
    }

    [Fact]
    public async Task ResumeAsync_EmptyUrl_ReturnsValidationError()
    {
        var userId = Guid.NewGuid();
        var result = await _service.ResumeAsync(userId, "");

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Validation");
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Website>()), Times.Never);
    }

    [Fact]
    public async Task ResumeAsync_WebsiteNotFound_ReturnsNotFoundError()
    {
        var userId = Guid.NewGuid();
        var url = "https://example.com";

        _mockRepository.Setup(r => r.GetByUserIdAndUrlAsync(userId, url))
            .ReturnsAsync((Website?)null);

        var result = await _service.ResumeAsync(userId, url);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Website>()), Times.Never);
    }
}
