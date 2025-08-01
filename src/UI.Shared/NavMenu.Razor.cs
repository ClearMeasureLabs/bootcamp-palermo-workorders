using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Microsoft.AspNetCore.Components;
using Palermo.BlazorMvc;

namespace ClearMeasure.Bootcamp.UI.Shared;

public partial class NavMenu : AppComponentBase, 
    IListener<UserLoggedInEvent>, IListener<UserLoggedOutEvent>
{
    [Inject]
    public IUserSession? UserSession { get; set; }

    private bool collapseNavMenu = true;

    private string? NavMenuCssClass => collapseNavMenu ? "collapse" : null;

    private void ToggleNavMenu()
    {
        collapseNavMenu = !collapseNavMenu;
    }

    private Employee? CurrentUser { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await SetCurrentUser();
    }

    private async Task SetCurrentUser()
    {
        CurrentUser = await UserSession!.GetCurrentUserAsync();
    }

    public void Handle(UserLoggedInEvent theEvent)
    {
        InvokeAsync(async () =>
        {
            await SetCurrentUser();
            StateHasChanged();
        });
    }

    public void Handle(UserLoggedOutEvent theEvent)
    {
        CurrentUser = null;
        StateHasChanged();
    }
}