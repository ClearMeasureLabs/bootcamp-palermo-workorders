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
public class WorkOrderManageAssetTagFieldTests
{
    [Test]
    public async Task WorkOrderManage_ShouldRenderAssetTagAsTextInputWithMaxLength10()
    {
        await using var ctx = CreateNewModeContext();

        var component = ctx.Render<WorkOrderManage>();

        var assetTag = await component.WaitForElementAsync(
            $"[data-testid='{WorkOrderManage.Elements.AssetTag}']");
        assetTag.TagName.ShouldBe("INPUT");
        assetTag.GetAttribute("maxlength").ShouldBe(WorkOrder.AssetTagMaxLength.ToString());
        assetTag.HasAttribute("disabled").ShouldBeFalse();
    }

    [Test]
    public async Task WorkOrderManage_ShouldDisableAssetTagWhenReadOnly()
    {
        await using var ctx = new BunitContext();

        var viewer = new Employee("viewer", "View", "Only", "viewer@example.com") { Id = Guid.NewGuid() };
        var creator = new Employee("creator", "Create", "Or", "creator@example.com") { Id = Guid.NewGuid() };
        var completedWorkOrder = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-AT",
            Status = WorkOrderStatus.Complete,
            Creator = creator,
            Assignee = creator,
            Title = "Completed",
            Description = "Done",
            AssetTag = "EQ-42"
        };

        ctx.Services.AddSingleton<IBus>(new StubWorkOrderManageBus(completedWorkOrder));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(Guid.NewGuid(), creator));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(viewer));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "Edit"));

        var component = ctx.Render<WorkOrderManage>(parameters =>
            parameters.Add(p => p.Id, completedWorkOrder.Number));

        var assetTag = await component.WaitForElementAsync(
            $"[data-testid='{WorkOrderManage.Elements.AssetTag}']");
        assetTag.HasAttribute("disabled").ShouldBeTrue();
        component.Instance.Model.AssetTag.ShouldBe("EQ-42");
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

    private class StubWorkOrderManageBus(WorkOrder? workOrderByNumber = null) : Bus(null!)
    {
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

            if (request is WorkOrderByNumberQuery && workOrderByNumber != null)
            {
                return Task.FromResult((TResponse)(object)workOrderByNumber);
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
                Number = "WO-ATAG",
                Status = WorkOrderStatus.Draft,
                Creator = creator,
                Title = "Asset tag field"
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
