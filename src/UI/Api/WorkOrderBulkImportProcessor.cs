using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Import;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Validates CSV uploads and imports parsed rows as draft work orders.
/// </summary>
internal sealed class WorkOrderBulkImportProcessor(IBus bus, IWorkOrderNumberGenerator numberGenerator)
{
    internal static string? ValidateUpload(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return "A non-empty CSV file is required (form field name: file).";
        }

        if (!IsCsvFile(file))
        {
            return "Upload must be a .csv file.";
        }

        return null;
    }

    internal static bool IsCsvFile(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName);
        return string.Equals(ext, ".csv", StringComparison.OrdinalIgnoreCase)
               || string.Equals(file.ContentType, "text/csv", StringComparison.OrdinalIgnoreCase)
               || string.Equals(file.ContentType, "application/vnd.ms-excel", StringComparison.OrdinalIgnoreCase);
    }

    internal async Task<WorkOrderBulkImportResponse> ImportAsync(
        IReadOnlyList<WorkOrderBulkImportRow> rows,
        CancellationToken cancellationToken)
    {
        var results = new List<WorkOrderBulkImportRowResult>(rows.Count);
        var created = 0;
        var creatorsByUsername = new Dictionary<string, Employee>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rowResult = await ImportRowAsync(row, creatorsByUsername, cancellationToken);
            if (rowResult.Success)
            {
                created++;
            }

            results.Add(rowResult);
        }

        return new WorkOrderBulkImportResponse(created, results);
    }

    private async Task<WorkOrderBulkImportRowResult> ImportRowAsync(
        WorkOrderBulkImportRow row,
        Dictionary<string, Employee> creatorsByUsername,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(row.Title)
            || string.IsNullOrWhiteSpace(row.Description)
            || string.IsNullOrWhiteSpace(row.CreatorUsername))
        {
            return new WorkOrderBulkImportRowResult(row.LineNumber, false, null,
                "Title, Description, and CreatorUsername are required on each data row.");
        }

        var creatorResult = await ResolveCreatorAsync(row, creatorsByUsername, cancellationToken);
        if (creatorResult.Error != null)
        {
            return new WorkOrderBulkImportRowResult(row.LineNumber, false, null, creatorResult.Error);
        }

        return await SaveDraftAsync(row, creatorResult.Creator!, cancellationToken);
    }

    private async Task<(Employee? Creator, string? Error)> ResolveCreatorAsync(
        WorkOrderBulkImportRow row,
        Dictionary<string, Employee> creatorsByUsername,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var username = row.CreatorUsername!;
        if (creatorsByUsername.TryGetValue(username, out var cached))
        {
            return (cached, null);
        }

        try
        {
            var creator = await bus.Send(new EmployeeByUserNameQuery(username));
            creatorsByUsername[username] = creator;
            return (creator, null);
        }
        catch (InvalidOperationException)
        {
            return (null, $"Employee with username '{username}' was not found.");
        }
    }

    private async Task<WorkOrderBulkImportRowResult> SaveDraftAsync(
        WorkOrderBulkImportRow row,
        Employee creator,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var workOrder = new WorkOrder
        {
            Title = row.Title,
            Description = row.Description,
            Creator = creator,
            Status = WorkOrderStatus.Draft,
            Number = numberGenerator.GenerateNumber(),
            Instructions = row.Instructions,
            RoomNumber = row.RoomNumber
        };

        try
        {
            var saveResult = await bus.Send(new SaveDraftCommand(workOrder, creator));
            return new WorkOrderBulkImportRowResult(row.LineNumber, true, saveResult.WorkOrder.Number, null);
        }
        catch (Exception ex)
        {
            return new WorkOrderBulkImportRowResult(row.LineNumber, false, null, ex.Message);
        }
    }
}
