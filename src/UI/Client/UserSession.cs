using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using Microsoft.AspNetCore.Components;

namespace ClearMeasure.Bootcamp.UI.Client;

public class UserSession(
    IBus bus,
    CustomAuthenticationStateProvider authProvider,
    NavigationManager navigationManager)
    : IUserSession
{
    /// <inheritdoc />
    public async Task<Employee?> GetCurrentUserAsync()
    {
#pragma warning disable BL0013 // State is read on demand per call and never cached, so it cannot go stale.
        await authProvider.GetAuthenticationStateAsync();
#pragma warning restore BL0013
        var username = authProvider.GetUsername();
        if (string.IsNullOrEmpty(username))
        {
            return null;
        }

        var currentUser = await bus.Send(new EmployeeByUserNameQuery(username));
        BlowUpIfEmployeeCannotLogin(currentUser);
        return currentUser;
    }

    /// <summary>
    /// Clears the persisted login and navigates to the login page.
    /// </summary>
    public async Task LogOut()
    {
        await authProvider.Logout();
        navigationManager.NavigateTo("/login");
    }

    private void BlowUpIfEmployeeCannotLogin(Employee? employee)
    {
        if (employee == null)
        {
            throw new Exception("That user doesn't exist or is not valid.");
        }
    }
}