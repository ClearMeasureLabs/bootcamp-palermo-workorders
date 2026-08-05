using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>
/// Service for sending email notifications.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email with HTML and plain text content.
    /// </summary>
    Task SendEmailAsync(string to, string subject, string htmlBody, string textBody);
    
    /// <summary>
    /// Sends notification when work order is assigned.
    /// </summary>
    Task SendWorkOrderAssignedAsync(WorkOrder workOrder, Employee assignee);
    
    /// <summary>
    /// Sends notification when work order is completed.
    /// </summary>
    Task SendWorkOrderCompletedAsync(WorkOrder workOrder, Employee creator);
    
    /// <summary>
    /// Sends notification when work order status changes.
    /// </summary>
    Task SendWorkOrderStatusChangedAsync(WorkOrder workOrder, WorkOrderStatus oldStatus, WorkOrderStatus newStatus);
}
