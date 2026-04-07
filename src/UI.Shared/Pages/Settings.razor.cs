using ClearMeasure.Bootcamp.UI.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

[Route("/settings")]
[Authorize]
public partial class Settings : AppComponentBase
{
    [Inject]
    private ThemePreferenceService Theme { get; set; } = default!;

    public enum Elements
    {
        DarkModeSwitch
    }

    protected override async Task OnInitializedAsync()
    {
        await Theme.InitializeAsync();
    }

    private async Task OnDarkModeChanged(ChangeEventArgs e)
    {
        var enabled = e.Value is bool b && b;
        await Theme.SetDarkModeAsync(enabled);
        await InvokeAsync(StateHasChanged);
    }
}
