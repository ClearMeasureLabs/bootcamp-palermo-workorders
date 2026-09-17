namespace ClearMeasure.Bootcamp.UI.Shared.Services;

/// <summary>
/// Holds cross-navigation state for the Work Order Search page within a session.
/// Registered as a singleton so that the "Assigned to me" toggle survives Blazor
/// component re-creation during client-side navigation.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global -- registered as singleton in UIClientServiceRegistry
public class WorkOrderSearchState
{
    public bool AssignedToMe { get; set; }
}
