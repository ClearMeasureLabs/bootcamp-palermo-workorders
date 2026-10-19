using System.ComponentModel.DataAnnotations;
using System.Reflection;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Hosting;
using ClearMeasure.Bootcamp.Core.Queries;
using Microsoft.JSInterop;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

[Route("/login")]
public partial class Login : AppComponentBase
{
    internal const string RememberedUsernameStorageKey = "rememberedUsername";

    [Inject] public CustomAuthenticationStateProvider? AuthStateProvider { get; set; }
    [Inject] public NavigationManager? NavigationManager { get; set; }
    [Inject] public IHostEnvironment? HostEnvironment { get; set; }
    [Inject] public IJSRuntime? JsRuntime { get; set; }

    public readonly LoginModel LoginModelValue = new();
    private string? _errorMessage;
    private Employee[] _employees = Array.Empty<Employee>();
    private bool _rememberMySelection;
    // ReSharper disable once MemberCanBePrivate.Global -- Razor template binding requires public access
    public string AppVersion { get; private set; } = string.Empty;
    // ReSharper disable once MemberCanBePrivate.Global -- Razor template binding requires public access
    public string AppEnvironment { get; private set; } = string.Empty;
    // ReSharper disable once MemberCanBePrivate.Global -- Razor template binding requires public access
    public string? WelcomeFullName =>
        _employees.FirstOrDefault(e => e.UserName == LoginModelValue.Username)?.GetFullName();

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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        await _employeesLoadTask;
        await ApplyRememberedUsernameAsync();
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

    private async Task ApplyRememberedUsernameAsync()
    {
        if (JsRuntime is null)
        {
            return;
        }

        try
        {
            var savedUsername = await JsRuntime.InvokeAsync<string?>("localStorage.getItem", RememberedUsernameStorageKey);
            if (string.IsNullOrEmpty(savedUsername))
            {
                return;
            }

            if (_employees.Any(employee => employee.UserName == savedUsername))
            {
                LoginModelValue.Username = savedUsername;
                _rememberMySelection = true;
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (Exception)
        {
            // localStorage unavailable (prerender, tests without JSInterop, privacy mode)
        }
    }

    private async Task PersistRememberedUsernameAsync()
    {
        if (JsRuntime is null)
        {
            return;
        }

        try
        {
            if (_rememberMySelection)
            {
                await JsRuntime.InvokeVoidAsync("localStorage.setItem", RememberedUsernameStorageKey, LoginModelValue.Username);
            }
            else
            {
                await JsRuntime.InvokeVoidAsync("localStorage.removeItem", RememberedUsernameStorageKey);
            }
        }
        catch (Exception)
        {
            // localStorage unavailable (prerender, tests without JSInterop, privacy mode)
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
            await PersistRememberedUsernameAsync();
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
