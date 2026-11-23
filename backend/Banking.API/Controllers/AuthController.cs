using Banking.Application.Dtos;
using Banking.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banking.API.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous] // these endpoints are how a client gets (or gives back) its tokens
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var response = await _authService.LoginAsync(request, ct);
        if (response is null)
        {
            return Unauthorized(new ProblemDetails { Status = 401, Title = "Invalid username or password." });
        }

        return Ok(response);
    }

    /// <summary>Exchanges a refresh token for a new access token + refresh token. The old refresh token stops working.</summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponse>> Refresh(RefreshTokenRequest request, CancellationToken ct)
    {
        var response = await _authService.RefreshAsync(request, ct);
        if (response is null)
        {
            return Unauthorized(new ProblemDetails { Status = 401, Title = "Session expired. Please sign in again." });
        }

        return Ok(response);
    }

    /// <summary>Ends the session on the server by revoking its refresh token.</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshTokenRequest request, CancellationToken ct)
    {
        await _authService.LogoutAsync(request, ct);
        return NoContent();
    }
}
