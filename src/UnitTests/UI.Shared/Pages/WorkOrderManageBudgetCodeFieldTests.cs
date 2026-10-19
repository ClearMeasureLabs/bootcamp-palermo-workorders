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
public class WorkOrderManageBudgetCodeFieldTests
{
    [Test]
    public async Task WorkOrderManage_ShouldRenderBudgetCodeInputWithMaxLength12()
    {
        await using var ctx = CreateNewModeContext();
        var component = ctx.Render<WorkOrderManage>();

        await component.WaitForAssertionAsync(() =>
        {
            var input = component.Find($"[data-testid='{WorkOrderManage.Elements.BudgetCode}']");
            input.ShouldNotBeNull();
            input.GetAttribute("maxlength").ShouldBe(WorkOrder.BudgetCodeMaxLength.ToString());
        });
    }

    [Test]
    public async Task WorkOrderManage_ShouldDisableBudgetCodeWhenReadOnly()
    {
        var user = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        user.AddRole(new Role("fulfiller", false, true));
        var existing = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-RO1",
            Title = "Read only",
            Description = "Desc",
            Status = WorkOrderStatus.Complete,
            Creator = user,
            BudgetCode = "FAC-9"
        };

        await using var ctx = new BunitContext();
        ctx.Services.AddSingleton<IBus>(new StubWorkOrderManageBus(existing));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(existing.Id, user));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(user));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "Edit")
            .Replace("Mode=Edit", "Mode=Edit&Id=WO-RO1"));

        var component = ctx.Render<WorkOrderManage>();

        await component.WaitForAssertionAsync(() =>
        {
            var input = component.Find($"[data-testid='{WorkOrderManage.Elements.BudgetCode}']");
            input.HasAttribute("disabled").ShouldBeTrue();
        });
    }

    private static BunitContext CreateNewModeContext()
    {
        var user = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        user.AddRole(new Role("creator", true, false));
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IBus>(new StubWorkOrderManageBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(Guid.NewGuid(), user));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(user));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();
        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));
        return ctx;
    }

    private sealed class StubWorkOrderManageBus(WorkOrder? workOrderByNumber = null) : Bus(null!)
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
                return Task.FromResult((TResponse)(object)(workOrderByNumber ?? throw new InvalidOperationException("missing")));
            }

            if (request is EmployeeByUserNameQuery byUser)
            {
                var employee = new Employee(byUser.Username, "A", "B", "a@b.com");
                return Task.FromResult((TResponse)(object)employee);
            }

            if (request is IStateCommand)
            {
                var wo = workOrderByNumber ?? new WorkOrder { Number = "WO-TEST", Status = WorkOrderStatus.Draft };
                return Task.FromResult((TResponse)(object)new StateCommandResult(wo));
            }

            throw new NotImplementedException(request.GetType().Name);
        }
    }

    private sealed class StubWorkOrderBuilder(Guid workOrderId, Employee creator) : IWorkOrderBuilder
    {
        public WorkOrder CreateNewWorkOrder(Employee currentUser)
        {
            return new WorkOrder
            {
                Id = workOrderId,
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
