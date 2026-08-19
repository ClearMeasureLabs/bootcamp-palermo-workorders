using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Server;

internal static class ApiControllerMapping
{
    internal static void ApplyRequestTimeout(WebApplication app, ControllerActionEndpointConventionBuilder apiControllers)
    {
        var apiRequestTimeoutOpts = app.Services.GetRequiredService<IOptions<ApiRequestTimeoutOptions>>().Value;
        if (!apiRequestTimeoutOpts.Enabled || apiRequestTimeoutOpts.TimeoutSeconds <= 0)
        {
            return;
        }

        apiControllers.WithRequestTimeout(ApiRequestTimeoutsExtensions.ApiControllersPolicyName);
    }

    internal static void ApplyCorsWhenActive(WebApplication app, ControllerActionEndpointConventionBuilder apiControllers)
    {
        if (!app.Services.IsServerCorsActive())
        {
            return;
        }

        apiControllers.RequireCors(ServerCorsOptions.PolicyName);
    }
}
