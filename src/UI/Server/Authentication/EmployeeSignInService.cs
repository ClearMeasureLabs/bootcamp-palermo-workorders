using System.Security.Claims;
using ClearMeasure.Bootcamp.Core.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace ClearMeasure.Bootcamp.UI.Server.Authentication;

/// <summary>
/// Signs employees in and out using the employee cookie authentication scheme.
/// </summary>
public sealed class EmployeeSignInService(IHttpContextAccessor httpContextAccessor) : IEmployeeSignInService
{
    /// <inheritdoc />
    public Task SignInAsync(string userName)
    {
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("No active HTTP context.");
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, userName)],
            EmployeeAuthenticationDefaults.Scheme);
        var principal = new ClaimsPrincipal(identity);
        return httpContext.SignInAsync(
            EmployeeAuthenticationDefaults.Scheme,
            principal,
            new AuthenticationProperties { IsPersistent = true });
    }

    /// <inheritdoc />
    public Task SignOutAsync()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return Task.CompletedTask;
        }

        return httpContext.SignOutAsync(EmployeeAuthenticationDefaults.Scheme);
    }
}
