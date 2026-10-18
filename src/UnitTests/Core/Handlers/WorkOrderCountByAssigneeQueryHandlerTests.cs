using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.Data.Sqlite;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Handlers;

[TestFixture]
public class WorkOrderCountByAssigneeQueryHandlerTests
{
    private DataContext _context = null!;

    [SetUp]
    public void SetUp()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"wocba_test_{Guid.NewGuid():N}.db");
        _context = new DataContext(new FileDbConfig($"Data Source={dbPath}"));
        _context.Database.EnsureCreated();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.DisposeAsync();
        SqliteConnection.ClearAllPools();
    }

    [Test]
    public async Task ShouldCountOpenWorkOrdersByAssignee_ExcludingCompleteAndCancelled()
    {
        var creator = new Employee("creator1", "First", "Last", "email@test.com");
        var alice = new Employee("alice1", "Alice", "A", "alice@test.com");
        var bob = new Employee("bob1", "Bob", "B", "bob@test.com");
        _context.Add(creator);
        _context.Add(alice);
        _context.Add(bob);
        AddWorkOrder(_context, creator, alice, "WOA1", WorkOrderStatus.Assigned);
        AddWorkOrder(_context, creator, alice, "WOA2", WorkOrderStatus.InProgress);
        AddWorkOrder(_context, creator, alice, "WOA3", WorkOrderStatus.Cancelled);
        AddWorkOrder(_context, creator, bob, "WOB1", WorkOrderStatus.Draft);
        AddWorkOrder(_context, creator, bob, "WOB2", WorkOrderStatus.Complete);
        await _context.SaveChangesAsync();

        var handler = new WorkOrderCountByAssigneeQueryHandler(_context);
        var result = await handler.Handle(new WorkOrderCountByAssigneeQuery());

        result["alice1"].ShouldBe(2);
        result["bob1"].ShouldBe(1);
    }

    private static void AddWorkOrder(
        DataContext context,
        Employee creator,
        Employee assignee,
        string number,
        WorkOrderStatus status)
    {
        context.Add(new WorkOrder
        {
            Creator = creator,
            Assignee = assignee,
            Number = number,
            Status = status
        });
    }

    private sealed class FileDbConfig(string connectionString) : IDatabaseConfiguration
    {
        public string GetConnectionString() => connectionString;

        public void ResetConnectionPool() { }
    }
}
