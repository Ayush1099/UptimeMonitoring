using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using UptimeMonitoring.Application.Interfaces;
using UptimeMonitoring.Domain.Entities;

namespace UptimeMonitoring.Application.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user)
    {
        // Read JWT configuration from environment variables with fallback to appsettings
        var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET") 
                     ?? _configuration["Jwt:Key"] 
                     ?? throw new InvalidOperationException("JWT secret key must be configured via JWT_SECRET environment variable or appsettings.json");
        
        var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") 
                        ?? _configuration["Jwt:Issuer"] 
                        ?? "UptimeMonitoring";
        
        var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
                         ?? _configuration["Jwt:Audience"] 
                         ?? "UptimeMonitoringUsers";

        var expiryMinutes = Environment.GetEnvironmentVariable("JWT_EXPIRY_MINUTES");
        var expiryMinutesValue = expiryMinutes != null && int.TryParse(expiryMinutes, out var parsed) 
                                  ? parsed 
                                  : int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "60");

        var key = Encoding.UTF8.GetBytes(jwtKey);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutesValue),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
