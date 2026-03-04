using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using Microsoft.AspNetCore.Components;
using Palermo.BlazorMvc;
using System.ComponentModel.DataAnnotations;

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
            NewTemplate.Title ?? "",
            NewTemplate.Description,
            NewTemplate.RoomNumber,
            currentUser.Id));

        NewTemplate = new TemplateFormModel();
        await LoadTemplates();
    }
}

public class TemplateFormModel
{
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(300)]
    public string? Title { get; set; }

    [StringLength(4000)]
    public string? Description { get; set; }

    [StringLength(50)]
    public string? RoomNumber { get; set; }
}
