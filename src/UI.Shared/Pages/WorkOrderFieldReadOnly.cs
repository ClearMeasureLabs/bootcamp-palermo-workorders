using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

/// <summary>
/// Field-level read-only rules for the work order manage page.
/// Completed and cancelled orders stay locked even when a command such as Reopen is available.
/// </summary>
internal static class WorkOrderFieldReadOnly
{
    public static bool Evaluate(WorkOrder workOrder, IEnumerable<IStateCommand> validCommands) =>
        workOrder.Status == WorkOrderStatus.Complete
        || workOrder.Status == WorkOrderStatus.Cancelled
        || !validCommands.Any();
}
