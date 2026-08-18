using System.ComponentModel.DataAnnotations;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Microsoft.AspNetCore.Components;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

[Route("/login")]
public partial class Login : AppComponentBase
{
    [Inject] public CustomAuthenticationStateProvider? AuthStateProvider { get; set; }
    [Inject] public NavigationManager? NavigationManager { get; set; }

    // Qodana InconsistentNaming: declined — renaming to "LoginModel" would collide with the
    // nested LoginModel type below, so the field keeps its current name.
    public readonly LoginModel loginModel = new();
    public string? ErrorMessage;
    public Employee[] Employees = Array.Empty<Employee>();

    protected override async Task OnInitializedAsync()
    {
        await LoadEmployees();
    }

    private async Task LoadEmployees()
    {
        try
        {
            Employees = await Bus.Send(new EmployeeGetAllQuery());
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error loading employees: " + ex.Message;
        }
    }

    /// <summary>
    /// Display-only formatting for the login member select: uppercase to match mainframe all-caps; does not alter stored names.
    /// </summary>
    private static string GetLoginDropdownDisplayName(Employee employee)
    {
        return LoginDisplayNameFormatter.FormatForLoginDropdown(employee.GetFullName());
    }

    private async Task HandleLogin()
    {
        if (string.IsNullOrEmpty(loginModel.Username))
        {
            ErrorMessage = "Please select an employee";
            return;
        }

        // Find the selected employee
        var selectedEmployee = Employees.FirstOrDefault(e => e.UserName == loginModel.Username);
        if (selectedEmployee != null)
        {
            // Successful login
            AuthStateProvider!.Login(loginModel.Username);
            EventBus.Notify(new UserLoggedInEvent(loginModel.Username));
            await Bus.Publish(new Core.Model.Events.UserLoggedInEvent(loginModel.Username));
            NavigationManager!.NavigateTo("/");
        }
        else
        {
            // Failed login
            ErrorMessage = "Invalid employee selection";
        }
    }

    public class LoginModel
    {
        [Required(ErrorMessage = "Employee selection is required")]
        public string Username { get; set; } = string.Empty;
    }
}