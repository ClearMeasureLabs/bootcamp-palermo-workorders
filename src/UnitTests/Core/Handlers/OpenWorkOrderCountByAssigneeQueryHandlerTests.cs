using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.Data.Sqlite;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Handlers;

[TestFixture]
public class OpenWorkOrderCountByAssigneeQueryHandlerTests
{
    private DataContext _context = null!;

    [SetUp]
    public void SetUp()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"owocba_test_{Guid.NewGuid():N}.db");
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
    public async Task ShouldCountOnlyOpenOrdersForAssignee()
    {
        var assignee = new Employee("alice", "A", "Lice", "a@test.com");
        var other = new Employee("bob", "B", "Ob", "b@test.com");
        var creator = new Employee("creator", "C", "R", "c@test.com");
        _context.Add(assignee);
        _context.Add(other);
        _context.Add(creator);
        AddWorkOrder(_context, creator, assignee, "WO-1", WorkOrderStatus.Assigned);
        AddWorkOrder(_context, creator, assignee, "WO-2", WorkOrderStatus.InProgress);
        AddWorkOrder(_context, creator, assignee, "WO-3", WorkOrderStatus.Draft);
        AddWorkOrder(_context, creator, other, "WO-4", WorkOrderStatus.Assigned);
        await _context.SaveChangesAsync();

        var handler = new OpenWorkOrderCountByAssigneeQueryHandler(_context);
        var result = await handler.Handle(new OpenWorkOrderCountByAssigneeQuery("alice"));

        result.ShouldBe(2);
    }

    private static void AddWorkOrder(
        DataContext context, Employee creator, Employee assignee, string number, WorkOrderStatus status)
    {
        context.Add(new WorkOrder
        {
            Creator = creator, Assignee = assignee, Number = number, Status = status
        });
    }

    private sealed class FileDbConfig(string connectionString) : IDatabaseConfiguration
    {
        public string GetConnectionString() => connectionString;

        public void ResetConnectionPool() { }
    }
}
