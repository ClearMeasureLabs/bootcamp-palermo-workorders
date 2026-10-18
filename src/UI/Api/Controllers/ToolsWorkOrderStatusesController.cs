using System.Net.Mime;
using Asp.Versioning;
using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/work-order-statuses")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/work-order-statuses")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class ToolsWorkOrderStatusesController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(WorkOrderStatusDto[]), StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        var statuses = WorkOrderStatus.GetAllItems()
            .OrderBy(s => s.SortBy)
            .Select(s => new WorkOrderStatusDto(s.Code, s.Key, s.FriendlyName, s.SortBy))
            .ToArray();

        return Ok(statuses);
    }
}

public record WorkOrderStatusDto(string Code, string Key, string FriendlyName, byte SortBy);
