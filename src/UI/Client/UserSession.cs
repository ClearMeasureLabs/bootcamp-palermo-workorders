using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;

namespace ClearMeasure.Bootcamp.UI.Client;

public class UserSession(
    IBus bus,
    CustomAuthenticationStateProvider authProvider)
    : IUserSession
{
    public async Task<Employee?> GetCurrentUserAsync()
    {
        var username = authProvider.GetUsername();
        if (string.IsNullOrEmpty(username))
        {
            return null;
        }

        var currentUser = await bus.Send(new EmployeeByUserNameQuery(username));
        BlowUpIfEmployeeCannotLogin(currentUser);
        return currentUser;
    }

    private void BlowUpIfEmployeeCannotLogin(Employee? employee)
    {
        if (employee == null)
        {
            throw new Exception("That user doesn't exist or is not valid.");
        }
    }
}