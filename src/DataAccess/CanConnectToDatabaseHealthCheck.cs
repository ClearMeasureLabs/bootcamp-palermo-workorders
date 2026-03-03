using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClearMeasure.Bootcamp.DataAccess;

public class CanConnectToDatabaseHealthCheck(DataContext context)
    : IHealthCheck
{
    private readonly DbContext _context = context;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context1,
        CancellationToken cancellationToken = new())
    {
        var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
        
        return new HealthCheckResult(canConnect 
            ? HealthStatus.Healthy 
            : HealthStatus.Unhealthy);
    }
}