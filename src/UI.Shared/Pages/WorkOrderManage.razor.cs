using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.Core.Services.Impl;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Microsoft.AspNetCore.Components;
using Palermo.BlazorMvc;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

[Route("/workorder/manage/{id?}")]
public partial class WorkOrderManage : AppComponentBase
{
    private WorkOrder? _workOrder;
    [Inject] public IWorkOrderBuilder? WorkOrderBuilder { get; set; }
    [Inject] public IUserSession? UserSession { get; set; }
    [Inject] private NavigationManager? NavigationManager { get; set; }

    public WorkOrderManageModel Model { get; set; } = new();
    public List<SelectListItem> UserOptions { get; set; } = new();
    public IEnumerable<IStateCommand> ValidCommands { get; set; } = new List<IStateCommand>();
    public string? SelectedCommand { get; set; }

    [Parameter] public string? Id { get; set; }

    [SupplyParameterFromQuery] public string? Mode { get; set; }
    public EditMode CurrentMode => Mode?.ToLower() == "edit" ? EditMode.Edit : EditMode.New;

    protected override async Task OnInitializedAsync()
    {
        await LoadUserOptions();
        await LoadWorkOrder();

    }

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (_workOrder != null)
        {
            EventBus.Notify(new WorkOrderSelectedEvent(_workOrder));
        }
        return base.OnAfterRenderAsync(firstRender);
    }

    private async Task LoadWorkOrder()
    {
        var currentUser = (await UserSession!.GetCurrentUserAsync())!;
        WorkOrder workOrder;

        if (CurrentMode == EditMode.New)
        {
            workOrder = WorkOrderBuilder!.CreateNewWorkOrder(currentUser);
            if (!string.IsNullOrEmpty(Id))
            {
                workOrder.Number = Id;
            }
        }
        else
        {
            workOrder = (await Bus.Send(new WorkOrderByNumberQuery(Id!)))!;
        }

        Model = CreateViewModel(CurrentMode, workOrder);
        var commandList = new StateCommandList();
        Model.IsReadOnly = !commandList!.GetValidStateCommands(workOrder, currentUser).Any();
        ValidCommands = commandList.GetValidStateCommands(workOrder, currentUser);
        _workOrder = workOrder;
        
    }

    private WorkOrderManageModel CreateViewModel(EditMode mode, WorkOrder workOrder)
    {
        return new WorkOrderManageModel
        {
            WorkOrder = workOrder,
            Mode = mode,
            WorkOrderNumber = workOrder.Number,
            Status = workOrder.Status!.FriendlyName,
            CreatorFullName = workOrder.Creator!.GetFullName(),
            AssignedToUserName = workOrder.Assignee?.UserName,
            Title = workOrder.Title,
            Description = workOrder.Description,
            Instructions = workOrder.Instructions,
            RoomNumber = workOrder.RoomNumber,
            CreatedDate = workOrder.CreatedDate.ToString(),
            AssignedDate = workOrder.AssignedDate?.ToString(),
            CompletedDate = workOrder.CompletedDate?.ToString()
        };
    }

    private async Task LoadUserOptions()
    {
        var employees = await Bus.Send(new EmployeeGetAllQuery());
        var items = employees.Select(e => new SelectListItem(e.UserName, e.GetFullName())).ToList();
        items.Insert(0, new SelectListItem("", ""));
        UserOptions = items;
    }

    private async Task HandleSubmit()
    {
        var currentUser = (await UserSession!.GetCurrentUserAsync())!;
        WorkOrder workOrder;

        if (Model.Mode == EditMode.New)
        {
            workOrder = WorkOrderBuilder!.CreateNewWorkOrder(currentUser);
        }
        else
        {
            workOrder = (await Bus.Send(new WorkOrderByNumberQuery(Model.WorkOrderNumber!)))!;
        }

        Employee? assignee = null;
        if (Model.AssignedToUserName != null)
        {
            assignee = await Bus.Send(new EmployeeByUserNameQuery(Model.AssignedToUserName));
        }

        workOrder.Number = Model.WorkOrderNumber;
        workOrder.Assignee = assignee;
        workOrder.Title = Model.Title;
        workOrder.Description = Model.Description;
        workOrder.Instructions = Model.Instructions;
        workOrder.RoomNumber = Model.RoomNumber;

        var matchingCommand = new StateCommandList()
            .GetMatchingCommand(workOrder, currentUser, SelectedCommand!);

        var result = await Bus.Send(matchingCommand);
        EventBus.Notify(new WorkOrderChangedEvent(result));

        NavigationManager!.NavigateTo("/workorder/search");
    }
}

public record WorkOrderChangedEvent(StateCommandResult Result) : IUiBusEvent
{
}