using UptimeMonitoring.Domain.Entities;

namespace UptimeMonitoring.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(Guid userId);
    Task AddAsync(User user);
}
