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
public class WorkOrderManageLastUpdatedDateTests
{
    [Test]
    public async Task ShouldShowLastUpdatedDate_WhenWorkOrderHasLastUpdatedDate()
    {
        await using var ctx = new BunitContext();
        var user = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        var lastUpdated = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var existing = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-UPD",
            Title = "Existing",
            Description = "Desc",
            Status = WorkOrderStatus.Draft,
            Creator = user,
            LastUpdatedDate = lastUpdated
        };

        ctx.Services.AddSingleton<IBus>(new StubLastUpdatedBus(existing));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(user));
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(user));
        ctx.Services.AddSingleton<ITranslationService>(new StubTranslationService());
        ctx.Services.AddSpeechSynthesis();
        ctx.Services.AddSpeechRecognition();

        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/?Mode=Edit&Id=WO-UPD");

        var component = ctx.Render<WorkOrderManage>(parameters => parameters
            .Add(p => p.Id, "WO-UPD"));

        var expected = lastUpdated.ToString("G", System.Globalization.CultureInfo.CurrentCulture);
        await component.WaitForAssertionAsync(() =>
        {
            component.Find($"[data-testid='{WorkOrderManage.Elements.LastUpdatedDate}']")
                .TextContent.Trim()
                .ShouldBe(expected);
        });
    }

    private sealed class StubLastUpdatedBus(WorkOrder existing) : Bus(null!)
    {
        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is EmployeeGetAllQuery)
            {
                return Task.FromResult((TResponse)(object)Array.Empty<Employee>());
            }

            if (request is WorkOrderByNumberQuery)
            {
                return Task.FromResult((TResponse)(object)existing);
            }

            if (request is WorkOrderAttachmentsQuery)
            {
                return Task.FromResult((TResponse)(object)Array.Empty<WorkOrderAttachment>());
            }

            throw new NotImplementedException($"Unhandled request type: {request.GetType().Name}");
        }
    }

    private sealed class StubWorkOrderBuilder(Employee creator) : IWorkOrderBuilder
    {
        public WorkOrder CreateNewWorkOrder(Employee currentUser)
        {
            return new WorkOrder
            {
                Id = Guid.NewGuid(),
                Number = "WO-NEW",
                Status = WorkOrderStatus.Draft,
                Creator = creator,
                Title = "New"
            };
        }
    }

    private sealed class StubUserSession(Employee user) : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync() => Task.FromResult<Employee?>(user);
    }

    private sealed class StubTranslationService : ITranslationService
    {
        public Task<string> TranslateAsync(string text, string targetLanguageCode) => Task.FromResult(text);
    }
}
