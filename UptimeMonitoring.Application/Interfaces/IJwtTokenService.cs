using UptimeMonitoring.Domain.Entities;

namespace UptimeMonitoring.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(User user);
}
