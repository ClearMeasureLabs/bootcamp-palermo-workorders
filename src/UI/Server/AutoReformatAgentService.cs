using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
///     Background service that periodically evaluates work orders and reformats
///     their title and description fields using an AI agent.
/// </summary>
public class AutoReformatAgentService : BackgroundService
{
    private readonly IServiceScope _serviceScope;
    private readonly ILogger<AutoReformatAgentService> _logger;
    private readonly IConfiguration _configuration;
    private readonly TimeProvider _timeProvider;

    public AutoReformatAgentService(
        IServiceProvider serviceProvider,
        ILogger<AutoReformatAgentService> logger,
        IConfiguration configuration,
        TimeProvider timeProvider)
    {
        _serviceScope = serviceProvider.CreateScope();
        _logger = logger;
        _configuration = configuration;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (IsDisabled(_configuration))
        {
            _logger.LogInformation("AutoReformatAgentService disabled via DISABLE_AUTO_REFORMAT_AGENT configuration");
            return;
        }

        _logger.LogInformation("AutoReformatAgentService started");
        await RunLoopAsync(stoppingToken);
        _logger.LogInformation("AutoReformatAgentService stopped");
    }

    internal static bool IsDisabled(IConfiguration configuration) =>
        string.Equals(configuration["DISABLE_AUTO_REFORMAT_AGENT"], "true", StringComparison.OrdinalIgnoreCase);

    private async Task RunLoopAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested && await TryRunOnceAsync(stoppingToken))
        {
        }
    }

    private async Task<bool> TryRunOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            await ReformatWorkOrdersAsync();
            await Task.Delay(TimeSpan.FromSeconds(5), _timeProvider, stoppingToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AutoReformatAgentService execution");
            await Task.Delay(TimeSpan.FromSeconds(30), _timeProvider, stoppingToken);
            return true;
        }
    }

    internal async Task ReformatWorkOrdersAsync()
    {
        var bus = _serviceScope.ServiceProvider.GetRequiredService<IBus>();
        var agent = _serviceScope.ServiceProvider.GetRequiredService<WorkOrderReformatAgent>();

        try
        {
            var draftWorkOrders = await LoadDraftWorkOrdersAsync(bus);
            _logger.LogDebug("Found {Count} draft work orders to evaluate for reformatting", draftWorkOrders.Length);

            foreach (var workOrder in draftWorkOrders)
            {
                await TryReformatSingleWorkOrderAsync(agent, workOrder);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving draft work orders for reformatting");
        }
    }

    internal static async Task<WorkOrder[]> LoadDraftWorkOrdersAsync(IBus bus)
    {
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchStatus(WorkOrderStatus.Draft);
        return await bus.Send(specification);
    }

    private async Task TryReformatSingleWorkOrderAsync(WorkOrderReformatAgent agent, WorkOrder workOrder)
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
            _logger.LogError(ex, "Error reformatting WorkOrder {WorkOrderNumber}", workOrder.Number);
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

            _logger.LogInformation("Successfully reformatted WorkOrder {WorkOrderNumber}", workOrder.Number);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving reformatted WorkOrder {WorkOrderNumber}", workOrder.Number);
        }
    }
}
