using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

[Route("/workorder/print/{id}")]
public partial class WorkOrderPrint : AppComponentBase
{
    [Parameter] public string? Id { get; set; }
    [Inject] private IJSRuntime? JSRuntime { get; set; }

    private WorkOrder? _workOrder;

    protected override async Task OnInitializedAsync()
    {
        if (Id != null)
        {
            _workOrder = await Bus.Send(new WorkOrderByNumberQuery(Id));
        }
    }

    private async Task PrintPage()
    {
        if (JSRuntime != null)
            await JSRuntime.InvokeVoidAsync("window.print");
    }
}
