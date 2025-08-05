using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using TestContext = Bunit.TestContext;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

public class WorkOrderSearchTests
{
    [Test]
    public void ShouldLoadDropDownsInitiallyOnLoad()
    {
        using var ctx = new TestContext();
        var stubUserSession = new StubUserSession();

        // Arrange
        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IUserSession>(stubUserSession);


        // Act
        var component = ctx.RenderComponent<WorkOrderSearch>();

        // Assert
        var creatorSelect = component.Find($"#{WorkOrderSearch.Elements.CreatorSelect}");
        var assigneeSelect = component.Find($"#{WorkOrderSearch.Elements.AssigneeSelect}");
        var statusSelect = component.Find($"#{WorkOrderSearch.Elements.StatusSelect}");

        creatorSelect.ShouldNotBeNull();
        assigneeSelect.ShouldNotBeNull();
        statusSelect.ShouldNotBeNull();

        // Verify user options are loaded (3 employees + "All" option = 4 options)
        var creatorOptions = creatorSelect.QuerySelectorAll("option");
        creatorOptions.Length.ShouldBe(4);
        creatorOptions[0].TextContent.ShouldBe("All");

        var assigneeOptions = assigneeSelect.QuerySelectorAll("option");
        assigneeOptions.Length.ShouldBe(4);
        assigneeOptions[0].TextContent.ShouldBe("All");

        // Verify status options are loaded (6 statuses + "All" option = 7 options)
        var statusOptions = statusSelect.QuerySelectorAll("option");
        statusOptions.Length.ShouldBe(7);
        statusOptions[0].TextContent.ShouldBe("All");
    }

    [Test]
    public void ShouldLoadWorkOrderTableWithAllFiltersSetToAllOnInitialLoad()
    {
        using var ctx = new TestContext();

        // Arrange
        var stubBus = new StubBus();
        var stubUserSession = new StubUserSession();
        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IUserSession>(stubUserSession);

        // Act
        var component = ctx.RenderComponent<WorkOrderSearch>();

        // Assert
        var workOrderTable = component.Find(".grid-data");
        workOrderTable.ShouldNotBeNull();

        var workOrderRows = workOrderTable.QuerySelectorAll("tbody tr");
        workOrderRows.Length.ShouldBe(2);
    }

    [Test]
    public void ShouldLoadWorkOrderTableWithCreatorFilterOnInitialLoad()
    {
        using var ctx = new TestContext();

        // Arrange
        var stubBus = new StubBus(); 
        var stubUserSession = new StubUserSession();

        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IUserSession>(stubUserSession);

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        var uri = navigationManager.GetUriWithQueryParameter("Creator", "somename");
        navigationManager.NavigateTo(uri);

        // Act
        var component = ctx.RenderComponent<WorkOrderSearch>();

        // Assert
        var workOrderTable = component.Find(".grid-data");
        var workOrderRows = workOrderTable.QuerySelectorAll("tbody tr");
        workOrderRows.Length.ShouldBe(2);
    }

    [Test]
    public void ShouldLoadWorkOrderTableWithAssigneeFilterOnInitialLoad()
    {
        using var ctx = new TestContext();

        // Arrange
        var stubBus = new StubBus();
        var stubUserSession = new StubUserSession();
        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IUserSession>(stubUserSession);


        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        var uri = navigationManager.GetUriWithQueryParameter("Assignee", "somename");
        navigationManager.NavigateTo(uri);

        // Act
        var component = ctx.RenderComponent<WorkOrderSearch>();

        // Assert
        var workOrderTable = component.Find(".grid-data");
        var workOrderRows = workOrderTable.QuerySelectorAll("tbody tr");
        workOrderRows.Length.ShouldBe(2);
    }

    [Test]
    public void ShouldLoadWorkOrderTableWithStatusFilterOnInitialLoad()
    {
        using var ctx = new TestContext();

        // Arrange
        var stubBus = new StubBus();
        var stubUserSession = new StubUserSession();
        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IUserSession>(stubUserSession);


        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        var uri = navigationManager.GetUriWithQueryParameter("Status", WorkOrderStatus.Assigned.Key);
        navigationManager.NavigateTo(uri);

        // Act
        var component = ctx.RenderComponent<WorkOrderSearch>();

        // Assert
        var workOrderTable = component.Find(".grid-data");
        var workOrderRows = workOrderTable.QuerySelectorAll("tbody tr");
        workOrderRows.Length.ShouldBe(2);
    }

    [Test]
    public void AfterInitialLoadSelectingAllThreeOptionsShouldLoadWorkOrders()
    {
        using var ctx = new TestContext();

        // Arrange
        var stubBus = new StubBus();
        var stubUserSession = new StubUserSession();
        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IUserSession>(stubUserSession);


        var component = ctx.RenderComponent<WorkOrderSearch>();

        // Act
        var creatorSelect = component.Find($"#{WorkOrderSearch.Elements.CreatorSelect}");
        var assigneeSelect = component.Find($"#{WorkOrderSearch.Elements.AssigneeSelect}");
        var statusSelect = component.Find($"#{WorkOrderSearch.Elements.StatusSelect}");

        creatorSelect.Change("jpalermo");
        assigneeSelect.Change("hsimpson");
        statusSelect.Change(WorkOrderStatus.InProgress.Key);

        var searchButton = component.Find($"#{WorkOrderSearch.Elements.SearchButton}");
        searchButton.Click();

        // Assert
        var workOrderTable = component.Find(".grid-data");
        workOrderTable.ShouldNotBeNull();

        var workOrderRows = workOrderTable.QuerySelectorAll("tbody tr");
        workOrderRows.Length.ShouldBe(2);
    }

    private class StubUserSession(Employee? currentUser = null) : IUserSession
    {
        private readonly Employee? _currentUser =
            currentUser ?? new Employee("testuser", "Test", "User", "test@example.com");

        public Task<Employee?> GetCurrentUserAsync()
        {
            return Task.FromResult(_currentUser);
        }
    }
}