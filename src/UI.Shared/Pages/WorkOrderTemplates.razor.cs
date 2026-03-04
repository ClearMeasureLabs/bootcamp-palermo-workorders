using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using Microsoft.AspNetCore.Components;
using Palermo.BlazorMvc;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

[Route("/workorder/templates")]
public partial class WorkOrderTemplates : AppComponentBase
{
    [Inject] public IUserSession? UserSession { get; set; }

    public WorkOrderTemplate[] Templates { get; set; } = [];

    public TemplateFormModel NewTemplate { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadTemplates();
    }

    private async Task LoadTemplates()
    {
        Templates = await Bus.Send(new WorkOrderTemplatesQuery());
    }

    private async Task HandleCreateTemplate()
    {
        var currentUser = (await UserSession!.GetCurrentUserAsync())!;

        await Bus.Send(new CreateWorkOrderTemplateCommand(
            NewTemplate.Title!,
            NewTemplate.Description,
            NewTemplate.RoomNumber,
            currentUser.Id));

        NewTemplate = new TemplateFormModel();
        await LoadTemplates();
    }
}

public class TemplateFormModel
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? RoomNumber { get; set; }
}
