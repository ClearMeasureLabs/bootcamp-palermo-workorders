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
public class WorkOrderManageAssignToMeTests
{
    [Test]
    public async Task ShouldSelectLoggedInUser_WhenAssignToMeClicked()
    {
        await using var ctx = new BunitContext();
        var user = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        user.AddRole(new Role("creator", true, false));
        var bus = new StubAssignToMeBus(user);

        ctx.Services.AddSingleton<IBus>(bus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(user));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(user));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));

        var component = ctx.Render<WorkOrderManage>();

        await component.WaitForAssertionAsync(() =>
        {
            component.Find($"[data-testid='{WorkOrderManage.Elements.AssignToMe}']").ShouldNotBeNull();
        });

        await component.Find($"[data-testid='{WorkOrderManage.Elements.AssignToMe}']").ClickAsync();

        await component.WaitForAssertionAsync(() =>
        {
            component.Instance.Model.AssignedToUserName.ShouldBe(user.UserName);
        });
    }

    [Test]
    public async Task ShouldHideAssignToMe_WhenAssigneeSelectDisabled()
    {
        await using var ctx = new BunitContext();
        var user = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        user.AddRole(new Role("fulfiller", false, true));
        var existing = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-ASN",
            Status = WorkOrderStatus.Assigned,
            Creator = user,
            Assignee = user,
            Title = "t",
            Description = "d"
        };
        var bus = new StubAssignToMeBus(user, existing);

        ctx.Services.AddSingleton<IBus>(bus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(user));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(user));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "Edit")
            .Replace("?", $"?id={existing.Number}&"));

        var component = ctx.Render<WorkOrderManage>(parameters => parameters.Add(p => p.Id, existing.Number));

        await component.WaitForAssertionAsync(() =>
        {
            component.Find($"[data-testid='{WorkOrderManage.Elements.Assignee}']").ShouldNotBeNull();
        });

        Should.Throw<ElementNotFoundException>(() =>
            component.Find($"[data-testid='{WorkOrderManage.Elements.AssignToMe}']"));
    }

    [Test]
    public async Task ShouldHideAssignToMe_WhenPageIsReadOnly()
    {
        await using var ctx = new BunitContext();
        var user = new Employee("viewer", "View", "Only", "v@example.com") { Id = Guid.NewGuid() };
        var creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        creator.AddRole(new Role("creator", true, false));
        var existing = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-RO",
            Status = WorkOrderStatus.Complete,
            Creator = creator,
            Assignee = creator,
            Title = "t",
            Description = "d"
        };
        var bus = new StubAssignToMeBus(user, existing);

        ctx.Services.AddSingleton<IBus>(bus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(user));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(user));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "Edit")
            .Replace("?", $"?id={existing.Number}&"));

        var component = ctx.Render<WorkOrderManage>(parameters => parameters.Add(p => p.Id, existing.Number));

        await component.WaitForAssertionAsync(() =>
        {
            component.Find($"[data-testid='{WorkOrderManage.Elements.ReadOnlyMessage}']").ShouldNotBeNull();
        });

        Should.Throw<ElementNotFoundException>(() =>
            component.Find($"[data-testid='{WorkOrderManage.Elements.AssignToMe}']"));
    }

    private sealed class StubAssignToMeBus(Employee current, WorkOrder? existing = null) : Bus(null!)
    {
        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is EmployeeGetAllQuery)
            {
                return Task.FromResult((TResponse)(object)(Employee[])[current]);
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
                var employee = byUser.Username == current.UserName
                    ? current
                    : new Employee(byUser.Username, "A", "B", "a@b.com");
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
