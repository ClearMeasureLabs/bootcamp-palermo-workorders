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
        AverageTemperature
    }

    /// <summary>
    ///     Average Celsius temperature across loaded forecasts, rounded to whole degrees.
    /// </summary>
    public int AverageTemperatureC =>
        Model is { Length: > 0 }
            ? (int)Math.Round(Model.Average(static f => f.TemperatureC))
            : 0;

    protected override async Task OnInitializedAsync()
    {
        Logger.LogInformation("FetchDataController");
        Model = await ApplicationBus!.Send(new ForecastQuery());
        //TODO: Make Azure OpenAI query for current weather in Austin, TX
    }
}