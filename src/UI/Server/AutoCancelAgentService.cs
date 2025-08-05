using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Copilot Prompt:
/// Inside UI.Server, I want a background job implemented as a serviceworker. I want it to wake up every 5 seconds and use Bus.Send for WorkOrderSpecificationQuery for all WorkOrders that are in Assigned Status.  get employees with EmployeeByUserNameQuery. And send a AssignedToCancelledCommand to the Bus with the current user being the creator of the workOrder. Don't create any tests. just the service worker that automatically starts with UI.Server starts up.  Use the microsoft.extensions.ai agent interfaces and base classes to make an ai agent with this logic.
/// Agent should use these instructions You are an AI agent responsible for evaluating work orders for automatic cancellation.Analyze this work order and determine if it should be cancelled:
/// Decision criteria:
///     �	Cancel if assigned more than 24 hours ago
///     �	Cancel if description contains keywords like 'test', 'demo', or 'temporary'
///     �	Cancel if room number is 'TEST' or similar
///     use ChatClientFactory
/// </summary>
public class AutoCancelAgentService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AutoCancelAgentService> _logger;
    private readonly TimeProvider _timeProvider;

    public AutoCancelAgentService(
        IServiceProvider serviceProvider, 
        ILogger<AutoCancelAgentService> logger,
        TimeProvider timeProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AutoCancelAgentService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EvaluateAssignedWorkOrdersAsync();
                await Task.Delay(TimeSpan.FromSeconds(5), _timeProvider, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AutoCancelAgentService execution");
                await Task.Delay(TimeSpan.FromSeconds(30), _timeProvider, stoppingToken);
            }
        }

        _logger.LogInformation("AutoCancelAgentService stopped");
    }

    private async Task EvaluateAssignedWorkOrdersAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IBus>();
        var agent = scope.ServiceProvider.GetRequiredService<WorkOrderEvaluationAgent>();

        try
        {
            // Query for all work orders with Assigned status
            var specification = new WorkOrderSpecificationQuery();
            specification.MatchStatus(WorkOrderStatus.Assigned);

            var assignedWorkOrders = await bus.Send(specification);

            _logger.LogDebug("Found {Count} assigned work orders to evaluate", assignedWorkOrders.Length);

            foreach (var workOrder in assignedWorkOrders)
            {
                try
                {
                    // Get employee details for the creator
                    if (workOrder.Creator?.UserName != null)
                    {
                        var creator = await bus.Send(new EmployeeByUserNameQuery(workOrder.Creator.UserName));
                        if (creator != null)
                        {
                            workOrder.Creator = creator;
                        }
                    }

                    // Use AI agent to evaluate if work order should be cancelled
                    var shouldCancel = await agent.ShouldCancelWorkOrderAsync(workOrder);

                    if (shouldCancel)
                    {
                        _logger.LogInformation("AI agent recommends cancelling WorkOrder {WorkOrderNumber}", 
                            workOrder.Number);
                        await agent.CancelWorkOrderAsync(workOrder);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error evaluating WorkOrder {WorkOrderNumber}", 
                        workOrder.Number);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving assigned work orders");
        }
    }
}