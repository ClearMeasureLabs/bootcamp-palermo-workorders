using System.Globalization;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

[Route("/workorder/search")]
[Authorize]
public partial class WorkOrderSearch : AppComponentBase
{
    private string? _sortColumn;
    private bool _sortAscending = true;

    [Inject] public TimeProvider Clock { get; set; } = TimeProvider.System;

    [SupplyParameterFromQuery] public string? Creator { get; set; }
    [SupplyParameterFromQuery] public string? Assignee { get; set; }
    [SupplyParameterFromQuery] public string? Status { get; set; }
    [SupplyParameterFromQuery] public bool OverdueOnly { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        OnSearch = HandleSearch;

        var employees = await Bus.Send(new EmployeeGetAllQuery());
        UserOptions = employees.Select(e => new SelectListItem(e.UserName, e.GetFullName())).ToList();
        StatusOptions = WorkOrderStatus.GetAllItems().Select(s => new SelectListItem(s.Key, s.FriendlyName)).ToList();
        Model = new WorkOrderSearchModel();

        // Apply any query parameters
        if (!string.IsNullOrEmpty(Creator))
        {
            Model.Filters.Creator = Creator;
        }

        if (!string.IsNullOrEmpty(Assignee))
        {
            Model.Filters.Assignee = Assignee;
        }

        if (!string.IsNullOrEmpty(Status))
        {
            Model.Filters.Status = Status;
        }

        if (OverdueOnly)
        {
            Model.Filters.OverdueOnly = OverdueOnly;
        }

        // Perform initial search
        await SearchWorkOrders();
    }

    private async Task SearchWorkOrders()
    {
        var creator = !string.IsNullOrWhiteSpace(Model.Filters.Creator)
            ? await Bus.Send(new EmployeeByUserNameQuery(Model.Filters.Creator))
            : null;

        var assignee = !string.IsNullOrWhiteSpace(Model.Filters.Assignee)
            ? await Bus.Send(new EmployeeByUserNameQuery(Model.Filters.Assignee))
            : null;

        var status = !string.IsNullOrWhiteSpace(Model.Filters.Status)
            ? WorkOrderStatus.FromKey(Model.Filters.Status)
            : null;

        var specification = new WorkOrderSpecificationQuery();
        specification.MatchCreator(creator);
        specification.MatchAssignee(assignee);
        specification.MatchStatus(status);
        specification.MatchOverdueOnly(Model.Filters.OverdueOnly);

        var workOrders = await Bus.Send(specification);
        Model.Results = workOrders.Select(MapSearchRow).ToArray();
        ApplySort();
        StateHasChanged();
    }

    private void SortBy(string column)
    {
        if (column == _sortColumn)
            _sortAscending = !_sortAscending;
        else
        {
            _sortColumn = column;
            _sortAscending = true;
        }

        ApplySort();
        StateHasChanged();
    }

    private void ApplySort()
    {
        if (_sortColumn == null) return;
        if (_sortColumn == "Status") ApplySortByStatus();
        else if (_sortColumn == "DueDate") ApplySortByDueDate();
        else if (_sortColumn == "Title") ApplySortByTitle();
        else if (_sortColumn == "Room") ApplySortByRoom();
    }

    private void ApplySortByStatus()
    {
        Model.Results = _sortAscending
            ? Model.Results.OrderBy(r => r.Status.FriendlyName).ToArray()
            : Model.Results.OrderByDescending(r => r.Status.FriendlyName).ToArray();
    }

    private void ApplySortByDueDate()
    {
        Model.Results = _sortAscending
            ? Model.Results.OrderBy(r => r.WorkOrder.DueDate.HasValue ? 0 : 1)
                           .ThenBy(r => r.WorkOrder.DueDate).ToArray()
            : Model.Results.OrderBy(r => r.WorkOrder.DueDate.HasValue ? 0 : 1)
                           .ThenByDescending(r => r.WorkOrder.DueDate).ToArray();
    }

    private void ApplySortByTitle()
    {
        Model.Results = _sortAscending
            ? Model.Results.OrderBy(r => r.Title, StringComparer.CurrentCultureIgnoreCase).ToArray()
            : Model.Results.OrderByDescending(r => r.Title, StringComparer.CurrentCultureIgnoreCase).ToArray();
    }

    private void ApplySortByRoom()
    {
        Model.Results = _sortAscending
            ? Model.Results.OrderBy(r => r.WorkOrder.RoomNumber, StringComparer.CurrentCultureIgnoreCase).ToArray()
            : Model.Results.OrderByDescending(r => r.WorkOrder.RoomNumber, StringComparer.CurrentCultureIgnoreCase).ToArray();
    }

    private WorkOrderSearchResultRow MapSearchRow(WorkOrder workOrder)
    {
        var urgency = DueDateUrgencyCalculator.Calculate(workOrder, Clock);
        return new WorkOrderSearchResultRow
        {
            WorkOrder = workOrder,
            DueDateDisplay = workOrder.DueDate?.ToString("MMM d, yyyy", CultureInfo.InvariantCulture),
            DueDateCssClass = DueDateUrgencyCalculator.CssClass(urgency),
            DueDateUrgencyText = DueDateUrgencyCalculator.ScreenReaderText(urgency),
            Urgency = urgency
        };
    }

    private async Task HandleSearch()
    {
        await SearchWorkOrders();
    }

    private bool HasActiveFilters =>
        !string.IsNullOrEmpty(Model.Filters.Creator) ||
        !string.IsNullOrEmpty(Model.Filters.Assignee) ||
        !string.IsNullOrEmpty(Model.Filters.Status) ||
        Model.Filters.OverdueOnly;

    private async Task HandleClearFilters()
    {
        Model.Filters.Creator = string.Empty;
        Model.Filters.Assignee = string.Empty;
        Model.Filters.Status = string.Empty;
        Model.Filters.OverdueOnly = false;
        await SearchWorkOrders();
    }
}
