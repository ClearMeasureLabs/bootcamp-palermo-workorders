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
public class WorkOrderManageSpeechTests
{
    [Test]
    public void ShouldRenderSpeakTitleButton()
    {
        using var ctx = new TestContext();

        var user = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com");
        user.Id = Guid.NewGuid();
        user.PreferredLanguage = "es-ES";

        ctx.Services.AddSingleton<IBus>(new StubBus());
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
            var element = component.Find($"[data-testid='{WorkOrderManage.Elements.SpeakTitle}']");
            element.ShouldNotBeNull();
            element.TagName.ShouldBe("BUTTON", StringCompareShould.IgnoreCase);
            element.GetAttribute("type").ShouldBe("button");
        });
    }

    [Test]
    public void ShouldRenderSpeakDescriptionButton()
    {
        using var ctx = new TestContext();

        var user = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com");
        user.Id = Guid.NewGuid();

        ctx.Services.AddSingleton<IBus>(new StubBus());
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
            var element = component.Find($"[data-testid='{WorkOrderManage.Elements.SpeakDescription}']");
            element.ShouldNotBeNull();
            element.TagName.ShouldBe("BUTTON", StringCompareShould.IgnoreCase);
            element.GetAttribute("type").ShouldBe("button");
        });
    }

    [Test]
    public void SpeakTitleButtonShouldInvokeTranslationService()
    {
        using var ctx = new TestContext();

        var user = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com");
        user.Id = Guid.NewGuid();
        user.PreferredLanguage = "es-ES";

        var translationService = new StubTranslationService();

        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(user));
        ctx.Services.AddSingleton<ITranslationService>(translationService);
        ctx.Services.AddSpeechSynthesis();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));

        var component = ctx.RenderComponent<WorkOrderManage>();

        component.WaitForAssertion(() =>
        {
            var titleInput = component.Find($"[data-testid='{WorkOrderManage.Elements.Title}']");
            titleInput.ShouldNotBeNull();
        });

        // Set the title value
        var titleElement = component.Find($"[data-testid='{WorkOrderManage.Elements.Title}']");
        titleElement.Change("Test title");

        var speakButton = component.Find($"[data-testid='{WorkOrderManage.Elements.SpeakTitle}']");
        speakButton.Click();

        component.WaitForAssertion(() =>
        {
            translationService.LastText.ShouldBe("Test title");
            translationService.LastTargetLanguage.ShouldBe("es-ES");
        });
    }

    private class StubBus() : Bus(null!)
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
        public string? LastText { get; private set; }
        public string? LastTargetLanguage { get; private set; }

        public Task<string> TranslateAsync(string text, string targetLanguageCode)
        {
            LastText = text;
            LastTargetLanguage = targetLanguageCode;
            return Task.FromResult(text);
        }
    }
}
