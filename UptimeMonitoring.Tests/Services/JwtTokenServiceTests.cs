using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using UptimeMonitoring.Application.Interfaces;
using UptimeMonitoring.Application.Services;
using UptimeMonitoring.Domain.Entities;
using Xunit;

namespace UptimeMonitoring.Tests.Services;

public class JwtTokenServiceTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly JwtTokenService _service;

    public JwtTokenServiceTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _service = new JwtTokenService(_mockConfiguration.Object);
    }

    [Fact]
    public void GenerateToken_ValidInput_ReturnsToken()
    {
        var user = new User 
        { 
            Id = Guid.NewGuid(), 
            Email = "test@example.com" 
        };
        var jwtKey = "test-secret-key-that-is-at-least-32-characters-long";
        var jwtIssuer = "TestIssuer";
        var jwtAudience = "TestAudience";
        var expiryMinutes = "30";

        _mockConfiguration.Setup(c => c["Jwt:Key"]).Returns(jwtKey);
        _mockConfiguration.Setup(c => c["Jwt:Issuer"]).Returns(jwtIssuer);
        _mockConfiguration.Setup(c => c["Jwt:Audience"]).Returns(jwtAudience);
        _mockConfiguration.Setup(c => c["Jwt:ExpiryMinutes"]).Returns(expiryMinutes);

        var token = _service.GenerateToken(user);

        token.Should().NotBeNullOrEmpty();
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(token);
        jsonToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
        jsonToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
        jsonToken.Issuer.Should().Be(jwtIssuer);
        jsonToken.Audiences.Should().Contain(jwtAudience);
    }

    [Fact]
    public void GenerateToken_UsesEnvironmentVariable_WhenAvailable()
    {
        var user = new User 
        { 
            Id = Guid.NewGuid(), 
            Email = "test@example.com" 
        };
        var envKey = "env-secret-key-that-is-at-least-32-characters-long";
        var envIssuer = "EnvIssuer";
        var envAudience = "EnvAudience";
        var envExpiry = "45";

        Environment.SetEnvironmentVariable("JWT_SECRET", envKey);
        Environment.SetEnvironmentVariable("JWT_ISSUER", envIssuer);
        Environment.SetEnvironmentVariable("JWT_AUDIENCE", envAudience);
        Environment.SetEnvironmentVariable("JWT_EXPIRY_MINUTES", envExpiry);

        try
        {
            var token = _service.GenerateToken(user);

            token.Should().NotBeNullOrEmpty();
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);
            jsonToken.Issuer.Should().Be(envIssuer);
            jsonToken.Audiences.Should().Contain(envAudience);
        }
        finally
        {
            Environment.SetEnvironmentVariable("JWT_SECRET", null);
            Environment.SetEnvironmentVariable("JWT_ISSUER", null);
            Environment.SetEnvironmentVariable("JWT_AUDIENCE", null);
            Environment.SetEnvironmentVariable("JWT_EXPIRY_MINUTES", null);
        }
    }

    [Fact]
    public void GenerateToken_FallsBackToConfiguration_WhenEnvVarNotSet()
    {
        var user = new User 
        { 
            Id = Guid.NewGuid(), 
            Email = "test@example.com" 
        };
        var configKey = "config-secret-key-that-is-at-least-32-characters-long";
        var configIssuer = "ConfigIssuer";
        var configAudience = "ConfigAudience";

        Environment.SetEnvironmentVariable("JWT_SECRET", null);
        Environment.SetEnvironmentVariable("JWT_ISSUER", null);
        Environment.SetEnvironmentVariable("JWT_AUDIENCE", null);
        Environment.SetEnvironmentVariable("JWT_EXPIRY_MINUTES", null);

        _mockConfiguration.Setup(c => c["Jwt:Key"]).Returns(configKey);
        _mockConfiguration.Setup(c => c["Jwt:Issuer"]).Returns(configIssuer);
        _mockConfiguration.Setup(c => c["Jwt:Audience"]).Returns(configAudience);
        _mockConfiguration.Setup(c => c["Jwt:ExpiryMinutes"]).Returns("60");

        var token = _service.GenerateToken(user);

        token.Should().NotBeNullOrEmpty();
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(token);
        jsonToken.Issuer.Should().Be(configIssuer);
        jsonToken.Audiences.Should().Contain(configAudience);
    }

    [Fact]
    public void GenerateToken_UsesDefaultValues_WhenConfigNotSet()
    {
        var user = new User 
        { 
            Id = Guid.NewGuid(), 
            Email = "test@example.com" 
        };
        var jwtKey = "default-secret-key-that-is-at-least-32-characters-long";

        Environment.SetEnvironmentVariable("JWT_SECRET", null);
        Environment.SetEnvironmentVariable("JWT_ISSUER", null);
        Environment.SetEnvironmentVariable("JWT_AUDIENCE", null);
        Environment.SetEnvironmentVariable("JWT_EXPIRY_MINUTES", null);

        _mockConfiguration.Setup(c => c["Jwt:Key"]).Returns(jwtKey);
        _mockConfiguration.Setup(c => c["Jwt:Issuer"]).Returns((string?)null);
        _mockConfiguration.Setup(c => c["Jwt:Audience"]).Returns((string?)null);
        _mockConfiguration.Setup(c => c["Jwt:ExpiryMinutes"]).Returns((string?)null);

        var token = _service.GenerateToken(user);

        token.Should().NotBeNullOrEmpty();
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(token);
        jsonToken.Issuer.Should().Be("UptimeMonitoring");
        jsonToken.Audiences.Should().Contain("UptimeMonitoringUsers");
    }

    [Fact]
    public void GenerateToken_MissingSecretKey_ThrowsException()
    {
        var user = new User 
        { 
            Id = Guid.NewGuid(), 
            Email = "test@example.com" 
        };

        Environment.SetEnvironmentVariable("JWT_SECRET", null);
        _mockConfiguration.Setup(c => c["Jwt:Key"]).Returns((string?)null);

        var act = () => _service.GenerateToken(user);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*JWT secret key must be configured*");
    }

    [Fact]
    public void GenerateToken_TokenExpiresInCorrectTime()
    {
        var user = new User 
        { 
            Id = Guid.NewGuid(), 
            Email = "test@example.com" 
        };
        var jwtKey = "expiry-secret-key-that-is-at-least-32-characters-long";
        var expiryMinutes = 15;

        _mockConfiguration.Setup(c => c["Jwt:Key"]).Returns(jwtKey);
        _mockConfiguration.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        _mockConfiguration.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
        _mockConfiguration.Setup(c => c["Jwt:ExpiryMinutes"]).Returns(expiryMinutes.ToString());

        var beforeGeneration = DateTime.UtcNow;
        var token = _service.GenerateToken(user);
        var afterGeneration = DateTime.UtcNow;

        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(token);
        var minExpiry = beforeGeneration.AddMinutes(expiryMinutes).AddSeconds(-1);
        var maxExpiry = afterGeneration.AddMinutes(expiryMinutes).AddSeconds(2);
        jsonToken.ValidTo.Should().BeAfter(minExpiry);
        jsonToken.ValidTo.Should().BeBefore(maxExpiry);
    }

    [Fact]
    public void GenerateToken_ContainsCorrectClaims()
    {
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var user = new User 
        { 
            Id = userId, 
            Email = email 
        };
        var jwtKey = "claims-secret-key-that-is-at-least-32-characters-long";

        _mockConfiguration.Setup(c => c["Jwt:Key"]).Returns(jwtKey);
        _mockConfiguration.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        _mockConfiguration.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
        _mockConfiguration.Setup(c => c["Jwt:ExpiryMinutes"]).Returns("60");

        var token = _service.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(token);
        jsonToken.Claims.Should().ContainSingle(c => 
            c.Type == JwtRegisteredClaimNames.Sub && 
            c.Value == userId.ToString());
        jsonToken.Claims.Should().ContainSingle(c => 
            c.Type == JwtRegisteredClaimNames.Email && 
            c.Value == email);
    }
}
