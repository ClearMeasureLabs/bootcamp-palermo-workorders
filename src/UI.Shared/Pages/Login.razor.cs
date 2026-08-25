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

    public readonly LoginModel LoginModelValue = new();
    public string? ErrorMessage;
    public Employee[] Employees = Array.Empty<Employee>();

    // #region agent log
    /// <summary>Debug-only: whether <see cref="LoadEmployees"/> finished (success or failure).</summary>
    public bool EmployeesLoadCompleted { get; private set; }

    /// <summary>Debug-only: last auth branch taken by Lovejoy/dropdown login.</summary>
    public string? LastAuthOutcome { get; private set; }
    // #endregion

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
        // #region agent log
        finally
        {
            EmployeesLoadCompleted = true;
        }
        // #endregion
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
        LoginModelValue.Username = TimothyLovejoyUsername;
        await AuthenticateAndNavigate();
    }

    private async Task HandleLogin()
    {
        if (string.IsNullOrEmpty(LoginModelValue.Username))
        {
            ErrorMessage = "Please select an employee";
            return;
        }

        await AuthenticateAndNavigate();
    }

    private async Task AuthenticateAndNavigate()
    {
        var selectedEmployee = Employees.FirstOrDefault(e => e.UserName == LoginModelValue.Username);
        if (selectedEmployee != null)
        {
            // #region agent log
            LastAuthOutcome =
                $"ok;employees={Employees.Length};hasTlovejoy={Employees.Any(e => e.UserName == TimothyLovejoyUsername)};loadDone={EmployeesLoadCompleted}";
            // #endregion
            AuthStateProvider!.Login(LoginModelValue.Username);
            EventBus.Notify(new UserLoggedInEvent(LoginModelValue.Username));
            await Bus.Publish(new Core.Model.Events.UserLoggedInEvent(LoginModelValue.Username));
            NavigationManager!.NavigateTo("/");
        }
        else
        {
            ErrorMessage = "Invalid employee selection";
            // #region agent log
            LastAuthOutcome =
                $"reject;employees={Employees.Length};hasTlovejoy={Employees.Any(e => e.UserName == TimothyLovejoyUsername)};loadDone={EmployeesLoadCompleted};username={LoginModelValue.Username}";
            // #endregion
        }
    }

    public class LoginModel
    {
        [Required(ErrorMessage = "Employee selection is required")]
        public string Username { get; set; } = string.Empty;
    }
}