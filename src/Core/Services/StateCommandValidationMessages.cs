using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>
/// Formats user-facing messages when a state command fails <see cref="IStateCommand.IsValid"/>.
/// </summary>
public static class StateCommandValidationMessages
{
    /// <summary>
    /// Builds the invalid-command message: not-allowed when status matches the required begin status;
    /// otherwise the status-mismatch message.
    /// </summary>
    public static string FormatInvalid(string commandName, WorkOrder workOrder, WorkOrderStatus requiredStatus) =>
        workOrder.Status == requiredStatus
            ? $"Command '{commandName}' cannot be executed. You are not allowed to run this command on this work order."
            : $"Command '{commandName}' cannot be executed. Work order is in '{workOrder.Status.FriendlyName}' status but the command requires '{requiredStatus.FriendlyName}' status.";
}
