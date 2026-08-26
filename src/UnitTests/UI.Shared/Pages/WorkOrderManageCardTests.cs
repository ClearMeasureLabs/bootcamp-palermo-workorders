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
public class WorkOrderManageCardTests
{
    [Test]
    public void ShouldDefineManageFocusCardAndPillButtons_InScopedCss()
    {
        var css = ReadScopedCss("WorkOrderManage.razor.css");

        css.ShouldContain(".focus-card");
        css.ShouldContain(".form-section");
        css.ShouldContain(".action-buttons .btn");
        css.ShouldContain("border-radius: 999px");
        css.ShouldContain(".due-date-field ::deep .due-date-today");
        css.ShouldContain(".due-date-field ::deep .due-date-overdue");
    }

    [Test]
    public async Task ShouldRenderManageAsFocusCardWithStackedSections_WhenEditMode()
    {
        await using var ctx = CreateManageContext();

        var component = ctx.Render<WorkOrderManage>();

        await component.WaitForAssertionAsync(() =>
        {
            component.Markup.ShouldNotContain("form-grid");
            component.Find(".focus-card").ShouldNotBeNull();
            component.Find($"[data-testid='{WorkOrderManage.Elements.Title}']").ShouldNotBeNull();
            component.Find($"[data-testid='{WorkOrderManage.Elements.Description}']").ShouldNotBeNull();
            component.Find($"[data-testid='{WorkOrderManage.Elements.Instructions}']").ShouldNotBeNull();
            component.Find($"[data-testid='{WorkOrderManage.Elements.RoomNumber}']").ShouldNotBeNull();
            component.Find($"[data-testid='{WorkOrderManage.Elements.DueDate}']").ShouldNotBeNull();
        });
    }

    [Test]
    public async Task ShouldRenderCommandButtonsAsPills_WhenValidCommandsPresent()
    {
        await using var ctx = CreateManageContext();

        var component = ctx.Render<WorkOrderManage>();

        var saveButton = await component.WaitForElementAsync(
            $"[data-testid='{WorkOrderManage.Elements.CommandButton}Save']");
        saveButton.ShouldNotBeNull();
        saveButton.ClassList.ShouldContain("btn");
    }

    private static BunitContext CreateManageContext()
    {
        var ctx = new BunitContext();
        var creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        var workOrderId = Guid.NewGuid();

        ctx.Services.AddSingleton<IBus>(new StubManageCardBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubManageCardWorkOrderBuilder(workOrderId, creator));
        ctx.Services.AddSingleton<IUserSession>(new StubManageCardUserSession(creator));
        ctx.Services.AddSingleton<ITranslationService>(new StubManageCardTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));

        return ctx;
    }

    private static string ReadScopedCss(string fileName)
    {
        var path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "UI.Shared", "Pages", fileName));

        File.Exists(path).ShouldBeTrue($"Expected scoped stylesheet at {path}");
        return File.ReadAllText(path).Replace("\r\n", "\n");
    }

    private class StubManageCardBus : Bus
    {
        public StubManageCardBus() : base(null!)
        {
        }

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

            throw new NotImplementedException($"Unhandled request type: {request.GetType().Name}");
        }
    }

    private class StubManageCardWorkOrderBuilder(Guid workOrderId, Employee creator) : IWorkOrderBuilder
    {
        public WorkOrder CreateNewWorkOrder(Employee currentUser)
        {
            return new WorkOrder
            {
                Id = workOrderId,
                Number = "WO-CARD",
                Status = WorkOrderStatus.Draft,
                Creator = creator,
                Title = "Card layout"
            };
        }
    }

    private class StubManageCardUserSession(Employee user) : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync() => Task.FromResult<Employee?>(user);
    }

    private class StubManageCardTranslationService : ITranslationService
    {
        public Task<string> TranslateAsync(string text, string targetLanguageCode) => Task.FromResult(text);
    }
}
