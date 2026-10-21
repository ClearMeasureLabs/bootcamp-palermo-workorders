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

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class WorkOrderManageCustomerRepFieldTests
{
    [Test]
    public async Task WorkOrderManage_ShouldRenderCustomerRepAsInputTextWithMaxLength50()
    {
        await using var ctx = CreateNewModeContext();

        var component = ctx.Render<WorkOrderManage>();

        var customerRep = await component.WaitForElementAsync(
            $"[data-testid='{WorkOrderManage.Elements.CustomerRep}']");
        customerRep.TagName.ShouldBe("INPUT");
        customerRep.GetAttribute("maxlength").ShouldBe(WorkOrder.CustomerRepMaxLength.ToString());
    }

    [Test]
    public async Task WorkOrderManage_ShouldPlaceCustomerRepImmediatelyBelowAssignedTo()
    {
        await using var ctx = CreateNewModeContext();

        var component = ctx.Render<WorkOrderManage>();

        await component.WaitForElementAsync($"[data-testid='{WorkOrderManage.Elements.CustomerRep}']");

        var markup = component.Markup;
        var assigneeIndex = markup.IndexOf($"data-testid=\"{WorkOrderManage.Elements.Assignee}\"", StringComparison.Ordinal);
        var customerRepIndex = markup.IndexOf($"data-testid=\"{WorkOrderManage.Elements.CustomerRep}\"", StringComparison.Ordinal);
        var titleIndex = markup.IndexOf($"data-testid=\"{WorkOrderManage.Elements.Title}\"", StringComparison.Ordinal);

        assigneeIndex.ShouldBeGreaterThanOrEqualTo(0);
        customerRepIndex.ShouldBeGreaterThan(assigneeIndex);
        titleIndex.ShouldBeGreaterThan(customerRepIndex);
    }

    private static BunitContext CreateNewModeContext()
    {
        var ctx = new BunitContext();
        var creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        var workOrderId = Guid.NewGuid();

        ctx.Services.AddSingleton<IBus>(new StubWorkOrderManageBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(workOrderId, creator));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(creator));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));

        return ctx;
    }

    private class StubWorkOrderManageBus : Bus
    {
        // ReSharper disable once ConvertToPrimaryConstructor -- left as classic ctor by Qodana policy
        public StubWorkOrderManageBus() : base(null!)
        {
        }

        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is EmployeeGetAllQuery)
            {
                var employees = Array.Empty<Employee>();
                return Task.FromResult((TResponse)(object)employees);
            }

            if (request is WorkOrderAttachmentsQuery)
            {
                return Task.FromResult((TResponse)(object)Array.Empty<WorkOrderAttachment>());
            }

            throw new NotImplementedException($"Unhandled request type: {request.GetType().Name}");
        }
    }

    private class StubWorkOrderBuilder(Guid workOrderId, Employee creator) : IWorkOrderBuilder
    {
        public WorkOrder CreateNewWorkOrder(Employee currentUser)
        {
            return new WorkOrder
            {
                Id = workOrderId,
                Number = "WO-CREP",
                Status = WorkOrderStatus.Draft,
                Creator = creator,
                Title = "Customer rep field"
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
}
