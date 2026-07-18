using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using TestContext = Bunit.TestContext;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

public class WorkRequestSearchTests
{
    [Test]
    public void ShouldLoadDropDownsInitiallyOnLoad()
    {
        using var ctx = new TestContext();

        // Arrange
        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        // Act
        var component = ctx.RenderComponent<WorkRequestSearch>();

        // Assert
        var creatorSelect = component.Find($"#{WorkRequestSearch.Elements.CreatorSelect}");
        var assigneeSelect = component.Find($"#{WorkRequestSearch.Elements.AssigneeSelect}");
        var statusSelect = component.Find($"#{WorkRequestSearch.Elements.StatusSelect}");

        creatorSelect.ShouldNotBeNull();
        assigneeSelect.ShouldNotBeNull();
        statusSelect.ShouldNotBeNull();

        // Verify user options are loaded (4 employees + "All" option = 5 options)
        var creatorOptions = creatorSelect.QuerySelectorAll("option");
        creatorOptions.Length.ShouldBe(5);
        creatorOptions[0].TextContent.ShouldBe("All");

        var assigneeOptions = assigneeSelect.QuerySelectorAll("option");
        assigneeOptions.Length.ShouldBe(5);
        assigneeOptions[0].TextContent.ShouldBe("All");

        // Verify status options are loaded (4 statuses + "All" option = 5 options)
        var statusOptions = statusSelect.QuerySelectorAll("option");
        statusOptions.Length.ShouldBe(6);
        statusOptions[0].TextContent.ShouldBe("All");
    }

    [Test]
    public void ShouldLoadWorkRequestTableWithAllFiltersSetToAllOnInitialLoad()
    {
        using var ctx = new TestContext();

        // Arrange
        var stubBus = new StubBus();
        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        // Act
        var component = ctx.RenderComponent<WorkRequestSearch>();

        // Assert
        var workRequestTable = component.Find(".grid-data");
        workRequestTable.ShouldNotBeNull();

        var workRequestRows = workRequestTable.QuerySelectorAll("tbody tr");
        workRequestRows.Length.ShouldBe(2);
    }

    [Test]
    public void ShouldLoadWorkRequestTableWithCreatorFilterOnInitialLoad()
    {
        using var ctx = new TestContext();

        // Arrange
        var stubBus = new StubBus();
        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        var uri = navigationManager.GetUriWithQueryParameter("Creator", "somename");
        navigationManager.NavigateTo(uri);

        // Act
        var component = ctx.RenderComponent<WorkRequestSearch>();

        // Assert
        var workRequestTable = component.Find(".grid-data");
        var workRequestRows = workRequestTable.QuerySelectorAll("tbody tr");
        workRequestRows.Length.ShouldBe(2);
    }

    [Test]
    public void ShouldLoadWorkRequestTableWithAssigneeFilterOnInitialLoad()
    {
        using var ctx = new TestContext();

        // Arrange
        var stubBus = new StubBus();
        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        var uri = navigationManager.GetUriWithQueryParameter("Assignee", "somename");
        navigationManager.NavigateTo(uri);

        // Act
        var component = ctx.RenderComponent<WorkRequestSearch>();

        // Assert
        var workRequestTable = component.Find(".grid-data");
        var workRequestRows = workRequestTable.QuerySelectorAll("tbody tr");
        workRequestRows.Length.ShouldBe(2);
    }

    [Test]
    public void ShouldLoadWorkRequestTableWithStatusFilterOnInitialLoad()
    {
        using var ctx = new TestContext();

        // Arrange
        var stubBus = new StubBus();
        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        var uri = navigationManager.GetUriWithQueryParameter("Status", WorkRequestStatus.Assigned.Key);
        navigationManager.NavigateTo(uri);

        // Act
        var component = ctx.RenderComponent<WorkRequestSearch>();

        // Assert
        var workRequestTable = component.Find(".grid-data");
        var workRequestRows = workRequestTable.QuerySelectorAll("tbody tr");
        workRequestRows.Length.ShouldBe(2);
    }

    [Test]
    public void AfterInitialLoadSelectingAllThreeOptionsShouldLoadWorkRequests()
    {
        using var ctx = new TestContext();

        // Arrange
        var stubBus = new StubBus();
        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        var component = ctx.RenderComponent<WorkRequestSearch>();

        // Act
        var creatorSelect = component.Find($"#{WorkRequestSearch.Elements.CreatorSelect}");
        var assigneeSelect = component.Find($"#{WorkRequestSearch.Elements.AssigneeSelect}");
        var statusSelect = component.Find($"#{WorkRequestSearch.Elements.StatusSelect}");

        creatorSelect.Change("jpalermo");
        assigneeSelect.Change("hsimpson");
        statusSelect.Change(WorkRequestStatus.InProgress.Key);

        var searchButton = component.Find($"#{WorkRequestSearch.Elements.SearchButton}");
        searchButton.Click();

        // Assert
        var workRequestTable = component.Find(".grid-data");
        workRequestTable.ShouldNotBeNull();

        var workRequestRows = workRequestTable.QuerySelectorAll("tbody tr");
        workRequestRows.Length.ShouldBe(2);
    }
}