# ADR-0004: Email Notifications for Status Changes

**Status:** Proposed  
**Date:** 2026-08-05  
**Epic:** #8236 - Email Notifications for Status Changes  
**Child Issues:** #8276, #8277, #8278

---

## Conceptual Definition Phase

### Business Context

Church staff managing work orders need timely communication about status changes:
- **Assignees** need to know when work is assigned to them
- **Creators** need to know when their requests are completed
- **Stakeholders** need updates on work order progress

Currently, users must manually check the system for updates, leading to:
- Delayed response to assigned work orders
- Lack of awareness about completion status
- Increased manual communication overhead
- Missed deadlines due to notification gaps

### Business Value

**Primary Benefits:**
1. **Timely Awareness:** Automatic notifications when work orders are assigned or completed
2. **Reduced Overhead:** Eliminate manual status checking and communication
3. **Faster Response:** Assignees notified immediately when work is assigned
4. **Improved Satisfaction:** Creators informed when their requests are completed
5. **Audit Trail:** Email records provide documentation of notifications

**Success Metrics:**
- 90% of assignees respond within 4 hours of assignment notification
- 50% reduction in "status check" inquiries
- 100% notification delivery rate for status changes
- User satisfaction score increase for communication

### User Stories

**As a Maintenance Technician:**
- I want to receive email when work is assigned to me so I can respond promptly
- I want to see work order details in the email so I can assess urgency
- I want to opt out of notifications if I prefer to check the system manually

**As a Work Order Creator:**
- I want to receive email when my work order is completed so I know it's done
- I want to receive updates on status changes so I can track progress
- I want a link to view the work order details in the system

**As a Facilities Manager:**
- I want all team members notified of assignments so work doesn't get missed
- I want notification preferences configurable so users can control their inbox
- I want email delivery failures logged so I can follow up manually

### Scope

**In Scope:**
- SMTP email configuration and infrastructure
- Email service abstraction (IEmailService)
- HTML and plain text email templates
- Notifications for status changes (Draft→Assigned, InProgress→Complete)
- User preference for enabling/disabling notifications
- Email delivery logging and error handling

**Out of Scope (Future Enhancements):**
- Digest emails (daily/weekly summaries)
- SMS/push notifications
- Custom notification rules per user
- Email scheduling/throttling
- Rich email formatting with images/branding
- Email read receipts/tracking

### Domain Model Changes

**New Service Interface:**
```csharp
public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    Task SendWorkOrderAssignedAsync(WorkOrder workOrder, Employee assignee);
    Task SendWorkOrderCompletedAsync(WorkOrder workOrder, Employee creator);
    Task SendWorkOrderStatusChangedAsync(WorkOrder workOrder, WorkOrderStatus oldStatus, WorkOrderStatus newStatus);
}
```

**Employee Entity Extension:**
```csharp
public class Employee
{
    // Existing properties...
    
    public bool EmailNotificationsEnabled { get; set; } = true;
}
```

### Business Rules

1. **Notification Triggers:**
   - Work order assigned: Send to assignee
   - Work order completed: Send to creator
   - Status changed: Send to creator and assignee (if different)

2. **Recipient Rules:**
   - Only send if recipient has EmailNotificationsEnabled = true
   - Only send if recipient has valid email address
   - Creator and assignee can be the same person (send one email)

3. **Email Content:**
   - Subject: Clear indication of action (e.g., "Work Order WO-123 Assigned to You")
   - Body: Work order number, title, description, status, link to view
   - Footer: Unsubscribe link, system information

4. **Delivery Handling:**
   - Log all email send attempts
   - Log failures but don't block work order operations
   - Retry failed sends (up to 3 attempts)

### Architecture Diagram

**Before State:**
```
┌─────────────────────────────────────────────────────────────┐
│ Current System: Manual Status Checking                      │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  User → WorkOrderManage → SaveDraftCommand                  │
│                                                              │
│  Status Changes:                                             │
│  - Work order assigned → No notification                     │
│  - Work order completed → No notification                    │
│  - Assignee must check system manually                       │
│  - Creator must check system manually                        │
│                                                              │
│  Problems:                                                   │
│  - Delayed awareness of assignments                          │
│  - Manual status checking required                           │
│  - Increased communication overhead                          │
└─────────────────────────────────────────────────────────────┘
```

**After State:**
```
┌─────────────────────────────────────────────────────────────┐
│ New System: Automatic Email Notifications                   │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  User → WorkOrderManage → SaveDraftCommand                  │
│         ↓                                                    │
│    WorkOrderStatusChanged Event                              │
│         ↓                                                    │
│    EmailNotificationHandler                                  │
│         ↓                                                    │
│    Check Employee.EmailNotificationsEnabled                  │
│         ↓                                                    │
│    IEmailService.SendWorkOrderAssignedAsync()                │
│         ↓                                                    │
│    SmtpEmailService → SMTP Server → Recipient                │
│                                                              │
│  Benefits:                                                   │
│  - Immediate notification of assignments                     │
│  - Automatic completion notifications                        │
│  - Reduced manual checking                                   │
│  - Configurable per user                                     │
└─────────────────────────────────────────────────────────────┘
```

---

## UX Design Phase

### Email Templates

**Work Order Assigned Email:**
```html
Subject: Work Order WO-{Number} Assigned to You

<html>
<body>
  <h2>Work Order Assigned</h2>
  <p>Hello {AssigneeName},</p>
  <p>A work order has been assigned to you:</p>
  
  <div style="border: 1px solid #ccc; padding: 15px; margin: 20px 0;">
    <strong>Work Order:</strong> WO-{Number}<br>
    <strong>Title:</strong> {Title}<br>
    <strong>Priority:</strong> {Priority}<br>
    <strong>Category:</strong> {Category}<br>
    <strong>Description:</strong> {Description}
  </div>
  
  <p><a href="{WorkOrderUrl}">View Work Order</a></p>
  
  <p>Please review and begin work as soon as possible.</p>
  
  <hr>
  <small>
    <a href="{UnsubscribeUrl}">Unsubscribe from notifications</a> | 
    Church Bulletin Work Orders
  </small>
</body>
</html>
```

**Work Order Completed Email:**
```html
Subject: Work Order WO-{Number} Completed

<html>
<body>
  <h2>Work Order Completed</h2>
  <p>Hello {CreatorName},</p>
  <p>Your work order has been completed:</p>
  
  <div style="border: 1px solid #ccc; padding: 15px; margin: 20px 0;">
    <strong>Work Order:</strong> WO-{Number}<br>
    <strong>Title:</strong> {Title}<br>
    <strong>Completed By:</strong> {AssigneeName}<br>
    <strong>Completed Date:</strong> {CompletedDate}
  </div>
  
  <p><a href="{WorkOrderUrl}">View Work Order</a></p>
  
  <p>Thank you for using the work order system.</p>
  
  <hr>
  <small>
    <a href="{UnsubscribeUrl}">Unsubscribe from notifications</a> | 
    Church Bulletin Work Orders
  </small>
</body>
</html>
```

### User Settings Page

**Notification Preferences Section:**
```razor
<div class="settings-section">
    <h3>Email Notifications</h3>
    <div class="form-check form-switch">
        <input class="form-check-input" 
               type="checkbox" 
               id="emailNotifications"
               @bind="Model.EmailNotificationsEnabled">
        <label class="form-check-label" for="emailNotifications">
            Receive email notifications for work order updates
        </label>
    </div>
    <small class="form-text text-muted">
        When enabled, you'll receive emails when work orders are assigned to you or completed.
    </small>
</div>
```

### Accessibility Requirements

- **Email Accessibility:** Plain text alternative for all HTML emails
- **Link Text:** Descriptive link text (not "click here")
- **Color Independence:** Information not conveyed by color alone
- **Screen Reader Friendly:** Proper heading hierarchy in emails

---

## Technical Design Phase

### Database Schema Changes

**Migration: 20260805_AddEmailNotificationPreferences**

```sql
-- Add email notification preference to Employee table
ALTER TABLE Employee ADD EmailNotificationsEnabled BIT NOT NULL DEFAULT 1;

-- Add index for notification queries
CREATE INDEX IX_Employee_EmailNotificationsEnabled 
ON Employee(EmailNotificationsEnabled) 
WHERE EmailNotificationsEnabled = 1;
```

### Email Service Implementation

**IEmailService.cs:**
```csharp
public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string htmlBody, string textBody);
    Task SendWorkOrderAssignedAsync(WorkOrder workOrder, Employee assignee);
    Task SendWorkOrderCompletedAsync(WorkOrder workOrder, Employee creator);
    Task SendWorkOrderStatusChangedAsync(WorkOrder workOrder, WorkOrderStatus oldStatus, WorkOrderStatus newStatus);
}
```

**SmtpEmailService.cs:**
```csharp
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
            await client.AuthenticateAsync(_settings.SmtpUsername, _settings.SmtpPassword);
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
            return;
            
        var subject = $"Work Order {workOrder.Number} Assigned to You";
        var htmlBody = RenderTemplate("WorkOrderAssigned", workOrder, assignee);
        var textBody = RenderTextTemplate("WorkOrderAssigned", workOrder, assignee);
        
        await SendEmailAsync(assignee.Email, subject, htmlBody, textBody);
    }
    
    public async Task SendWorkOrderCompletedAsync(WorkOrder workOrder, Employee creator)
    {
        if (!creator.EmailNotificationsEnabled || string.IsNullOrEmpty(creator.Email))
            return;
            
        var subject = $"Work Order {workOrder.Number} Completed";
        var htmlBody = RenderTemplate("WorkOrderCompleted", workOrder, creator);
        var textBody = RenderTextTemplate("WorkOrderCompleted", workOrder, creator);
        
        await SendEmailAsync(creator.Email, subject, htmlBody, textBody);
    }
}
```

**EmailSettings.cs:**
```csharp
public class EmailSettings
{
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string SmtpUsername { get; set; } = "";
    public string SmtpPassword { get; set; } = "";
    public string FromAddress { get; set; } = "";
    public string FromName { get; set; } = "Church Bulletin Work Orders";
    public string BaseUrl { get; set; } = "";
}
```

### Event Handler Implementation

**EmailNotificationHandler.cs:**
```csharp
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
```

### Configuration

**appsettings.json:**
```json
{
  "EmailSettings": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "UseSsl": true,
    "SmtpUsername": "",
    "SmtpPassword": "",
    "FromAddress": "noreply@churchbulletin.org",
    "FromName": "Church Bulletin Work Orders",
    "BaseUrl": "https://workorders.churchbulletin.org"
  }
}
```

**Program.cs:**
```csharp
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<INotificationHandler<WorkOrderAssignedEvent>, EmailNotificationHandler>();
builder.Services.AddScoped<INotificationHandler<WorkOrderCompletedEvent>, EmailNotificationHandler>();
```

---

## Test Design Phase

### Unit Tests

**EmailService Tests:**
```csharp
[TestFixture]
public class SmtpEmailServiceTests
{
    [Test]
    public async Task SendWorkOrderAssignedAsync_ShouldNotSendIfNotificationsDisabled()
    {
        // Arrange
        var emailService = new SmtpEmailService(Mock.Of<ILogger<SmtpEmailService>>(), Options.Create(new EmailSettings()));
        var workOrder = new WorkOrder { Number = "WO-123" };
        var assignee = new Employee { EmailNotificationsEnabled = false, Email = "test@example.com" };
        
        // Act
        await emailService.SendWorkOrderAssignedAsync(workOrder, assignee);
        
        // Assert
        // No email should be sent (verify via mock SMTP client)
    }
    
    [Test]
    public async Task SendWorkOrderCompletedAsync_ShouldSendIfNotificationsEnabled()
    {
        // Arrange
        var emailService = new SmtpEmailService(Mock.Of<ILogger<SmtpEmailService>>(), Options.Create(new EmailSettings()));
        var workOrder = new WorkOrder { Number = "WO-123", Title = "Fix Door" };
        var creator = new Employee { EmailNotificationsEnabled = true, Email = "creator@example.com" };
        
        // Act
        await emailService.SendWorkOrderCompletedAsync(workOrder, creator);
        
        // Assert
        // Verify email sent with correct subject and content
    }
}
```

### Integration Tests

**EmailNotificationHandler Tests:**
```csharp
[TestFixture]
public class EmailNotificationHandlerTests : IntegrationTestBase
{
    [Test]
    public async Task Handle_WorkOrderAssignedEvent_ShouldSendEmail()
    {
        // Arrange
        var assignee = new Employee 
        { 
            UserName = "tech1", 
            Email = "tech1@example.com",
            EmailNotificationsEnabled = true 
        };
        var workOrder = new WorkOrder 
        { 
            Number = "WO-123", 
            Assignee = assignee 
        };
        await SaveAsync(assignee, workOrder);
        
        var handler = new EmailNotificationHandler(
            ServiceProvider.GetRequiredService<IEmailService>(),
            Mock.Of<ILogger<EmailNotificationHandler>>());
        
        // Act
        await handler.Handle(new WorkOrderAssignedEvent(workOrder), CancellationToken.None);
        
        // Assert
        // Verify email was sent (check email service mock or test inbox)
    }
}
```

### Acceptance Tests

**Feature: Email Notifications**
```gherkin
Feature: Email Notifications for Work Order Status Changes
  As a user
  I want to receive email notifications for work order updates
  So that I stay informed without manually checking the system

Scenario: Assignee receives email when work order assigned
  Given I am a maintenance technician with email notifications enabled
  When a work order is assigned to me
  Then I should receive an email with the work order details
  And the email should include a link to view the work order

Scenario: Creator receives email when work order completed
  Given I created a work order
  And I have email notifications enabled
  When the work order is marked as complete
  Then I should receive a completion notification email
  And the email should show who completed it and when

Scenario: User can disable email notifications
  Given I am logged in
  When I navigate to settings
  And I disable email notifications
  And a work order is assigned to me
  Then I should not receive an email notification

Scenario: Email not sent if user has no email address
  Given I am a user without an email address configured
  When a work order is assigned to me
  Then no email should be sent
  And the work order assignment should still succeed
```

---

## Implementation Checklist

### Phase 1: Infrastructure (#8276)
- [ ] Create IEmailService interface
- [ ] Implement SmtpEmailService with MailKit
- [ ] Add EmailSettings configuration class
- [ ] Create email templates (HTML and text)
- [ ] Configure dependency injection
- [ ] Write unit tests for email service
- [ ] Commit: "Configure email notification infrastructure"

### Phase 2: Status Change Notifications (#8277)
- [ ] Create WorkOrderAssignedEvent
- [ ] Create WorkOrderCompletedEvent
- [ ] Implement EmailNotificationHandler
- [ ] Publish events from state commands
- [ ] Add email delivery logging
- [ ] Handle email failures gracefully
- [ ] Write integration tests
- [ ] Commit: "Implement status change email notifications"

### Phase 3: User Preferences (#8278)
- [ ] Add EmailNotificationsEnabled to Employee entity
- [ ] Update EmployeeMap for EF Core configuration
- [ ] Create database migration
- [ ] Add notification preferences to settings page
- [ ] Check preference before sending emails
- [ ] Write unit tests for preference checking
- [ ] Commit: "Add user email notification preferences"

### Phase 4: Testing & Documentation
- [ ] Run full test suite
- [ ] Test with real SMTP server
- [ ] Update user documentation
- [ ] Run PrivateBuild.ps1
- [ ] Create pull request

---

## Decision

**Approved for Implementation**

This design provides a robust email notification system with:
- Clean service abstraction (IEmailService)
- Event-driven architecture for notifications
- User control via preferences
- Graceful failure handling
- Comprehensive email templates

The implementation follows existing patterns and integrates seamlessly with the current architecture.

---

## Consequences

**Positive:**
- Timely awareness of work order status changes
- Reduced manual communication overhead
- User control over notification preferences
- Audit trail via email records
- Improved response times for assignments

**Negative:**
- Requires SMTP server configuration
- Email delivery depends on external service
- Potential for email spam if not configured properly
- Additional complexity in error handling

**Mitigations:**
- Comprehensive logging of email operations
- Graceful degradation (email failure doesn't block work orders)
- User preferences to control notification volume
- Rate limiting and throttling (future enhancement)

---

## References

- Epic #8236: Email Notifications for Status Changes
- Child Issues: #8276, #8277, #8278
- Related: ADR-0001, ADR-0002, ADR-0003
- MailKit Documentation: https://github.com/jstedfast/MailKit
