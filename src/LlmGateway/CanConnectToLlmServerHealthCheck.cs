using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace ProgrammingWithPalermo.ChurchBulletin.DataAccess;

public class CanConnectToLlmServerHealthCheck : IHealthCheck
{
    private readonly ILogger<CanConnectToLlmServerHealthCheck> _logger;

    public CanConnectToLlmServerHealthCheck(ILogger<CanConnectToLlmServerHealthCheck> logger)
    {
        _logger = logger;
    }
    
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        if (!connectedOk()) 
            return Task.FromResult(HealthCheckResult.Unhealthy("Cannot connect to LLM Server"));
        
        _logger.LogInformation($"Health check success");
        return Task.FromResult(HealthCheckResult.Healthy());
    }

    private bool connectedOk()
    {
        return true;
    }
}