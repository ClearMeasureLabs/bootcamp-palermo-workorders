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
    [Inject] public TimeProvider Clock { get; set; } = TimeProvider.System;

    [SupplyParameterFromQuery] public string? Creator { get; set; }
    [SupplyParameterFromQuery] public string? Assignee { get; set; }
    [SupplyParameterFromQuery] public string? Status { get; set; }

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

        var workOrders = await Bus.Send(specification);
        Model.Results = workOrders.Select(MapSearchRow).ToArray();
        StateHasChanged();
    }

    private WorkOrderSearchResultRow MapSearchRow(WorkOrder workOrder)
    {
        var urgency = DueDateUrgencyCalculator.Calculate(workOrder, Clock);
        return new WorkOrderSearchResultRow
        {
            WorkOrder = workOrder,
            DueDateDisplay = workOrder.DueDate?.ToString("MMM d, yyyy", CultureInfo.InvariantCulture),
            DueDateCssClass = DueDateUrgencyCalculator.CssClass(urgency),
            DueDateUrgencyText = DueDateUrgencyCalculator.ScreenReaderText(urgency)
        };
    }

    private async Task HandleSearch()
    {
        await SearchWorkOrders();
    }
}
