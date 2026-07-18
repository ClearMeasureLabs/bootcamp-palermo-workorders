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

[Route("/workrequest/manage/{id?}")]
public partial class WorkRequestManage : AppComponentBase, IAsyncDisposable
{
    private WorkRequest? _workRequest;
    private WorkRequestAttachment[] _attachments = [];
    private string _preferredLanguage = "en-US";
    private DictationTarget _dictationTarget = DictationTarget.None;
    [Inject] public IWorkRequestBuilder? WorkRequestBuilder { get; set; }
    [Inject] public IUserSession? UserSession { get; set; }
    [Inject] private NavigationManager? NavigationManager { get; set; }
    [Inject] public ITranslationService? TranslationService { get; set; }
    [Inject] public SpeechSynthesis? SpeechSynthesis { get; set; }
    [Inject] public SpeechRecognition? SpeechRecognition { get; set; }

    public WorkRequestManageModel Model { get; set; } = new();
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
        await LoadWorkRequest();

    }

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (_workRequest != null)
        {
            EventBus.Notify(new WorkRequestSelectedEvent(_workRequest));
        }
        return base.OnAfterRenderAsync(firstRender);
    }

    private async Task LoadWorkRequest()
    {
        var currentUser = (await UserSession!.GetCurrentUserAsync())!;
        _preferredLanguage = currentUser.PreferredLanguage;
        WorkRequest workRequest;

        if (CurrentMode == EditMode.New)
        {
            workRequest = WorkRequestBuilder!.CreateNewWorkRequest(currentUser);
            if (!string.IsNullOrEmpty(Id))
            {
                workRequest.Number = Id;
            }
        }
        else
        {
            workRequest = (await Bus.Send(new WorkRequestByNumberQuery(Id!)))!;
        }

        Model = CreateViewModel(CurrentMode, workRequest);
        var commandList = new StateCommandList();
        Model.IsReadOnly = !commandList!.GetValidStateCommands(workRequest, currentUser).Any();
        ValidCommands = commandList.GetValidStateCommands(workRequest, currentUser);
        _workRequest = workRequest;

        if (workRequest.Id != Guid.Empty)
        {
            _attachments = await Bus.Send(new WorkRequestAttachmentsQuery(workRequest.Id));
        }
    }

    private WorkRequestManageModel CreateViewModel(EditMode mode, WorkRequest workRequest)
    {
        return new WorkRequestManageModel
        {
            WorkRequest = workRequest,
            Mode = mode,
            WorkRequestNumber = workRequest.Number,
            Status = workRequest.Status!.FriendlyName,
            CreatorFullName = workRequest.Creator!.GetFullName(),
            AssignedToUserName = workRequest.Assignee?.UserName,
            Title = workRequest.Title,
            Description = workRequest.Description,
            Instructions = workRequest.Instructions,
            RoomNumber = workRequest.RoomNumber,
            CreatedDate = workRequest.CreatedDate?.ToString("G", CultureInfo.CurrentCulture),
            AssignedDate = workRequest.AssignedDate?.ToString("G", CultureInfo.CurrentCulture),
            CompletedDate = workRequest.CompletedDate?.ToString("G", CultureInfo.CurrentCulture)
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
        WorkRequest workRequest;

        if (Model.Mode == EditMode.New)
        {
            workRequest = WorkRequestBuilder!.CreateNewWorkRequest(currentUser);
        }
        else
        {
            workRequest = (await Bus.Send(new WorkRequestByNumberQuery(Model.WorkRequestNumber!)))!;
        }

        Employee? assignee = null;
        if (Model.AssignedToUserName != null)
        {
            assignee = await Bus.Send(new EmployeeByUserNameQuery(Model.AssignedToUserName));
        }

        workRequest.Number = Model.WorkRequestNumber;
        workRequest.Assignee = assignee;
        workRequest.Title = Model.Title;
        workRequest.Description = Model.Description;
        workRequest.Instructions = Model.Instructions;
        workRequest.RoomNumber = Model.RoomNumber;

        var matchingCommand = new StateCommandList()
            .GetMatchingCommand(workRequest, currentUser, SelectedCommand!);

        var result = await Bus.Send(matchingCommand);
        EventBus.Notify(new WorkRequestChangedEvent(result));

        NavigationManager!.NavigateTo("/workrequest/search");
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

public record WorkRequestChangedEvent(StateCommandResult Result) : IUiBusEvent
{
}