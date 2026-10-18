using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Handlers;

[TestFixture]
public class EmployeeCountQueryHandlerTests
{
    private DataContext _context = null!;

    [SetUp]
    public void SetUp()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"ecnt_test_{Guid.NewGuid():N}.db");
        _context = new DataContext(new FileDbConfig($"Data Source={dbPath}"));
        _context.Database.EnsureCreated();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.DisposeAsync();
    }

    [Test]
    public async Task ShouldReturnZero_WhenNoEmployees()
    {
        var handler = new EmployeeCountQueryHandler(_context);
        var result = await handler.Handle(new EmployeeCountQuery());
        result.ShouldBe(0);
    }

    [Test]
    public async Task ShouldReturnCountOfEmployees()
    {
        _context.Add(new Employee("u1", "A", "One", "a@test.com"));
        _context.Add(new Employee("u2", "B", "Two", "b@test.com"));
        await _context.SaveChangesAsync();

        var handler = new EmployeeCountQueryHandler(_context);
        var result = await handler.Handle(new EmployeeCountQuery());
        result.ShouldBe(2);
    }

    private sealed class FileDbConfig(string connectionString) : IDatabaseConfiguration
    {
        public string GetConnectionString() => connectionString;
        public void ResetConnectionPool() { }
    }
}
