using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.IntegrationTests.DataAccess;
using ClearMeasure.Bootcamp.McpServer.Tools;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer;

[TestFixture]
public class McpCreateDatedWorkOrdersToolTests
{
    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    [SetUp]
    public void Setup()
    {
        new DatabaseTests().Clean();
    }

    [Test]
    public async Task CreateDatedWorkOrders_WithSaturdayCount_CreatesTen()
    {
        await SeedLovejoyAndWillieAsync();
        var bus = TestHost.GetRequiredService<IBus>();
        // Saturday 2026-08-29 noon Chicago → UTC
        var clock = ChicagoNoon(2026, 8, 29);

        var result = await WorkOrderTools.CreateDatedWorkOrders(
            bus,
            clock,
            "tlovejoy",
            "gwillie",
            "Mow the grass",
            "Weekly Saturday mow",
            saturdayCount: 10);

        result.ShouldContain("Created 10 work orders");
        result.ShouldContain("2026-08-29");
        result.ShouldContain("2026-10-31");

        await using var context = TestHost.GetRequiredService<DbContext>();
        (await context.Set<WorkOrder>().CountAsync()).ShouldBe(10);
    }

    [Test]
    public async Task CreateDatedWorkOrders_WithExplicitDueDates_UsesThoseDates()
    {
        await SeedLovejoyAndWillieAsync();
        var bus = TestHost.GetRequiredService<IBus>();

        var result = await WorkOrderTools.CreateDatedWorkOrders(
            bus,
            TimeProvider.System,
            "tlovejoy",
            "gwillie",
            "Mow the grass",
            "desc",
            dueDates: "2026-09-05,2026-09-12");

        result.ShouldContain("Created 2 work orders");
        result.ShouldContain("2026-09-05");
        result.ShouldContain("2026-09-12");
    }

    [Test]
    public async Task CreateDatedWorkOrders_WhenAssigneeMissing_CreatesNothing()
    {
        var creator = new Employee("tlovejoy", "Timothy", "Lovejoy", "t@test.com");
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.CreateDatedWorkOrders(
            bus,
            TimeProvider.System,
            "tlovejoy",
            "missing",
            "Mow",
            "desc",
            saturdayCount: 3);

        result.ShouldContain("not found");
        await using var db = TestHost.GetRequiredService<DbContext>();
        (await db.Set<WorkOrder>().CountAsync()).ShouldBe(0);
    }

    [Test]
    public void ResolveDueDates_WhenInvalidList_ReturnsError()
    {
        var (dates, error) = DatedWorkOrderScheduling.ResolveDueDates(
            TimeProvider.System,
            "not-a-date",
            10);

        dates.Count.ShouldBe(0);
        error.ShouldNotBeNull();
        error.ShouldContain("Invalid due date");
    }

    [Test]
    public void ResolveDueDates_WhenSaturdayCountInvalid_ReturnsError()
    {
        var (dates, error) = DatedWorkOrderScheduling.ResolveDueDates(
            TimeProvider.System,
            null,
            0);

        dates.Count.ShouldBe(0);
        error.ShouldNotBeNull();
        error.ShouldContain("saturdayCount");
    }

    [Test]
    public void ShouldRejectSaturdayCountAboveTransactionalBatchLimit()
    {
        var (dates, error) = DatedWorkOrderScheduling.ResolveDueDates(
            TimeProvider.System,
            null,
            11);

        dates.Count.ShouldBe(0);
        error.ShouldNotBeNull();
        error.ShouldContain("between 1 and 10");
    }

    [Test]
    public void ShouldRejectExplicitDateListAboveTransactionalBatchLimit()
    {
        var dueDates = string.Join(',', Enumerable.Repeat("2026-09-01", 11));

        var (dates, error) = DatedWorkOrderScheduling.ResolveDueDates(
            TimeProvider.System,
            dueDates,
            10);

        dates.Count.ShouldBe(0);
        error.ShouldNotBeNull();
        error.ShouldContain("cannot exceed 10");
    }

    [Test]
    public void FormatResult_WhenFailure_ReturnsMessage()
    {
        var text = DatedWorkOrderScheduling.FormatResult(
            new CreateDatedWorkOrdersResult(false, "Assignee missing", []));
        text.ShouldBe("Assignee missing");
    }

    [Test]
    public void BuildComingSaturdays_ReturnsConsecutiveWeeks()
    {
        var dates = DatedWorkOrderScheduling.BuildComingSaturdays(ChicagoNoon(2026, 8, 28), 3);
        dates.ShouldBe([
            new DateOnly(2026, 8, 29),
            new DateOnly(2026, 9, 5),
            new DateOnly(2026, 9, 12)
        ]);
    }

    private static async Task SeedLovejoyAndWillieAsync()
    {
        var creator = new Employee("tlovejoy", "Timothy", "Lovejoy", "t@test.com");
        var assignee = new Employee("gwillie", "Groundskeeper Willie", "MacDougal", "w@test.com");
        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(assignee);
        await context.SaveChangesAsync();
    }

    private static FixedTimeProvider ChicagoNoon(int year, int month, int day)
    {
        var local = new DateTime(year, month, day, 12, 0, 0, DateTimeKind.Unspecified);
        var utc = TimeZoneInfo.ConvertTimeToUtc(local, ChurchTimeZone.Chicago);
        return new FixedTimeProvider(new DateTimeOffset(utc, TimeSpan.Zero));
    }
}
