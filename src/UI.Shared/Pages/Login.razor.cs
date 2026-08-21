using System.ComponentModel.DataAnnotations;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Microsoft.AspNetCore.Components;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

[Route("/login")]
public partial class Login : AppComponentBase
{
    [Inject] public CustomAuthenticationStateProvider? AuthStateProvider { get; set; }
    [Inject] public NavigationManager? NavigationManager { get; set; }

    public readonly LoginModel loginModel = new();
    public string? errorMessage;
    public Employee[] employees = Array.Empty<Employee>();

    protected override async Task OnInitializedAsync()
    {
        await LoadEmployees();
    }

    private async Task LoadEmployees()
    {
        try
        {
            employees = await Bus.Send(new EmployeeGetAllQuery());
        }
        catch (Exception ex)
        {
            errorMessage = "Error loading employees: " + ex.Message;
        }
    }

    /// <summary>
    /// Display-only formatting for the login member select: uppercase to match mainframe all-caps; does not alter stored names.
    /// </summary>
    private static string GetLoginDropdownDisplayName(Employee employee)
    {
        return LoginDisplayNameFormatter.FormatForLoginDropdown(employee.GetFullName());
    }

    private const string TimothyLovejoyUsername = "tlovejoy";

    private async Task LoginAsTimothyLovejoy()
    {
        loginModel.Username = TimothyLovejoyUsername;
        await AuthenticateAndNavigate();
    }

    private async Task HandleLogin()
    {
        if (string.IsNullOrEmpty(loginModel.Username))
        {
            errorMessage = "Please select an employee";
            return;
        }

        await AuthenticateAndNavigate();
    }

    private async Task AuthenticateAndNavigate()
    {
        var selectedEmployee = employees.FirstOrDefault(e => e.UserName == loginModel.Username);
        if (selectedEmployee != null)
        {
            AuthStateProvider!.Login(loginModel.Username);
            EventBus.Notify(new UserLoggedInEvent(loginModel.Username));
            await Bus.Publish(new Core.Model.Events.UserLoggedInEvent(loginModel.Username));
            NavigationManager!.NavigateTo("/");
        }
        else
        {
            errorMessage = "Invalid employee selection";
        }
    }

    public class LoginModel
    {
        [Required(ErrorMessage = "Employee selection is required")]
        public string Username { get; set; } = string.Empty;
    }
}