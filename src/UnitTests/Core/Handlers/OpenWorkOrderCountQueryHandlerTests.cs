using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.Data.Sqlite;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Handlers;

[TestFixture]
public class OpenWorkOrderCountQueryHandlerTests
{
    private DataContext _context = null!;

    [SetUp]
    public void SetUp()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"owoc_test_{Guid.NewGuid():N}.db");
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
    public async Task ShouldCountOpenStatuses_ExcludingComplete()
    {
        var creator = new Employee("user1", "First", "Last", "email@test.com");
        _context.Add(creator);
        AddWorkOrder(_context, creator, "WO-1", WorkOrderStatus.Draft);
        AddWorkOrder(_context, creator, "WO-2", WorkOrderStatus.Assigned);
        AddWorkOrder(_context, creator, "WO-3", WorkOrderStatus.InProgress);
        AddWorkOrder(_context, creator, "WO-4", WorkOrderStatus.Cancelled);
        AddWorkOrder(_context, creator, "WO-5", WorkOrderStatus.Complete);
        await _context.SaveChangesAsync();

        var handler = new OpenWorkOrderCountQueryHandler(_context);
        var result = await handler.Handle(new OpenWorkOrderCountQuery());

        result.ShouldBe(4);
    }

    [Test]
    public async Task ShouldReturnZero_WhenOnlyCompleteExist()
    {
        var creator = new Employee("user2", "First", "Last", "email@test.com");
        _context.Add(creator);
        AddWorkOrder(_context, creator, "WO-1", WorkOrderStatus.Complete);
        AddWorkOrder(_context, creator, "WO-2", WorkOrderStatus.Complete);
        await _context.SaveChangesAsync();

        var handler = new OpenWorkOrderCountQueryHandler(_context);
        var result = await handler.Handle(new OpenWorkOrderCountQuery());

        result.ShouldBe(0);
    }

    [Test]
    public async Task ShouldReturnZero_WhenNoWorkOrders()
    {
        var handler = new OpenWorkOrderCountQueryHandler(_context);
        var result = await handler.Handle(new OpenWorkOrderCountQuery());

        result.ShouldBe(0);
    }

    private static void AddWorkOrder(DataContext context, Employee creator, string number, WorkOrderStatus status)
    {
        context.Add(new WorkOrder { Creator = creator, Number = number, Status = status });
    }

    private sealed class FileDbConfig(string connectionString) : IDatabaseConfiguration
    {
        public string GetConnectionString() => connectionString;

        public void ResetConnectionPool() { }
    }
}
