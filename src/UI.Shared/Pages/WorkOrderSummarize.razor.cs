using ClearMeasure.Bootcamp.Core.Queries;
using Microsoft.AspNetCore.Components;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

/// <summary>
/// Edit-mode control that requests a one-paragraph AI summary of the work order fields.
/// </summary>
public partial class WorkOrderSummarize
{
    private string? _summary;
    private bool _isSummarizing;

    /// <summary>
    /// Work order title sent to the LLM.
    /// </summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>
    /// Work order description sent to the LLM.
    /// </summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>
    /// Work order instructions sent to the LLM.
    /// </summary>
    [Parameter]
    public string? Instructions { get; set; }

    private async Task SummarizeAsync()
    {
        _isSummarizing = true;
        try
        {
            var result = await Bus.Send(new WorkOrderSummarizeQuery(
                Title ?? string.Empty,
                Description ?? string.Empty,
                Instructions ?? string.Empty));
            _summary = result.Summary;
        }
        finally
        {
            _isSummarizing = false;
        }
    }
}
