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
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using Toolbelt.Blazor.Extensions.DependencyInjection;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class WorkOrderManageDueDateQuickPickTests
{
    [Test]
    public async Task ShouldSetDueDateToToday_WhenTodayClicked()
    {
        var fixedUtc = new DateTimeOffset(2024, 1, 17, 18, 0, 0, TimeSpan.Zero);
        await using var ctx = CreateContext(fixedUtc);
        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));
        var component = ctx.Render<WorkOrderManage>();

        await component.Find($"[data-testid='{WorkOrderManage.Elements.DueDateToday}']")
            .ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(() =>
        {
            component.Instance.Model.DueDateInput.ShouldBe(new DateOnly(2024, 1, 17));
        });
    }

    [Test]
    public async Task ShouldSetDueDateToComingSaturday_WhenNextSaturdayClicked()
    {
        var fixedUtc = new DateTimeOffset(2024, 1, 17, 18, 0, 0, TimeSpan.Zero);
        await using var ctx = CreateContext(fixedUtc);
        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));
        var component = ctx.Render<WorkOrderManage>();

        await component.Find($"[data-testid='{WorkOrderManage.Elements.DueDateNextSaturday}']")
            .ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(() =>
        {
            component.Instance.Model.DueDateInput.ShouldBe(new DateOnly(2024, 1, 20));
        });
    }

    [Test]
    public async Task ShouldHideQuickPickButtons_WhenPageIsReadOnly()
    {
        var creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        var existing = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-RO1",
            Status = WorkOrderStatus.Complete,
            Title = "done",
            Description = "done",
            Creator = creator
        };
        await using var ctx = CreateContext(DateTimeOffset.UtcNow, existing, creator);
        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo($"/workorder/manage/{existing.Number}?Mode=Edit");
        var component = ctx.Render<WorkOrderManage>(parameters => parameters
            .Add(p => p.Id, existing.Number));

        await component.WaitForAssertionAsync(() =>
        {
            component.Instance.Model.IsReadOnly.ShouldBeTrue();
            Should.Throw<ElementNotFoundException>(() =>
                component.Find($"[data-testid='{WorkOrderManage.Elements.DueDateToday}']"));
            Should.Throw<ElementNotFoundException>(() =>
                component.Find($"[data-testid='{WorkOrderManage.Elements.DueDateNextSaturday}']"));
        });
    }

    private static BunitContext CreateContext(
        DateTimeOffset utcNow,
        WorkOrder? existing = null,
        Employee? user = null)
    {
        var ctx = new BunitContext();
        user ??= new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        user.AddRole(new Role("creator", true, false));
        var bus = new StubQuickPickBus(existing);

        ctx.Services.AddSingleton<IBus>(bus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<TimeProvider>(new FixedTimeProvider(utcNow));
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(user));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(user));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        return ctx;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class StubQuickPickBus(WorkOrder? existing = null) : Bus(null!)
    {
        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is EmployeeGetAllQuery)
            {
                return Task.FromResult((TResponse)(object)Array.Empty<Employee>());
            }

            if (request is WorkOrderAttachmentsQuery)
            {
                return Task.FromResult((TResponse)(object)Array.Empty<WorkOrderAttachment>());
            }

            if (request is WorkOrderByNumberQuery)
            {
                return Task.FromResult((TResponse)(object)(existing ?? throw new InvalidOperationException("missing")));
            }

            if (request is EmployeeByUserNameQuery byUser)
            {
                var employee = new Employee(byUser.Username, "A", "B", "a@b.com");
                return Task.FromResult((TResponse)(object)employee);
            }

            if (request is IStateCommand)
            {
                var wo = existing ?? new WorkOrder { Number = "WO-TEST", Status = WorkOrderStatus.Draft };
                return Task.FromResult((TResponse)(object)new StateCommandResult(wo));
            }

            throw new NotImplementedException(request.GetType().Name);
        }
    }

    private sealed class StubWorkOrderBuilder(Employee creator) : IWorkOrderBuilder
    {
        public WorkOrder CreateNewWorkOrder(Employee current)
        {
            return new WorkOrder
            {
                Id = Guid.NewGuid(),
                Number = "WO-NEW",
                Status = WorkOrderStatus.Draft,
                Creator = creator,
                Title = "",
                Description = ""
            };
        }
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
