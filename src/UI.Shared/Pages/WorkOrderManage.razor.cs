using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.Core.Services.Impl;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Microsoft.AspNetCore.Components;
using Palermo.BlazorMvc;
using System.Globalization;

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
        var model = new WorkOrderManageModel
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
            CreatedDate = workOrder.CreatedDate?.ToString("G", CultureInfo.CurrentCulture),
            AssignedDate = workOrder.AssignedDate?.ToString("G", CultureInfo.CurrentCulture),
            CompletedDate = workOrder.CompletedDate?.ToString("G", CultureInfo.CurrentCulture),
            Deadline = workOrder.Deadline
        };

        if (workOrder.Deadline.HasValue)
        {
            model.DeadlineDate = workOrder.Deadline.Value.ToString("yyyy-MM-dd");
            var hour = workOrder.Deadline.Value.Hour;
            var isPm = hour >= 12;
            var hour12 = hour % 12;
            if (hour12 == 0) hour12 = 12;
            model.DeadlineTime = $"{hour12:D2}:{workOrder.Deadline.Value.Minute:D2}";
            model.DeadlineAmPm = isPm ? "PM" : "AM";
        }

        return model;
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
        workOrder.Deadline = ParseDeadline();

        var matchingCommand = new StateCommandList()
            .GetMatchingCommand(workOrder, currentUser, SelectedCommand!);

        var result = await Bus.Send(matchingCommand);
        EventBus.Notify(new WorkOrderChangedEvent(result));

        NavigationManager!.NavigateTo("/workorder/search");
    }

    private DateTime? ParseDeadline()
    {
        if (string.IsNullOrWhiteSpace(Model.DeadlineDate))
            return null;

        if (!DateTime.TryParse(Model.DeadlineDate, out var date))
            return null;

        var hour = 0;
        var minute = 0;

        if (!string.IsNullOrWhiteSpace(Model.DeadlineTime))
        {
            var timeParts = Model.DeadlineTime.Split(':');
            if (timeParts.Length >= 2)
            {
                int.TryParse(timeParts[0], out hour);
                int.TryParse(timeParts[1], out minute);
            }
        }

        if (Model.DeadlineAmPm == "PM" && hour < 12)
            hour += 12;
        else if (Model.DeadlineAmPm == "AM" && hour == 12)
            hour = 0;

        return new DateTime(date.Year, date.Month, date.Day, hour, minute, 0);
    }
}

public record WorkOrderChangedEvent(StateCommandResult Result) : IUiBusEvent
{
}