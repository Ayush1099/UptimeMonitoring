using FluentAssertions;
using Moq;
using System.Security.Cryptography;
using System.Text;
using UptimeMonitoring.Application.Common;
using UptimeMonitoring.Application.Interfaces;
using UptimeMonitoring.Application.Services;
using UptimeMonitoring.Domain.Entities;
using Xunit;

namespace UptimeMonitoring.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _mockRepository = new Mock<IUserRepository>();
        _service = new UserService(_mockRepository.Object);
    }

    [Fact]
    public async Task RegisterAsync_ValidInput_ReturnsSuccess()
    {
        var email = "test@example.com";
        var password = "Password123!";

        _mockRepository.Setup(r => r.GetByEmailAsync(email))
            .ReturnsAsync((User?)null);

        var result = await _service.RegisterAsync(email, password);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Email.Should().Be(email);
        result.Value.PasswordHash.Should().NotBeNullOrEmpty();
        result.Value.PasswordHash.Should().NotBe(password);
        result.Value.PasswordHash.Should().StartWith("$2");
        _mockRepository.Verify(r => r.AddAsync(It.Is<User>(u => 
            u.Email == email && 
            u.PasswordHash != password &&
            u.PasswordHash.StartsWith("$2"))), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_UserAlreadyExists_ReturnsConflictError()
    {
        var email = "test@example.com";
        var password = "Password123!";
        var existingUser = new User { Id = Guid.NewGuid(), Email = email };

        _mockRepository.Setup(r => r.GetByEmailAsync(email))
            .ReturnsAsync(existingUser);

        var result = await _service.RegisterAsync(email, password);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Conflict");
        result.Error.Message.Should().Contain("already exists");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccess()
    {
        var email = "test@example.com";
        var password = "Password123!";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        var user = new User 
        { 
            Id = Guid.NewGuid(), 
            Email = email, 
            PasswordHash = passwordHash 
        };

        _mockRepository.Setup(r => r.GetByEmailAsync(email))
            .ReturnsAsync(user);

        var result = await _service.LoginAsync(email, password);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Email.Should().Be(email);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ReturnsNotFoundError()
    {
        var email = "test@example.com";
        var password = "Password123!";

        _mockRepository.Setup(r => r.GetByEmailAsync(email))
            .ReturnsAsync((User?)null);

        var result = await _service.LoginAsync(email, password);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotFound");
        result.Error.Message.Should().Contain("Not Found");
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsUnauthorizedError()
    {
        var email = "test@example.com";
        var password = "Password123!";
        var wrongPassword = "WrongPassword";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        var user = new User 
        { 
            Id = Guid.NewGuid(), 
            Email = email, 
            PasswordHash = passwordHash 
        };

        _mockRepository.Setup(r => r.GetByEmailAsync(email))
            .ReturnsAsync(user);

        var result = await _service.LoginAsync(email, wrongPassword);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Unauthorized");
        result.Error.Message.Should().Contain("Invalid credentials");
    }

    [Fact]
    public async Task LoginAsync_LegacySha256Hash_ValidPassword_MigratesToBcrypt()
    {
        var email = "test@example.com";
        var password = "Password123!";
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        var legacyHash = Convert.ToBase64String(hash);
        var user = new User 
        { 
            Id = Guid.NewGuid(), 
            Email = email, 
            PasswordHash = legacyHash 
        };

        _mockRepository.Setup(r => r.GetByEmailAsync(email))
            .ReturnsAsync(user);

        var result = await _service.LoginAsync(email, password);

        result.IsSuccess.Should().BeTrue();
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<User>(u => 
            u.Email == email && 
            u.PasswordHash.StartsWith("$2"))), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_LegacySha256Hash_InvalidPassword_ReturnsUnauthorized()
    {
        var email = "test@example.com";
        var password = "Password123!";
        var wrongPassword = "WrongPassword";
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        var legacyHash = Convert.ToBase64String(hash);
        var user = new User 
        { 
            Id = Guid.NewGuid(), 
            Email = email, 
            PasswordHash = legacyHash 
        };

        _mockRepository.Setup(r => r.GetByEmailAsync(email))
            .ReturnsAsync(user);

        var result = await _service.LoginAsync(email, wrongPassword);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Unauthorized");
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_InvalidBcryptHash_ReturnsUnauthorized()
    {
        var email = "test@example.com";
        var password = "Password123!";
        var user = new User 
        { 
            Id = Guid.NewGuid(), 
            Email = email, 
            PasswordHash = "$2a$invalidhash" 
        };

        _mockRepository.Setup(r => r.GetByEmailAsync(email))
            .ReturnsAsync(user);

        var result = await _service.LoginAsync(email, password);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Unauthorized");
    }
}
