using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using UptimeMonitoring.Application.Common;
using UptimeMonitoring.Application.Interfaces;
using UptimeMonitoring.Domain.Entities;

namespace UptimeMonitoring.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<User>> RegisterAsync(string email, string password)
    {
        var existingUser = await _userRepository.GetByEmailAsync(email);
        if (existingUser != null)
        {
            return Result<User>.Failure(Error.Conflict("User already exists"));
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = HashPassword(password),
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);
        return Result<User>.Success(user);
    }
    public async Task<Result<User>> LoginAsync(string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
            return Result<User>.Failure(Error.NotFound("User Not Found"));

        var (isValid, needsMigration) = VerifyPassword(password, user.PasswordHash);
        
        if (!isValid)
            return Result<User>.Failure(Error.Unauthorized("Invalid credentials"));

        // Automatically migrate password from SHA256 to BCrypt if needed
        if (needsMigration)
        {
            user.PasswordHash = HashPassword(password);
            await _userRepository.UpdateAsync(user);
        }

        return Result<User>.Success(user);
    }

    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    private static (bool isValid, bool needsMigration) VerifyPassword(string password, string passwordHash)
    {
        // Check if the hash is a BCrypt hash (starts with $2a$, $2b$, $2x$, or $2y$)
        if (passwordHash.StartsWith("$2a$") || passwordHash.StartsWith("$2b$") || 
            passwordHash.StartsWith("$2x$") || passwordHash.StartsWith("$2y$"))
        {
            // BCrypt hash - verify normally
            try
            {
                var isValid = BCrypt.Net.BCrypt.Verify(password, passwordHash);
                return (isValid, false);
            }
            catch
            {
                // Invalid BCrypt hash format
                return (false, false);
            }
        }
        else
        {
            // Legacy SHA256 hash - verify and mark for migration
            try
            {
                using var sha256 = SHA256.Create();
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                var computedHash = Convert.ToBase64String(hash);
                
                var isValid = passwordHash.Equals(computedHash, StringComparison.Ordinal);
                return (isValid, isValid); // If valid, needs migration to BCrypt
            }
            catch
            {
                return (false, false);
            }
        }
    }

}
