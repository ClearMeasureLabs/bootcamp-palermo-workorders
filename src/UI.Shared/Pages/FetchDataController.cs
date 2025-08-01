using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Palermo.BlazorMvc;
using ProgrammingWithPalermo.ChurchBulletin.Core.Model;

namespace UI.Shared.Pages;

[Route("/fetchdata")]
public class FetchDataController : ControllerComponentBase<FetchDataView>
{
    private WeatherForecast[]? _forecasts;
    [Inject] public HttpClient? Http { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Logger.LogInformation("FetchDataController");
        Debug.Assert(Http != null, nameof(Http) + " != null");
        _forecasts = await Http.GetFromJsonAsync<WeatherForecast[]>("WeatherForecast");
        View.Model = _forecasts;
    }
}