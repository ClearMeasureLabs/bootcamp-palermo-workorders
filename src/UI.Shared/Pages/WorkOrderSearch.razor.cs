using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

[Route("/workorder/search")]
[Authorize]
public partial class WorkOrderSearch : AppComponentBase
{
    [SupplyParameterFromQuery] public string? Creator { get; set; }
    [SupplyParameterFromQuery] public string? Assignee { get; set; }
    [SupplyParameterFromQuery] public string? Status { get; set; }
    [SupplyParameterFromQuery] public string? RoomId { get; set; }

    protected override void OnParametersSet()
    {
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        OnSearch = HandleSearch;

        var employees = await Bus.Send(new EmployeeGetAllQuery());
        UserOptions = employees.Select(e => new SelectListItem(e.UserName, e.GetFullName())).ToList();
        StatusOptions = WorkOrderStatus.GetAllItems().Select(s => new SelectListItem(s.Key, s.FriendlyName)).ToList();
        
        var rooms = await Bus.Send(new RoomGetAllQuery());
        RoomOptions = rooms.Select(r => new SelectListItem(r.Id.ToString(), r.Name)).ToList();
        
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

        if (!string.IsNullOrEmpty(RoomId))
        {
            Model.Filters.RoomId = RoomId;
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

        Room? room = null;
        if (!string.IsNullOrWhiteSpace(Model.Filters.RoomId) && Guid.TryParse(Model.Filters.RoomId, out var roomId))
        {
            var allRooms = await Bus.Send(new RoomGetAllQuery());
            room = allRooms.FirstOrDefault(r => r.Id == roomId);
        }

        var specification = new WorkOrderSpecificationQuery();
        specification.MatchCreator(creator);
        specification.MatchAssignee(assignee);
        specification.MatchStatus(status);
        specification.MatchRoom(room);

        Model.Results = await Bus.Send(specification);
        StateHasChanged();
    }

    private async Task HandleSearch()
    {
        await SearchWorkOrders();
    }
}