using Asp.Versioning;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Export;
using ClearMeasure.Bootcamp.Core.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exports work orders as CSV for integrators and operators.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/workorders")]
[Route($"{ApiRoutes.VersionedApiPrefix}/workorders")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public sealed class WorkOrdersExportController(IBus bus) : ControllerBase
{
    /// <summary>
    /// Returns all work orders as RFC 4180 CSV (Number,Title,Status,Assignee,DueDate).
    /// </summary>
    [HttpGet("export.csv")]
    [AllowAnonymous]
    [Produces("text/csv")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExportCsv()
    {
        var workOrders = await bus.Send(new WorkOrderSpecificationQuery());
        var csv = WorkOrderCsvFormatter.Format(workOrders);
        return new ContentResult
        {
            Content = csv,
            ContentType = "text/csv; charset=utf-8",
            StatusCode = StatusCodes.Status200OK
        };
    }
}
