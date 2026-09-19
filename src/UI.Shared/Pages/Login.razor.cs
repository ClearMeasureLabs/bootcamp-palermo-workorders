using System.ComponentModel.DataAnnotations;
using System.Reflection;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Hosting;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

[Route("/login")]
public partial class Login : AppComponentBase
{
    [Inject] public CustomAuthenticationStateProvider? AuthStateProvider { get; set; }
    [Inject] public NavigationManager? NavigationManager { get; set; }
    [Inject] public IHostEnvironment? HostEnvironment { get; set; }

    public readonly LoginModel LoginModelValue = new();
    private string? _errorMessage;
    private Employee[] _employees = Array.Empty<Employee>();
    // ReSharper disable once MemberCanBePrivate.Global -- Razor template binding requires public access
    public string AppVersion { get; private set; } = string.Empty;
    // ReSharper disable once MemberCanBePrivate.Global -- Razor template binding requires public access
    public string AppEnvironment { get; private set; } = string.Empty;
    // ReSharper disable once MemberCanBePrivate.Global -- Razor template binding requires public access
    public string? WelcomeFullName =>
        _employees.FirstOrDefault(e => e.UserName == LoginModelValue.Username)?.GetFullName()
            is var raw
                && !string.IsNullOrEmpty(raw)
                    ? LoginDisplayNameFormatter.FormatForLoginDropdown(raw)
                    : null;

    private Task _employeesLoadTask = Task.CompletedTask;

    protected override Task OnInitializedAsync()
    {
        AppVersion = typeof(Login).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? typeof(Login).Assembly.GetName().Version?.ToString()
            ?? string.Empty;
        AppEnvironment = string.IsNullOrWhiteSpace(HostEnvironment?.EnvironmentName)
            ? "unknown"
            : HostEnvironment.EnvironmentName;
        _employeesLoadTask = LoadEmployees();
        return _employeesLoadTask;
    }

    private async Task LoadEmployees()
    {
        try
        {
            _employees = await Bus.Send(new EmployeeGetAllQuery());
        }
        catch (Exception ex)
        {
            _errorMessage = "Error loading employees: " + ex.Message;
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
        await _employeesLoadTask;
        LoginModelValue.Username = TimothyLovejoyUsername;
        await AuthenticateAndNavigate();
    }

    private async Task HandleLogin()
    {
        if (string.IsNullOrEmpty(LoginModelValue.Username))
        {
            _errorMessage = "Please select an employee";
            return;
        }

        await AuthenticateAndNavigate();
    }

    private async Task AuthenticateAndNavigate()
    {
        var selectedEmployee = _employees.FirstOrDefault(e => e.UserName == LoginModelValue.Username);
        if (selectedEmployee != null)
        {
            await AuthStateProvider!.Login(LoginModelValue.Username);
            EventBus.Notify(new UserLoggedInEvent(LoginModelValue.Username));
            await Bus.Publish(new Core.Model.Events.UserLoggedInEvent(LoginModelValue.Username));
            NavigationManager!.NavigateTo("/");
        }
        else
        {
            _errorMessage = "Invalid employee selection";
        }
    }

    public class LoginModel
    {
        [Required(ErrorMessage = "Employee selection is required")]
        public string Username { get; set; } = string.Empty;
    }
}
