using ClearMeasure.Bootcamp.Core;
using DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

public class DataContext : DbContext
{
    private readonly IDatabaseConfiguration _config;
    private readonly ILogger<DataContext>? _logger;

    public DataContext(IDatabaseConfiguration config, ILogger<DataContext>? logger = null)
    {
        _config = config;
        _logger = logger;
        _logger?.LogDebug(ToString());
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.UseSqlServer(_config.GetConnectionString());

        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        new WorkOrderMap().Map(modelBuilder);
        new EmployeeMap().Map(modelBuilder);
        new RoleMap().Map(modelBuilder);
    }

    public sealed override string ToString()
    {
        return base.ToString() + "-" + GetHashCode();
    }
}