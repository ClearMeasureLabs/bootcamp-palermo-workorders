using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>
/// Picks the next work order a technician should work on.
/// </summary>
public static class NextWorkOrderSelector
{
    /// <summary>
    /// Returns the oldest assigned or in-progress work order for the technician, or null if none.
    /// </summary>
    public static WorkOrder? SelectNext(IEnumerable<WorkOrder> workOrders, string technicianUserName)
    {
        ArgumentNullException.ThrowIfNull(workOrders);
        ArgumentException.ThrowIfNullOrWhiteSpace(technicianUserName);

        return workOrders
            .Where(wo =>
                wo.Assignee is not null
                && string.Equals(wo.Assignee.UserName, technicianUserName, StringComparison.OrdinalIgnoreCase)
                && (wo.Status == WorkOrderStatus.Assigned || wo.Status == WorkOrderStatus.InProgress))
            .OrderBy(wo => wo.AssignedDate ?? DateTime.MaxValue)
            .ThenBy(wo => wo.Number, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }
}
