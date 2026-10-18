using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.Data.Sqlite;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Handlers;

[TestFixture]
public class WorkOrdersByRoomQueryHandlerTests
{
    private DataContext _context = null!;

    [SetUp]
    public void SetUp()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"wobr_test_{Guid.NewGuid():N}.db");
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
    public async Task ShouldReturnOnlyWorkOrdersMatchingRoomNumber()
    {
        var creator = new Employee("user1", "First", "Last", "email@test.com");
        _context.Add(creator);
        _context.Add(new WorkOrder { Creator = creator, Number = "WO-1", RoomNumber = "A-100" });
        _context.Add(new WorkOrder { Creator = creator, Number = "WO-2", RoomNumber = "A-100" });
        _context.Add(new WorkOrder { Creator = creator, Number = "WO-3", RoomNumber = "B-200" });
        await _context.SaveChangesAsync();

        var handler = new WorkOrdersByRoomQueryHandler(_context);
        var result = await handler.Handle(new WorkOrdersByRoomQuery("A-100"));

        result.Length.ShouldBe(2);
        result.Select(wo => wo.Number).ShouldBe(["WO-1", "WO-2"], ignoreOrder: true);
    }

    private sealed class FileDbConfig(string connectionString) : IDatabaseConfiguration
    {
        public string GetConnectionString() => connectionString;

        public void ResetConnectionPool() { }
    }
}
