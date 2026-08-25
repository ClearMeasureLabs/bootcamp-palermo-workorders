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
public class WorkOrderManageSpeechTests
{
    [Test]
    public async Task ShouldRenderSpeakTitleButton()
    {
        await using var ctx = new BunitContext();

        var user = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com")
        {
            Id = Guid.NewGuid(),
            PreferredLanguage = "es-ES"
        };

        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(user));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));

        var component = ctx.Render<WorkOrderManage>();

        await component.WaitForAssertionAsync(() =>
        {
            var element = component.Find($"[data-testid='{WorkOrderManage.Elements.SpeakTitle}']");
            element.ShouldNotBeNull();
            element.TagName.ShouldBe("BUTTON", StringCompareShould.IgnoreCase);
            element.GetAttribute("type").ShouldBe("button");
        });
    }

    [Test]
    public async Task ShouldRenderSpeakDescriptionButton()
    {
        await using var ctx = new BunitContext();

        var user = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };

        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(user));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));

        var component = ctx.Render<WorkOrderManage>();

        await component.WaitForAssertionAsync(() =>
        {
            var element = component.Find($"[data-testid='{WorkOrderManage.Elements.SpeakDescription}']");
            element.ShouldNotBeNull();
            element.TagName.ShouldBe("BUTTON", StringCompareShould.IgnoreCase);
            element.GetAttribute("type").ShouldBe("button");
        });
    }

    [Test]
    public async Task SpeakTitleButtonShouldInvokeTranslationService()
    {
        await using var ctx = new BunitContext();

        var user = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com")
        {
            Id = Guid.NewGuid(),
            PreferredLanguage = "es-ES"
        };

        var translationService = new StubTranslationService();

        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(user));
        ctx.Services.AddSingleton<ITranslationService>(translationService);
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));

        var component = ctx.Render<WorkOrderManage>();

        await component.WaitForAssertionAsync(() =>
        {
            var titleInput = component.Find($"[data-testid='{WorkOrderManage.Elements.Title}']");
            titleInput.ShouldNotBeNull();
        });

        // Set the title value
        var titleElement = component.Find($"[data-testid='{WorkOrderManage.Elements.Title}']");
        await titleElement.ChangeAsync(new() { Value = "Test title" });

        var speakButton = component.Find($"[data-testid='{WorkOrderManage.Elements.SpeakTitle}']");
        await speakButton.ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
        {
            translationService.LastText.ShouldBe("Test title");
            translationService.LastTargetLanguage.ShouldBe("es-ES");
        });
    }

    [Test]
    public async Task SpeakDescriptionButtonShouldInvokeTranslationService()
    {
        await using var ctx = new BunitContext();

        var user = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com")
        {
            Id = Guid.NewGuid(),
            PreferredLanguage = "fr-FR"
        };

        var translationService = new StubTranslationService();

        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(user));
        ctx.Services.AddSingleton<ITranslationService>(translationService);
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));

        var component = ctx.Render<WorkOrderManage>();

        await component.WaitForAssertionAsync(() =>
        {
            component.Find($"[data-testid='{WorkOrderManage.Elements.Description}']").ShouldNotBeNull();
        });

        await component.Find($"[data-testid='{WorkOrderManage.Elements.Description}']").ChangeAsync(new() { Value = "Test description" });
        await component.Find($"[data-testid='{WorkOrderManage.Elements.SpeakDescription}']").ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
        {
            translationService.LastText.ShouldBe("Test description");
            translationService.LastTargetLanguage.ShouldBe("fr-FR");
        });
    }

    [Test]
    public async Task SpeakTitleButton_WhenTitleEmpty_DoesNotInvokeTranslationService()
    {
        await using var ctx = new BunitContext();

        var user = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com")
        {
            Id = Guid.NewGuid(),
            PreferredLanguage = "es-ES"
        };

        var translationService = new StubTranslationService();

        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilderEmptyTitle());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(user));
        ctx.Services.AddSingleton<ITranslationService>(translationService);
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));

        var component = ctx.Render<WorkOrderManage>();

        await component.WaitForAssertionAsync(() =>
        {
            component.Find($"[data-testid='{WorkOrderManage.Elements.SpeakTitle}']").ShouldNotBeNull();
        });

        await component.Find($"[data-testid='{WorkOrderManage.Elements.SpeakTitle}']").ClickAsync(new());

        translationService.LastText.ShouldBeNull();
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

    private class StubWorkOrderBuilderEmptyTitle : IWorkOrderBuilder
    {
        public WorkOrder CreateNewWorkOrder(Employee creator)
        {
            return new WorkOrder
            {
                Id = Guid.NewGuid(),
                Number = "WO-TEST",
                Status = WorkOrderStatus.Draft,
                Creator = creator,
                Title = ""
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
