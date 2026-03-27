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
    public void WorkOrderManage_ShouldRenderInstructionsTextAreaWithTestId()
    {
        using var ctx = new TestContext();

        var user = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com");
        user.Id = Guid.NewGuid();
        var workOrderId = Guid.NewGuid();

        ctx.Services.AddSingleton<IBus>(new StubInstructionsBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilderForInstructions(workOrderId));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSessionForInstructions(user));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationServiceForInstructions());
        ctx.Services.AddSpeechSynthesis();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));

        var component = ctx.RenderComponent<WorkOrderManage>();

        component.WaitForAssertion(() =>
        {
            var instructions = component.Find($"[data-testid='{WorkOrderManage.Elements.Instructions}']");
            instructions.ShouldNotBeNull();
        });
    }

    private class StubInstructionsBus() : Bus(null!)
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

    private class StubWorkOrderBuilderForInstructions(Guid workOrderId) : IWorkOrderBuilder
    {
        public WorkOrder CreateNewWorkOrder(Employee creator)
        {
            return new WorkOrder
            {
                Id = workOrderId,
                Number = "WO-TEST",
                Status = WorkOrderStatus.Draft,
                Creator = creator,
                Title = "Test Order"
            };
        }
    }

    private class StubUserSessionForInstructions(Employee user) : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync() => Task.FromResult<Employee?>(user);
    }

    private class StubTranslationServiceForInstructions : ITranslationService
    {
        public Task<string> TranslateAsync(string text, string targetLanguageCode) => Task.FromResult(text);
    }
}
