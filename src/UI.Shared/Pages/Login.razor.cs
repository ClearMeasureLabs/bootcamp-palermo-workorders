using System.ComponentModel.DataAnnotations;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Microsoft.AspNetCore.Components;

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

    private async void HandleLogin()
    {
        if (string.IsNullOrEmpty(loginModel.Username))
        {
            errorMessage = "Please select an employee";
            return;
        }

        // Find the selected employee
        var selectedEmployee = employees.FirstOrDefault(e => e.UserName == loginModel.Username);
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
            errorMessage = "Invalid employee selection";
        }
    }

    public class LoginModel
    {
        [Required(ErrorMessage = "Employee selection is required")]
        public string Username { get; set; } = string.Empty;
    }
}