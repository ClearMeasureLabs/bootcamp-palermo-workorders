using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.Handlers;

[TestFixture]
public class WeeklyCompletedSummaryQueryHandlerTests
{
    private string _dbPath = null!;
    private DataContext _context = null!;

    [SetUp]
    public void SetUp()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".db");
        Environment.SetEnvironmentVariable("DATABASE_ENGINE", "SQLite");
        _context = new DataContext(new StubDatabaseConfiguration($"Data Source={_dbPath}"));
        _context.Database.EnsureCreated();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    [Test]
    public async Task Should_Handle_CountCompletedInLastWeek()
    {
        var recent = new WorkOrder
        {
            Number = "WO-1",
            Title = "Done recently",
            Status = WorkOrderStatus.Complete,
            CompletedDate = DateTime.UtcNow.AddDays(-2)
        };
        var old = new WorkOrder
        {
            Number = "WO-2",
            Title = "Done long ago",
            Status = WorkOrderStatus.Complete,
            CompletedDate = DateTime.UtcNow.AddDays(-30)
        };
        _context.Set<WorkOrder>().AddRange(recent, old);
        await _context.SaveChangesAsync();

        var handler = new WeeklyCompletedSummaryQueryHandler(_context);

        var count = await handler.Handle(new WeeklyCompletedSummaryQuery());

        count.ShouldBe(1);
    }

    private sealed class StubDatabaseConfiguration(string connectionString) : IDatabaseConfiguration
    {
        public string GetConnectionString() => connectionString;
        public void ResetConnectionPool()
        {
        }
    }
}
