using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using Toolbelt.Blazor.Extensions.DependencyInjection;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class WorkOrderManageUnsavedChangesTests
{
    [Test]
    public async Task ShouldShowUnsavedChangesBadge_WhenTitleDiffersFromLoaded()
    {
        await using var ctx = CreateContext(out _, out var existing);

        var component = RenderEdit(ctx, existing);

        await component.WaitForAssertionAsync(() =>
        {
            component.FindAll($"[data-testid='{WorkOrderManage.Elements.UnsavedChangesBadge}']")
                .Count.ShouldBe(0);
        });

        await component.Find($"[data-testid='{WorkOrderManage.Elements.Title}']")
            .ChangeAsync(new() { Value = "Changed title" });

        await component.WaitForAssertionAsync(() =>
        {
            var badge = component.Find($"[data-testid='{WorkOrderManage.Elements.UnsavedChangesBadge}']");
            badge.TextContent.ShouldContain("Unsaved changes");
        });
    }

    [Test]
    public async Task ShouldHideUnsavedChangesBadge_WhenValuesReverted()
    {
        await using var ctx = CreateContext(out _, out var existing);

        var component = RenderEdit(ctx, existing);

        await component.Find($"[data-testid='{WorkOrderManage.Elements.Title}']")
            .ChangeAsync(new() { Value = "Changed title" });

        await component.WaitForAssertionAsync(() =>
        {
            component.Find($"[data-testid='{WorkOrderManage.Elements.UnsavedChangesBadge}']")
                .ShouldNotBeNull();
        });

        await component.Find($"[data-testid='{WorkOrderManage.Elements.Title}']")
            .ChangeAsync(new() { Value = existing.Title! });

        await component.WaitForAssertionAsync(() =>
        {
            component.FindAll($"[data-testid='{WorkOrderManage.Elements.UnsavedChangesBadge}']")
                .Count.ShouldBe(0);
        });
    }

    [Test]
    public async Task ShouldHideUnsavedChangesBadge_AfterSuccessfulSave()
    {
        await using var ctx = CreateContext(out var bus, out var existing);

        var component = RenderEdit(ctx, existing);

        await component.Find($"[data-testid='{WorkOrderManage.Elements.Title}']")
            .ChangeAsync(new() { Value = "Changed title" });

        await component.WaitForAssertionAsync(() =>
        {
            component.Find($"[data-testid='{WorkOrderManage.Elements.UnsavedChangesBadge}']")
                .ShouldNotBeNull();
        });

        var saveButton = component.Find($"[data-testid='{WorkOrderManage.Elements.CommandButton}Save']");
        await saveButton.ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
        {
            bus.LastCommand.ShouldNotBeNull();
            component.FindAll($"[data-testid='{WorkOrderManage.Elements.UnsavedChangesBadge}']")
                .Count.ShouldBe(0);
        });
    }

    private static BunitContext CreateContext(out StubSubmitBus bus, out WorkOrder existing)
    {
        var ctx = new BunitContext();
        var user = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        user.AddRole(new Role("creator", true, false));
        existing = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-DIRT",
            Title = "Existing",
            Description = "Desc",
            Instructions = "Do it",
            RoomNumber = "101",
            Status = WorkOrderStatus.Draft,
            Creator = user
        };
        bus = new StubSubmitBus(existing);

        ctx.Services.AddSingleton<IBus>(bus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(user));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(user));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/?Mode=Edit&Id=WO-DIRT");

        return ctx;
    }

    private static IRenderedComponent<WorkOrderManage> RenderEdit(BunitContext ctx, WorkOrder existing)
    {
        var component = ctx.Render<WorkOrderManage>(parameters => parameters
            .Add(p => p.Id, existing.Number));

        component.WaitForAssertion(() =>
        {
            component.Find($"[data-testid='{WorkOrderManage.Elements.Title}']").GetAttribute("value")
                .ShouldBe(existing.Title);
        });

        return component;
    }

    private sealed class StubSubmitBus(WorkOrder? existing = null) : Bus(null!)
    {
        public object? LastCommand { get; private set; }

        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is EmployeeGetAllQuery)
            {
                return Task.FromResult((TResponse)(object)Array.Empty<Employee>());
            }

            if (request is EmployeeByUserNameQuery)
            {
                return Task.FromResult((TResponse)(object)null!);
            }

            if (request is WorkOrderByNumberQuery)
            {
                return Task.FromResult((TResponse)(object)existing!);
            }

            if (request is WorkOrderAttachmentsQuery)
            {
                return Task.FromResult((TResponse)(object)Array.Empty<WorkOrderAttachment>());
            }

            if (request is IStateCommand)
            {
                LastCommand = request;
                var workOrder = existing ?? new WorkOrder();
                return Task.FromResult((TResponse)(object)new StateCommandResult(workOrder));
            }

            throw new NotImplementedException(request.GetType().Name);
        }
    }

    private sealed class StubWorkOrderBuilder(Employee creator) : IWorkOrderBuilder
    {
        public WorkOrder CreateNewWorkOrder(Employee current) =>
            new() { Creator = creator, Status = WorkOrderStatus.Draft, Number = "WO-NEW" };
    }

    private sealed class StubUserSession(Employee user) : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync() => Task.FromResult<Employee?>(user);
    }

    private sealed class StubTranslationService : ITranslationService
    {
        public Task<string> TranslateAsync(string text, string targetLanguageCode) => Task.FromResult(text);
    }
}
