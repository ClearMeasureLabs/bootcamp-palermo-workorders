using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

[Route("/workrequest/search")]
[Authorize]
public partial class WorkRequestSearch : AppComponentBase
{
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
        StatusOptions = WorkRequestStatus.GetAllItems().Select(s => new SelectListItem(s.Key, s.FriendlyName)).ToList();
        Model = new WorkRequestSearchModel();

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
        await SearchWorkRequests();
    }

    private async Task SearchWorkRequests()
    {
        var creator = !string.IsNullOrWhiteSpace(Model.Filters.Creator)
            ? await Bus.Send(new EmployeeByUserNameQuery(Model.Filters.Creator))
            : null;

        var assignee = !string.IsNullOrWhiteSpace(Model.Filters.Assignee)
            ? await Bus.Send(new EmployeeByUserNameQuery(Model.Filters.Assignee))
            : null;

        var status = !string.IsNullOrWhiteSpace(Model.Filters.Status)
            ? WorkRequestStatus.FromKey(Model.Filters.Status)
            : null;

        var specification = new WorkRequestSpecificationQuery();
        specification.MatchCreator(creator);
        specification.MatchAssignee(assignee);
        specification.MatchStatus(status);

        Model.Results = await Bus.Send(specification);
        StateHasChanged();
    }

    private async Task HandleSearch()
    {
        await SearchWorkRequests();
    }
}