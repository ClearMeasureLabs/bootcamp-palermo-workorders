# ADR 0001: Work Order Priority Levels

**Status:** Proposed  
**Date:** 2026-08-05  
**Epic:** #8227

## Context

The church maintenance staff currently manages work orders without a formal priority system. All work orders are treated equally, making it difficult to triage urgent issues (broken HVAC, safety hazards) versus routine maintenance tasks. Staff must manually track which items need immediate attention, leading to potential delays in addressing critical issues.

## Decision

We will add a priority classification system to work orders with four levels:
- **Urgent**: Safety hazards, critical system failures requiring immediate attention
- **High**: Important issues affecting operations but not immediately critical
- **Normal**: Standard maintenance and routine repairs (default)
- **Low**: Nice-to-have improvements and non-urgent tasks

## Conceptual Definition

### Business Value
Church staff can prioritize urgent repairs (broken HVAC, safety hazards) over routine maintenance, ensuring critical issues are addressed first. This improves response times for critical issues and helps staff allocate resources effectively.

### User Stories
1. As a maintenance coordinator, I want to mark work orders as Urgent so that staff know which issues require immediate attention
2. As a maintenance worker, I want to see work orders sorted by priority so I can address the most critical issues first
3. As a facilities manager, I want to filter work orders by priority level so I can review all urgent items at once

### Conceptual Model
```
WorkOrder
├── Priority (enum: Urgent, High, Normal, Low)
├── [existing fields...]
└── Visual indicators (colors/icons) for each priority level
```

### Priority Level Definitions

**Urgent (Red)**
- Safety hazards (exposed wiring, gas leaks, structural damage)
- Critical system failures (HVAC in extreme weather, no water, power outage)
- Security issues (broken locks, alarm system failures)
- Response time: Same day

**High (Orange)**
- Important operational issues (broken sound system before service, leaking roof)
- Equipment failures affecting ministry activities
- Issues affecting multiple areas or people
- Response time: Within 2-3 days

**Normal (Blue)**
- Standard maintenance requests
- Routine repairs
- Cosmetic issues
- Response time: Within 1-2 weeks

**Low (Gray)**
- Nice-to-have improvements
- Non-urgent cosmetic updates
- Future enhancements
- Response time: As time permits

### UI/UX Considerations
- Priority dropdown on create/edit forms with clear descriptions
- Color-coded priority badges on work order cards
- Default priority: Normal (to avoid forcing users to make a choice)
- Sort work orders by priority (Urgent → High → Normal → Low)
- Filter capability to show only specific priority levels

## Architecture Diagrams

### Before: Current Work Order Model
![Before](0001-before.png)

### After: Work Order Model with Priority
![After](0001-after.png)

## Consequences

### Positive
- Clear prioritization helps staff focus on critical issues first
- Improved response times for urgent matters
- Better resource allocation and scheduling
- Visual indicators make priority immediately obvious
- Filtering and sorting capabilities improve workflow efficiency

### Negative
- Requires training staff on when to use each priority level
- Risk of "priority inflation" (marking everything as Urgent)
- Additional field to maintain in the database
- Existing work orders will need default priority assigned

### Mitigation
- Provide clear guidelines and examples for each priority level
- Monitor priority usage patterns and provide feedback to staff
- Consider requiring justification for Urgent priority
- Migration script will set all existing work orders to Normal priority

## Implementation Notes

This epic decomposes into three child issues:
1. Add WorkOrderPriority enum to Core model (#8264)
2. Add priority field to work order UI forms (#8265)
3. Add priority sorting and filtering to work order list (#8266)

## UX Design

### Visual Design System

**Priority Badges**
- Urgent: Red badge with exclamation icon (⚠️)
- High: Orange badge with up arrow (↑)
- Normal: Blue badge with equals sign (=)
- Low: Gray badge with down arrow (↓)

**Color Palette**
```
Urgent:  #DC2626 (red-600)
High:    #EA580C (orange-600)
Normal:  #2563EB (blue-600)
Low:     #6B7280 (gray-500)
```

### UI Components

**Priority Dropdown (Create/Edit Forms)**
```
┌─────────────────────────────────┐
│ Priority *                      │
│ ┌─────────────────────────────┐ │
│ │ ⚠️  Urgent                   ▼│ │
│ └─────────────────────────────┘ │
│                                 │
│ Options:                        │
│ ⚠️  Urgent - Safety hazards     │
│ ↑  High - Important issues      │
│ =  Normal - Standard (default)  │
│ ↓  Low - Nice-to-have          │
└─────────────────────────────────┘
```

**Work Order Card with Priority**
```
┌────────────────────────────────────┐
│ ⚠️ WO-123: Broken HVAC System     │
│ Status: Assigned  Priority: Urgent│
│ Assignee: John Smith              │
│ Created: 2026-08-05               │
└────────────────────────────────────┘
```

**Work Order List with Priority Column**
```
┌──────────┬────────────────┬──────────┬──────────┐
│ Priority │ Number         │ Title    │ Status   │
├──────────┼────────────────┼──────────┼──────────┤
│ ⚠️ Urgent│ WO-123        │ HVAC...  │ Assigned │
│ ⚠️ Urgent│ WO-125        │ Gas...   │ Draft    │
│ ↑ High   │ WO-124        │ Roof...  │ Progress │
│ = Normal │ WO-122        │ Paint... │ Draft    │
└──────────┴────────────────┴──────────┴──────────┘
```

### User Interactions

**Priority Selection Flow**
1. User opens create/edit work order form
2. Priority dropdown defaults to "Normal"
3. User clicks dropdown to see all options with descriptions
4. User selects appropriate priority level
5. Visual badge updates immediately to show selected priority
6. Form validation ensures priority is set before save

**Filtering & Sorting**
- Filter dropdown above work order list: "All Priorities", "Urgent", "High", "Normal", "Low"
- Default sort: Priority (Urgent → High → Normal → Low), then by Created Date (newest first)
- Click priority column header to toggle sort direction
- Active filters shown as removable chips

### Accessibility Considerations
- Color is not the only indicator (icons + text labels)
- ARIA labels for screen readers: "Priority: Urgent - requires immediate attention"
- Keyboard navigation support for priority dropdown
- High contrast mode support
- Focus indicators on interactive elements

### Mobile Responsiveness
- Priority badges scale appropriately on small screens
- Dropdown touch targets minimum 44x44px
- Priority column visible on mobile (essential information)
- Swipe gestures for filtering on mobile

## Technical Design

### Database Schema Changes

**New Enum Type**
```csharp
public enum WorkOrderPriority
{
    Urgent = 0,   // Highest priority
    High = 1,
    Normal = 2,   // Default
    Low = 3       // Lowest priority
}
```

**WorkOrder Entity Update**
```csharp
public class WorkOrder : Entity
{
    // Existing properties...
    public Guid Id { get; set; }
    public string Number { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Instructions { get; set; }
    public string RoomNumber { get; set; }
    public WorkOrderStatus Status { get; set; }
    
    // NEW PROPERTY
    public WorkOrderPriority Priority { get; set; } = WorkOrderPriority.Normal;
    
    // Existing properties...
    public Employee Creator { get; set; }
    public Employee Assignee { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? AssignedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
}
```

**EF Core Migration**
```sql
-- Add Priority column with default value
ALTER TABLE WorkOrders 
ADD Priority INT NOT NULL DEFAULT 2;  -- Normal = 2

-- Add check constraint
ALTER TABLE WorkOrders
ADD CONSTRAINT CK_WorkOrders_Priority 
CHECK (Priority IN (0, 1, 2, 3));

-- Create index for priority-based queries
CREATE INDEX IX_WorkOrders_Priority_CreatedDate 
ON WorkOrders(Priority ASC, CreatedDate DESC);
```

### API Changes

**DTOs**
```csharp
// WorkOrderDto.cs - Add Priority property
public class WorkOrderDto
{
    public Guid Id { get; set; }
    public string Number { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public WorkOrderPriority Priority { get; set; }  // NEW
    public WorkOrderStatus Status { get; set; }
    // ... other properties
}

// CreateWorkOrderCommand.cs - Add Priority parameter
public class CreateWorkOrderCommand : IRequest<WorkOrderDto>
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string Instructions { get; set; }
    public string RoomNumber { get; set; }
    public WorkOrderPriority Priority { get; set; } = WorkOrderPriority.Normal;  // NEW
    // ... other properties
}

// UpdateWorkOrderCommand.cs - Add Priority parameter
public class UpdateWorkOrderCommand : IRequest<WorkOrderDto>
{
    public Guid Id { get; set; }
    public WorkOrderPriority Priority { get; set; }  // NEW
    // ... other properties
}
```

**Query Extensions**
```csharp
// WorkOrderQueryExtensions.cs
public static class WorkOrderQueryExtensions
{
    public static IQueryable<WorkOrder> OrderByPriority(
        this IQueryable<WorkOrder> query)
    {
        return query
            .OrderBy(w => w.Priority)           // Urgent first (0)
            .ThenByDescending(w => w.CreatedDate);
    }
    
    public static IQueryable<WorkOrder> FilterByPriority(
        this IQueryable<WorkOrder> query, 
        WorkOrderPriority? priority)
    {
        return priority.HasValue 
            ? query.Where(w => w.Priority == priority.Value)
            : query;
    }
}
```

### Blazor Component Changes

**Priority Selector Component**
```razor
<!-- PrioritySelector.razor -->
<div class="priority-selector">
    <label for="priority">Priority *</label>
    <select id="priority" 
            @bind="Value" 
            class="form-select">
        <option value="@WorkOrderPriority.Urgent">
            ⚠️ Urgent - Safety hazards, critical failures
        </option>
        <option value="@WorkOrderPriority.High">
            ↑ High - Important operational issues
        </option>
        <option value="@WorkOrderPriority.Normal" selected>
            = Normal - Standard maintenance (default)
        </option>
        <option value="@WorkOrderPriority.Low">
            ↓ Low - Nice-to-have improvements
        </option>
    </select>
</div>

@code {
    [Parameter]
    public WorkOrderPriority Value { get; set; } = WorkOrderPriority.Normal;
    
    [Parameter]
    public EventCallback<WorkOrderPriority> ValueChanged { get; set; }
}
```

**Priority Badge Component**
```razor
<!-- PriorityBadge.razor -->
<span class="priority-badge priority-@Priority.ToString().ToLower()"
      aria-label="Priority: @GetAriaLabel()">
    @GetIcon() @Priority
</span>

@code {
    [Parameter]
    public WorkOrderPriority Priority { get; set; }
    
    private string GetIcon() => Priority switch
    {
        WorkOrderPriority.Urgent => "⚠️",
        WorkOrderPriority.High => "↑",
        WorkOrderPriority.Normal => "=",
        WorkOrderPriority.Low => "↓",
        _ => ""
    };
    
    private string GetAriaLabel() => Priority switch
    {
        WorkOrderPriority.Urgent => "Urgent - requires immediate attention",
        WorkOrderPriority.High => "High - important issue",
        WorkOrderPriority.Normal => "Normal - standard maintenance",
        WorkOrderPriority.Low => "Low - nice to have",
        _ => Priority.ToString()
    };
}
```

**CSS Styles**
```css
/* PriorityBadge.razor.css */
.priority-badge {
    display: inline-flex;
    align-items: center;
    gap: 0.25rem;
    padding: 0.25rem 0.75rem;
    border-radius: 0.375rem;
    font-size: 0.875rem;
    font-weight: 500;
}

.priority-urgent {
    background-color: #FEE2E2;
    color: #DC2626;
}

.priority-high {
    background-color: #FFEDD5;
    color: #EA580C;
}

.priority-normal {
    background-color: #DBEAFE;
    color: #2563EB;
}

.priority-low {
    background-color: #F3F4F6;
    color: #6B7280;
}
```

### Validation Rules

```csharp
// CreateWorkOrderCommandValidator.cs
public class CreateWorkOrderCommandValidator 
    : AbstractValidator<CreateWorkOrderCommand>
{
    public CreateWorkOrderCommandValidator()
    {
        // Existing validations...
        
        RuleFor(x => x.Priority)
            .IsInEnum()
            .WithMessage("Priority must be a valid value (Urgent, High, Normal, or Low)");
    }
}
```

### Testing Strategy

**Unit Tests**
- WorkOrderPriority enum values and ordering
- Priority default value (Normal)
- Priority validation rules
- Priority badge component rendering
- Priority selector component binding

**Integration Tests**
- Create work order with each priority level
- Update work order priority
- Query work orders filtered by priority
- Query work orders sorted by priority
- Migration applies correctly

**Acceptance Tests**
- User can select priority when creating work order
- User can update priority on existing work order
- Work orders display with correct priority badge
- Work order list sorts by priority correctly
- Work order list filters by priority correctly

### Performance Considerations

- Index on (Priority, CreatedDate) for efficient sorting
- Enum stored as INT (4 bytes) - minimal storage overhead
- Priority filtering uses indexed column
- No N+1 queries (Priority is on WorkOrder entity)

### Security Considerations

- Priority enum validation prevents invalid values
- No authorization changes needed (same permissions as other work order fields)
- Audit log captures priority changes

### Backward Compatibility

- Existing work orders get Priority = Normal via migration default
- API clients not sending Priority get Normal default
- No breaking changes to existing endpoints

## Test Design

### Unit Tests

**WorkOrderPriority Enum Tests**
```csharp
[TestFixture]
public class WorkOrderPriorityTests
{
    [Test]
    public void Priority_Values_Should_Be_Ordered_Correctly()
    {
        Assert.That((int)WorkOrderPriority.Urgent, Is.EqualTo(0));
        Assert.That((int)WorkOrderPriority.High, Is.EqualTo(1));
        Assert.That((int)WorkOrderPriority.Normal, Is.EqualTo(2));
        Assert.That((int)WorkOrderPriority.Low, Is.EqualTo(3));
    }
    
    [Test]
    public void Priority_Should_Sort_Urgent_First()
    {
        var priorities = new[] { 
            WorkOrderPriority.Low, 
            WorkOrderPriority.Normal, 
            WorkOrderPriority.Urgent, 
            WorkOrderPriority.High 
        };
        
        var sorted = priorities.OrderBy(p => p).ToArray();
        
        Assert.That(sorted[0], Is.EqualTo(WorkOrderPriority.Urgent));
        Assert.That(sorted[1], Is.EqualTo(WorkOrderPriority.High));
        Assert.That(sorted[2], Is.EqualTo(WorkOrderPriority.Normal));
        Assert.That(sorted[3], Is.EqualTo(WorkOrderPriority.Low));
    }
}
```

**WorkOrder Entity Tests**
```csharp
[TestFixture]
public class WorkOrderEntityTests
{
    [Test]
    public void New_WorkOrder_Should_Default_To_Normal_Priority()
    {
        var workOrder = new WorkOrder();
        Assert.That(workOrder.Priority, Is.EqualTo(WorkOrderPriority.Normal));
    }
    
    [Test]
    public void WorkOrder_Should_Accept_All_Priority_Values()
    {
        var workOrder = new WorkOrder();
        
        foreach (WorkOrderPriority priority in Enum.GetValues(typeof(WorkOrderPriority)))
        {
            workOrder.Priority = priority;
            Assert.That(workOrder.Priority, Is.EqualTo(priority));
        }
    }
}
```

**Command Validator Tests**
```csharp
[TestFixture]
public class CreateWorkOrderCommandValidatorTests
{
    private CreateWorkOrderCommandValidator _validator;
    
    [SetUp]
    public void SetUp()
    {
        _validator = new CreateWorkOrderCommandValidator();
    }
    
    [Test]
    public void Should_Accept_Valid_Priority_Values()
    {
        var command = new CreateWorkOrderCommand
        {
            Title = "Test",
            Priority = WorkOrderPriority.Urgent
        };
        
        var result = _validator.Validate(command);
        Assert.That(result.IsValid, Is.True);
    }
    
    [Test]
    public void Should_Reject_Invalid_Priority_Value()
    {
        var command = new CreateWorkOrderCommand
        {
            Title = "Test",
            Priority = (WorkOrderPriority)999
        };
        
        var result = _validator.Validate(command);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.PropertyName == "Priority"), Is.True);
    }
}
```

**Blazor Component Tests**
```csharp
[TestFixture]
public class PriorityBadgeTests : TestContext
{
    [Test]
    public void Should_Render_Urgent_Badge_With_Correct_Icon_And_Color()
    {
        var cut = RenderComponent<PriorityBadge>(parameters => parameters
            .Add(p => p.Priority, WorkOrderPriority.Urgent));
        
        var badge = cut.Find(".priority-badge");
        Assert.That(badge.ClassList, Does.Contain("priority-urgent"));
        Assert.That(badge.TextContent, Does.Contain("⚠️"));
        Assert.That(badge.TextContent, Does.Contain("Urgent"));
    }
    
    [Test]
    public void Should_Have_Accessible_Aria_Label()
    {
        var cut = RenderComponent<PriorityBadge>(parameters => parameters
            .Add(p => p.Priority, WorkOrderPriority.High));
        
        var badge = cut.Find(".priority-badge");
        Assert.That(badge.GetAttribute("aria-label"), 
            Is.EqualTo("Priority: High - important issue"));
    }
}

[TestFixture]
public class PrioritySelectorTests : TestContext
{
    [Test]
    public void Should_Default_To_Normal_Priority()
    {
        var cut = RenderComponent<PrioritySelector>();
        
        var select = cut.Find("select");
        Assert.That(select.GetAttribute("value"), 
            Is.EqualTo(WorkOrderPriority.Normal.ToString()));
    }
    
    [Test]
    public void Should_Trigger_ValueChanged_On_Selection()
    {
        WorkOrderPriority? changedValue = null;
        var cut = RenderComponent<PrioritySelector>(parameters => parameters
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<WorkOrderPriority>(
                this, value => changedValue = value)));
        
        var select = cut.Find("select");
        select.Change(WorkOrderPriority.Urgent.ToString());
        
        Assert.That(changedValue, Is.EqualTo(WorkOrderPriority.Urgent));
    }
}
```

### Integration Tests

**Database Migration Tests**
```csharp
[TestFixture]
public class PriorityMigrationTests : IntegrationTestBase
{
    [Test]
    public async Task Migration_Should_Add_Priority_Column_With_Default()
    {
        // Arrange: Create work order before migration
        var workOrder = new WorkOrder { Title = "Test" };
        await SaveAsync(workOrder);
        
        // Act: Run migration (handled by test setup)
        
        // Assert: Existing work order has Normal priority
        var updated = await LoadAsync<WorkOrder>(workOrder.Id);
        Assert.That(updated.Priority, Is.EqualTo(WorkOrderPriority.Normal));
    }
    
    [Test]
    public async Task Should_Enforce_Priority_Check_Constraint()
    {
        // Attempt to insert invalid priority value directly via SQL
        var exception = Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await ExecuteSqlAsync(
                "INSERT INTO WorkOrders (Id, Number, Title, Priority) " +
                "VALUES (NEWID(), 'WO-999', 'Test', 999)");
        });
        
        Assert.That(exception.Message, Does.Contain("CK_WorkOrders_Priority"));
    }
}
```

**Query Tests**
```csharp
[TestFixture]
public class WorkOrderQueryTests : IntegrationTestBase
{
    [Test]
    public async Task Should_Sort_By_Priority_Then_CreatedDate()
    {
        // Arrange
        var urgent = await CreateWorkOrderAsync("Urgent", WorkOrderPriority.Urgent);
        var normal = await CreateWorkOrderAsync("Normal", WorkOrderPriority.Normal);
        var high = await CreateWorkOrderAsync("High", WorkOrderPriority.High);
        var low = await CreateWorkOrderAsync("Low", WorkOrderPriority.Low);
        
        // Act
        var query = DbContext.WorkOrders.OrderByPriority();
        var results = await query.ToListAsync();
        
        // Assert
        Assert.That(results[0].Id, Is.EqualTo(urgent.Id));
        Assert.That(results[1].Id, Is.EqualTo(high.Id));
        Assert.That(results[2].Id, Is.EqualTo(normal.Id));
        Assert.That(results[3].Id, Is.EqualTo(low.Id));
    }
    
    [Test]
    public async Task Should_Filter_By_Priority()
    {
        // Arrange
        await CreateWorkOrderAsync("Urgent 1", WorkOrderPriority.Urgent);
        await CreateWorkOrderAsync("Urgent 2", WorkOrderPriority.Urgent);
        await CreateWorkOrderAsync("Normal", WorkOrderPriority.Normal);
        
        // Act
        var query = DbContext.WorkOrders.FilterByPriority(WorkOrderPriority.Urgent);
        var results = await query.ToListAsync();
        
        // Assert
        Assert.That(results.Count, Is.EqualTo(2));
        Assert.That(results.All(w => w.Priority == WorkOrderPriority.Urgent), Is.True);
    }
}
```

**API Tests**
```csharp
[TestFixture]
public class WorkOrderApiTests : IntegrationTestBase
{
    [Test]
    public async Task Create_WorkOrder_Should_Accept_Priority()
    {
        var command = new CreateWorkOrderCommand
        {
            Title = "Urgent Repair",
            Description = "Broken HVAC",
            Priority = WorkOrderPriority.Urgent
        };
        
        var result = await SendAsync(command);
        
        Assert.That(result.Priority, Is.EqualTo(WorkOrderPriority.Urgent));
    }
    
    [Test]
    public async Task Create_WorkOrder_Should_Default_To_Normal_Priority()
    {
        var command = new CreateWorkOrderCommand
        {
            Title = "Standard Repair",
            Description = "Paint touch-up"
            // Priority not specified
        };
        
        var result = await SendAsync(command);
        
        Assert.That(result.Priority, Is.EqualTo(WorkOrderPriority.Normal));
    }
    
    [Test]
    public async Task Update_WorkOrder_Should_Change_Priority()
    {
        // Arrange
        var workOrder = await CreateWorkOrderAsync("Test", WorkOrderPriority.Normal);
        
        // Act
        var command = new UpdateWorkOrderCommand
        {
            Id = workOrder.Id,
            Priority = WorkOrderPriority.Urgent
        };
        await SendAsync(command);
        
        // Assert
        var updated = await LoadAsync<WorkOrder>(workOrder.Id);
        Assert.That(updated.Priority, Is.EqualTo(WorkOrderPriority.Urgent));
    }
}
```

### Acceptance Tests

**Priority Selection Tests**
```csharp
[TestFixture]
public class PrioritySelectionAcceptanceTests : AcceptanceTestBase
{
    [Test]
    public async Task User_Can_Select_Priority_When_Creating_WorkOrder()
    {
        // Navigate to create work order page
        await Page.GotoAsync("/workorders/create");
        
        // Fill in required fields
        await Page.FillAsync("#title", "Broken HVAC");
        await Page.FillAsync("#description", "No cooling");
        
        // Select Urgent priority
        await Page.SelectOptionAsync("#priority", WorkOrderPriority.Urgent.ToString());
        
        // Submit form
        await Page.ClickAsync("button[type=submit]");
        
        // Verify work order created with correct priority
        await Page.WaitForSelectorAsync(".priority-badge.priority-urgent");
        var badge = await Page.TextContentAsync(".priority-badge");
        Assert.That(badge, Does.Contain("Urgent"));
    }
    
    [Test]
    public async Task Priority_Defaults_To_Normal()
    {
        await Page.GotoAsync("/workorders/create");
        
        var selectedValue = await Page.GetAttributeAsync("#priority", "value");
        Assert.That(selectedValue, Is.EqualTo(WorkOrderPriority.Normal.ToString()));
    }
}
```

**Priority Display Tests**
```csharp
[TestFixture]
public class PriorityDisplayAcceptanceTests : AcceptanceTestBase
{
    [Test]
    public async Task WorkOrder_List_Shows_Priority_Badges()
    {
        // Arrange: Create work orders with different priorities
        await CreateWorkOrderAsync("Urgent", WorkOrderPriority.Urgent);
        await CreateWorkOrderAsync("High", WorkOrderPriority.High);
        await CreateWorkOrderAsync("Normal", WorkOrderPriority.Normal);
        
        // Act: Navigate to work order list
        await Page.GotoAsync("/workorders");
        
        // Assert: All priority badges are visible
        await Page.WaitForSelectorAsync(".priority-badge.priority-urgent");
        await Page.WaitForSelectorAsync(".priority-badge.priority-high");
        await Page.WaitForSelectorAsync(".priority-badge.priority-normal");
    }
}
```

**Priority Sorting Tests**
```csharp
[TestFixture]
public class PrioritySortingAcceptanceTests : AcceptanceTestBase
{
    [Test]
    public async Task WorkOrders_Sort_By_Priority_By_Default()
    {
        // Arrange: Create work orders in random order
        await CreateWorkOrderAsync("Low Priority", WorkOrderPriority.Low);
        await CreateWorkOrderAsync("Urgent Priority", WorkOrderPriority.Urgent);
        await CreateWorkOrderAsync("Normal Priority", WorkOrderPriority.Normal);
        
        // Act: Navigate to work order list
        await Page.GotoAsync("/workorders");
        
        // Assert: First item is Urgent
        var firstBadge = await Page.TextContentAsync(".work-order-card:first-child .priority-badge");
        Assert.That(firstBadge, Does.Contain("Urgent"));
    }
}
```

**Priority Filtering Tests**
```csharp
[TestFixture]
public class PriorityFilteringAcceptanceTests : AcceptanceTestBase
{
    [Test]
    public async Task User_Can_Filter_By_Priority()
    {
        // Arrange
        await CreateWorkOrderAsync("Urgent 1", WorkOrderPriority.Urgent);
        await CreateWorkOrderAsync("Urgent 2", WorkOrderPriority.Urgent);
        await CreateWorkOrderAsync("Normal", WorkOrderPriority.Normal);
        
        // Act: Navigate and filter
        await Page.GotoAsync("/workorders");
        await Page.SelectOptionAsync("#priority-filter", WorkOrderPriority.Urgent.ToString());
        
        // Assert: Only urgent work orders shown
        var cards = await Page.QuerySelectorAllAsync(".work-order-card");
        Assert.That(cards.Count, Is.EqualTo(2));
        
        foreach (var card in cards)
        {
            var badge = await card.QuerySelectorAsync(".priority-badge.priority-urgent");
            Assert.That(badge, Is.Not.Null);
        }
    }
}
```

### Test Coverage Goals

- **Unit Tests**: 100% coverage of Priority enum, entity defaults, validators, components
- **Integration Tests**: All CRUD operations, queries, migration, database constraints
- **Acceptance Tests**: All user workflows (create, edit, view, sort, filter)

### Test Data Setup

```csharp
public static class WorkOrderTestData
{
    public static WorkOrder CreateUrgentWorkOrder() => new()
    {
        Title = "Broken HVAC System",
        Description = "No cooling in sanctuary",
        Priority = WorkOrderPriority.Urgent,
        Status = WorkOrderStatus.Draft
    };
    
    public static WorkOrder CreateNormalWorkOrder() => new()
    {
        Title = "Paint Touch-up",
        Description = "Scuff marks in hallway",
        Priority = WorkOrderPriority.Normal,
        Status = WorkOrderStatus.Draft
    };
}
```

## References
- Epic Issue: #8227
- Child Issues: #8264, #8265, #8266
