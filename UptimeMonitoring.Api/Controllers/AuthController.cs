using Microsoft.AspNetCore.Mvc;
using UptimeMonitoring.Application.DTOs;
using UptimeMonitoring.Application.Interfaces;

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
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterUserRequest request)
    {
        var result = await _userService.RegisterAsync(
            request.Email,
            request.Password
        );

        if (result.IsFailure)
        {
            return result.Error!.Code switch
            {
                "Conflict" => Conflict(result.Error.Message),
                _ => BadRequest(result.Error.Message),
            };
        }

        return Ok(new UserResponse
        {
            Id = result.Value!.Id,
            Email = result.Value.Email
        });
    }
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _userService.LoginAsync(
            request.Email,
            request.Password
        );

        if (result.IsFailure)
        {
            return result.Error!.Code switch
            {
                "NotFound" => NotFound(result.Error.Message),
                "Unauthorized" => BadRequest(result.Error.Message),
                _ => BadRequest(result.Error.Message),
            };
        }

        var token = _jwtTokenService.GenerateToken(result.Value!);

        return Ok(new LoginResponse
        {
            Token = token
        });
    }
}
