# ADR-0002: Recurring Maintenance Schedules

**Status:** Proposed  
**Date:** 2026-08-05  
**Epic:** #8228 - Recurring Work Orders  
**Child Issues:** #8268, #8269, #8270

---

## Conceptual Definition Phase

### Business Context

Church facilities require regular, predictable maintenance tasks that repeat on fixed schedules:
- Weekly HVAC filter checks
- Monthly fire extinguisher inspections
- Quarterly elevator maintenance
- Annual roof inspections
- Bi-weekly lawn care

Currently, staff must manually create these work orders each time, leading to:
- Forgotten maintenance tasks
- Inconsistent scheduling
- Administrative overhead
- Compliance risks for safety inspections

### Business Value

**Primary Benefits:**
1. **Automation:** System automatically generates recurring work orders on schedule
2. **Consistency:** Ensures regular maintenance tasks are never forgotten
3. **Compliance:** Maintains audit trail for required inspections
4. **Efficiency:** Reduces administrative burden of manual work order creation
5. **Planning:** Provides visibility into upcoming maintenance needs

**Success Metrics:**
- 80% reduction in manual work order creation for routine tasks
- 100% on-time completion of scheduled maintenance
- Zero missed safety inspections
- 50% reduction in emergency repairs due to preventive maintenance

### User Stories

**As a Facilities Manager:**
- I want to configure a work order to recur weekly so HVAC filters are checked every Monday
- I want to see upcoming scheduled maintenance so I can plan resource allocation
- I want recurring work orders to auto-generate so I don't forget routine tasks

**As a Maintenance Technician:**
- I want to see which work orders are part of a recurring schedule so I understand the pattern
- I want to complete recurring instances without affecting the template so the schedule continues

**As a Church Administrator:**
- I want to track compliance with recurring inspections so we meet safety regulations
- I want to modify recurrence patterns when schedules change (e.g., seasonal adjustments)

### Scope

**In Scope:**
- Recurrence patterns: Weekly, Monthly, Quarterly, Annually
- Configurable interval multipliers (e.g., every 2 weeks)
- Automatic work order generation via background service
- Parent-child relationship tracking (template → instances)
- Next scheduled date calculation and display
- UI for configuring recurrence on work orders
- Filtering/searching by recurring status

**Out of Scope (Future Enhancements):**
- Custom recurrence patterns (e.g., "first Monday of month")
- Recurrence end dates or occurrence limits
- Bulk editing of recurring work order templates
- Calendar view of scheduled maintenance
- Email notifications for upcoming recurring tasks
- Recurrence pattern validation against business hours

### Domain Model Changes

**New Enum: RecurrencePattern**
```csharp
public enum RecurrencePattern
{
    None = 0,      // One-time work order
    Weekly = 1,    // Repeats every N weeks
    Monthly = 2,   // Repeats every N months
    Quarterly = 3, // Repeats every N quarters (3 months)
    Annually = 4   // Repeats every N years
}
```

**WorkOrder Entity Extensions:**
```csharp
public class WorkOrder
{
    // Existing properties...
    
    // Recurrence properties
    public bool IsRecurring { get; set; }
    public RecurrencePattern RecurrencePattern { get; set; }
    public int RecurrenceInterval { get; set; } // Multiplier (e.g., 2 = every 2 weeks)
    public DateTime? NextScheduledDate { get; set; }
    public Guid? ParentWorkOrderId { get; set; } // Links generated instances to template
    
    // Navigation
    public virtual WorkOrder? ParentWorkOrder { get; set; }
    public virtual ICollection<WorkOrder> ChildWorkOrders { get; set; }
}
```

### Business Rules

1. **Recurrence Configuration:**
   - Only Draft or Assigned work orders can be marked as recurring
   - RecurrenceInterval must be >= 1
   - NextScheduledDate is required when IsRecurring = true
   - RecurrencePattern cannot be None when IsRecurring = true

2. **Work Order Generation:**
   - Background service runs hourly to check for due recurring work orders
   - New instances created when NextScheduledDate <= CurrentDateTime
   - Generated work orders start in Draft status
   - Parent work order's NextScheduledDate is updated after generation

3. **Field Inheritance:**
   - Generated instances copy: Title, Description, Priority, AssignedTo
   - Generated instances get new: Number, CreatedDate, Status (Draft)
   - Generated instances reference: ParentWorkOrderId

4. **Lifecycle Management:**
   - Completing a recurring instance does NOT affect the template
   - Deleting a template stops future generation but preserves existing instances
   - Modifying a template does NOT retroactively change existing instances

### Architecture Diagram

**Before State:**
```
┌─────────────────────────────────────────────────────────────┐
│ Current System: Manual Work Order Creation                  │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  User → WorkOrderManage.razor → SaveDraftCommand            │
│                                                              │
│  Every week: User manually creates "HVAC Filter Check"      │
│  Every month: User manually creates "Fire Extinguisher"     │
│                                                              │
│  Problems:                                                   │
│  - Forgotten tasks                                           │
│  - Inconsistent timing                                       │
│  - Administrative overhead                                   │
└─────────────────────────────────────────────────────────────┘
```

**After State:**
```
┌─────────────────────────────────────────────────────────────┐
│ New System: Automated Recurring Work Orders                 │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  One-time Setup:                                             │
│  User → WorkOrderManage.razor → Configure Recurrence        │
│         ↓                                                    │
│    SaveDraftCommand (IsRecurring=true, Pattern=Weekly)      │
│         ↓                                                    │
│    WorkOrder Template (NextScheduledDate calculated)         │
│                                                              │
│  Automated Generation:                                       │
│  RecurringWorkOrderService (IHostedService)                 │
│         ↓ (runs hourly)                                      │
│    RecurringWorkOrdersQuery → Find due templates            │
│         ↓                                                    │
│    SaveDraftCommand → Create new instance                   │
│         ↓                                                    │
│    Update NextScheduledDate on template                     │
│                                                              │
│  Benefits:                                                   │
│  - Zero manual intervention                                  │
│  - Consistent scheduling                                     │
│  - Audit trail via ParentWorkOrderId                        │
└─────────────────────────────────────────────────────────────┘
```

---

## UX Design Phase

### Visual Design System

**Recurrence Indicator Colors:**
- 🟢 **Green (#10b981):** Active recurring work order (template)
- 🔵 **Blue (#3b82f6):** Generated instance (child of recurring template)
- ⚪ **Gray (#6b7280):** One-time work order (not recurring)

**Component Hierarchy:**
```
WorkOrderManage.razor
├── RecurrenceSelector.razor (configuration component)
│   ├── Pattern Dropdown (None, Weekly, Monthly, Quarterly, Annually)
│   ├── Interval Input (numeric, min=1, visible when pattern != None)
│   └── Next Scheduled Display (read-only, auto-calculated)
└── Form Actions (Save, Cancel)

WorkOrderSearch.razor
├── Search Filters
│   └── IsRecurring Filter (All, Recurring Only, One-Time Only)
├── Results Table
│   ├── RecurrenceBadge.razor (status indicator)
│   └── Next Scheduled Column (for recurring templates)
└── Pagination
```

### UI Components Specification

**RecurrenceSelector.razor:**
```razor
<div class="recurrence-selector">
    <label>Recurrence Pattern</label>
    <select @bind="Pattern" class="form-select">
        <option value="0">One-Time (No Recurrence)</option>
        <option value="1">Weekly</option>
        <option value="2">Monthly</option>
        <option value="3">Quarterly</option>
        <option value="4">Annually</option>
    </select>
    
    @if (Pattern != RecurrencePattern.None)
    {
        <label>Repeat Every</label>
        <div class="input-group">
            <input type="number" @bind="Interval" min="1" class="form-control" />
            <span class="input-group-text">@GetIntervalLabel()</span>
        </div>
        
        <div class="next-scheduled">
            <label>Next Scheduled</label>
            <input type="text" value="@NextScheduledDate?.ToString("MMM dd, yyyy")" 
                   class="form-control" readonly />
        </div>
    }
</div>
```

**RecurrenceBadge.razor:**
```razor
<span class="badge recurrence-badge @GetBadgeClass()">
    @if (IsRecurring)
    {
        <i class="bi bi-arrow-repeat"></i>
        <text>@GetRecurrenceText()</text>
    }
    else if (HasParent)
    {
        <i class="bi bi-link-45deg"></i>
        <text>Instance</text>
    }
    else
    {
        <i class="bi bi-dash-circle"></i>
        <text>One-Time</text>
    }
</span>
```

**RecurrenceBadge.razor.css:**
```css
.recurrence-badge {
    display: inline-flex;
    align-items: center;
    gap: 0.25rem;
    padding: 0.25rem 0.75rem;
    font-size: 0.875rem;
    font-weight: 500;
    border-radius: 9999px;
}

.recurrence-badge.recurring {
    background-color: #d1fae5;
    color: #065f46;
}

.recurrence-badge.instance {
    background-color: #dbeafe;
    color: #1e40af;
}

.recurrence-badge.one-time {
    background-color: #f3f4f6;
    color: #4b5563;
}
```

### User Workflows

**Workflow 1: Create Recurring Work Order**
1. User navigates to "Create Work Order"
2. User fills in Title, Description, Priority, AssignedTo
3. User selects RecurrencePattern = "Weekly"
4. User sets Interval = 1 (every 1 week)
5. System auto-calculates NextScheduledDate = Today + 1 week
6. User clicks "Save"
7. System creates recurring template work order
8. System displays success: "Recurring work order created. Next instance: [date]"

**Workflow 2: View Recurring Work Orders**
1. User navigates to "Search Work Orders"
2. User selects filter: "Recurring Only"
3. System displays work orders where IsRecurring = true
4. Each row shows RecurrenceBadge (green) and NextScheduledDate
5. User can click to edit template or view generated instances

**Workflow 3: Complete Recurring Instance**
1. Background service generates new work order instance
2. Technician sees new work order in Draft status
3. Technician assigns to self and moves to In Progress
4. Technician completes work and marks Complete
5. Template remains active, NextScheduledDate updates
6. Next instance will be generated on schedule

### Accessibility Requirements

- **Keyboard Navigation:** All recurrence controls accessible via Tab/Shift+Tab
- **Screen Readers:** ARIA labels for pattern dropdown and interval input
- **Color Independence:** Recurrence status indicated by icon + text, not just color
- **Focus Indicators:** Clear focus rings on all interactive elements
- **Error Messages:** Clear validation messages for invalid intervals

---

## Technical Design Phase

### Database Schema Changes

**Migration: 20260805_AddRecurringWorkOrders**

```sql
-- Add recurrence columns to WorkOrder table
ALTER TABLE WorkOrder ADD IsRecurring BIT NOT NULL DEFAULT 0;
ALTER TABLE WorkOrder ADD RecurrencePattern INT NOT NULL DEFAULT 0;
ALTER TABLE WorkOrder ADD RecurrenceInterval INT NOT NULL DEFAULT 1;
ALTER TABLE WorkOrder ADD NextScheduledDate DATETIME2 NULL;
ALTER TABLE WorkOrder ADD ParentWorkOrderId UNIQUEIDENTIFIER NULL;

-- Add foreign key for parent-child relationship
ALTER TABLE WorkOrder 
ADD CONSTRAINT FK_WorkOrder_ParentWorkOrder 
FOREIGN KEY (ParentWorkOrderId) REFERENCES WorkOrder(Id);

-- Add index for recurring work order queries
CREATE INDEX IX_WorkOrder_Recurring 
ON WorkOrder(IsRecurring, NextScheduledDate) 
WHERE IsRecurring = 1;

-- Add index for parent-child lookups
CREATE INDEX IX_WorkOrder_ParentId 
ON WorkOrder(ParentWorkOrderId) 
WHERE ParentWorkOrderId IS NOT NULL;

-- Add check constraint for recurrence interval
ALTER TABLE WorkOrder 
ADD CONSTRAINT CK_WorkOrder_RecurrenceInterval 
CHECK (RecurrenceInterval >= 1);
```

**EF Core Configuration (WorkOrderMap.cs):**
```csharp
public class WorkOrderMap : IEntityTypeConfiguration<WorkOrder>
{
    public void Configure(EntityTypeBuilder<WorkOrder> builder)
    {
        // Existing configuration...
        
        // Recurrence properties
        builder.Property(x => x.IsRecurring)
            .IsRequired()
            .HasDefaultValue(false);
            
        builder.Property(x => x.RecurrencePattern)
            .IsRequired()
            .HasDefaultValue(RecurrencePattern.None);
            
        builder.Property(x => x.RecurrenceInterval)
            .IsRequired()
            .HasDefaultValue(1);
            
        builder.Property(x => x.NextScheduledDate)
            .IsRequired(false);
            
        builder.Property(x => x.ParentWorkOrderId)
            .IsRequired(false);
            
        // Self-referencing relationship
        builder.HasOne(x => x.ParentWorkOrder)
            .WithMany(x => x.ChildWorkOrders)
            .HasForeignKey(x => x.ParentWorkOrderId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // Indexes
        builder.HasIndex(x => new { x.IsRecurring, x.NextScheduledDate })
            .HasFilter("IsRecurring = 1");
            
        builder.HasIndex(x => x.ParentWorkOrderId)
            .HasFilter("ParentWorkOrderId IS NOT NULL");
    }
}
```

### API Changes

**New Query: RecurringWorkOrdersQuery**
```csharp
public class RecurringWorkOrdersQuery : IRequest<RecurringWorkOrdersQueryResult>
{
    public DateTime AsOfDate { get; set; } = DateTime.UtcNow;
}

public class RecurringWorkOrdersQueryResult
{
    public List<WorkOrderDto> DueWorkOrders { get; set; }
}

public class RecurringWorkOrdersQueryHandler : 
    IRequestHandler<RecurringWorkOrdersQuery, RecurringWorkOrdersQueryResult>
{
    public async Task<RecurringWorkOrdersQueryResult> Handle(
        RecurringWorkOrdersQuery request, 
        CancellationToken cancellationToken)
    {
        var dueWorkOrders = await _context.WorkOrders
            .Where(x => x.IsRecurring 
                && x.NextScheduledDate.HasValue 
                && x.NextScheduledDate.Value <= request.AsOfDate)
            .ToListAsync(cancellationToken);
            
        return new RecurringWorkOrdersQueryResult 
        { 
            DueWorkOrders = _mapper.Map<List<WorkOrderDto>>(dueWorkOrders) 
        };
    }
}
```

**Modified Command: SaveDraftCommand**
```csharp
public class SaveDraftCommand : IRequest<SaveDraftCommandResult>
{
    // Existing properties...
    
    // New recurrence properties
    public bool IsRecurring { get; set; }
    public RecurrencePattern RecurrencePattern { get; set; }
    public int RecurrenceInterval { get; set; }
    public DateTime? NextScheduledDate { get; set; }
    public Guid? ParentWorkOrderId { get; set; }
}
```

### Background Service Implementation

**RecurringWorkOrderService.cs (Worker project):**
```csharp
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
        
        // Run every hour
        _timer = new Timer(
            DoWork, 
            null, 
            TimeSpan.Zero, 
            TimeSpan.FromHours(1));
            
        return Task.CompletedTask;
    }
    
    private async void DoWork(object? state)
    {
        _logger.LogInformation("Checking for due recurring work orders");
        
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
            
            foreach (var template in result.DueWorkOrders)
            {
                // Generate new instance
                var command = new SaveDraftCommand
                {
                    Title = template.Title,
                    Description = template.Description,
                    Priority = template.Priority,
                    AssignedTo = template.AssignedTo,
                    ParentWorkOrderId = template.Id,
                    IsRecurring = false // Instances are not recurring
                };
                
                var newWorkOrder = await bus.Send(command);
                
                _logger.LogInformation(
                    "Generated work order {Number} from recurring template {TemplateNumber}",
                    newWorkOrder.Number,
                    template.Number);
                
                // Update template's next scheduled date
                var nextDate = CalculateNextScheduledDate(
                    template.RecurrencePattern,
                    template.RecurrenceInterval,
                    template.NextScheduledDate.Value);
                    
                var updateCommand = new UpdateRecurrenceCommand
                {
                    WorkOrderId = template.Id,
                    NextScheduledDate = nextDate
                };
                
                await bus.Send(updateCommand);
            }
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
```

**Program.cs (Worker project):**
```csharp
builder.Services.AddHostedService<RecurringWorkOrderService>();
```

### Component Implementation

**RecurrenceSelector.razor:**
```razor
@using ClearMeasure.Bootcamp.Core.Model

<div class="recurrence-selector">
    <div class="mb-3">
        <label class="form-label">Recurrence Pattern</label>
        <select @bind="Pattern" class="form-select" @bind:after="OnPatternChanged">
            <option value="@RecurrencePattern.None">One-Time (No Recurrence)</option>
            <option value="@RecurrencePattern.Weekly">Weekly</option>
            <option value="@RecurrencePattern.Monthly">Monthly</option>
            <option value="@RecurrencePattern.Quarterly">Quarterly</option>
            <option value="@RecurrencePattern.Annually">Annually</option>
        </select>
    </div>
    
    @if (Pattern != RecurrencePattern.None)
    {
        <div class="mb-3">
            <label class="form-label">Repeat Every</label>
            <div class="input-group">
                <input type="number" 
                       @bind="Interval" 
                       @bind:after="OnIntervalChanged"
                       min="1" 
                       class="form-control" 
                       aria-label="Recurrence interval" />
                <span class="input-group-text">@GetIntervalLabel()</span>
            </div>
        </div>
        
        <div class="mb-3">
            <label class="form-label">Next Scheduled</label>
            <input type="text" 
                   value="@NextScheduledDate?.ToString("MMM dd, yyyy h:mm tt")" 
                   class="form-control" 
                   readonly 
                   aria-label="Next scheduled date" />
        </div>
    }
</div>

@code {
    [Parameter] public RecurrencePattern Pattern { get; set; }
    [Parameter] public EventCallback<RecurrencePattern> PatternChanged { get; set; }
    
    [Parameter] public int Interval { get; set; } = 1;
    [Parameter] public EventCallback<int> IntervalChanged { get; set; }
    
    [Parameter] public DateTime? NextScheduledDate { get; set; }
    [Parameter] public EventCallback<DateTime?> NextScheduledDateChanged { get; set; }
    
    private async Task OnPatternChanged()
    {
        await PatternChanged.InvokeAsync(Pattern);
        await CalculateNextScheduledDate();
    }
    
    private async Task OnIntervalChanged()
    {
        await IntervalChanged.InvokeAsync(Interval);
        await CalculateNextScheduledDate();
    }
    
    private async Task CalculateNextScheduledDate()
    {
        if (Pattern == RecurrencePattern.None)
        {
            await NextScheduledDateChanged.InvokeAsync(null);
            return;
        }
        
        var baseDate = DateTime.Now;
        var nextDate = Pattern switch
        {
            RecurrencePattern.Weekly => baseDate.AddDays(7 * Interval),
            RecurrencePattern.Monthly => baseDate.AddMonths(Interval),
            RecurrencePattern.Quarterly => baseDate.AddMonths(3 * Interval),
            RecurrencePattern.Annually => baseDate.AddYears(Interval),
            _ => baseDate
        };
        
        await NextScheduledDateChanged.InvokeAsync(nextDate);
    }
    
    private string GetIntervalLabel()
    {
        return Pattern switch
        {
            RecurrencePattern.Weekly => Interval == 1 ? "week" : "weeks",
            RecurrencePattern.Monthly => Interval == 1 ? "month" : "months",
            RecurrencePattern.Quarterly => Interval == 1 ? "quarter" : "quarters",
            RecurrencePattern.Annually => Interval == 1 ? "year" : "years",
            _ => ""
        };
    }
}
```

**RecurrenceBadge.razor:**
```razor
@using ClearMeasure.Bootcamp.Core.Model

<span class="badge recurrence-badge @GetBadgeClass()" title="@GetTooltip()">
    <i class="bi @GetIcon()"></i>
    <span>@GetText()</span>
</span>

@code {
    [Parameter] public bool IsRecurring { get; set; }
    [Parameter] public RecurrencePattern Pattern { get; set; }
    [Parameter] public int Interval { get; set; }
    [Parameter] public bool HasParent { get; set; }
    
    private string GetBadgeClass()
    {
        if (IsRecurring) return "recurring";
        if (HasParent) return "instance";
        return "one-time";
    }
    
    private string GetIcon()
    {
        if (IsRecurring) return "bi-arrow-repeat";
        if (HasParent) return "bi-link-45deg";
        return "bi-dash-circle";
    }
    
    private string GetText()
    {
        if (IsRecurring)
        {
            var intervalText = Interval > 1 ? $"Every {Interval} " : "";
            return Pattern switch
            {
                RecurrencePattern.Weekly => $"{intervalText}Weekly",
                RecurrencePattern.Monthly => $"{intervalText}Monthly",
                RecurrencePattern.Quarterly => $"{intervalText}Quarterly",
                RecurrencePattern.Annually => $"{intervalText}Annually",
                _ => "Recurring"
            };
        }
        if (HasParent) return "Instance";
        return "One-Time";
    }
    
    private string GetTooltip()
    {
        if (IsRecurring) return "This is a recurring work order template";
        if (HasParent) return "This work order was generated from a recurring template";
        return "This is a one-time work order";
    }
}
```

### Search and Filtering

**WorkOrderSearchModel.cs:**
```csharp
public class WorkOrderSearchModel
{
    public class SearchFilters
    {
        // Existing filters...
        
        public bool? IsRecurring { get; set; } // null = all, true = recurring only, false = one-time only
    }
}
```

**WorkOrderSpecificationQuery.cs:**
```csharp
public class WorkOrderSpecificationQuery
{
    public static Expression<Func<WorkOrder, bool>> MatchRecurring(bool? isRecurring)
    {
        if (!isRecurring.HasValue)
            return x => true;
            
        return x => x.IsRecurring == isRecurring.Value;
    }
}
```

---

## Test Design Phase

### Unit Tests

**RecurrencePattern Enum Tests:**
```csharp
[TestFixture]
public class RecurrencePatternTests
{
    [Test]
    public void RecurrencePattern_ShouldHaveCorrectValues()
    {
        Assert.That((int)RecurrencePattern.None, Is.EqualTo(0));
        Assert.That((int)RecurrencePattern.Weekly, Is.EqualTo(1));
        Assert.That((int)RecurrencePattern.Monthly, Is.EqualTo(2));
        Assert.That((int)RecurrencePattern.Quarterly, Is.EqualTo(3));
        Assert.That((int)RecurrencePattern.Annually, Is.EqualTo(4));
    }
}
```

**WorkOrder Recurrence Property Tests:**
```csharp
[TestFixture]
public class WorkOrderRecurrenceTests
{
    [Test]
    public void WorkOrder_ShouldAllowRecurrenceConfiguration()
    {
        var workOrder = new WorkOrder
        {
            IsRecurring = true,
            RecurrencePattern = RecurrencePattern.Weekly,
            RecurrenceInterval = 2,
            NextScheduledDate = DateTime.Now.AddDays(14)
        };
        
        Assert.That(workOrder.IsRecurring, Is.True);
        Assert.That(workOrder.RecurrencePattern, Is.EqualTo(RecurrencePattern.Weekly));
        Assert.That(workOrder.RecurrenceInterval, Is.EqualTo(2));
        Assert.That(workOrder.NextScheduledDate, Is.Not.Null);
    }
    
    [Test]
    public void WorkOrder_ShouldSupportParentChildRelationship()
    {
        var parent = new WorkOrder { Id = Guid.NewGuid(), IsRecurring = true };
        var child = new WorkOrder { ParentWorkOrderId = parent.Id };
        
        Assert.That(child.ParentWorkOrderId, Is.EqualTo(parent.Id));
    }
}
```

**Next Scheduled Date Calculation Tests:**
```csharp
[TestFixture]
public class RecurrenceCalculationTests
{
    [Test]
    public void CalculateNextScheduledDate_Weekly_ShouldAddWeeks()
    {
        var baseDate = new DateTime(2026, 8, 5);
        var nextDate = CalculateNextScheduledDate(RecurrencePattern.Weekly, 1, baseDate);
        
        Assert.That(nextDate, Is.EqualTo(new DateTime(2026, 8, 12)));
    }
    
    [Test]
    public void CalculateNextScheduledDate_BiWeekly_ShouldAddTwoWeeks()
    {
        var baseDate = new DateTime(2026, 8, 5);
        var nextDate = CalculateNextScheduledDate(RecurrencePattern.Weekly, 2, baseDate);
        
        Assert.That(nextDate, Is.EqualTo(new DateTime(2026, 8, 19)));
    }
    
    [Test]
    public void CalculateNextScheduledDate_Monthly_ShouldAddMonths()
    {
        var baseDate = new DateTime(2026, 8, 5);
        var nextDate = CalculateNextScheduledDate(RecurrencePattern.Monthly, 1, baseDate);
        
        Assert.That(nextDate, Is.EqualTo(new DateTime(2026, 9, 5)));
    }
    
    [Test]
    public void CalculateNextScheduledDate_Quarterly_ShouldAddThreeMonths()
    {
        var baseDate = new DateTime(2026, 8, 5);
        var nextDate = CalculateNextScheduledDate(RecurrencePattern.Quarterly, 1, baseDate);
        
        Assert.That(nextDate, Is.EqualTo(new DateTime(2026, 11, 5)));
    }
    
    [Test]
    public void CalculateNextScheduledDate_Annually_ShouldAddYears()
    {
        var baseDate = new DateTime(2026, 8, 5);
        var nextDate = CalculateNextScheduledDate(RecurrencePattern.Annually, 1, baseDate);
        
        Assert.That(nextDate, Is.EqualTo(new DateTime(2027, 8, 5)));
    }
}
```

### Integration Tests

**RecurringWorkOrdersQuery Tests:**
```csharp
[TestFixture]
public class RecurringWorkOrdersQueryHandlerTests : IntegrationTestBase
{
    [Test]
    public async Task Handle_ShouldReturnDueRecurringWorkOrders()
    {
        // Arrange
        var dueWorkOrder = new WorkOrder
        {
            IsRecurring = true,
            RecurrencePattern = RecurrencePattern.Weekly,
            NextScheduledDate = DateTime.UtcNow.AddDays(-1) // Due yesterday
        };
        
        var notDueWorkOrder = new WorkOrder
        {
            IsRecurring = true,
            RecurrencePattern = RecurrencePattern.Monthly,
            NextScheduledDate = DateTime.UtcNow.AddDays(7) // Due next week
        };
        
        await SaveAsync(dueWorkOrder, notDueWorkOrder);
        
        // Act
        var query = new RecurringWorkOrdersQuery();
        var result = await Bus.Send(query);
        
        // Assert
        Assert.That(result.DueWorkOrders, Has.Count.EqualTo(1));
        Assert.That(result.DueWorkOrders[0].Id, Is.EqualTo(dueWorkOrder.Id));
    }
    
    [Test]
    public async Task Handle_ShouldNotReturnNonRecurringWorkOrders()
    {
        // Arrange
        var oneTimeWorkOrder = new WorkOrder
        {
            IsRecurring = false,
            NextScheduledDate = DateTime.UtcNow.AddDays(-1)
        };
        
        await SaveAsync(oneTimeWorkOrder);
        
        // Act
        var query = new RecurringWorkOrdersQuery();
        var result = await Bus.Send(query);
        
        // Assert
        Assert.That(result.DueWorkOrders, Is.Empty);
    }
}
```

**SaveDraftCommand with Recurrence Tests:**
```csharp
[TestFixture]
public class SaveDraftCommandRecurrenceTests : IntegrationTestBase
{
    [Test]
    public async Task Handle_ShouldSaveRecurringWorkOrder()
    {
        // Arrange
        var command = new SaveDraftCommand
        {
            Title = "Weekly HVAC Check",
            Description = "Check HVAC filters",
            IsRecurring = true,
            RecurrencePattern = RecurrencePattern.Weekly,
            RecurrenceInterval = 1,
            NextScheduledDate = DateTime.UtcNow.AddDays(7)
        };
        
        // Act
        var result = await Bus.Send(command);
        
        // Assert
        var workOrder = await LoadAsync<WorkOrder>(result.Id);
        Assert.That(workOrder.IsRecurring, Is.True);
        Assert.That(workOrder.RecurrencePattern, Is.EqualTo(RecurrencePattern.Weekly));
        Assert.That(workOrder.RecurrenceInterval, Is.EqualTo(1));
        Assert.That(workOrder.NextScheduledDate, Is.Not.Null);
    }
    
    [Test]
    public async Task Handle_ShouldSaveGeneratedInstance()
    {
        // Arrange
        var parent = new WorkOrder
        {
            Id = Guid.NewGuid(),
            IsRecurring = true,
            Number = "WO-100"
        };
        await SaveAsync(parent);
        
        var command = new SaveDraftCommand
        {
            Title = "Generated HVAC Check",
            ParentWorkOrderId = parent.Id,
            IsRecurring = false
        };
        
        // Act
        var result = await Bus.Send(command);
        
        // Assert
        var workOrder = await LoadAsync<WorkOrder>(result.Id);
        Assert.That(workOrder.ParentWorkOrderId, Is.EqualTo(parent.Id));
        Assert.That(workOrder.IsRecurring, Is.False);
    }
}
```

**RecurringWorkOrderService Tests:**
```csharp
[TestFixture]
public class RecurringWorkOrderServiceTests : IntegrationTestBase
{
    [Test]
    public async Task DoWork_ShouldGenerateNewInstances()
    {
        // Arrange
        var template = new WorkOrder
        {
            Title = "Weekly HVAC Check",
            IsRecurring = true,
            RecurrencePattern = RecurrencePattern.Weekly,
            RecurrenceInterval = 1,
            NextScheduledDate = DateTime.UtcNow.AddDays(-1),
            Status = WorkOrderStatus.Draft
        };
        await SaveAsync(template);
        
        var service = new RecurringWorkOrderService(
            Mock.Of<ILogger<RecurringWorkOrderService>>(),
            ServiceProvider);
        
        // Act
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(2000); // Wait for timer to fire
        
        // Assert
        var instances = await Query<WorkOrder>()
            .Where(x => x.ParentWorkOrderId == template.Id)
            .ToListAsync();
            
        Assert.That(instances, Has.Count.GreaterThan(0));
        Assert.That(instances[0].Title, Is.EqualTo(template.Title));
        Assert.That(instances[0].IsRecurring, Is.False);
    }
}
```

### Acceptance Tests

**Feature: Recurring Work Orders**
```gherkin
Feature: Recurring Work Orders
  As a Facilities Manager
  I want to configure work orders to recur automatically
  So that routine maintenance tasks are never forgotten

Scenario: Create weekly recurring work order
  Given I am logged in as a Facilities Manager
  When I navigate to "Create Work Order"
  And I enter title "HVAC Filter Check"
  And I select recurrence pattern "Weekly"
  And I set interval to "1"
  And I click "Save"
  Then I should see "Work order created successfully"
  And the work order should be marked as recurring
  And the next scheduled date should be 7 days from now

Scenario: Background service generates recurring instances
  Given a recurring work order exists with pattern "Weekly"
  And the next scheduled date is yesterday
  When the background service runs
  Then a new work order instance should be created
  And the instance should have the same title as the template
  And the instance should reference the template as parent
  And the template's next scheduled date should be updated to next week

Scenario: Filter recurring work orders in search
  Given I am logged in as a Facilities Manager
  And there are 5 recurring work orders
  And there are 10 one-time work orders
  When I navigate to "Search Work Orders"
  And I select filter "Recurring Only"
  Then I should see 5 work orders
  And each work order should display a recurring badge

Scenario: Complete recurring instance without affecting template
  Given a recurring work order template exists
  And an instance has been generated from the template
  When I complete the instance
  Then the instance status should be "Complete"
  And the template should remain in "Draft" status
  And the template should still be marked as recurring
```

**Test Implementation:**
```csharp
[TestFixture]
public class RecurringWorkOrdersAcceptanceTests : AcceptanceTestBase
{
    [Test]
    public async Task CreateWeeklyRecurringWorkOrder()
    {
        // Given
        await LoginAsAsync("facilities.manager@church.org");
        
        // When
        await NavigateToAsync("/workorders/create");
        await EnterTextAsync("#title", "HVAC Filter Check");
        await SelectAsync("#recurrence-pattern", "Weekly");
        await EnterTextAsync("#recurrence-interval", "1");
        await ClickAsync("#save-button");
        
        // Then
        await AssertTextAsync(".alert-success", "Work order created successfully");
        
        var workOrder = await GetLastCreatedWorkOrderAsync();
        Assert.That(workOrder.IsRecurring, Is.True);
        Assert.That(workOrder.RecurrencePattern, Is.EqualTo(RecurrencePattern.Weekly));
        Assert.That(workOrder.NextScheduledDate, Is.EqualTo(DateTime.UtcNow.AddDays(7)).Within(TimeSpan.FromMinutes(1)));
    }
    
    [Test]
    public async Task BackgroundServiceGeneratesRecurringInstances()
    {
        // Given
        var template = await CreateRecurringWorkOrderAsync(
            pattern: RecurrencePattern.Weekly,
            nextScheduledDate: DateTime.UtcNow.AddDays(-1));
        
        // When
        await TriggerBackgroundServiceAsync();
        
        // Then
        var instances = await GetWorkOrderInstancesAsync(template.Id);
        Assert.That(instances, Has.Count.EqualTo(1));
        Assert.That(instances[0].Title, Is.EqualTo(template.Title));
        Assert.That(instances[0].ParentWorkOrderId, Is.EqualTo(template.Id));
        
        var updatedTemplate = await LoadAsync<WorkOrder>(template.Id);
        Assert.That(updatedTemplate.NextScheduledDate, Is.GreaterThan(DateTime.UtcNow));
    }
}
```

---

## Implementation Checklist

### Phase 1: Domain Model (#8268)
- [ ] Create RecurrencePattern enum
- [ ] Add recurrence properties to WorkOrder entity
- [ ] Update WorkOrderMap for EF Core configuration
- [ ] Create database migration
- [ ] Write unit tests for recurrence properties
- [ ] Commit: "Add RecurrencePattern enum and WorkOrder recurrence fields"

### Phase 2: UI Components (#8269)
- [ ] Create RecurrenceSelector.razor component
- [ ] Create RecurrenceBadge.razor component
- [ ] Add RecurrenceBadge.razor.css styling
- [ ] Integrate RecurrenceSelector into WorkOrderManage.razor
- [ ] Add recurrence fields to WorkOrderManageModel
- [ ] Display RecurrenceBadge in WorkOrderSearch results
- [ ] Add IsRecurring filter to search page
- [ ] Update WorkOrderSearchModel with recurrence filter
- [ ] Write component unit tests
- [ ] Commit: "Add recurrence UI components and form integration"

### Phase 3: Background Service (#8270)
- [ ] Create RecurringWorkOrdersQuery and handler
- [ ] Create RecurringWorkOrderService (IHostedService)
- [ ] Implement next scheduled date calculation logic
- [ ] Register service in Worker Program.cs
- [ ] Update SaveDraftCommand to support ParentWorkOrderId
- [ ] Add logging for generated work orders
- [ ] Write integration tests for query handler
- [ ] Write integration tests for background service
- [ ] Commit: "Implement background job for recurring work order generation"

### Phase 4: Testing & Documentation
- [ ] Run full test suite (unit + integration + acceptance)
- [ ] Update API documentation
- [ ] Create user guide for recurring work orders
- [ ] Run PrivateBuild.ps1 to verify all tests pass
- [ ] Create pull request with ADR and implementation

---

## Decision

**Approved for Implementation**

This design provides a robust foundation for recurring maintenance schedules with:
- Clear domain model with RecurrencePattern enum
- Intuitive UI components for configuration
- Automated background service for work order generation
- Parent-child relationship tracking for audit trail
- Comprehensive test coverage

The implementation follows existing patterns in the codebase and integrates seamlessly with the current architecture.

---

## Consequences

**Positive:**
- Eliminates manual work order creation for routine tasks
- Ensures consistent maintenance scheduling
- Provides audit trail via parent-child relationships
- Reduces administrative overhead
- Improves compliance with safety inspection requirements

**Negative:**
- Adds complexity to WorkOrder entity
- Requires background service monitoring
- Database migration required for existing deployments
- Potential for orphaned instances if template is deleted

**Mitigations:**
- Comprehensive logging in background service
- Database constraints prevent invalid recurrence configurations
- Soft delete on templates to preserve instance relationships
- Monitoring alerts for background service failures

---

## References

- Epic #8228: Recurring Work Orders
- Child Issues: #8268, #8269, #8270
- Related: ADR-0001 (Priority Levels)
- Architecture: arch/arch-c4-component-project-dependencies.puml
