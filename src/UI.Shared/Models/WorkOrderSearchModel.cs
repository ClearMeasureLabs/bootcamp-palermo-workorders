using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UI.Shared.Models
{
    public class WorkOrderSearchModel
    {
        public SearchFilters Filters { get; set; } = new();
        public WorkOrder[] Results { get; set; } = [];

        public class SearchFilters
        {
            public string? Creator { get; set; }
            public string? Assignee { get; set; }
            public string? Status { get; set; }
        }
    }
}