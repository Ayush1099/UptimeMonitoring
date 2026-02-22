using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using UptimeMonitoring.Api.Controllers;
using UptimeMonitoring.Application.Common;
using UptimeMonitoring.Application.DTOs;
using UptimeMonitoring.Application.Interfaces;
using UptimeMonitoring.Domain.Entities;
using Xunit;

namespace UptimeMonitoring.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IJwtTokenService> _mockJwtTokenService;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mockUserService = new Mock<IUserService>();
        _mockJwtTokenService = new Mock<IJwtTokenService>();
        _controller = new AuthController(_mockUserService.Object, _mockJwtTokenService.Object);
    }

    [Fact]
    public async Task Register_ValidRequest_ReturnsOkWithUserResponse()
    {
        var request = new RegisterUserRequest { Email = "test@example.com", Password = "Password123!" };
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = request.Email };

        _mockUserService.Setup(s => s.RegisterAsync(request.Email, request.Password))
            .ReturnsAsync(Result<User>.Success(user));

        var result = await _controller.Register(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<UserResponse>().Subject;
        response.Id.Should().Be(userId);
        response.Email.Should().Be(request.Email);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var request = new RegisterUserRequest { Email = "test@example.com", Password = "Password123!" };

        _mockUserService.Setup(s => s.RegisterAsync(request.Email, request.Password))
            .ReturnsAsync(Result<User>.Failure(Error.Conflict("User already exists")));

        var result = await _controller.Register(request);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Register_ValidationError_ReturnsBadRequest()
    {
        var request = new RegisterUserRequest { Email = "test@example.com", Password = "Password123!" };

        _mockUserService.Setup(s => s.RegisterAsync(request.Email, request.Password))
            .ReturnsAsync(Result<User>.Failure(Error.Validation("Invalid email format")));

        var result = await _controller.Register(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithToken()
    {
        var request = new LoginRequest { Email = "test@example.com", Password = "Password123!" };
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = request.Email };
        var token = "test-jwt-token";

        _mockUserService.Setup(s => s.LoginAsync(request.Email, request.Password))
            .ReturnsAsync(Result<User>.Success(user));
        _mockJwtTokenService.Setup(s => s.GenerateToken(user))
            .Returns(token);

        var result = await _controller.Login(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<LoginResponse>().Subject;
        response.Token.Should().Be(token);
        _mockJwtTokenService.Verify(s => s.GenerateToken(user), Times.Once);
    }

    [Fact]
    public async Task Login_UserNotFound_ReturnsNotFound()
    {
        var request = new LoginRequest { Email = "test@example.com", Password = "Password123!" };

        _mockUserService.Setup(s => s.LoginAsync(request.Email, request.Password))
            .ReturnsAsync(Result<User>.Failure(Error.NotFound("User Not Found")));

        var result = await _controller.Login(request);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsBadRequest()
    {
        var request = new LoginRequest { Email = "test@example.com", Password = "WrongPassword" };

        _mockUserService.Setup(s => s.LoginAsync(request.Email, request.Password))
            .ReturnsAsync(Result<User>.Failure(Error.Unauthorized("Invalid credentials")));

        var result = await _controller.Login(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Login_GeneratesJwtToken()
    {
        var request = new LoginRequest { Email = "test@example.com", Password = "Password123!" };
        var user = new User { Id = Guid.NewGuid(), Email = request.Email };
        var token = "test-jwt-token";

        _mockUserService.Setup(s => s.LoginAsync(request.Email, request.Password))
            .ReturnsAsync(Result<User>.Success(user));
        _mockJwtTokenService.Setup(s => s.GenerateToken(user))
            .Returns(token);

        await _controller.Login(request);

        _mockJwtTokenService.Verify(s => s.GenerateToken(It.Is<User>(u => u.Email == request.Email)), Times.Once);
    }
}
