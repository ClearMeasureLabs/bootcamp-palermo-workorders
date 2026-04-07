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
public class WorkOrderManageInstructionsTests
{
    [Test]
    public void ShouldRenderInstructionsTextArea_When_EditableMode()
    {
        using var ctx = new TestContext();

        var user = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com");
        user.Id = Guid.NewGuid();

        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(user));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(user));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));

        var component = ctx.RenderComponent<WorkOrderManage>();

        component.WaitForAssertion(() =>
        {
            var instructions = component.Find($"[data-testid='{WorkOrderManage.Elements.Instructions}']");
            instructions.ShouldNotBeNull();
            instructions.TagName.ShouldBe("TEXTAREA", StringCompareShould.IgnoreCase);
        });
    }

    [Test]
    public void ShouldRenderInstructionsAsPlainText_When_ReadOnly()
    {
        using var ctx = new TestContext();

        var creator = new Employee("creator", "C", "R", "c@t.test");
        creator.Id = Guid.NewGuid();
        var viewer = new Employee("viewer", "V", "W", "v@t.test");
        viewer.Id = Guid.NewGuid();

        var workOrder = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-RO",
            Title = "T",
            Description = "D",
            Instructions = "Bring tools from shed",
            Status = WorkOrderStatus.Draft,
            Creator = creator
        };

        ctx.Services.AddSingleton<IBus>(new StubReadOnlyBus(workOrder));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(creator));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(viewer));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("/workorder/manage/WO-RO?Mode=edit");

        var component = ctx.RenderComponent<WorkOrderManage>(parameters => parameters
            .Add(p => p.Id, "WO-RO"));

        component.WaitForAssertion(() =>
        {
            var span = component.Find($"span[data-testid='{WorkOrderManage.Elements.Instructions}']");
            span.TextContent.ShouldBe("Bring tools from shed");
        });
    }

    private class StubBus() : Bus(null!)
    {
        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is EmployeeGetAllQuery)
            {
                return Task.FromResult<TResponse>((TResponse)(object)Array.Empty<Employee>());
            }

            if (request is WorkOrderAttachmentsQuery)
            {
                return Task.FromResult<TResponse>((TResponse)(object)Array.Empty<WorkOrderAttachment>());
            }

            throw new NotImplementedException($"Unhandled request type: {request.GetType().Name}");
        }
    }

    private class StubReadOnlyBus(WorkOrder workOrder) : Bus(null!)
    {
        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is EmployeeGetAllQuery)
            {
                return Task.FromResult<TResponse>((TResponse)(object)Array.Empty<Employee>());
            }

            if (request is WorkOrderByNumberQuery)
            {
                return Task.FromResult<TResponse>((TResponse)(object)workOrder);
            }

            if (request is WorkOrderAttachmentsQuery)
            {
                return Task.FromResult<TResponse>((TResponse)(object)Array.Empty<WorkOrderAttachment>());
            }

            throw new NotImplementedException($"Unhandled request type: {request.GetType().Name}");
        }
    }

    private class StubWorkOrderBuilder(Employee creator) : IWorkOrderBuilder
    {
        public WorkOrder CreateNewWorkOrder(Employee _)
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
        public Task<string> TranslateAsync(string text, string targetLanguageCode) => Task.FromResult(text);
    }
}
