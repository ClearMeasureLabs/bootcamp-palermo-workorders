using BlazorApplicationInsights.Interfaces;
using BlazorApplicationInsights.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Palermo.BlazorMvc;

namespace UI.Shared.Pages;

[Route("/counter")]
public class CounterController : ControllerComponentBase<CounterView>
{
    private int _currentCount;

    protected override void OnViewInitialized()
    {
        View.Model = _currentCount;
        View.OnIncrement = IncrementCount;
        Logger.LogInformation("Counter Page init");
    }

    private void IncrementCount()
    {
        _currentCount++;
        Logger.LogInformation("Increment Count to: " + _currentCount);
        View.Model = _currentCount;
    }
}