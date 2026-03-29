using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.Core.Services.Impl;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Microsoft.AspNetCore.Components;
using Palermo.BlazorMvc;
using System.Globalization;
using Toolbelt.Blazor.SpeechSynthesis;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

[Route("/workorder/manage/{id?}")]
public partial class WorkOrderManage : AppComponentBase
{
    private WorkOrder? _workOrder;
    private WorkOrderAttachment[] _attachments = [];
    private string _preferredLanguage = "en-US";
    [Inject] public IWorkOrderBuilder? WorkOrderBuilder { get; set; }
    [Inject] public IUserSession? UserSession { get; set; }
    [Inject] private NavigationManager? NavigationManager { get; set; }
    [Inject] public ITranslationService? TranslationService { get; set; }
    [Inject] public SpeechSynthesis? SpeechSynthesis { get; set; }

    public WorkOrderManageModel Model { get; set; } = new();
    public List<SelectListItem> UserOptions { get; set; } = new();
    public IEnumerable<IStateCommand> ValidCommands { get; set; } = new List<IStateCommand>();
    public string? SelectedCommand { get; set; }

    private bool _showConfirmationModal;
    private WorkOrder? _inProgressWorkOrder;
    private Employee? _pendingAssignee;
    private WorkOrder? _pendingWorkOrder;
    private IStateCommand? _pendingCommand;

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
        _preferredLanguage = currentUser.PreferredLanguage;
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

        if (workOrder.Id != Guid.Empty)
        {
            _attachments = await Bus.Send(new WorkOrderAttachmentsQuery(workOrder.Id));
        }
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
            RoomNumber = workOrder.RoomNumber,
            CreatedDate = workOrder.CreatedDate?.ToString("G", CultureInfo.CurrentCulture),
            AssignedDate = workOrder.AssignedDate?.ToString("G", CultureInfo.CurrentCulture),
            CompletedDate = workOrder.CompletedDate?.ToString("G", CultureInfo.CurrentCulture)
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

        if (assignee != null)
        {
            _inProgressWorkOrder = await Bus.Send(new EmployeeInProgressWorkOrderQuery(assignee));
            if (_inProgressWorkOrder != null)
            {
                _pendingAssignee = assignee;
                _pendingWorkOrder = workOrder;
                _pendingWorkOrder.Number = Model.WorkOrderNumber;
                _pendingWorkOrder.Title = Model.Title;
                _pendingWorkOrder.Description = Model.Description;
                _pendingWorkOrder.RoomNumber = Model.RoomNumber;
                _pendingCommand = new StateCommandList()
                    .GetMatchingCommand(_pendingWorkOrder, currentUser, SelectedCommand!);
                _showConfirmationModal = true;
                StateHasChanged();
                return;
            }
        }

        workOrder.Number = Model.WorkOrderNumber;
        workOrder.Assignee = assignee;
        workOrder.Title = Model.Title;
        workOrder.Description = Model.Description;
        workOrder.RoomNumber = Model.RoomNumber;

        var matchingCommand = new StateCommandList()
            .GetMatchingCommand(workOrder, currentUser, SelectedCommand!);

        var result = await Bus.Send(matchingCommand);
        EventBus.Notify(new WorkOrderChangedEvent(result));

        NavigationManager!.NavigateTo("/workorder/search");
    }

    private async Task OnConfirmAssignment()
    {
        if (_pendingAssignee != null && _pendingWorkOrder != null && _pendingCommand != null)
        {
            _pendingWorkOrder.Assignee = _pendingAssignee;
            var result = await Bus.Send(_pendingCommand);
            EventBus.Notify(new WorkOrderChangedEvent(result));
            _showConfirmationModal = false;
            NavigationManager!.NavigateTo("/workorder/search");
        }
    }

    private void OnCancelAssignment()
    {
        _showConfirmationModal = false;
        _inProgressWorkOrder = null;
        _pendingAssignee = null;
        _pendingWorkOrder = null;
        _pendingCommand = null;
        StateHasChanged();
    }

    private async Task SpeakTitleAsync()
    {
        await SpeakTextAsync(Model.Title);
    }

    private async Task SpeakDescriptionAsync()
    {
        await SpeakTextAsync(Model.Description);
    }

    private async Task SpeakTextAsync(string? text)
    {
        if (string.IsNullOrEmpty(text) || SpeechSynthesis == null || TranslationService == null)
        {
            return;
        }

        var translatedText = await TranslationService.TranslateAsync(text, _preferredLanguage);

        try
        {
            var utterance = new SpeechSynthesisUtterance
            {
                Text = translatedText,
                Lang = _preferredLanguage
            };

            var voices = await SpeechSynthesis.GetVoicesAsync();
            var langPrefix = _preferredLanguage.Split('-')[0];
            var matchingVoice = voices.FirstOrDefault(v => v.Lang?.StartsWith(langPrefix, StringComparison.OrdinalIgnoreCase) == true);
            if (matchingVoice != null)
            {
                utterance.Voice = matchingVoice;
            }

            await SpeechSynthesis.SpeakAsync(utterance);
        }
        catch
        {
            // Speech synthesis may not be available in all environments
        }
    }
}

public record WorkOrderChangedEvent(StateCommandResult Result) : IUiBusEvent
{
}