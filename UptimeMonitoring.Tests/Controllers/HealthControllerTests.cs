using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using UptimeMonitoring.Api.Controllers;
using Xunit;

namespace UptimeMonitoring.Tests.Controllers;

public class HealthControllerTests
{
    private readonly HealthController _controller;

    public HealthControllerTests()
    {
        _controller = new HealthController();
    }

    [Fact]
    public void Get_ReturnsOkWithStatusInfo()
    {
        var result = _controller.Get();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public void Get_ResponseStructure()
    {
        var result = _controller.Get();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<object>().Subject;
        response.Should().NotBeNull();
    }
}
