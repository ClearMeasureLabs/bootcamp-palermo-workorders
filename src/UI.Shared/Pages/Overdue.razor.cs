using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

/// <summary>
/// Lists open work orders whose due date is before today.
/// </summary>
[Route("/workorders/overdue")]
[Authorize]
public partial class Overdue : AppComponentBase
{
    private WorkOrder[] _results = [];

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchOverdueOnly(true);
        _results = await Bus.Send(specification);
    }
}
