using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace ClearMeasure.Bootcamp.UI.Server;

internal static class ProblemDetailsStatusCodePagesExtensions
{
    /// <summary>
    /// For <c>/api</c> and <c>/mcp</c> requests, writes RFC 7807 bodies when the pipeline sets an error status without a body.
    /// </summary>
    internal static void UseMachineClientStatusCodeProblemDetails(this WebApplication app)
    {
        app.UseStatusCodePages(async context =>
        {
            var httpContext = context.HttpContext;
            if (httpContext.Response.HasStarted)
            {
                return;
            }

            if (!ProblemDetailsPaths.IsMachineOriented(httpContext.Request.Path))
            {
                await context.Next(httpContext);
                return;
            }

            var statusCode = httpContext.Response.StatusCode;
            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = ReasonPhrases.GetReasonPhrase(statusCode),
                Type = statusCode switch
                {
                    StatusCodes.Status404NotFound => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    StatusCodes.Status400BadRequest => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
                }
            };

            if (string.IsNullOrEmpty(problemDetails.Title))
            {
                problemDetails.Title = "An error occurred.";
            }

            var problemDetailsService = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
            await problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problemDetails
            });
        });
    }
}

internal static class ProblemDetailsPaths
{
    internal static bool IsMachineOriented(PathString path) =>
        path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments("/mcp", StringComparison.OrdinalIgnoreCase);
}
