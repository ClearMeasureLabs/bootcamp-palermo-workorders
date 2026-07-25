using System.Globalization;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

[Route("/profile")]
[Authorize]
public partial class Profile : AppComponentBase
{
    [Inject]
    private CustomAuthenticationStateProvider AuthStateProvider { get; set; } = default!;

    private Employee? _employee;

    public enum Elements
    {
        FullName,
        Username,
        Email,
        LastLogin,
        FirstLoginHelper
    }

    protected override async Task OnInitializedAsync()
    {
        var username = AuthStateProvider.GetUsername();
        if (string.IsNullOrEmpty(username))
        {
            return;
        }

        _employee = await Bus.Send(new EmployeeByUserNameQuery(username));
    }

    private static string FormatLastLogin(Employee employee)
    {
        var culture = new CultureInfo(employee.PreferredLanguage);
        return employee.LastLoginUtc!.Value.ToLocalTime().ToString("MMM d, yyyy 'at' h:mm tt", culture);
    }
}
