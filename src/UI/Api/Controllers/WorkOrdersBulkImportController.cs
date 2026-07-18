using System.Net.Mime;
using Asp.Versioning;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Import;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Accepts CSV uploads to create multiple draft work orders in one request.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/work-orders/bulk-import")]
[Route($"{ApiRoutes.VersionedApiPrefix}/work-orders/bulk-import")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public sealed class WorkOrdersBulkImportController(
    IBus bus,
    IWorkOrderNumberGenerator numberGenerator) : ControllerBase
{
    /// <summary>
    /// Imports work orders from a CSV file. Form field name: <c>file</c>.
    /// Header row required: Title, Description, CreatorUsername, RoomNumber (optional).
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    [AllowAnonymous]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(WorkOrderBulkImportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return Problem(
                detail: "A non-empty CSV file is required (form field name: file).",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var ext = Path.GetExtension(file.FileName);
        if (!string.Equals(ext, ".csv", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(file.ContentType, "text/csv", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(file.ContentType, "application/vnd.ms-excel", StringComparison.OrdinalIgnoreCase))
        {
            return Problem(
                detail: "Upload must be a .csv file.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        await using var stream = file.OpenReadStream();
        var parseResult = WorkOrderBulkImportCsvParser.Parse(stream, cancellationToken);
        if (!parseResult.Success)
        {
            return Problem(detail: parseResult.Error, statusCode: StatusCodes.Status400BadRequest);
        }

        if (parseResult.Rows.Count == 0)
        {
            return Problem(detail: "CSV contains no data rows.", statusCode: StatusCodes.Status400BadRequest);
        }

        var results = new List<WorkOrderBulkImportRowResult>(parseResult.Rows.Count);
        var created = 0;
        var creatorsByUsername = new Dictionary<string, Employee>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in parseResult.Rows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(row.Title)
                || string.IsNullOrWhiteSpace(row.Description)
                || string.IsNullOrWhiteSpace(row.CreatorUsername))
            {
                results.Add(new WorkOrderBulkImportRowResult(row.LineNumber, false, null,
                    "Title, Description, and CreatorUsername are required on each data row."));
                continue;
            }

            if (!creatorsByUsername.TryGetValue(row.CreatorUsername, out var creator))
            {
                try
                {
                    creator = await bus.Send(new EmployeeByUserNameQuery(row.CreatorUsername));
                }
                catch (InvalidOperationException)
                {
                    results.Add(new WorkOrderBulkImportRowResult(row.LineNumber, false, null,
                        $"Employee with username '{row.CreatorUsername}' was not found."));
                    continue;
                }

                creatorsByUsername[row.CreatorUsername] = creator;
            }

            var workOrder = new WorkOrder
            {
                Title = row.Title,
                Description = row.Description,
                Creator = creator,
                Status = WorkOrderStatus.Draft,
                Number = numberGenerator.GenerateNumber(),
                RoomNumber = row.RoomNumber
            };

            try
            {
                var saveResult = await bus.Send(new SaveDraftCommand(workOrder, creator));
                created++;
                results.Add(new WorkOrderBulkImportRowResult(row.LineNumber, true, saveResult.WorkOrder.Number, null));
            }
            catch (Exception ex)
            {
                results.Add(new WorkOrderBulkImportRowResult(row.LineNumber, false, null, ex.Message));
            }
        }

        return Ok(new WorkOrderBulkImportResponse(created, results));
    }
}
