using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using UptimeMonitoring.Api.Controllers;
using UptimeMonitoring.Application.Common;
using UptimeMonitoring.Application.DTOs;
using UptimeMonitoring.Application.Interfaces;
using UptimeMonitoring.Application.Services;
using UptimeMonitoring.Domain.Entities;
using Xunit;

namespace UptimeMonitoring.Tests.Controllers;

public class WebsitesControllerTests
{
    private readonly Mock<IWebsiteRepository> _mockRepository;
    private readonly Mock<IAlertStateStore> _mockAlertStateStore;
    private readonly WebsiteService _service;
    private readonly WebsitesController _controller;
    private readonly Guid _testUserId = Guid.NewGuid();

    public WebsitesControllerTests()
    {
        _mockRepository = new Mock<IWebsiteRepository>();
        _mockAlertStateStore = new Mock<IAlertStateStore>();
        _service = new WebsiteService(_mockRepository.Object, _mockAlertStateStore.Object);
        _controller = new WebsitesController(_service);
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
    public async Task Add_ValidRequest_ReturnsOk()
    {
        var request = new AddWebsiteRequest { Url = "https://example.com", CheckIntervalMinutes = 5 };

        _mockRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(new List<Website>());

        var result = await _controller.Add(request);

        result.Should().BeOfType<OkResult>();
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Website>()), Times.Once);
    }

    [Fact]
    public async Task Add_ValidationError_ReturnsBadRequest()
    {
        var request = new AddWebsiteRequest { Url = "invalid-url", CheckIntervalMinutes = 5 };

        var result = await _controller.Add(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Add_DuplicateUrl_ReturnsConflict()
    {
        var request = new AddWebsiteRequest { Url = "https://example.com", CheckIntervalMinutes = 5 };
        var existingWebsite = new Website { Id = Guid.NewGuid(), UserId = _testUserId, Url = request.Url };

        _mockRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(new List<Website> { existingWebsite });

        var result = await _controller.Add(request);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Add_ExtractsUserIdFromClaims()
    {
        var request = new AddWebsiteRequest { Url = "https://example.com", CheckIntervalMinutes = 5 };

        _mockRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(new List<Website>());

        await _controller.Add(request);

        _mockRepository.Verify(r => r.GetByUserIdAsync(_testUserId), Times.Once);
    }

    [Fact]
    public async Task Get_ReturnsWebsitesForUser()
    {
        var websites = new List<Website>
        {
            new Website { Id = Guid.NewGuid(), UserId = _testUserId, Url = "https://example.com", IsActive = true, CheckIntervalMinutes = 5 },
            new Website { Id = Guid.NewGuid(), UserId = _testUserId, Url = "https://test.com", IsActive = false, CheckIntervalMinutes = 10 }
        };

        _mockRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(websites);

        var result = await _controller.Get();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<WebsiteResponse>>().Subject;
        response.Should().HaveCount(2);
    }

    [Fact]
    public async Task Get_EmptyList_ReturnsEmptyList()
    {
        _mockRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(new List<Website>());

        var result = await _controller.Get();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<WebsiteResponse>>().Subject;
        response.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_ResponseMapping()
    {
        var websiteId = Guid.NewGuid();
        var websites = new List<Website>
        {
            new Website { Id = websiteId, UserId = _testUserId, Url = "https://example.com", IsActive = true, CheckIntervalMinutes = 5 }
        };

        _mockRepository.Setup(r => r.GetByUserIdAsync(_testUserId))
            .ReturnsAsync(websites);

        var result = await _controller.Get();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<WebsiteResponse>>().Subject;
        var websiteResponse = response.First();
        websiteResponse.Id.Should().Be(websiteId);
        websiteResponse.Url.Should().Be("https://example.com");
        websiteResponse.IsActive.Should().BeTrue();
        websiteResponse.CheckIntervalMinutes.Should().Be(5);
    }

    [Fact]
    public async Task Delete_ValidRequest_ReturnsOk()
    {
        var websiteId = Guid.NewGuid();
        var website = new Website { Id = websiteId, UserId = _testUserId, Url = "https://example.com" };

        _mockRepository.Setup(r => r.GetByIdAsync(websiteId))
            .ReturnsAsync(website);

        var result = await _controller.Delete(websiteId);

        result.Should().BeOfType<OkObjectResult>();
        _mockRepository.Verify(r => r.DeleteAsync(website), Times.Once);
    }

    [Fact]
    public async Task Delete_WebsiteNotFound_ReturnsNotFound()
    {
        var websiteId = Guid.NewGuid();

        _mockRepository.Setup(r => r.GetByIdAsync(websiteId))
            .ReturnsAsync((Website?)null);

        var result = await _controller.Delete(websiteId);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_UnauthorizedUser_ReturnsUnauthorized()
    {
        var websiteId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var website = new Website { Id = websiteId, UserId = otherUserId, Url = "https://example.com" };

        _mockRepository.Setup(r => r.GetByIdAsync(websiteId))
            .ReturnsAsync(website);

        var result = await _controller.Delete(websiteId);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Pause_ValidRequest_ReturnsOkWithPausedStatus()
    {
        var request = new WebsiteActionRequest { Url = "https://example.com" };
        var websiteId = Guid.NewGuid();
        var website = new Website { Id = websiteId, UserId = _testUserId, Url = request.Url, IsActive = true };

        _mockRepository.Setup(r => r.GetByUserIdAndUrlAsync(_testUserId, request.Url))
            .ReturnsAsync(website);

        var result = await _controller.Pause(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<Website>(w => w.IsActive == false)), Times.Once);
    }

    [Fact]
    public async Task Pause_WebsiteNotFound_ReturnsNotFound()
    {
        var request = new WebsiteActionRequest { Url = "https://example.com" };

        _mockRepository.Setup(r => r.GetByUserIdAndUrlAsync(_testUserId, request.Url))
            .ReturnsAsync((Website?)null);

        var result = await _controller.Pause(request);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Resume_ValidRequest_ReturnsOkWithActiveStatus()
    {
        var request = new WebsiteActionRequest { Url = "https://example.com" };
        var websiteId = Guid.NewGuid();
        var website = new Website { Id = websiteId, UserId = _testUserId, Url = request.Url, IsActive = false };

        _mockRepository.Setup(r => r.GetByUserIdAndUrlAsync(_testUserId, request.Url))
            .ReturnsAsync(website);

        var result = await _controller.Resume(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<Website>(w => w.IsActive == true)), Times.Once);
    }

    [Fact]
    public async Task Resume_WebsiteNotFound_ReturnsNotFound()
    {
        var request = new WebsiteActionRequest { Url = "https://example.com" };

        _mockRepository.Setup(r => r.GetByUserIdAndUrlAsync(_testUserId, request.Url))
            .ReturnsAsync((Website?)null);

        var result = await _controller.Resume(request);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
