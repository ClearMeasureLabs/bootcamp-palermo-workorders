using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class WorkOrderSummarizeTests
{
    [Test]
    public void ShouldRenderSummarizeButton()
    {
        using var ctx = CreateContext();

        var cut = ctx.Render<WorkOrderSummarize>(parameters => parameters
            .Add(p => p.Title, "Fix sink")
            .Add(p => p.Description, "Leaking")
            .Add(p => p.Instructions, "Shut off water"));

        cut.Find("[data-testid=work-order-summarize]").TextContent.ShouldContain("Summarize");
        cut.FindAll("[data-testid=work-order-summary]").Count.ShouldBe(0);
    }

    [Test]
    public async Task ShouldShowSummaryUnderButton_WhenSummarizeClicked()
    {
        using var ctx = CreateContext(new WorkOrderSummarizeResult("One paragraph about the sink."));

        var cut = ctx.Render<WorkOrderSummarize>(parameters => parameters
            .Add(p => p.Title, "Fix sink")
            .Add(p => p.Description, "Leaking")
            .Add(p => p.Instructions, "Shut off water"));

        await cut.Find("[data-testid=work-order-summarize]").ClickAsync();

        cut.Find("[data-testid=work-order-summary]").TextContent
            .ShouldBe("One paragraph about the sink.");
    }

    [Test]
    public async Task ShouldShowFriendlyMessage_WhenLlmUnavailable()
    {
        using var ctx = CreateContext(new WorkOrderSummarizeResult(
            "AI summary is unavailable right now. Please try again later."));

        var cut = ctx.Render<WorkOrderSummarize>(parameters => parameters
            .Add(p => p.Title, "T")
            .Add(p => p.Description, "D")
            .Add(p => p.Instructions, "I"));

        await cut.Find("[data-testid=work-order-summarize]").ClickAsync();

        cut.Find("[data-testid=work-order-summary]").TextContent
            .ShouldContain("unavailable");
    }

    private static BunitContext CreateContext(WorkOrderSummarizeResult? result = null)
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IBus>(new StubSummarizeBus(result ?? new WorkOrderSummarizeResult("")));
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        return ctx;
    }

    private sealed class StubSummarizeBus(WorkOrderSummarizeResult result) : IBus
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is WorkOrderSummarizeQuery)
            {
                return Task.FromResult((TResponse)(object)result);
            }

            throw new NotImplementedException();
        }

        public Task<object?> Send(object request) => throw new NotImplementedException();

        public Task Publish(INotification notification) => throw new NotImplementedException();
    }
}
