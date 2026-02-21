using ClearMeasure.Bootcamp.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

public class DataContext : DbContext
{
    private readonly IDatabaseConfiguration _config;

    public DataContext(IDatabaseConfiguration config, ILogger<DataContext>? logger = null)
    {
        _config = config;
        logger?.LogDebug(ToString());
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.EnableSensitiveDataLogging();

        var connectionString = _config.GetConnectionString();
        if (connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
        {
            optionsBuilder.UseSqlite(connectionString);
        }
        else
        {
            optionsBuilder.UseSqlServer(connectionString);
        }

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