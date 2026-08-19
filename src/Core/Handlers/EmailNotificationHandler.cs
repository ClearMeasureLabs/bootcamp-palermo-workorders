using ClearMeasure.Bootcamp.Core.Events;
using ClearMeasure.Bootcamp.Core.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.Core.Handlers;

/// <summary>
/// Handles work order events and sends email notifications.
/// </summary>
public class EmailNotificationHandler : 
    INotificationHandler<WorkOrderAssignedEvent>,
    INotificationHandler<WorkOrderCompletedEvent>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailNotificationHandler> _logger;
    
    public EmailNotificationHandler(
        IEmailService emailService,
        ILogger<EmailNotificationHandler> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }
    
    public async Task Handle(WorkOrderAssignedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            if (notification.WorkOrder.Assignee != null)
            {
                _logger.LogInformation(
                    "Sending assignment email for work order {Number} to {UserName}",
                    notification.WorkOrder.Number,
                    notification.WorkOrder.Assignee.UserName);
                    
                await _emailService.SendWorkOrderAssignedAsync(
                    notification.WorkOrder, 
                    notification.WorkOrder.Assignee);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to send assignment email for work order {Number}", 
                notification.WorkOrder.Number);
            // Don't throw - email failure shouldn't block work order operations
        }
    }
    
    public async Task Handle(WorkOrderCompletedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            if (notification.WorkOrder.Creator != null)
            {
                _logger.LogInformation(
                    "Sending completion email for work order {Number} to {UserName}",
                    notification.WorkOrder.Number,
                    notification.WorkOrder.Creator.UserName);
                    
                await _emailService.SendWorkOrderCompletedAsync(
                    notification.WorkOrder, 
                    notification.WorkOrder.Creator);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to send completion email for work order {Number}", 
                notification.WorkOrder.Number);
            // Don't throw - email failure shouldn't block work order operations
        }
    }
}
