using Asp.Versioning;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Exports matching work orders as CSV using the existing search query.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/workorders/export")]
[Route($"{ApiRoutes.VersionedApiPrefix}/workorders/export")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class WorkOrdersExportController(IBus bus) : ControllerBase
{
    /// <summary>
    /// Returns text/csv for work orders matching optional status, creator, and assignee filters.
    /// </summary>
    [HttpGet]
    [Produces("text/csv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromQuery] string? status = null,
        [FromQuery] string? creator = null,
        [FromQuery] string? assignee = null)
    {
        var query = new WorkOrderSpecificationQuery();
        if (!string.IsNullOrWhiteSpace(status))
        {
            query.MatchStatus(WorkOrderStatus.FromKey(status));
        }

        if (!string.IsNullOrWhiteSpace(creator))
        {
            query.MatchCreator(new Employee(creator, string.Empty, string.Empty, string.Empty));
        }

        if (!string.IsNullOrWhiteSpace(assignee))
        {
            query.MatchAssignee(new Employee(assignee, string.Empty, string.Empty, string.Empty));
        }

        var workOrders = await bus.Send(query);
        var csv = WorkOrderCsvWriter.Write(workOrders);
        return Content(csv, "text/csv; charset=utf-8");
    }
}
