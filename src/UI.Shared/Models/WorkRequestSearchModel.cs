using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UI.Shared.Models;

public class WorkRequestSearchModel
{
    public SearchFilters Filters { get; set; } = new();
    public WorkRequest[] Results { get; set; } = [];

    public class SearchFilters
    {
        public string? Creator { get; set; }
        public string? Assignee { get; set; }
        public string? Status { get; set; }
    }
}