using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Handlers;

[TestFixture]
public class WorkOrderCountByStatusQueryHandlerTests
{
    private string? _dbPath;
    private DataContext? _context;

    [SetUp]
    public void SetUp()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"wocbs_test_{Guid.NewGuid():N}.db");
        _context = new DataContext(new FileDbConfig($"Data Source={_dbPath}"), null);
        _context.Database.EnsureCreated();
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_context != null)
        {
            await _context.DisposeAsync();
        }

        if (_dbPath != null && File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    [Test]
    public async Task ShouldReturnCorrectCountsForEachStatus()
    {
        var creator = new Employee("user1", "First", "Last", "email@test.com");
        _context!.Add(creator);
        AddWorkOrder(_context, creator, "WO-1", WorkOrderStatus.Draft);
        AddWorkOrder(_context, creator, "WO-2", WorkOrderStatus.Draft);
        AddWorkOrder(_context, creator, "WO-3", WorkOrderStatus.Assigned);
        AddWorkOrder(_context, creator, "WO-4", WorkOrderStatus.InProgress);
        AddWorkOrder(_context, creator, "WO-5", WorkOrderStatus.InProgress);
        AddWorkOrder(_context, creator, "WO-6", WorkOrderStatus.InProgress);
        await _context.SaveChangesAsync();

        var handler = new WorkOrderCountByStatusQueryHandler(_context);
        var result = await handler.Handle(new WorkOrderCountByStatusQuery());

        result[WorkOrderStatus.Draft.Key].ShouldBe(2);
        result[WorkOrderStatus.Assigned.Key].ShouldBe(1);
        result[WorkOrderStatus.InProgress.Key].ShouldBe(3);
        result[WorkOrderStatus.Complete.Key].ShouldBe(0);
        result[WorkOrderStatus.Cancelled.Key].ShouldBe(0);
    }

    [Test]
    public async Task ShouldDefaultMissingStatusesToZero()
    {
        var creator = new Employee("user2", "First", "Last", "email@test.com");
        _context!.Add(creator);
        AddWorkOrder(_context, creator, "WO-1", WorkOrderStatus.Complete);
        await _context.SaveChangesAsync();

        var handler = new WorkOrderCountByStatusQueryHandler(_context);
        var result = await handler.Handle(new WorkOrderCountByStatusQuery());

        result.Count.ShouldBe(WorkOrderStatus.GetAllItems().Length);
        result[WorkOrderStatus.Draft.Key].ShouldBe(0);
        result[WorkOrderStatus.Assigned.Key].ShouldBe(0);
        result[WorkOrderStatus.InProgress.Key].ShouldBe(0);
        result[WorkOrderStatus.Complete.Key].ShouldBe(1);
        result[WorkOrderStatus.Cancelled.Key].ShouldBe(0);
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
