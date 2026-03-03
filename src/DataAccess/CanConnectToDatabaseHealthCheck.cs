using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.DataAccess;

public class CanConnectToDatabaseHealthCheck(
    DataContext context,
    ILogger<CanConnectToDatabaseHealthCheck> logger)
    : IHealthCheck
{
    private readonly DbContext _context = context;
    private readonly ILogger<CanConnectToDatabaseHealthCheck> _logger = logger;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context1,
        CancellationToken cancellationToken = new())
    {
        var canConnect = false;
        
        try
        {
            canConnect = await _context.Database.CanConnectAsync(cancellationToken);

            if (canConnect is false)
            {
                _logger.LogError("Database connection failed in health check");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Database health check cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection failed in health check with exception");
            
            return HealthCheckResult.Unhealthy(
                description: "Database connection health check failed",
                exception: ex);
        }

        return new HealthCheckResult(canConnect
            ? HealthStatus.Healthy
            : HealthStatus.Unhealthy);
    }
}