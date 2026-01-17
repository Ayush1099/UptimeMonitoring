using UptimeMonitoring.Domain.Entities;

namespace UptimeMonitoring.Application.Interfaces;

public interface IUserService
{
    Task<User> RegisterAsync(string email, string password);
    Task<User> LoginAsync(string email, string password);
}
