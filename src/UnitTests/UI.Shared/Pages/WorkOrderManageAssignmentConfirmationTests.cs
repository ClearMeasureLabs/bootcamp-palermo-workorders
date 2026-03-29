using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
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
public class WorkOrderManageAssignmentConfirmationTests
{
    [Test]
    public void ShouldNotShowConfirmationModalWhenAssigneeHasNoInProgressWorkOrder()
    {
        using var ctx = new TestContext();

        var user = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com");
        user.Id = Guid.NewGuid();

        var assignee = new Employee("jdoe", "John", "Doe", "jdoe@example.com");
        assignee.Id = Guid.NewGuid();

        ctx.Services.AddSingleton<IBus>(new StubBus(assignee, null));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(user));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));

        var component = ctx.RenderComponent<WorkOrderManage>();

        component.WaitForAssertion(() =>
        {
            var assigneeSelect = component.Find($"[data-testid='{WorkOrderManage.Elements.Assignee}']");
            assigneeSelect.ShouldNotBeNull();
        });

        var titleElement = component.Find($"[data-testid='{WorkOrderManage.Elements.Title}']");
        titleElement.Change("Test Work Order");

        var assigneeElement = component.Find($"[data-testid='{WorkOrderManage.Elements.Assignee}']");
        assigneeElement.Change("jdoe");

        var assignButton = component.FindAll("button[type='submit']").FirstOrDefault(b => b.TextContent.Contains("Assign"));
        assignButton?.Click();

        component.WaitForAssertion(() =>
        {
            var modal = component.FindComponents<ConfirmAssignmentModal>().FirstOrDefault();
            modal?.Instance.Visible.ShouldBe(false);
        });
    }

    [Test]
    public void ShouldShowConfirmationModalWhenAssigneeHasInProgressWorkOrder()
    {
        using var ctx = new TestContext();

        var user = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com");
        user.Id = Guid.NewGuid();

        var assignee = new Employee("jdoe", "John", "Doe", "jdoe@example.com");
        assignee.Id = Guid.NewGuid();

        var inProgressWorkOrder = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-001",
            Title = "In Progress Work Order",
            Status = WorkOrderStatus.InProgress,
            Assignee = assignee,
            Creator = user
        };

        ctx.Services.AddSingleton<IBus>(new StubBus(assignee, inProgressWorkOrder));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(user));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));

        var component = ctx.RenderComponent<WorkOrderManage>();

        component.WaitForAssertion(() =>
        {
            var assigneeSelect = component.Find($"[data-testid='{WorkOrderManage.Elements.Assignee}']");
            assigneeSelect.ShouldNotBeNull();
        });

        var titleElement = component.Find($"[data-testid='{WorkOrderManage.Elements.Title}']");
        titleElement.Change("Test Work Order");

        var assigneeElement = component.Find($"[data-testid='{WorkOrderManage.Elements.Assignee}']");
        assigneeElement.Change("jdoe");

        var assignButton = component.FindAll("button[type='submit']").FirstOrDefault(b => b.TextContent.Contains("Assign"));
        assignButton?.Click();

        component.WaitForAssertion(() =>
        {
            var modal = component.FindComponents<ConfirmAssignmentModal>().FirstOrDefault();
            modal?.Instance.Visible.ShouldBe(true);
            modal?.Instance.InProgressWorkOrder?.ShouldNotBeNull();
        });
    }

    private class StubBus(Employee? assignee, WorkOrder? inProgressWorkOrder) : Bus(null!)
    {
        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is EmployeeGetAllQuery)
            {
                var employees = assignee != null ? [assignee] : Array.Empty<Employee>();
                return Task.FromResult<TResponse>((TResponse)(object)employees);
            }

            if (request is EmployeeByUserNameQuery employeeQuery)
            {
                return Task.FromResult<TResponse>((TResponse)(object?)assignee!);
            }

            if (request is EmployeeInProgressWorkOrderQuery)
            {
                return Task.FromResult<TResponse>((TResponse)(object?)inProgressWorkOrder!);
            }

            if (request is WorkOrderAttachmentsQuery)
            {
                var attachments = Array.Empty<WorkOrderAttachment>();
                return Task.FromResult<TResponse>((TResponse)(object)attachments);
            }

            throw new NotImplementedException($"Unhandled request type: {request.GetType().Name}");
        }
    }

    private class StubWorkOrderBuilder : IWorkOrderBuilder
    {
        public WorkOrder CreateNewWorkOrder(Employee creator)
        {
            return new WorkOrder
            {
                Id = Guid.NewGuid(),
                Number = "WO-TEST",
                Status = WorkOrderStatus.Draft,
                Creator = creator,
                Title = "Test title"
            };
        }
    }

    private class StubUserSession(Employee user) : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync() => Task.FromResult<Employee?>(user);
    }

    private class StubTranslationService : ITranslationService
    {
        public Task<string> TranslateAsync(string text, string targetLanguageCode)
        {
            return Task.FromResult(text);
        }
    }

    private class StubUiBus : IUiBus
    {
        public void Notify<TEvent>(TEvent theEvent) where TEvent : IUiBusEvent
        {
        }

        public void Subscribe<TEvent>(IListener<TEvent> listener) where TEvent : IUiBusEvent
        {
        }

        public void Unsubscribe<TEvent>(IListener<TEvent> listener) where TEvent : IUiBusEvent
        {
        }
    }
}
