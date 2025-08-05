using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.UI.Server;

public class WorkOrderAutoAssignmentService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WorkOrderAutoAssignmentService> _logger;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(5));

    public WorkOrderAutoAssignmentService(IServiceProvider serviceProvider, ILogger<WorkOrderAutoAssignmentService> logger)
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
                await ProcessDraftWorkOrders();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing draft work orders");
            }
        }
    }

    private async Task ProcessDraftWorkOrders()
    {
        using var scope = _serviceProvider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IBus>();

        // Query for all draft work orders
        var query = new WorkOrderSpecificationQuery();
        query.MatchStatus(WorkOrderStatus.Draft);
        
        var draftWorkOrders = await bus.Send(query);
        
        if (draftWorkOrders.Length == 0)
            return;

        

        // Process each draft work order
        foreach (var workOrder in draftWorkOrders)
        {
            try
            {
                var assignee = await DecideAssignee(bus, workOrder);

                if (assignee == null)
                {
                    _logger.LogWarning("Employee with username 'hsimpson' not found");
                    return;
                }

                // Set assignee and send DraftToAssignedCommand
                workOrder.Assignee = assignee;
                var command = new DraftToAssignedCommand(workOrder, workOrder.Creator);
                await bus.Send(command);
                
                _logger.LogInformation("Successfully assigned work order {WorkOrderNumber} to {AssigneeUserName}", 
                    workOrder.Number, assignee.UserName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to assign work order {WorkOrderNumber}", workOrder.Number);
            }
        }
    }

    private static async Task<Employee> DecideAssignee(IBus bus, WorkOrder workOrder)
    {
        var assigneeQuery = new EmployeeByUserNameQuery("hsimpson");
        var assignee = await bus.Send(assigneeQuery);
        return assignee;
    }

    public override void Dispose()
    {
        _timer.Dispose();
        base.Dispose();
    }
}