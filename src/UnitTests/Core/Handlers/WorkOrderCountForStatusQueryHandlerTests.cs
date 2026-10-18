using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.Data.Sqlite;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Handlers;

[TestFixture]
public class WorkOrderCountForStatusQueryHandlerTests
{
    private DataContext _context = null!;

    [SetUp]
    public void SetUp()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"wocfs_test_{Guid.NewGuid():N}.db");
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
    public async Task ShouldReturnCountForGivenStatus()
    {
        var creator = new Employee("user1", "First", "Last", "email@test.com");
        _context.Add(creator);
        _context.Add(new WorkOrder { Creator = creator, Number = "WO-1", Status = WorkOrderStatus.Draft });
        _context.Add(new WorkOrder { Creator = creator, Number = "WO-2", Status = WorkOrderStatus.Draft });
        _context.Add(new WorkOrder { Creator = creator, Number = "WO-3", Status = WorkOrderStatus.Assigned });
        await _context.SaveChangesAsync();

        var handler = new WorkOrderCountForStatusQueryHandler(_context);
        var draftCount = await handler.Handle(new WorkOrderCountForStatusQuery(WorkOrderStatus.Draft.Key));
        var assignedCount = await handler.Handle(new WorkOrderCountForStatusQuery(WorkOrderStatus.Assigned.Key));

        draftCount.ShouldBe(2);
        assignedCount.ShouldBe(1);
    }

    private sealed class FileDbConfig(string connectionString) : IDatabaseConfiguration
    {
        public string GetConnectionString() => connectionString;

        public void ResetConnectionPool() { }
    }
}
