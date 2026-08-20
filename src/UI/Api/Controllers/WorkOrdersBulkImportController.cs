using System.Net.Mime;
using System.Text;
using Asp.Versioning;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Import;
using ClearMeasure.Bootcamp.Core.Services;
using Microsoft.AspNetCore.Authorization;
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
    /// Imports work orders from a CSV file (multipart field <c>file</c>) or CSV text
    /// (url-encoded field <c>csv</c>).
    /// Header row required: Title, Description, CreatorUsername; optional columns: Instructions, RoomNumber.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [Consumes("multipart/form-data", "application/x-www-form-urlencoded")]
    [AllowAnonymous]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(WorkOrderBulkImportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post(IFormFile? file, CancellationToken cancellationToken)
    {
        var resolveError = await TryResolveCsvStreamAsync(file, cancellationToken);
        if (resolveError.Error != null)
        {
            return Problem(detail: resolveError.Error, statusCode: StatusCodes.Status400BadRequest);
        }

        await using var stream = resolveError.Stream!;
        var parseResult = WorkOrderBulkImportCsvParser.Parse(stream, cancellationToken);
        if (!parseResult.Success)
        {
            return Problem(detail: parseResult.Error, statusCode: StatusCodes.Status400BadRequest);
        }

        if (parseResult.Rows.Count == 0)
        {
            return Problem(detail: "CSV contains no data rows.", statusCode: StatusCodes.Status400BadRequest);
        }

        var processor = new WorkOrderBulkImportProcessor(bus, numberGenerator);
        var response = await processor.ImportAsync(parseResult.Rows, cancellationToken);
        return Ok(response);
    }

    private async Task<(Stream? Stream, string? Error)> TryResolveCsvStreamAsync(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file != null)
        {
            var uploadError = WorkOrderBulkImportProcessor.ValidateUpload(file);
            if (uploadError != null)
            {
                return (null, uploadError);
            }

            return (file.OpenReadStream(), null);
        }

        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync(cancellationToken);
            var csvField = form["csv"].ToString();
            if (string.IsNullOrEmpty(csvField))
            {
                return (null,
                    "Provide a CSV file (multipart field name: file) or CSV text (url-encoded field name: csv).");
            }

            return (new MemoryStream(Encoding.UTF8.GetBytes(csvField)), null);
        }

        return (null,
            "Provide a CSV file (multipart field name: file) or CSV text (url-encoded field name: csv).");
    }
}
