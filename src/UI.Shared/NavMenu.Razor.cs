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

    [Inject] private NavigationManager Navigation { get; set; } = null!;

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
        return IsB3WorkOrderFilterActive(filter, relativeUri)
            ? "nav-link active"
            : "nav-link";
    }

    /// <summary>
    /// Resolves which B3 work-order filter link is active for the current relative URI.
    /// </summary>
    internal static bool IsB3WorkOrderFilterActive(B3WorkOrderFilter filter, string relativeUri)
    {
        var hasAssignee = relativeUri.Contains("Assignee=", StringComparison.OrdinalIgnoreCase);
        var hasStatus = relativeUri.Contains("Status=", StringComparison.OrdinalIgnoreCase);
        return filter switch
        {
            B3WorkOrderFilter.Mine => !hasAssignee && !hasStatus,
            B3WorkOrderFilter.AssignedToMe => hasAssignee,
            B3WorkOrderFilter.InProgress =>
                relativeUri.Contains($"Status={WorkOrderStatus.InProgress.Key}", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
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

    /// <summary>
    /// B3 sidebar filter targets for work-order search links.
    /// </summary>
    internal enum B3WorkOrderFilter
    {
        Mine,
        AssignedToMe,
        InProgress
    }
}