using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Microsoft.AspNetCore.Components;
using Palermo.BlazorMvc;

namespace ClearMeasure.Bootcamp.UI.Shared;

public partial class NavMenu : AppComponentBase,
    IListener<UserLoggedInEvent>, IListener<UserLoggedOutEvent>
{
    /// <summary>
    /// Gets or sets whether the compact B3 work-order navigation is rendered.
    /// </summary>
    [Parameter]
    public bool IsWorkOrderSearchChrome { get; set; }

    [Inject] public IUserSession? UserSession { get; set; }

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private bool _collapseNavMenu = true;

    private string? NavMenuCssClass => _collapseNavMenu ? "collapse" : null;

    private void ToggleNavMenu()
    {
        _collapseNavMenu = !_collapseNavMenu;
    }

    private Employee? CurrentUser { get; set; }

    private string B3WorkOrderNavClass(B3WorkOrderFilter filter)
    {
        var relativeUri = Navigation.ToBaseRelativePath(Navigation.Uri);
        var isActive = filter switch
        {
            B3WorkOrderFilter.Mine =>
                !relativeUri.Contains("Assignee=", StringComparison.OrdinalIgnoreCase) &&
                !relativeUri.Contains("Status=", StringComparison.OrdinalIgnoreCase),
            B3WorkOrderFilter.AssignedToMe =>
                relativeUri.Contains("Assignee=", StringComparison.OrdinalIgnoreCase),
            B3WorkOrderFilter.InProgress =>
                relativeUri.Contains($"Status={WorkOrderStatus.InProgress.Key}", StringComparison.OrdinalIgnoreCase),
            _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, null)
        };

        return $"nav-link{(isActive ? " active" : string.Empty)}";
    }

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

    private enum B3WorkOrderFilter
    {
        Mine,
        AssignedToMe,
        InProgress
    }
}