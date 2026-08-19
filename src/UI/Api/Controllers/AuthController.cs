using Asp.Versioning;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Programmatic employee login and logout for API consumers and test harnesses.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/auth")]
[Route($"{ApiRoutes.VersionedApiPrefix}/auth")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class AuthController(IBus bus, IEmployeeSignInService signInService) : ControllerBase
{
    /// <summary>
    /// Establishes an authenticated employee session for the given username.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid login",
                Detail = "Username is required.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            await bus.Send(new EmployeeByUserNameQuery(request.UserName));
        }
        catch (InvalidOperationException)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid login",
                Detail = $"No employee found with username '{request.UserName}'.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        await signInService.SignInAsync(request.UserName);
        return NoContent();
    }

    /// <summary>
    /// Clears the authenticated employee session.
    /// </summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        await signInService.SignOutAsync();
        return NoContent();
    }
}

/// <summary>
/// Request body for <c>POST /api/auth/login</c>.
/// </summary>
public record LoginRequest(string UserName);
