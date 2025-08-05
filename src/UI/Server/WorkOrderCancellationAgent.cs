using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.LlmGateway;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.UI.Server;

public class WorkOrderCancellationAgent : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WorkOrderCancellationAgent> _logger;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(5));

    public WorkOrderCancellationAgent(IServiceProvider serviceProvider, ILogger<WorkOrderCancellationAgent> logger)
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
                var shouldCancel = await EvaluateWorkOrderForCancellation(workOrder, scope.ServiceProvider);

                if (shouldCancel)
                {
                    var creator = workOrder.Creator;
                    if (creator == null)
                    {
                        _logger.LogWarning("Work order {WorkOrderNumber} has no creator, skipping cancellation", workOrder.Number);
                        continue;
                    }

                    var command = new AssignedToCancelledCommand(workOrder, creator);
                    await bus.Send(command);
                    
                    _logger.LogInformation("Successfully cancelled work order {WorkOrderNumber} based on AI evaluation", workOrder.Number);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to evaluate work order {WorkOrderNumber} for cancellation", workOrder.Number);
            }
        }
    }

    private async Task<bool> EvaluateWorkOrderForCancellation(WorkOrder workOrder, IServiceProvider serviceProvider)
    {
        try
        {
            var chatClientFactory = serviceProvider.GetService<ChatClientFactory>();
            if (chatClientFactory == null)
            {
                _logger.LogWarning("ChatClientFactory not available, falling back to rule-based evaluation");
                return EvaluateWithRules(workOrder);
            }

            var chatClient = await chatClientFactory.GetChatClient();
            
            var systemPrompt = @"You are an AI agent responsible for evaluating work orders for automatic cancellation.
Analyze this work order and determine if it should be cancelled:

Decision criteria:
- Cancel if assigned more than 24 hours ago
- Cancel if description contains keywords like 'test', 'demo', or 'temporary'
- Cancel if room number is 'TEST' or similar

Respond with only 'YES' to cancel or 'NO' to keep the work order.";

            var workOrderInfo = $@"Work Order: {workOrder.Number}
Title: {workOrder.Title}
Description: {workOrder.Description}
Room Number: {workOrder.RoomNumber}
Assigned Date: {workOrder.AssignedDate}
Current Time: {DateTime.UtcNow}
Hours Since Assignment: {GetHoursSinceAssignment(workOrder)}";

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, systemPrompt),
                new(ChatRole.User, workOrderInfo)
            };
        
            var response = await chatClient.GetResponseAsync(messages);
            var decision = response.Text?.Trim().ToUpperInvariant();
            
            _logger.LogDebug("AI evaluation for work order {WorkOrderNumber}: {Decision}", workOrder.Number, decision);
            
            return decision == "YES";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AI evaluation for work order {WorkOrderNumber}, falling back to rule-based evaluation", workOrder.Number);
            return EvaluateWithRules(workOrder);
        }
    }

    private static bool EvaluateWithRules(WorkOrder workOrder)
    {
        // Fallback rule-based evaluation
        if (GetHoursSinceAssignment(workOrder) > 24)
            return true;

        var description = workOrder.Description?.ToLowerInvariant() ?? "";
        if (description.Contains("test") || description.Contains("demo") || description.Contains("temporary"))
            return true;

        var roomNumber = workOrder.RoomNumber?.ToUpperInvariant() ?? "";
        if (roomNumber == "TEST" || roomNumber.Contains("TEST"))
            return true;

        return false;
    }

    private static double GetHoursSinceAssignment(WorkOrder workOrder)
    {
        if (workOrder.AssignedDate == null)
            return 0;

        return (DateTime.UtcNow - workOrder.AssignedDate.Value).TotalHours;
    }

    public override void Dispose()
    {
        _timer.Dispose();
        base.Dispose();
    }
}