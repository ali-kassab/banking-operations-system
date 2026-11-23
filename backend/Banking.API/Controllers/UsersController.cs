using Banking.API.Authorization;
using Banking.Application.Dtos;
using Banking.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banking.API.Controllers;

/// <summary>Staff account management. Only administrators may use these endpoints.</summary>
[ApiController]
[Route("api/users")]
[Authorize(Roles = Roles.Admin)]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<UserResponse>>> GetAll([FromQuery] PageQuery page, CancellationToken ct) =>
        Ok(await _userService.GetPageAsync(page, ct));

    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create(CreateUserRequest request, CancellationToken ct)
    {
        var user = await _userService.CreateAsync(request, ct);
        return Created($"/api/users/{user.Id}", user);
    }

    /// <summary>Activates or deactivates a user. Deactivated users cannot sign in and lose their sessions.</summary>
    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<UserResponse>> SetStatus(int id, UpdateUserStatusRequest request, CancellationToken ct) =>
        Ok(await _userService.SetActiveAsync(id, request.IsActive, User.Identity!.Name!, ct));

    [HttpPut("{id:int}/password")]
    public async Task<IActionResult> ResetPassword(int id, ResetPasswordRequest request, CancellationToken ct)
    {
        await _userService.ResetPasswordAsync(id, request, ct);
        return NoContent();
    }
}
