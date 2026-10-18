using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UI.Shared.Models;

public class WorkOrderSearchModel
{
    public SearchFilters Filters { get; } = new();
    public WorkOrderSearchResultRow[] Results { get; set; } = [];

    public class SearchFilters
    {
        public string? Creator { get; set; }
        public string? Assignee { get; set; }
        public string? Status { get; set; }
        public bool OverdueOnly { get; set; }

        /// <summary>
        /// True when any search filter is active.
        /// </summary>
        public bool HasAny =>
            !string.IsNullOrEmpty(Creator)
            || !string.IsNullOrEmpty(Assignee)
            || !string.IsNullOrEmpty(Status)
            || OverdueOnly;
    }
}

/// <summary>
/// Search row projection including read-time due-date display and urgency.
/// </summary>
public class WorkOrderSearchResultRow
{
    public required WorkOrder WorkOrder { get; init; }
    public string Number => WorkOrder.Number ?? string.Empty;
    public Employee? Creator => WorkOrder.Creator;
    public Employee? Assignee => WorkOrder.Assignee;
    public WorkOrderStatus Status => WorkOrder.Status;
    public string? Title => WorkOrder.Title;
    public string? DueDateDisplay { get; init; }
    public string DueDateCssClass { get; init; } = string.Empty;
    public string? DueDateUrgencyText { get; init; }
    public DueDateUrgency Urgency { get; init; }
}
