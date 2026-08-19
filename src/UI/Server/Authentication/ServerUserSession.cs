using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using Microsoft.AspNetCore.Http;

namespace ClearMeasure.Bootcamp.UI.Server.Authentication;

/// <summary>
/// Resolves the authenticated employee from the server-side cookie session.
/// </summary>
public sealed class ServerUserSession(IHttpContextAccessor httpContextAccessor, IBus bus) : IUserSession
{
    /// <inheritdoc />
    public async Task<Employee?> GetCurrentUserAsync()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var userName = httpContext.User.Identity.Name;
        if (string.IsNullOrEmpty(userName))
        {
            return null;
        }

        try
        {
            return await bus.Send(new EmployeeByUserNameQuery(userName));
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }
}
