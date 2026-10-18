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
public class WorkOrderManageTitleCharCountTests
{
    [Test]
    public async Task TitleCharCount_ShowsFullLimit_WhenTitleIsEmpty()
    {
        await using var ctx = CreateNewModeContext(title: null);

        var component = ctx.Render<WorkOrderManage>();

        var caption = await component.WaitForElementAsync(
            $"[data-testid='{WorkOrderManage.Elements.TitleCharCount}']");
        caption.TextContent.Trim().ShouldBe("200 characters remaining");
        caption.ClassList.ShouldContain("text-muted");
    }

    [Test]
    public async Task TitleCharCount_Decrements_AsTitleLengthGrows()
    {
        await using var ctx = CreateNewModeContext(title: new string('A', 10));

        var component = ctx.Render<WorkOrderManage>();

        var caption = await component.WaitForElementAsync(
            $"[data-testid='{WorkOrderManage.Elements.TitleCharCount}']");
        caption.TextContent.Trim().ShouldBe("190 characters remaining");
    }

    [Test]
    public async Task TitleCharCount_ShowsWarning_WhenTwentyOrFewerRemain()
    {
        await using var ctx = CreateNewModeContext(title: new string('X', WorkOrder.TitleMaxLength - 20));

        var component = ctx.Render<WorkOrderManage>();

        var caption = await component.WaitForElementAsync(
            $"[data-testid='{WorkOrderManage.Elements.TitleCharCount}']");
        caption.TextContent.Trim().ShouldBe("20 characters remaining");
        caption.ClassList.ShouldContain("text-warning");
    }

    [Test]
    public async Task TitleInput_HasMaxLengthAttribute_EqualToTitleMaxLength()
    {
        await using var ctx = CreateNewModeContext(title: null);

        var component = ctx.Render<WorkOrderManage>();

        var input = await component.WaitForElementAsync(
            $"[data-testid='{WorkOrderManage.Elements.Title}']");
        input.GetAttribute("maxlength").ShouldBe(WorkOrder.TitleMaxLength.ToString());
    }

    private static BunitContext CreateNewModeContext(string? title)
    {
        var ctx = new BunitContext();
        var creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        var workOrderId = Guid.NewGuid();

        ctx.Services.AddSingleton<IBus>(new StubWorkOrderManageBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(workOrderId, creator, title));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(creator));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));

        return ctx;
    }

    private class StubWorkOrderManageBus() : Bus(null!)
    {
        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is EmployeeGetAllQuery)
                return Task.FromResult((TResponse)(object)Array.Empty<Employee>());

            if (request is WorkOrderAttachmentsQuery)
                return Task.FromResult((TResponse)(object)Array.Empty<WorkOrderAttachment>());

            throw new NotImplementedException($"Unhandled request type: {request.GetType().Name}");
        }
    }

    private class StubWorkOrderBuilder(Guid workOrderId, Employee creator, string? title) : IWorkOrderBuilder
    {
        public WorkOrder CreateNewWorkOrder(Employee currentUser)
        {
            return new WorkOrder
            {
                Id = workOrderId,
                Number = "WO-TTL",
                Status = WorkOrderStatus.Draft,
                Creator = creator,
                Title = title,
                Description = "Description"
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
