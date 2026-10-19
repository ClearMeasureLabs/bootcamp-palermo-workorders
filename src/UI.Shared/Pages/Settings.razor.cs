using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

[Route("/settings")]
[Authorize]
public partial class Settings : AppComponentBase
{
    [Inject]
    private ThemePreferenceService Theme { get; set; } = null!;

    [Inject]
    private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;

    private string _preferredLanguage = "en-US";
    private string? _userName;

    public enum Elements
    {
        DarkModeSwitch,
        SettingsSubtitle,
        PreferredLanguageSelect
    }

    protected override async Task OnInitializedAsync()
    {
        await Theme.InitializeAsync();

        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        _userName = authState.User.Identity?.Name;
        if (string.IsNullOrEmpty(_userName))
        {
            return;
        }

        var employee = await Bus.Send(new EmployeeByUserNameQuery(_userName));
        _preferredLanguage = employee.PreferredLanguage;
    }

    private async Task OnDarkModeChanged(ChangeEventArgs e)
    {
        var enabled = e.Value is true;
        await Theme.SetDarkModeAsync(enabled);
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnPreferredLanguageChanged(ChangeEventArgs e)
    {
        if (e.Value is not string language || string.IsNullOrEmpty(_userName))
        {
            return;
        }

        _preferredLanguage = language;
        await Bus.Send(new UpdatePreferredLanguageCommand(_userName, language));
    }
}
