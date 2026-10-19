using System.Net.Mime;
using Asp.Versioning;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Returns how many work orders are currently open (Draft, Assigned, or In Progress).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/open-work-order-count")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/open-work-order-count")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class ToolsOpenWorkOrderCountController(IBus bus) : ControllerBase
{
    /// <summary>
    /// Returns JSON <c>{ "open": N }</c> where N is the count of Draft, Assigned, and In Progress work orders.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(OpenWorkOrderCountResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get()
    {
        var counts = await bus.Send(new WorkOrderCountByStatusQuery());
        var open = CountOpen(counts);
        return Ok(new OpenWorkOrderCountResponse(open));
    }

    private static int CountOpen(Dictionary<string, int> counts) =>
        counts.GetValueOrDefault(WorkOrderStatus.Draft.Key)
        + counts.GetValueOrDefault(WorkOrderStatus.Assigned.Key)
        + counts.GetValueOrDefault(WorkOrderStatus.InProgress.Key);
}

/// <summary>
/// JSON payload for <c>GET /api/tools/open-work-order-count</c>.
/// </summary>
public record OpenWorkOrderCountResponse(int Open);
