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
    public void ShouldRenderInstructionsTextAreaBetweenDescriptionAndRoom()
    {
        using var ctx = CreateNewWorkOrderContext(out _);

        var component = ctx.RenderComponent<WorkOrderManage>();

        component.WaitForAssertion(() =>
        {
            var instructions = component.Find($"[data-testid='{WorkOrderManage.Elements.Instructions}']");
            instructions.ShouldNotBeNull();
            instructions.TagName.ShouldBe("TEXTAREA", StringCompareShould.IgnoreCase);
            var cssClass = instructions.GetAttribute("class") ?? string.Empty;
            cssClass.ShouldContain("form-control");
            cssClass.ShouldContain("input-textarea");

            var description = component.Find($"[data-testid='{WorkOrderManage.Elements.Description}']");
            var room = component.Find($"[data-testid='{WorkOrderManage.Elements.RoomNumber}']");

            var formGroups = component.FindAll(".form-group");
            var descriptionIndex = IndexOfFormGroupContaining(formGroups, description);
            var instructionsIndex = IndexOfFormGroupContaining(formGroups, instructions);
            var roomIndex = IndexOfFormGroupContaining(formGroups, room);

            instructionsIndex.ShouldBeGreaterThan(descriptionIndex);
            roomIndex.ShouldBeGreaterThan(instructionsIndex);
        });
    }

    [Test]
    public void ShouldDisableInstructionsWhenWorkOrderIsReadOnly()
    {
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var user = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com");
        user.Id = Guid.NewGuid();

        var creator = new Employee("someoneelse", "Someone", "Else", "se@example.com");
        creator.Id = Guid.NewGuid();

        var completedWorkOrder = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-DONE",
            Status = WorkOrderStatus.Complete,
            Creator = creator,
            Assignee = creator,
            Title = "Completed work order",
            Instructions = "Bring ladder and safety gear"
        };

        ctx.Services.AddSingleton<IBus>(new StubBus(completedWorkOrder));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(user));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "Edit"));

        var component = ctx.RenderComponent<WorkOrderManage>(parameters =>
            parameters.Add(p => p.Id, "WO-DONE"));

        component.WaitForAssertion(() =>
        {
            var instructions = component.Find($"[data-testid='{WorkOrderManage.Elements.Instructions}']");
            instructions.HasAttribute("disabled").ShouldBeTrue();
            instructions.GetAttribute("value").ShouldBe("Bring ladder and safety gear");
        });
    }

    private static TestContext CreateNewWorkOrderContext(out Employee user)
    {
        var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        user = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com");
        user.Id = Guid.NewGuid();

        ctx.Services.AddSingleton<IBus>(new StubBus(null));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(user));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));

        return ctx;
    }

    private static int IndexOfFormGroupContaining(IReadOnlyList<AngleSharp.Dom.IElement> formGroups, AngleSharp.Dom.IElement element)
    {
        for (var i = 0; i < formGroups.Count; i++)
        {
            if (formGroups[i].Contains(element))
            {
                return i;
            }
        }

        return -1;
    }

    private class StubBus(WorkOrder? workOrderByNumber) : Bus(null!)
    {
        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is EmployeeGetAllQuery)
            {
                var employees = Array.Empty<Employee>();
                return Task.FromResult<TResponse>((TResponse)(object)employees);
            }

            if (request is WorkOrderAttachmentsQuery)
            {
                var attachments = Array.Empty<WorkOrderAttachment>();
                return Task.FromResult<TResponse>((TResponse)(object)attachments);
            }

            if (request is WorkOrderByNumberQuery && workOrderByNumber != null)
            {
                return Task.FromResult<TResponse>((TResponse)(object)workOrderByNumber);
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
}
