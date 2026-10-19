using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.Data.Sqlite;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Handlers;

[TestFixture]
public class AssignedOpenWorkOrderCountQueryHandlerTests
{
    private DataContext _context = null!;

    [SetUp]
    public void SetUp()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aowoc_test_{Guid.NewGuid():N}.db");
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
    public async Task ShouldCount_OnlyAssignedAndInProgress_ForGivenAssignee()
    {
        var assignee = new Employee("jdoe", "J", "Doe", "j@test.com");
        var other = new Employee("other", "O", "Ther", "o@test.com");
        _context.Add(assignee);
        _context.Add(other);
        AddWorkOrder(_context, assignee, "WO-1", WorkOrderStatus.Assigned);
        AddWorkOrder(_context, assignee, "WO-2", WorkOrderStatus.InProgress);
        AddWorkOrder(_context, assignee, "WO-3", WorkOrderStatus.Complete);
        AddWorkOrder(_context, other, "WO-4", WorkOrderStatus.Assigned);
        await _context.SaveChangesAsync();

        var handler = new AssignedOpenWorkOrderCountQueryHandler(_context);
        var count = await handler.Handle(new AssignedOpenWorkOrderCountQuery("jdoe"));

        count.ShouldBe(2);
    }

    private static void AddWorkOrder(DataContext context, Employee assignee, string number, WorkOrderStatus status)
    {
        context.Add(new WorkOrder { Assignee = assignee, Creator = assignee, Number = number, Status = status });
    }

    private sealed class FileDbConfig(string connectionString) : IDatabaseConfiguration
    {
        public string GetConnectionString() => connectionString;

        public void ResetConnectionPool() { }
    }
}
