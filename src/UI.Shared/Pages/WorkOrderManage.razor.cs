using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.Core.Services.Impl;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Microsoft.AspNetCore.Components;
using Palermo.BlazorMvc;
using Microsoft.JSInterop;
using System.Globalization;
using Toolbelt.Blazor.SpeechRecognition;
using Toolbelt.Blazor.SpeechSynthesis;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

[Route("/workorder/manage/{id?}")]
public partial class WorkOrderManage : AppComponentBase, IAsyncDisposable
{
    private WorkOrder? _workOrder;
    private WorkOrderAttachment[] _attachments = [];
    private string _preferredLanguage = "en-US";
    private DictationTarget _dictationTarget = DictationTarget.None;
    [Inject] public IWorkOrderBuilder? WorkOrderBuilder { get; set; }
    [Inject] public IUserSession? UserSession { get; set; }
    [Inject] private NavigationManager? NavigationManager { get; set; }
    [Inject] public ITranslationService? TranslationService { get; set; }
    [Inject] public SpeechSynthesis? SpeechSynthesis { get; set; }
    [Inject] public SpeechRecognition? SpeechRecognition { get; set; }

    public WorkOrderManageModel Model { get; set; } = new();
    public List<SelectListItem> UserOptions { get; set; } = new();
    public IEnumerable<IStateCommand> ValidCommands { get; set; } = new List<IStateCommand>();
    public string? SelectedCommand { get; set; }

    [Parameter] public string? Id { get; set; }

    [SupplyParameterFromQuery] public string? Mode { get; set; }
    public EditMode CurrentMode => Mode?.ToLower() == "edit" ? EditMode.Edit : EditMode.New;

    protected override async Task OnInitializedAsync()
    {
        if (SpeechRecognition != null)
        {
            SpeechRecognition.Result += OnSpeechResult;
            SpeechRecognition.End += OnSpeechEnd;
        }

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
            Instructions = workOrder.Instructions,
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
            var voices = await SpeechSynthesis.GetVoicesAsync();
            var utterance = WorkOrderSpeechHelper.CreateUtterance(translatedText, _preferredLanguage, voices);
            await SpeechSynthesis.SpeakAsync(utterance);
        }
        catch
        {
            // Speech synthesis may not be available in all environments
        }
    }

    private async Task DictateTitleAsync()
    {
        await ToggleDictationAsync(DictationTarget.Title);
    }

    private async Task DictateDescriptionAsync()
    {
        await ToggleDictationAsync(DictationTarget.Description);
    }

    private async Task ToggleDictationAsync(DictationTarget target)
    {
        if (SpeechRecognition == null)
        {
            return;
        }

        if (_dictationTarget == target)
        {
            _dictationTarget = DictationTarget.None;
            try
            {
                await SpeechRecognition.StopAsync();
            }
            catch
            {
                // Speech recognition may not be available in all environments
            }
            return;
        }

        SpeechRecognition.Lang = _preferredLanguage;
        SpeechRecognition.Continuous = false;
        SpeechRecognition.InterimResults = false;
        _dictationTarget = target;

        try
        {
            await SpeechRecognition.StartAsync();
        }
        catch
        {
            // Speech recognition may not be available in all environments
            _dictationTarget = DictationTarget.None;
        }
    }

    private void OnSpeechResult(object? sender, SpeechRecognitionEventArgs args)
    {
        var transcripts = (args.Results ?? [])
            .Skip(args.ResultIndex)
            .Where(result => result.IsFinal)
            .Select(result => result.Items?.FirstOrDefault()?.Transcript?.Trim())
            .Where(transcript => !string.IsNullOrEmpty(transcript));
        var transcript = string.Join(" ", transcripts);

        if (string.IsNullOrEmpty(transcript))
        {
            return;
        }

        if (_dictationTarget == DictationTarget.Title)
        {
            Model.Title = AppendTranscript(Model.Title, transcript);
        }
        else if (_dictationTarget == DictationTarget.Description)
        {
            Model.Description = AppendTranscript(Model.Description, transcript);
        }

        InvokeAsync(StateHasChanged);
    }

    private void OnSpeechEnd(object? sender, EventArgs e)
    {
        _dictationTarget = DictationTarget.None;
        InvokeAsync(StateHasChanged);
    }

    private static string AppendTranscript(string? existingText, string transcript)
    {
        return string.IsNullOrEmpty(existingText) ? transcript : $"{existingText} {transcript}";
    }

    private string DictateButtonClass(DictationTarget target)
    {
        return _dictationTarget == target ? "btn btn-sm btn-danger" : "btn btn-sm btn-outline-secondary";
    }

    private string DictateAriaPressed(DictationTarget target)
    {
        return _dictationTarget == target ? "true" : "false";
    }

    public async ValueTask DisposeAsync()
    {
        if (SpeechRecognition == null)
        {
            return;
        }

        SpeechRecognition.Result -= OnSpeechResult;
        SpeechRecognition.End -= OnSpeechEnd;

        try
        {
            if (_dictationTarget != DictationTarget.None)
            {
                await SpeechRecognition.StopAsync();
            }

            await SpeechRecognition.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }
    }

    private enum DictationTarget
    {
        None,
        Title,
        Description
    }
}

public record WorkOrderChangedEvent(StateCommandResult Result) : IUiBusEvent
{
}