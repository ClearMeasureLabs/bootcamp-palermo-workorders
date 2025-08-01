using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Palermo.BlazorMvc;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

[Route("/fetchdata")]
public partial class FetchData : AppComponentBase
{
    public WeatherForecast[]? Model { get; set; }

    [Inject] public IBus? ApplicationBus { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Logger.LogInformation("FetchDataController");
        Model = await ApplicationBus!.Send(new ForecastQuery());
        //TODO: Make Azure OpenAI query for current weather in Austin, TX
    }
}