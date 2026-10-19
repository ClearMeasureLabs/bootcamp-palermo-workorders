using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class OpenAssignedWorkBadgeTests
{
    [Test]
    public void ShouldHideBadge_WhenCountIsZero()
    {
        using var ctx = CreateContext(count: 0);

        var cut = ctx.Render<OpenAssignedWorkBadge>();

        cut.FindAll($"[data-testid=\"{OpenAssignedWorkBadge.Elements.Badge}\"]").Count.ShouldBe(0);
    }

    [Test]
    public void ShouldShowCount_WhenAssignedOpenWorkExists()
    {
        using var ctx = CreateContext(count: 3);

        var cut = ctx.Render<OpenAssignedWorkBadge>();

        var badge = cut.Find($"[data-testid=\"{OpenAssignedWorkBadge.Elements.Badge}\"]");
        badge.TextContent.Trim().ShouldBe("3");
    }

    [Test]
    public void ShouldUpdateCount_AfterWorkOrderChangedEvent()
    {
        var bus = new StubAssignedCountBus(1);
        using var ctx = CreateContext(bus);
        var cut = ctx.Render<OpenAssignedWorkBadge>();
        cut.Find($"[data-testid=\"{OpenAssignedWorkBadge.Elements.Badge}\"]").TextContent.Trim()
            .ShouldBe("1");

        bus.NextCount = 4;
        var order = new WorkOrder { Number = "WO-1", Status = WorkOrderStatus.Assigned };
        cut.Instance.Handle(new WorkOrderChangedEvent(new StateCommandResult(order, "Save", "ok")));

        cut.WaitForAssertion(() =>
            cut.Find($"[data-testid=\"{OpenAssignedWorkBadge.Elements.Badge}\"]").TextContent.Trim()
                .ShouldBe("4"));
    }

    private static BunitContext CreateContext(int count)
    {
        return CreateContext(new StubAssignedCountBus(count));
    }

    private static BunitContext CreateContext(StubAssignedCountBus bus)
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IBus>(bus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IUserSession>(new StubLoggedInUserSession());
        return ctx;
    }

    private sealed class StubLoggedInUserSession : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync() =>
            Task.FromResult<Employee?>(new Employee("jdoe", "J", "Doe", "j@test.com"));
    }

    private sealed class StubAssignedCountBus(int initialCount) : StubBus
    {
        public int NextCount { get; set; } = initialCount;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is AssignedOpenWorkOrderCountQuery)
            {
                return Task.FromResult((TResponse)(object)NextCount);
            }

            return base.Send(request);
        }
    }
}
