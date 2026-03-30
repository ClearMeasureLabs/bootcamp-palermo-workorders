using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Writes RFC 7807 problem details for unhandled exceptions (used only on machine-oriented branches of the pipeline).
/// </summary>
internal static class ProblemDetailsExceptionHandler
{
    internal static async Task HandleAsync(HttpContext context, IHostEnvironment environment)
    {
        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var exception = context.Features.Get<IExceptionHandlerPathFeature>()?.Error;
        var problemDetails = new ProblemDetails
        {
            Title = "An error occurred while processing your request.",
            Status = StatusCodes.Status500InternalServerError,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };

        if (environment.IsDevelopment() && exception is not null)
        {
            problemDetails.Detail = exception.ToString();
        }

        var problemDetailsService = context.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = problemDetails
        });
    }
}
