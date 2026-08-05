using ClearMeasure.Bootcamp.Core.Model;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ClearMeasure.Bootcamp.Core.Services.Impl;

/// <summary>
/// SMTP-based email service implementation using MailKit.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly EmailSettings _settings;
    
    public SmtpEmailService(
        ILogger<SmtpEmailService> logger,
        IOptions<EmailSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }
    
    public async Task SendEmailAsync(string to, string subject, string htmlBody, string textBody)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            
            var builder = new BodyBuilder
            {
                HtmlBody = htmlBody,
                TextBody = textBody
            };
            message.Body = builder.ToMessageBody();
            
            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, _settings.UseSsl);
            
            if (!string.IsNullOrEmpty(_settings.SmtpUsername))
            {
                await client.AuthenticateAsync(_settings.SmtpUsername, _settings.SmtpPassword);
            }
            
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            
            _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}: {Subject}", to, subject);
            throw;
        }
    }
    
    public async Task SendWorkOrderAssignedAsync(WorkOrder workOrder, Employee assignee)
    {
        if (!assignee.EmailNotificationsEnabled || string.IsNullOrEmpty(assignee.Email))
        {
            _logger.LogDebug("Skipping email to {UserName} - notifications disabled or no email", assignee.UserName);
            return;
        }
            
        var subject = $"Work Order {workOrder.Number} Assigned to You";
        var htmlBody = RenderWorkOrderAssignedHtml(workOrder, assignee);
        var textBody = RenderWorkOrderAssignedText(workOrder, assignee);
        
        await SendEmailAsync(assignee.Email, subject, htmlBody, textBody);
    }
    
    public async Task SendWorkOrderCompletedAsync(WorkOrder workOrder, Employee creator)
    {
        if (!creator.EmailNotificationsEnabled || string.IsNullOrEmpty(creator.Email))
        {
            _logger.LogDebug("Skipping email to {UserName} - notifications disabled or no email", creator.UserName);
            return;
        }
            
        var subject = $"Work Order {workOrder.Number} Completed";
        var htmlBody = RenderWorkOrderCompletedHtml(workOrder, creator);
        var textBody = RenderWorkOrderCompletedText(workOrder, creator);
        
        await SendEmailAsync(creator.Email, subject, htmlBody, textBody);
    }
    
    public async Task SendWorkOrderStatusChangedAsync(WorkOrder workOrder, WorkOrderStatus oldStatus, WorkOrderStatus newStatus)
    {
        var recipients = new List<Employee>();
        
        if (workOrder.Creator != null && workOrder.Creator.EmailNotificationsEnabled && !string.IsNullOrEmpty(workOrder.Creator.Email))
        {
            recipients.Add(workOrder.Creator);
        }
        
        if (workOrder.Assignee != null && workOrder.Assignee.EmailNotificationsEnabled && !string.IsNullOrEmpty(workOrder.Assignee.Email))
        {
            // Don't send duplicate if creator and assignee are the same
            if (workOrder.Creator == null || workOrder.Assignee.Id != workOrder.Creator.Id)
            {
                recipients.Add(workOrder.Assignee);
            }
        }
        
        var subject = $"Work Order {workOrder.Number} Status Changed";
        var htmlBody = RenderWorkOrderStatusChangedHtml(workOrder, oldStatus, newStatus);
        var textBody = RenderWorkOrderStatusChangedText(workOrder, oldStatus, newStatus);
        
        foreach (var recipient in recipients)
        {
            await SendEmailAsync(recipient.Email!, subject, htmlBody, textBody);
        }
    }
    
    private string RenderWorkOrderAssignedHtml(WorkOrder workOrder, Employee assignee)
    {
        var workOrderUrl = $"{_settings.BaseUrl}/workorder/manage/{workOrder.Number}?mode=Edit";
        var unsubscribeUrl = $"{_settings.BaseUrl}/settings";
        
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Work Order Assigned</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <h2 style=""color: #2563eb;"">Work Order Assigned</h2>
        <p>Hello {assignee.GetFullName()},</p>
        <p>A work order has been assigned to you:</p>
        
        <div style=""border: 1px solid #e5e7eb; border-radius: 8px; padding: 20px; margin: 20px 0; background-color: #f9fafb;"">
            <p style=""margin: 5px 0;""><strong>Work Order:</strong> {workOrder.Number}</p>
            <p style=""margin: 5px 0;""><strong>Title:</strong> {workOrder.Title}</p>
            <p style=""margin: 5px 0;""><strong>Priority:</strong> {workOrder.Priority}</p>
            <p style=""margin: 5px 0;""><strong>Category:</strong> {workOrder.Category}</p>
            <p style=""margin: 5px 0;""><strong>Description:</strong> {workOrder.Description}</p>
        </div>
        
        <p><a href=""{workOrderUrl}"" style=""display: inline-block; padding: 10px 20px; background-color: #2563eb; color: white; text-decoration: none; border-radius: 5px;"">View Work Order</a></p>
        
        <p>Please review and begin work as soon as possible.</p>
        
        <hr style=""border: none; border-top: 1px solid #e5e7eb; margin: 30px 0;"">
        <p style=""font-size: 12px; color: #6b7280;"">
            <a href=""{unsubscribeUrl}"" style=""color: #6b7280;"">Manage notification preferences</a> | 
            Church Bulletin Work Orders
        </p>
    </div>
</body>
</html>";
    }
    
    private string RenderWorkOrderAssignedText(WorkOrder workOrder, Employee assignee)
    {
        var workOrderUrl = $"{_settings.BaseUrl}/workorder/manage/{workOrder.Number}?mode=Edit";
        
        return $@"Work Order Assigned

Hello {assignee.GetFullName()},

A work order has been assigned to you:

Work Order: {workOrder.Number}
Title: {workOrder.Title}
Priority: {workOrder.Priority}
Category: {workOrder.Category}
Description: {workOrder.Description}

View Work Order: {workOrderUrl}

Please review and begin work as soon as possible.

---
Church Bulletin Work Orders
Manage notification preferences: {_settings.BaseUrl}/settings";
    }
    
    private string RenderWorkOrderCompletedHtml(WorkOrder workOrder, Employee creator)
    {
        var workOrderUrl = $"{_settings.BaseUrl}/workorder/manage/{workOrder.Number}?mode=Edit";
        var unsubscribeUrl = $"{_settings.BaseUrl}/settings";
        var completedBy = workOrder.Assignee?.GetFullName() ?? "Unknown";
        var completedDate = workOrder.CompletedDate?.ToString("MMM dd, yyyy h:mm tt") ?? "Unknown";
        
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Work Order Completed</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <h2 style=""color: #10b981;"">Work Order Completed</h2>
        <p>Hello {creator.GetFullName()},</p>
        <p>Your work order has been completed:</p>
        
        <div style=""border: 1px solid #e5e7eb; border-radius: 8px; padding: 20px; margin: 20px 0; background-color: #f9fafb;"">
            <p style=""margin: 5px 0;""><strong>Work Order:</strong> {workOrder.Number}</p>
            <p style=""margin: 5px 0;""><strong>Title:</strong> {workOrder.Title}</p>
            <p style=""margin: 5px 0;""><strong>Completed By:</strong> {completedBy}</p>
            <p style=""margin: 5px 0;""><strong>Completed Date:</strong> {completedDate}</p>
        </div>
        
        <p><a href=""{workOrderUrl}"" style=""display: inline-block; padding: 10px 20px; background-color: #10b981; color: white; text-decoration: none; border-radius: 5px;"">View Work Order</a></p>
        
        <p>Thank you for using the work order system.</p>
        
        <hr style=""border: none; border-top: 1px solid #e5e7eb; margin: 30px 0;"">
        <p style=""font-size: 12px; color: #6b7280;"">
            <a href=""{unsubscribeUrl}"" style=""color: #6b7280;"">Manage notification preferences</a> | 
            Church Bulletin Work Orders
        </p>
    </div>
</body>
</html>";
    }
    
    private string RenderWorkOrderCompletedText(WorkOrder workOrder, Employee creator)
    {
        var workOrderUrl = $"{_settings.BaseUrl}/workorder/manage/{workOrder.Number}?mode=Edit";
        var completedBy = workOrder.Assignee?.GetFullName() ?? "Unknown";
        var completedDate = workOrder.CompletedDate?.ToString("MMM dd, yyyy h:mm tt") ?? "Unknown";
        
        return $@"Work Order Completed

Hello {creator.GetFullName()},

Your work order has been completed:

Work Order: {workOrder.Number}
Title: {workOrder.Title}
Completed By: {completedBy}
Completed Date: {completedDate}

View Work Order: {workOrderUrl}

Thank you for using the work order system.

---
Church Bulletin Work Orders
Manage notification preferences: {_settings.BaseUrl}/settings";
    }
    
    private string RenderWorkOrderStatusChangedHtml(WorkOrder workOrder, WorkOrderStatus oldStatus, WorkOrderStatus newStatus)
    {
        var workOrderUrl = $"{_settings.BaseUrl}/workorder/manage/{workOrder.Number}?mode=Edit";
        var unsubscribeUrl = $"{_settings.BaseUrl}/settings";
        
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Work Order Status Changed</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <h2 style=""color: #f59e0b;"">Work Order Status Changed</h2>
        <p>A work order status has been updated:</p>
        
        <div style=""border: 1px solid #e5e7eb; border-radius: 8px; padding: 20px; margin: 20px 0; background-color: #f9fafb;"">
            <p style=""margin: 5px 0;""><strong>Work Order:</strong> {workOrder.Number}</p>
            <p style=""margin: 5px 0;""><strong>Title:</strong> {workOrder.Title}</p>
            <p style=""margin: 5px 0;""><strong>Old Status:</strong> {oldStatus.FriendlyName}</p>
            <p style=""margin: 5px 0;""><strong>New Status:</strong> {newStatus.FriendlyName}</p>
        </div>
        
        <p><a href=""{workOrderUrl}"" style=""display: inline-block; padding: 10px 20px; background-color: #f59e0b; color: white; text-decoration: none; border-radius: 5px;"">View Work Order</a></p>
        
        <hr style=""border: none; border-top: 1px solid #e5e7eb; margin: 30px 0;"">
        <p style=""font-size: 12px; color: #6b7280;"">
            <a href=""{unsubscribeUrl}"" style=""color: #6b7280;"">Manage notification preferences</a> | 
            Church Bulletin Work Orders
        </p>
    </div>
</body>
</html>";
    }
    
    private string RenderWorkOrderStatusChangedText(WorkOrder workOrder, WorkOrderStatus oldStatus, WorkOrderStatus newStatus)
    {
        var workOrderUrl = $"{_settings.BaseUrl}/workorder/manage/{workOrder.Number}?mode=Edit";
        
        return $@"Work Order Status Changed

A work order status has been updated:

Work Order: {workOrder.Number}
Title: {workOrder.Title}
Old Status: {oldStatus.FriendlyName}
New Status: {newStatus.FriendlyName}

View Work Order: {workOrderUrl}

---
Church Bulletin Work Orders
Manage notification preferences: {_settings.BaseUrl}/settings";
    }
}
