using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using Bunit;
using IndexPage = ClearMeasure.Bootcamp.UI.Shared.Pages.Index;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class IndexTests
{
    [Test]
    public void ShouldLinkEachStatusCardToSearchWithStatusKeyAndAccessibleLabel()
    {
        using var ctx = CreateContext();

        var component = ctx.Render<IndexPage>();

        foreach (var status in WorkOrderStatus.GetAllItems())
        {
            var count = StatusCounts[status.Key];
            var card = component.Find($"[data-testid='{nameof(IndexPage.Elements.StatusCard)}{status.Key}']");
            card.GetAttribute("href").ShouldBe($"/workorder/search?status={status.Key}");
            card.GetAttribute("aria-label").ShouldBe($"View {count} {status.FriendlyName} work orders");
        }
    }

    private static readonly Dictionary<string, int> StatusCounts = new()
    {
        [WorkOrderStatus.Draft.Key] = 3,
        [WorkOrderStatus.Assigned.Key] = 2,
        [WorkOrderStatus.InProgress.Key] = 1,
        [WorkOrderStatus.Complete.Key] = 0,
        [WorkOrderStatus.Cancelled.Key] = 4
    };

    private static BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IBus>(new StubStatusCountBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.AddAuthorization().SetAuthorized("testuser");
        return ctx;
    }

    // ReSharper disable once ConvertToPrimaryConstructor -- epic guardrail: no mass primary-constructor conversion
    private sealed class StubStatusCountBus : Bus
    {
        public StubStatusCountBus() : base(null!)
        {
        }

        public override Task Publish(INotification notification)
        {
            return Task.CompletedTask;
        }

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is WorkOrderCountByStatusQuery)
            {
                return Task.FromResult((TResponse)(object)StatusCounts);
            }

            throw new NotImplementedException(request.GetType().Name);
        }
    }
}
