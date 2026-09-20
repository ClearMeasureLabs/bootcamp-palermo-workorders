using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// Display formatting for work order status values.
/// </summary>
public static class WorkOrderStatusDisplayFormatter
{
    /// <summary>
    /// Returns the friendly name of the status, or an empty string when the status is null.
    /// </summary>
    public static string FormatForDisplay(WorkOrderStatus? status) =>
        status?.FriendlyName ?? string.Empty;
}
