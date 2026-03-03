using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClearMeasure.Bootcamp.DataAccess;

public class CanConnectToDatabaseHealthCheck(DataContext dataContext)
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        bool canConnect;
        try
        {
            canConnect = await dataContext.Database.CanConnectAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("Cannot connect: operation cancelled.");
        }

        return canConnect
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Cannot connect to the database.");
    }
}