using UptimeMonitoring.Application.Common;
using UptimeMonitoring.Domain.Entities;

namespace UptimeMonitoring.Application.Interfaces;

public interface IUserService
{
    Task<Result<User>> RegisterAsync(string email, string password);
    Task<Result<User>> LoginAsync(string email, string password);
}
