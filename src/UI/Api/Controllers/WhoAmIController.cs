using Asp.Versioning;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exposes the authenticated employee identity for programmatic API consumers.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/whoami")]
[Route($"{ApiRoutes.VersionedApiPrefix}/whoami")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
[Authorize]
public class WhoAmIController(IUserSession userSession) : ControllerBase
{
    /// <summary>
    /// Returns the current authenticated employee as JSON.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var employee = await userSession.GetCurrentUserAsync();
        if (employee is null)
        {
            return Unauthorized();
        }

        var payload = MapToResponse(employee);
        return ConditionalGetEtag.JsonContent(payload);
    }

    private static WhoAmIResponse MapToResponse(Employee employee) =>
        new(
            UserName: employee.UserName,
            FirstName: employee.FirstName,
            LastName: employee.LastName,
            EmailAddress: employee.EmailAddress,
            PreferredLanguage: employee.PreferredLanguage,
            Roles: employee.Roles
                .Select(role => new WhoAmIRoleResponse(
                    role.Name,
                    role.CanCreateWorkOrder,
                    role.CanFulfillWorkOrder))
                .ToArray());
}

/// <summary>
/// JSON payload for <c>GET /api/whoami</c> and <c>GET /api/v1.0/whoami</c>.
/// </summary>
public record WhoAmIResponse(
    string UserName,
    string FirstName,
    string LastName,
    string EmailAddress,
    string PreferredLanguage,
    IReadOnlyList<WhoAmIRoleResponse> Roles);

/// <summary>
/// Role projection included in <see cref="WhoAmIResponse"/>.
/// </summary>
public record WhoAmIRoleResponse(
    string Name,
    bool CanCreateWorkOrder,
    bool CanFulfillWorkOrder);
