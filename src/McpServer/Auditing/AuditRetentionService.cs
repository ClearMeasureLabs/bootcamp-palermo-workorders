using ClearMeasure.Bootcamp.Core.Services;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.McpServer.Auditing;

/// <summary>Audit retention configuration shared by the server hosts.</summary>
public sealed class AuditRetentionOptions
{
    /// <summary>Number of days of audit history retained; defaults to ninety.</summary>
    public int RetentionDays { get; set; } = 90;
}

/// <summary>Removes expired history daily, retrying failures without stopping the host.</summary>
public sealed class AuditRetentionService(
    IServiceScopeFactory scopeFactory,
    IOptions<AuditRetentionOptions> options,
    TimeProvider timeProvider,
    ILogger<AuditRetentionService> logger) : BackgroundService
{
    /// <summary>Purges expired history in an independent dependency scope.</summary>
    public async Task PurgeExpiredAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var audit = scope.ServiceProvider.GetRequiredService<IAuditLog>();
        await audit.PurgeAsync(timeProvider.GetUtcNow().UtcDateTime.AddDays(-options.Value.RetentionDays),
            cancellationToken);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeSpan.FromDays(1);
            try
            {
                await PurgeExpiredAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Audit retention failed; retrying in five minutes");
                delay = TimeSpan.FromMinutes(5);
            }

            try
            {
                await Task.Delay(delay, timeProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }
}
