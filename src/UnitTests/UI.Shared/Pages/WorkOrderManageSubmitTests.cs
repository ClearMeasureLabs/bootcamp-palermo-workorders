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
using TestContext = Bunit.TestContext;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class WorkOrderManageSubmitTests
{
    [Test]
    public void ShouldNavigateToSearch_WhenSaveCommandSubmittedForNewWorkOrder()
    {
        using var ctx = new TestContext();
        var user = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        user.AddRole(new Role("creator", true, false));
        var bus = new StubSubmitBus();

        ctx.Services.AddSingleton<IBus>(bus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(user));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(user));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));

        var component = ctx.RenderComponent<WorkOrderManage>();

        component.WaitForAssertion(() =>
        {
            component.Find($"[data-testid='{WorkOrderManage.Elements.Title}']").ShouldNotBeNull();
        });

        component.Find($"[data-testid='{WorkOrderManage.Elements.Title}']").Change("Submit title");
        component.Find($"[data-testid='{WorkOrderManage.Elements.Description}']").Change("Submit description");

        var saveButton = component.Find($"[data-testid='{WorkOrderManage.Elements.CommandButton}Save']");
        saveButton.Click();

        component.WaitForAssertion(() =>
        {
            bus.LastCommand.ShouldNotBeNull();
            bus.LastCommand.ShouldBeOfType<SaveDraftCommand>();
            navigationManager.Uri.ShouldContain("/workorder/search");
        });
    }

    [Test]
    public void ShouldLoadExistingWorkOrder_WhenEditModeSubmitted()
    {
        using var ctx = new TestContext();
        var user = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        user.AddRole(new Role("creator", true, false));
        var existing = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-EDIT",
            Title = "Existing",
            Description = "Desc",
            Status = WorkOrderStatus.Draft,
            Creator = user
        };
        var bus = new StubSubmitBus(existing);

        ctx.Services.AddSingleton<IBus>(bus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(user));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(user));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/?Mode=Edit&Id=WO-EDIT");

        var component = ctx.RenderComponent<WorkOrderManage>(parameters => parameters
            .Add(p => p.Id, "WO-EDIT"));

        component.WaitForAssertion(() =>
        {
            component.Find($"[data-testid='{WorkOrderManage.Elements.Title}']").GetAttribute("value")
                .ShouldBe("Existing");
        });

        var saveButton = component.Find($"[data-testid='{WorkOrderManage.Elements.CommandButton}Save']");
        saveButton.Click();

        component.WaitForAssertion(() =>
        {
            bus.WorkOrderByNumberHits.ShouldBeGreaterThan(0);
            bus.LastCommand.ShouldNotBeNull();
            navigationManager.Uri.ShouldContain("/workorder/search");
        });
    }

    private sealed class StubSubmitBus(WorkOrder? existing = null) : Bus(null!)
    {
        public object? LastCommand { get; private set; }
        public int WorkOrderByNumberHits { get; private set; }

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
                WorkOrderByNumberHits++;
                return Task.FromResult((TResponse)(object)(existing ?? throw new InvalidOperationException("missing")));
            }

            if (request is EmployeeByUserNameQuery byUser)
            {
                var employee = new Employee(byUser.Username, "A", "B", "a@b.com");
                return Task.FromResult((TResponse)(object)employee);
            }

            if (request is IStateCommand)
            {
                LastCommand = request;
                var wo = existing ?? new WorkOrder { Number = "WO-TEST", Status = WorkOrderStatus.Draft };
                var result = new StateCommandResult(wo);
                return Task.FromResult((TResponse)(object)result);
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
