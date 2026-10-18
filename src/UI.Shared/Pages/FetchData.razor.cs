using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

[Route("/fetchdata")]
public partial class FetchData : AppComponentBase
{
    public WeatherForecast[]? Model { get; private set; }

    [Inject] public IBus? ApplicationBus { get; set; }

    public enum Elements
    {
        ForecastSourceNote,
        ForecastTableTooltip,
        Refresh
    }

    protected override async Task OnInitializedAsync()
    {
        Logger.LogInformation("FetchDataController");
        await LoadForecastsAsync();
        //TODO: Make Azure OpenAI query for current weather in Austin, TX
    }

    private async Task RefreshAsync()
    {
        await LoadForecastsAsync();
    }

    private async Task LoadForecastsAsync()
    {
        Model = await ApplicationBus!.Send(new ForecastQuery());
    }
}
