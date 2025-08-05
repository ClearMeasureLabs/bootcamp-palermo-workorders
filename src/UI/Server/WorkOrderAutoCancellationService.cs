using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.UI.Server;

public class WorkOrderAutoCancellationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WorkOrderAutoCancellationService> _logger;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(5));

    public WorkOrderAutoCancellationService(IServiceProvider serviceProvider, ILogger<WorkOrderAutoCancellationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAssignedWorkOrders();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing assigned work orders");
            }
        }
    }

    private async Task ProcessAssignedWorkOrders()
    {
        using var scope = _serviceProvider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IBus>();

        // Query for all assigned work orders
        var query = new WorkOrderSpecificationQuery();
        query.MatchStatus(WorkOrderStatus.Assigned);

        var assignedWorkOrders = await bus.Send(query);

        if (assignedWorkOrders.Length == 0)
            return;

        foreach (var workOrder in assignedWorkOrders)
        {
            try
            {
                if (workOrder.Creator == null)
                {
                    _logger.LogWarning("Work order {WorkOrderNumber} has no creator, cannot cancel", workOrder.Number);
                    continue;
                }

                // Send AssignedToCancelledCommand (assumed to exist)
                var command = new AssignedToCancelledCommand(workOrder, workOrder.Creator);
                await bus.Send(command);

                _logger.LogInformation("Cancelled work order {WorkOrderNumber} by creator {CreatorUserName}",
                    workOrder.Number, workOrder.Creator.UserName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel work order {WorkOrderNumber}", workOrder.Number);
            }
        }
    }

    public override void Dispose()
    {
        _timer.Dispose();
        base.Dispose();
    }
}
