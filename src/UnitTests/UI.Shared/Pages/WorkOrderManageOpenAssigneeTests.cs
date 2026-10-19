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
public class WorkOrderManageOpenAssigneeTests
{
    [Test]
    public async Task ShouldShowOpenCountInAssigneeDropdownOptions()
    {
        await using var ctx = CreateTestContext();

        var component = ctx.Render<WorkOrderManage>();

        await component.WaitForAssertionAsync(() =>
        {
            var select = component.Find($"[data-testid='{WorkOrderManage.Elements.Assignee}']");
            var options = select.QuerySelectorAll("option").ToList();
            options.Count.ShouldBeGreaterThan(1);
            options.Any(o => o.TextContent == "HOMER SIMPSON (2 open)").ShouldBeTrue();
            options.Any(o => o.TextContent == "Montgomery Burns (0 open)").ShouldBeTrue();
        });
    }

    private static BunitContext CreateTestContext()
    {
        var ctx = new BunitContext();
        var user = new Employee("jpalermo", "Jeffrey", "Palermo", "jeffrey@example.com")
        {
            PreferredLanguage = "en-US"
        };

        ctx.Services.AddSingleton<IBus>(new StubOpenCountBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(user));
        ctx.Services.AddSingleton<ITranslationService>(new PassThroughTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter("Mode", "New"));

        return ctx;
    }

    private class StubOpenCountBus() : Bus(null!)
    {
        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is EmployeeGetAllQuery)
            {
                Employee[] employees =
                [
                    new("hsimpson", "HOMER", "SIMPSON", "homer@springfield.com"),
                    new("mburns", "Montgomery", "Burns", "burns@plant.com")
                ];
                return Task.FromResult<TResponse>((TResponse)(object)employees);
            }

            if (request is OpenWorkOrderCountByAssigneeQuery)
            {
                var counts = new Dictionary<string, int> { ["hsimpson"] = 2 };
                return Task.FromResult<TResponse>((TResponse)(object)counts);
            }

            if (request is WorkOrderAttachmentsQuery)
            {
                return Task.FromResult<TResponse>((TResponse)(object)Array.Empty<WorkOrderAttachment>());
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

    private class PassThroughTranslationService : ITranslationService
    {
        public Task<string> TranslateAsync(string text, string targetLanguage) =>
            Task.FromResult(text);
    }
}
