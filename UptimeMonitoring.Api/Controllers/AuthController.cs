using Microsoft.AspNetCore.Mvc;
using UptimeMonitoring.Application.DTOs;
using UptimeMonitoring.Application.Interfaces;
using UptimeMonitoring.Application.Services;

namespace UptimeMonitoring.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IJwtTokenService _jwtTokenService;
    public AuthController(IUserService userService, IJwtTokenService jwtTokenService)
    {
        _userService = userService;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterUserRequest request)
    {
        var user = await _userService.RegisterAsync(
            request.Email,
            request.Password
        );

        return Ok(new UserResponse
        {
            Id = user.Id,
            Email = user.Email
        });
    }
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _userService.LoginAsync(
            request.Email,
            request.Password
        );

        var token = _jwtTokenService.GenerateToken(user);

        return Ok(new LoginResponse
        {
            Token = token
        });
    }
}
