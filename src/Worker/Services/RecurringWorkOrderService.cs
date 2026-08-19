using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.Worker.Services;

/// <summary>
/// Background service that automatically generates new work order instances from recurring templates.
/// Runs hourly to check for due recurring work orders.
/// </summary>
public class RecurringWorkOrderService : IHostedService, IDisposable
{
    private readonly ILogger<RecurringWorkOrderService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private Timer? _timer;
    
    public RecurringWorkOrderService(
        ILogger<RecurringWorkOrderService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Recurring Work Order Service starting");
        
        // Run every hour (3600000 milliseconds)
        _timer = new Timer(
            DoWork, 
            null, 
            TimeSpan.Zero,  // Start immediately
            TimeSpan.FromHours(1)); // Run every hour
            
        return Task.CompletedTask;
    }
    
    private async void DoWork(object? state)
    {
        _logger.LogInformation("Checking for due recurring work orders at {Time}", DateTime.UtcNow);
        
        using var scope = _serviceProvider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IBus>();
        
        try
        {
            // Find due recurring work orders
            var query = new RecurringWorkOrdersQuery 
            { 
                AsOfDate = DateTime.UtcNow 
            };
            var result = await bus.Send(query);
            
            _logger.LogInformation("Found {Count} recurring work orders due for generation", result.DueWorkOrders.Length);
            
            foreach (var template in result.DueWorkOrders)
            {
                try
                {
                    // Generate new instance from template
                    var command = new SaveDraftCommand
                    {
                        Title = template.Title,
                        Description = template.Description,
                        Instructions = template.Instructions,
                        RoomNumber = template.RoomNumber,
                        Priority = template.Priority,
                        AssignedToUserName = template.Assignee?.UserName,
                        ParentWorkOrderId = template.Id,
                        IsRecurring = false, // Instances are not recurring
                        RecurrencePattern = RecurrencePattern.None
                    };
                    
                    var newWorkOrder = await bus.Send(command);
                    
                    _logger.LogInformation(
                        "Generated work order {Number} from recurring template {TemplateNumber}",
                        newWorkOrder.Number,
                        template.Number);
                    
                    // Calculate next scheduled date
                    var nextDate = CalculateNextScheduledDate(
                        template.RecurrencePattern,
                        template.RecurrenceInterval,
                        template.NextScheduledDate!.Value);
                    
                    // Update template's next scheduled date
                    template.NextScheduledDate = nextDate;
                    
                    // Save the updated template
                    var updateCommand = new SaveDraftCommand
                    {
                        WorkOrderId = template.Id,
                        Title = template.Title,
                        Description = template.Description,
                        Instructions = template.Instructions,
                        RoomNumber = template.RoomNumber,
                        Priority = template.Priority,
                        AssignedToUserName = template.Assignee?.UserName,
                        IsRecurring = template.IsRecurring,
                        RecurrencePattern = template.RecurrencePattern,
                        RecurrenceInterval = template.RecurrenceInterval,
                        NextScheduledDate = nextDate
                    };
                    
                    await bus.Send(updateCommand);
                    
                    _logger.LogInformation(
                        "Updated template {TemplateNumber} next scheduled date to {NextDate}",
                        template.Number,
                        nextDate);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "Error generating instance from recurring work order {Number}", 
                        template.Number);
                }
            }
            
            _logger.LogInformation("Completed recurring work order generation check");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing recurring work orders");
        }
    }
    
    private DateTime CalculateNextScheduledDate(
        RecurrencePattern pattern,
        int interval,
        DateTime currentDate)
    {
        return pattern switch
        {
            RecurrencePattern.Weekly => currentDate.AddDays(7 * interval),
            RecurrencePattern.Monthly => currentDate.AddMonths(interval),
            RecurrencePattern.Quarterly => currentDate.AddMonths(3 * interval),
            RecurrencePattern.Annually => currentDate.AddYears(interval),
            _ => currentDate
        };
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Recurring Work Order Service stopping");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }
    
    public void Dispose()
    {
        _timer?.Dispose();
    }
}
