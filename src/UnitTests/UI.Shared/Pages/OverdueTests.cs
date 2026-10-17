using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

public class OverdueTests
{
    private static readonly DateOnly Yesterday = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1));
    private static readonly DateOnly Tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1));

    [Test]
    public async Task ShouldRender_OnlyOverdueOpenOrders()
    {
        var creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jeffrey@example.com");
        var overdueOpen = new WorkOrder
        {
            Number = "OVR-001",
            Title = "Fix leak",
            Status = WorkOrderStatus.InProgress,
            Creator = creator,
            DueDate = Yesterday
        };
        var futureOpen = new WorkOrder
        {
            Number = "FUT-001",
            Title = "Paint wall",
            Status = WorkOrderStatus.Assigned,
            Creator = creator,
            DueDate = Tomorrow
        };
        var overdueComplete = new WorkOrder
        {
            Number = "CMP-001",
            Title = "Old job",
            Status = WorkOrderStatus.Complete,
            Creator = creator,
            DueDate = Yesterday
        };

        await using var ctx = CreateContext(new OverdueFilteringStubBus(overdueOpen, futureOpen, overdueComplete));
        var component = ctx.Render<Overdue>();

        component.FindAll($"[data-testid='{Overdue.Elements.Row}OVR-001']").Count.ShouldBe(1);
        component.FindAll($"[data-testid='{Overdue.Elements.Row}FUT-001']").Count.ShouldBe(0);
        component.FindAll($"[data-testid='{Overdue.Elements.Row}CMP-001']").Count.ShouldBe(0);
    }

    private static BunitContext CreateContext(IBus bus)
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton(bus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        return ctx;
    }

    private sealed class OverdueFilteringStubBus(params WorkOrder[] all) : Bus(null!)
    {
        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is WorkOrderSpecificationQuery { OverdueOnly: true })
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
                var filtered = all
                    .Where(wo =>
                        wo.DueDate < today &&
                        (wo.Status == WorkOrderStatus.Draft ||
                         wo.Status == WorkOrderStatus.Assigned ||
                         wo.Status == WorkOrderStatus.InProgress))
                    .ToArray();
                return Task.FromResult((TResponse)(object)filtered);
            }

            throw new NotImplementedException();
        }
    }
}
