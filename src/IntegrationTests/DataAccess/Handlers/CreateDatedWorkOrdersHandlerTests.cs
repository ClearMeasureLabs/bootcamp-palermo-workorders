using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.Core.Services.Impl;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class CreateDatedWorkOrdersHandlerTests
{
    private sealed class ThrowingAfterCountNumberGenerator(int succeedCount) : IWorkOrderNumberGenerator
    {
        private int _count;

        public string GenerateNumber()
        {
            _count++;
            if (_count > succeedCount)
            {
                throw new InvalidOperationException("Simulated failure mid-batch");
            }

            return Guid.NewGuid().ToString("N")[..7].ToUpperInvariant();
        }
    }

    [Test]
    public async Task Handle_ShouldPersistNullAndDateWithoutUtcShift()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("creator1", "John", "Doe", "john@example.com");
        var withDate = new WorkOrder
        {
            Number = "WO-D1",
            Title = "Has due date",
            Description = "desc",
            Creator = creator,
            Status = WorkOrderStatus.Draft,
            DueDate = new DateOnly(2026, 8, 29)
        };
        var withoutDate = new WorkOrder
        {
            Number = "WO-D2",
            Title = "No due date",
            Description = "desc",
            Creator = creator,
            Status = WorkOrderStatus.Draft
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(withDate);
            context.Add(withoutDate);
            await context.SaveChangesAsync();
        }

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var loadedWith = await context.Set<WorkOrder>().SingleAsync(w => w.Number == "WO-D1");
            var loadedWithout = await context.Set<WorkOrder>().SingleAsync(w => w.Number == "WO-D2");

            loadedWith.DueDate.ShouldBe(new DateOnly(2026, 8, 29));
            loadedWithout.DueDate.ShouldBeNull();
        }
    }

    [Test]
    public async Task Handle_ShouldCreateTenInOneTransaction()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("tlovejoy", "Timothy", "Lovejoy", "t@test.com");
        var assignee = new Employee("gwillie", "Groundskeeper Willie", "MacDougal", "w@test.com");
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            await context.SaveChangesAsync();
        }

        var dueDates = Enumerable.Range(0, 10)
            .Select(i => new DateOnly(2026, 8, 29).AddDays(7 * i))
            .ToList();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await bus.Send(new CreateDatedWorkOrdersCommand(
            "tlovejoy",
            "gwillie",
            "Mow the grass",
            "Weekly Saturday mow",
            dueDates));

        result.Success.ShouldBeTrue();
        result.WorkOrders.Count.ShouldBe(10);

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var created = await context.Set<WorkOrder>()
                .Where(w => w.Assignee!.UserName == "gwillie")
                .OrderBy(w => w.DueDate)
                .ToListAsync();
            created.Count.ShouldBe(10);
            created.Select(w => w.DueDate!.Value).ToList().ShouldBe(dueDates);
            created.ShouldAllBe(w => w.Status == WorkOrderStatus.Assigned);
            created.ShouldAllBe(w => w.Creator!.UserName == "tlovejoy");
        }
    }

    [Test]
    public async Task Handle_WhenAssigneeMissing_CreatesZero()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("tlovejoy", "Timothy", "Lovejoy", "t@test.com");
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await bus.Send(new CreateDatedWorkOrdersCommand(
            "tlovejoy",
            "missing-willie",
            "Mow the grass",
            "Weekly Saturday mow",
            [new DateOnly(2026, 8, 29)]));

        result.Success.ShouldBeFalse();
        result.WorkOrders.Count.ShouldBe(0);

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            (await context.Set<WorkOrder>().CountAsync()).ShouldBe(0);
        }
    }

    [Test]
    public async Task Handle_WhenDueDatesEmpty_CreatesZero()
    {
        new DatabaseTests().Clean();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await bus.Send(new CreateDatedWorkOrdersCommand(
            "tlovejoy",
            "gwillie",
            "Mow the grass",
            "Weekly Saturday mow",
            []));

        result.Success.ShouldBeFalse();
        result.Message.ShouldContain("At least one due date");
        result.WorkOrders.Count.ShouldBe(0);
    }

    [Test]
    public async Task Handle_WhenCreatorMissing_CreatesZero()
    {
        new DatabaseTests().Clean();

        var assignee = new Employee("gwillie", "Groundskeeper Willie", "MacDougal", "w@test.com");
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(assignee);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await bus.Send(new CreateDatedWorkOrdersCommand(
            "missing-lovejoy",
            "gwillie",
            "Mow the grass",
            "Weekly Saturday mow",
            [new DateOnly(2026, 8, 29)]));

        result.Success.ShouldBeFalse();
        result.WorkOrders.Count.ShouldBe(0);

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            (await context.Set<WorkOrder>().CountAsync()).ShouldBe(0);
        }
    }

    [Test]
    public async Task Handle_WhenPartialBatchFails_RollsBackAll()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("tlovejoy", "Timothy", "Lovejoy", "t@test.com");
        var assignee = new Employee("gwillie", "Groundskeeper Willie", "MacDougal", "w@test.com");
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            await context.SaveChangesAsync();
        }

        var dueDates = Enumerable.Range(0, 10)
            .Select(i => new DateOnly(2026, 8, 29).AddDays(7 * i))
            .ToList();

        await using var db = TestHost.GetRequiredService<DbContext>();
        var bus = TestHost.GetRequiredService<IBus>();
        var handler = new CreateDatedWorkOrdersHandler(
            db,
            bus,
            new ThrowingAfterCountNumberGenerator(5),
            TimeProvider.System,
            NullLogger<CreateDatedWorkOrdersHandler>.Instance);

        await Should.ThrowAsync<InvalidOperationException>(() => handler.Handle(
            new CreateDatedWorkOrdersCommand(
                "tlovejoy",
                "gwillie",
                "Mow the grass",
                "Weekly Saturday mow",
                dueDates),
            CancellationToken.None));

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            (await context.Set<WorkOrder>().CountAsync()).ShouldBe(0);
        }
    }
}
