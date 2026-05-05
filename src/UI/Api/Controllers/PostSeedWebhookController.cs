using System.Net.Mime;
using Asp.Versioning;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Inbound automation webhook invoked after database seed to acknowledge receipt and confirm seeded catalog rows via existing reads.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/post-seed-webhook")]
[Route($"{ApiRoutes.VersionedApiPrefix}/post-seed-webhook")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public sealed class PostSeedWebhookController(IBus bus, ILogger<PostSeedWebhookController> logger) : ControllerBase
{
    /// <summary>
    /// Accepts a JSON notification when seed completes; uses <see cref="EmployeeGetAllQuery"/> to verify at least one employee exists.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(PostSeedWebhookResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post(PostSeedWebhookRequest request, CancellationToken cancellationToken)
    {
        var correlation = string.IsNullOrWhiteSpace(request.CorrelationId) ? "(none)" : request.CorrelationId;
        logger.LogInformation(
            "Post-seed webhook received. Event={Event}, CorrelationId={CorrelationId}",
            request.Event ?? "(null)",
            correlation);

        if (string.IsNullOrWhiteSpace(request.Event))
        {
            return Problem(
                detail: "The event field is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        Employee[] employees;
        try
        {
            employees = await bus.Send(new EmployeeGetAllQuery());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Post-seed webhook failed while querying employees.");
            return Problem(
                detail: "Unable to verify seeded data.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var seedDetected = employees.Length > 0;
        return Ok(new PostSeedWebhookResponse(Received: true, SeedDataDetected: seedDetected));
    }
}
