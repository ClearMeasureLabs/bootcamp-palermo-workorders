using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.UI.Server;

public class WorkOrderCancellationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WorkOrderCancellationService> _logger;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(5));

    public WorkOrderCancellationService(IServiceProvider serviceProvider, ILogger<WorkOrderCancellationService> logger)
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
                _logger.LogError(ex, "Error occurred while processing assigned work orders for cancellation");
            }
        }
    }

    private async Task ProcessAssignedWorkOrders()
    {
        using var scope = _serviceProvider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IBus>();
        var cancellationAgent = scope.ServiceProvider.GetRequiredService<WorkOrderCancellationAgent>();

        // Query for all assigned work orders
        var query = new WorkOrderSpecificationQuery();
        query.MatchStatus(WorkOrderStatus.Assigned);
        
        var assignedWorkOrders = await bus.Send(query);
        
        if (assignedWorkOrders.Length == 0)
            return;

        _logger.LogInformation("Processing {Count} assigned work orders for potential cancellation", assignedWorkOrders.Length);

        // Process each assigned work order
        foreach (var workOrder in assignedWorkOrders)
        {
            try
            {
                // Use AI agent to determine if work order should be cancelled
                var shouldCancel = await cancellationAgent.ShouldCancelWorkOrder(workOrder);

                if (shouldCancel && workOrder.Creator != null)
                {
                    // Get the creator as current user for the command
                    var creator = await bus.Send(new EmployeeByUserNameQuery(workOrder.Creator.UserName));
                    
                    if (creator != null)
                    {
                        // Send AssignedToCancelledCommand with creator as current user
                        var command = new AssignedToCancelledCommand(workOrder, creator);
                        await bus.Send(command);
                        
                        _logger.LogInformation("AI cancelled work order {WorkOrderNumber}: assigned more than 24 hours ago or contains test keywords", 
                            workOrder.Number);
                    }
                }
                else
                {
                    _logger.LogDebug("Work order {WorkOrderNumber} does not meet cancellation criteria", workOrder.Number);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process work order {WorkOrderNumber} for cancellation", workOrder.Number);
            }
        }
    }

    public override void Dispose()
    {
        _timer.Dispose();
        base.Dispose();
    }
}