using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
///     Background service that periodically evaluates work orders and reformats
///     their title and description fields using an AI agent.
///     Title is corrected to start with a capital letter.
///     Description is corrected for grammar and punctuation.
/// </summary>
public class AutoReformatAgentService : BackgroundService
{
    private readonly IServiceScope _serviceScope;
    private readonly ILogger<AutoReformatAgentService> _logger;
    private readonly TimeProvider _timeProvider;

    public AutoReformatAgentService(
        IServiceProvider serviceProvider,
        ILogger<AutoReformatAgentService> logger,
        TimeProvider timeProvider)
    {
        _serviceScope = serviceProvider.CreateScope();
        _logger = logger;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.Equals(Environment.GetEnvironmentVariable("DISABLE_AUTO_REFORMAT_AGENT"), "true",
                StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("AutoReformatAgentService disabled via DISABLE_AUTO_REFORMAT_AGENT environment variable");
            return;
        }

        _logger.LogInformation("AutoReformatAgentService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReformatWorkOrdersAsync();
                await Task.Delay(TimeSpan.FromSeconds(5), _timeProvider, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AutoReformatAgentService execution");
                await Task.Delay(TimeSpan.FromSeconds(30), _timeProvider, stoppingToken);
            }
        }

        _logger.LogInformation("AutoReformatAgentService stopped");
    }

    private async Task ReformatWorkOrdersAsync()
    {
        var bus = _serviceScope.ServiceProvider.GetRequiredService<IBus>();
        var agent = _serviceScope.ServiceProvider.GetRequiredService<WorkOrderReformatAgent>();

        try
        {
            var specification = new WorkOrderSpecificationQuery();
            specification.MatchStatus(WorkOrderStatus.Draft);

            var draftWorkOrders = await bus.Send(specification);

            _logger.LogDebug("Found {Count} draft work orders to evaluate for reformatting", draftWorkOrders.Length);

            foreach (var workOrder in draftWorkOrders)
            {
                try
                {
                    var result = await agent.ReformatWorkOrderAsync(workOrder);

                    if (result != null)
                    {
                        await ApplyReformatAsync(workOrder, result);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reformatting WorkOrder {WorkOrderNumber}",
                        workOrder.Number);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving draft work orders for reformatting");
        }
    }

    private async Task ApplyReformatAsync(WorkOrder workOrder, ReformatResult result)
    {
        using var scope = _serviceScope.ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();

        try
        {
            workOrder.Title = result.Title;
            workOrder.Description = result.Description;

            dbContext.Attach(workOrder);
            dbContext.Update(workOrder);
            await dbContext.SaveChangesAsync();

            _logger.LogInformation("Successfully reformatted WorkOrder {WorkOrderNumber}",
                workOrder.Number);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving reformatted WorkOrder {WorkOrderNumber}",
                workOrder.Number);
        }
    }
}
