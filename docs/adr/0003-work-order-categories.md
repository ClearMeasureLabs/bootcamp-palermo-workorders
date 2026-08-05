# ADR-0003: Work Order Categories

**Status:** Proposed  
**Date:** 2026-08-05  
**Epic:** #8232 - Work Order Categories  
**Child Issues:** #8272, #8273, #8274

---

## Conceptual Definition Phase

### Business Context

Church facilities have diverse maintenance needs across different operational areas:
- **Facilities:** Building maintenance, plumbing, electrical, structural repairs
- **Audio-Visual:** Sound systems, projectors, lighting, streaming equipment
- **Grounds:** Landscaping, parking lots, outdoor areas, signage
- **HVAC:** Heating, ventilation, air conditioning systems
- **Other:** Miscellaneous tasks that don't fit standard categories

Currently, all work orders are treated uniformly without classification, leading to:
- Difficulty filtering work orders by maintenance area
- Inability to track workload distribution across departments
- Challenges in resource allocation and budgeting by category
- No visibility into which areas require most attention

### Business Value

**Primary Benefits:**
1. **Organization:** Clear classification of work orders by operational area
2. **Filtering:** Quick access to work orders by category (e.g., "Show all HVAC work")
3. **Reporting:** Track maintenance costs and workload by category
4. **Resource Planning:** Identify which areas need more staff or budget
5. **Specialization:** Assign work orders to staff with relevant expertise

**Success Metrics:**
- 100% of work orders have assigned categories
- 50% reduction in time to find relevant work orders
- Clear visibility into workload distribution across categories
- Improved resource allocation based on category data

### User Stories

**As a Facilities Manager:**
- I want to categorize work orders by type so I can organize maintenance by operational area
- I want to filter work orders by category so I can focus on specific maintenance areas
- I want to see category distribution so I can understand workload across departments

**As a Maintenance Technician:**
- I want to see work order categories so I know which tasks match my expertise
- I want to filter by my specialty category so I can focus on relevant work

**As a Church Administrator:**
- I want to track costs by category so I can budget appropriately for each area
- I want to see which categories have most work orders so I can allocate resources

### Scope

**In Scope:**
- WorkOrderCategory enum with 5 standard categories
- Category property on WorkOrder entity
- Category dropdown selector in UI forms
- Category badge display component
- Filter work orders by category in search
- Category column in search results

**Out of Scope (Future Enhancements):**
- Custom/user-defined categories
- Category-based permissions/access control
- Category-specific workflows or approval processes
- Automatic category suggestion based on title/description
- Category-based SLA (Service Level Agreement) rules
- Multi-category assignment (work orders can only have one category)

### Domain Model Changes

**New Enum: WorkOrderCategory**
```csharp
public enum WorkOrderCategory
{
    Facilities = 0,   // Building maintenance, plumbing, electrical
    AudioVisual = 1,  // Sound, video, lighting, streaming
    Grounds = 2,      // Landscaping, parking, outdoor areas
    HVAC = 3,         // Heating, ventilation, air conditioning
    Other = 4         // Miscellaneous/uncategorized
}
```

**WorkOrder Entity Extension:**
```csharp
public class WorkOrder
{
    // Existing properties...
    
    // Category property
    public WorkOrderCategory Category { get; set; } = WorkOrderCategory.Other;
}
```

### Business Rules

1. **Category Assignment:**
   - Every work order must have a category
   - Default category is "Other" for new work orders
   - Category can be changed at any time (no workflow restrictions)

2. **Category Values:**
   - Facilities: General building maintenance and repairs
   - AudioVisual: All A/V equipment and systems
   - Grounds: Outdoor maintenance and landscaping
   - HVAC: Climate control systems
   - Other: Default for uncategorized or miscellaneous work

3. **Display Rules:**
   - Category badge shows icon + text
   - Color-coded for quick visual identification
   - Category appears in search results and work order details

### Architecture Diagram

**Before State:**
```
┌─────────────────────────────────────────────────────────────┐
│ Current System: Uncategorized Work Orders                   │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  WorkOrder Properties:                                       │
│  - Title                                                     │
│  - Description                                               │
│  - Status                                                    │
│  - Priority                                                  │
│  - AssignedTo                                                │
│                                                              │
│  Problems:                                                   │
│  - No way to classify by maintenance area                    │
│  - Cannot filter by operational category                     │
│  - No visibility into workload distribution                  │
│  - Difficult to assign to specialized staff                  │
└─────────────────────────────────────────────────────────────┘
```

**After State:**
```
┌─────────────────────────────────────────────────────────────┐
│ New System: Categorized Work Orders                         │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  WorkOrder Properties:                                       │
│  - Title                                                     │
│  - Description                                               │
│  - Status                                                    │
│  - Priority                                                  │
│  - Category (NEW) ← WorkOrderCategory enum                   │
│  - AssignedTo                                                │
│                                                              │
│  UI Components:                                              │
│  - CategorySelector (dropdown for selection)                 │
│  - CategoryBadge (color-coded display)                       │
│                                                              │
│  Search Enhancements:                                        │
│  - Category filter dropdown                                  │
│  - Category column in results                                │
│  - Category-based sorting                                    │
│                                                              │
│  Benefits:                                                   │
│  - Clear classification by operational area                  │
│  - Easy filtering and searching                              │
│  - Visual identification via color-coded badges              │
│  - Better resource allocation                                │
└─────────────────────────────────────────────────────────────┘
```

---

## UX Design Phase

### Visual Design System

**Category Badge Colors:**
- 🔵 **Blue (#3b82f6):** Facilities (building icon)
- 🟣 **Purple (#a855f7):** Audio-Visual (camera-video icon)
- 🟢 **Green (#10b981):** Grounds (tree icon)
- 🟠 **Orange (#f97316):** HVAC (thermometer icon)
- ⚪ **Gray (#6b7280):** Other (gear icon)

**Component Hierarchy:**
```
WorkOrderManage.razor
├── CategorySelector.razor (dropdown component)
│   └── Options: Facilities, Audio-Visual, Grounds, HVAC, Other
└── Form Actions (Save, Cancel)

WorkOrderSearch.razor
├── Search Filters
│   └── Category Filter (All, Facilities, Audio-Visual, Grounds, HVAC, Other)
├── Results Table
│   ├── Category Column
│   └── CategoryBadge.razor (icon + text, color-coded)
└── Pagination
```

### UI Components Specification

**CategorySelector.razor:**
```razor
<div class="category-selector">
    <label class="form-label">Category</label>
    <select @bind="Value" class="form-select" disabled="@Disabled">
        <option value="@WorkOrderCategory.Facilities">Facilities</option>
        <option value="@WorkOrderCategory.AudioVisual">Audio-Visual</option>
        <option value="@WorkOrderCategory.Grounds">Grounds</option>
        <option value="@WorkOrderCategory.HVAC">HVAC</option>
        <option value="@WorkOrderCategory.Other">Other</option>
    </select>
</div>
```

**CategoryBadge.razor:**
```razor
<span class="badge category-badge @GetBadgeClass()" title="@GetTooltip()">
    <i class="bi @GetIcon()"></i>
    <span>@GetText()</span>
</span>
```

**CategoryBadge.razor.css:**
```css
.category-badge {
    display: inline-flex;
    align-items: center;
    gap: 0.25rem;
    padding: 0.25rem 0.75rem;
    font-size: 0.875rem;
    font-weight: 500;
    border-radius: 9999px;
}

.category-badge.facilities {
    background-color: #dbeafe;
    color: #1e40af;
}

.category-badge.audiovisual {
    background-color: #f3e8ff;
    color: #7e22ce;
}

.category-badge.grounds {
    background-color: #d1fae5;
    color: #065f46;
}

.category-badge.hvac {
    background-color: #ffedd5;
    color: #c2410c;
}

.category-badge.other {
    background-color: #f3f4f6;
    color: #4b5563;
}
```

### User Workflows

**Workflow 1: Create Work Order with Category**
1. User navigates to "Create Work Order"
2. User fills in Title, Description, Priority
3. User selects Category from dropdown (defaults to "Other")
4. User clicks "Save"
5. System creates work order with selected category
6. CategoryBadge displays in search results

**Workflow 2: Filter Work Orders by Category**
1. User navigates to "Search Work Orders"
2. User selects Category filter: "HVAC"
3. System displays only HVAC work orders
4. Each row shows CategoryBadge with orange HVAC indicator
5. User can quickly identify all HVAC-related work

**Workflow 3: Change Work Order Category**
1. User opens existing work order
2. User changes Category dropdown from "Other" to "Facilities"
3. User clicks "Save"
4. System updates category
5. CategoryBadge updates to blue Facilities indicator

### Accessibility Requirements

- **Keyboard Navigation:** Category dropdown accessible via Tab/Shift+Tab
- **Screen Readers:** ARIA labels for category selector
- **Color Independence:** Category indicated by icon + text, not just color
- **Focus Indicators:** Clear focus rings on dropdown
- **Tooltips:** Descriptive tooltips on category badges

---

## Technical Design Phase

### Database Schema Changes

**Migration: 20260805_AddWorkOrderCategory**

```sql
-- Add category column to WorkOrder table
ALTER TABLE WorkOrder ADD Category INT NOT NULL DEFAULT 4; -- Default to Other

-- Add index for category-based queries
CREATE INDEX IX_WorkOrder_Category 
ON WorkOrder(Category);

-- Add check constraint for valid category values
ALTER TABLE WorkOrder 
ADD CONSTRAINT CK_WorkOrder_Category 
CHECK (Category IN (0, 1, 2, 3, 4));
```

**EF Core Configuration (WorkOrderMap.cs):**
```csharp
public class WorkOrderMap : IEntityTypeConfiguration<WorkOrder>
{
    public void Configure(EntityTypeBuilder<WorkOrder> builder)
    {
        // Existing configuration...
        
        // Category property
        builder.Property(x => x.Category)
            .IsRequired()
            .HasDefaultValue(WorkOrderCategory.Other);
            
        // Index for category queries
        builder.HasIndex(x => x.Category)
            .HasDatabaseName("IX_WorkOrder_Category");
    }
}
```

### API Changes

**Modified Query: WorkOrderSpecificationQuery**
```csharp
public record WorkOrderSpecificationQuery : IRequest<WorkOrder[]>, IRemotableRequest
{
    // Existing properties...
    
    public WorkOrderCategory? Category { get; set; }
    
    public void MatchCategory(WorkOrderCategory? category)
    {
        Category = category;
    }
}
```

**Modified Handler: WorkOrderSearchHandler**
```csharp
public async Task<WorkOrder[]> Handle(
    WorkOrderSpecificationQuery specification,
    CancellationToken cancellationToken = default)
{
    IQueryable<WorkOrder> query = context.Set<WorkOrder>();
    
    // Existing filters...
    
    if (specification.Category != null)
    {
        query = query.Where(wo => wo.Category == specification.Category);
    }
    
    return await query.ToArrayAsync(cancellationToken);
}
```

### Component Implementation

**CategorySelector.razor:**
```razor
@using ClearMeasure.Bootcamp.Core.Model

<div class="category-selector">
    <label class="form-label">Category</label>
    <select @bind="Value" 
            class="form-select" 
            disabled="@Disabled"
            aria-label="Work order category">
        <option value="@WorkOrderCategory.Facilities">Facilities</option>
        <option value="@WorkOrderCategory.AudioVisual">Audio-Visual</option>
        <option value="@WorkOrderCategory.Grounds">Grounds</option>
        <option value="@WorkOrderCategory.HVAC">HVAC</option>
        <option value="@WorkOrderCategory.Other">Other</option>
    </select>
</div>

@code {
    [Parameter] public WorkOrderCategory Value { get; set; } = WorkOrderCategory.Other;
    [Parameter] public EventCallback<WorkOrderCategory> ValueChanged { get; set; }
    [Parameter] public bool Disabled { get; set; }
}
```

**CategoryBadge.razor:**
```razor
@using ClearMeasure.Bootcamp.Core.Model

<span class="badge category-badge @GetBadgeClass()" title="@GetTooltip()">
    <i class="bi @GetIcon()"></i>
    <span>@GetText()</span>
</span>

@code {
    [Parameter] public WorkOrderCategory Category { get; set; }
    
    private string GetBadgeClass()
    {
        return Category switch
        {
            WorkOrderCategory.Facilities => "facilities",
            WorkOrderCategory.AudioVisual => "audiovisual",
            WorkOrderCategory.Grounds => "grounds",
            WorkOrderCategory.HVAC => "hvac",
            WorkOrderCategory.Other => "other",
            _ => "other"
        };
    }
    
    private string GetIcon()
    {
        return Category switch
        {
            WorkOrderCategory.Facilities => "bi-building",
            WorkOrderCategory.AudioVisual => "bi-camera-video",
            WorkOrderCategory.Grounds => "bi-tree",
            WorkOrderCategory.HVAC => "bi-thermometer",
            WorkOrderCategory.Other => "bi-gear",
            _ => "bi-gear"
        };
    }
    
    private string GetText()
    {
        return Category switch
        {
            WorkOrderCategory.Facilities => "Facilities",
            WorkOrderCategory.AudioVisual => "Audio-Visual",
            WorkOrderCategory.Grounds => "Grounds",
            WorkOrderCategory.HVAC => "HVAC",
            WorkOrderCategory.Other => "Other",
            _ => "Other"
        };
    }
    
    private string GetTooltip()
    {
        return Category switch
        {
            WorkOrderCategory.Facilities => "Building maintenance and repairs",
            WorkOrderCategory.AudioVisual => "Sound, video, and lighting systems",
            WorkOrderCategory.Grounds => "Landscaping and outdoor areas",
            WorkOrderCategory.HVAC => "Heating, ventilation, and air conditioning",
            WorkOrderCategory.Other => "Miscellaneous work",
            _ => "Uncategorized work order"
        };
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
        
        public string? Category { get; set; } // Enum value as string
    }
}
```

**WorkOrderSearch.razor:**
```razor
<div class="filter-group">
    <label class="filter-label" for="category">Category</label>
    <InputSelect id="@Elements.CategorySelect" 
                 @bind-Value="Model.Filters.Category" 
                 class="form-control">
        <option value="">All</option>
        <option value="Facilities">Facilities</option>
        <option value="AudioVisual">Audio-Visual</option>
        <option value="Grounds">Grounds</option>
        <option value="HVAC">HVAC</option>
        <option value="Other">Other</option>
    </InputSelect>
</div>
```

---

## Test Design Phase

### Unit Tests

**WorkOrderCategory Enum Tests:**
```csharp
[TestFixture]
public class WorkOrderCategoryTests
{
    [Test]
    public void WorkOrderCategory_ShouldHaveCorrectValues()
    {
        Assert.That((int)WorkOrderCategory.Facilities, Is.EqualTo(0));
        Assert.That((int)WorkOrderCategory.AudioVisual, Is.EqualTo(1));
        Assert.That((int)WorkOrderCategory.Grounds, Is.EqualTo(2));
        Assert.That((int)WorkOrderCategory.HVAC, Is.EqualTo(3));
        Assert.That((int)WorkOrderCategory.Other, Is.EqualTo(4));
    }
}
```

**WorkOrder Category Property Tests:**
```csharp
[TestFixture]
public class WorkOrderCategoryPropertyTests
{
    [Test]
    public void WorkOrder_ShouldDefaultToOtherCategory()
    {
        var workOrder = new WorkOrder();
        
        Assert.That(workOrder.Category, Is.EqualTo(WorkOrderCategory.Other));
    }
    
    [Test]
    public void WorkOrder_ShouldAllowCategoryAssignment()
    {
        var workOrder = new WorkOrder
        {
            Category = WorkOrderCategory.HVAC
        };
        
        Assert.That(workOrder.Category, Is.EqualTo(WorkOrderCategory.HVAC));
    }
}
```

### Integration Tests

**WorkOrderSpecificationQuery Category Tests:**
```csharp
[TestFixture]
public class WorkOrderCategoryFilterTests : IntegrationTestBase
{
    [Test]
    public async Task Handle_ShouldFilterByCategory()
    {
        // Arrange
        var hvacWorkOrder = new WorkOrder
        {
            Title = "Fix AC",
            Category = WorkOrderCategory.HVAC
        };
        
        var facilitiesWorkOrder = new WorkOrder
        {
            Title = "Fix Door",
            Category = WorkOrderCategory.Facilities
        };
        
        await SaveAsync(hvacWorkOrder, facilitiesWorkOrder);
        
        // Act
        var query = new WorkOrderSpecificationQuery();
        query.MatchCategory(WorkOrderCategory.HVAC);
        var result = await Bus.Send(query);
        
        // Assert
        Assert.That(result, Has.Length.EqualTo(1));
        Assert.That(result[0].Category, Is.EqualTo(WorkOrderCategory.HVAC));
    }
}
```

### Acceptance Tests

**Feature: Work Order Categories**
```gherkin
Feature: Work Order Categories
  As a Facilities Manager
  I want to categorize work orders by type
  So that I can organize and filter maintenance by operational area

Scenario: Create work order with category
  Given I am logged in as a Facilities Manager
  When I navigate to "Create Work Order"
  And I enter title "Fix HVAC Unit"
  And I select category "HVAC"
  And I click "Save"
  Then I should see "Work order created successfully"
  And the work order should have category "HVAC"

Scenario: Filter work orders by category
  Given I am logged in as a Facilities Manager
  And there are 3 HVAC work orders
  And there are 5 Facilities work orders
  When I navigate to "Search Work Orders"
  And I select category filter "HVAC"
  Then I should see 3 work orders
  And each work order should display an HVAC badge

Scenario: Change work order category
  Given I am logged in as a Facilities Manager
  And a work order exists with category "Other"
  When I open the work order
  And I change category to "Grounds"
  And I click "Save"
  Then the work order category should be "Grounds"
  And the badge should display green with tree icon
```

---

## Implementation Checklist

### Phase 1: Domain Model (#8272)
- [ ] Create WorkOrderCategory enum
- [ ] Add Category property to WorkOrder entity
- [ ] Update WorkOrderMap for EF Core configuration
- [ ] Create database migration
- [ ] Write unit tests for category property
- [ ] Commit: "Add WorkOrderCategory enum and model field"

### Phase 2: UI Components (#8273)
- [ ] Create CategorySelector.razor component
- [ ] Create CategoryBadge.razor component
- [ ] Add CategoryBadge.razor.css styling
- [ ] Integrate CategorySelector into WorkOrderManage.razor
- [ ] Add Category to WorkOrderManageModel
- [ ] Display CategoryBadge in WorkOrderSearch results
- [ ] Add Category column to search results table
- [ ] Write component unit tests
- [ ] Commit: "Add category UI components and form integration"

### Phase 3: Search Integration (#8274)
- [ ] Add Category filter to WorkOrderSearch page
- [ ] Update WorkOrderSearchModel with Category filter
- [ ] Add MatchCategory to WorkOrderSpecificationQuery
- [ ] Update WorkOrderSearchHandler for category filtering
- [ ] Write integration tests for category filtering
- [ ] Commit: "Add category filtering and search integration"

### Phase 4: Testing & Documentation
- [ ] Run full test suite (unit + integration + acceptance)
- [ ] Update API documentation
- [ ] Run PrivateBuild.ps1 to verify all tests pass
- [ ] Create pull request with ADR and implementation

---

## Decision

**Approved for Implementation**

This design provides a simple, effective categorization system with:
- Clear enum-based categories for common maintenance areas
- Intuitive UI components with color-coded visual indicators
- Easy filtering and searching by category
- Minimal complexity (single enum, single property)

The implementation follows existing patterns in the codebase and integrates seamlessly with the current architecture.

---

## Consequences

**Positive:**
- Clear organization of work orders by operational area
- Easy filtering and reporting by category
- Visual identification via color-coded badges
- Better resource allocation and workload visibility
- Foundation for future category-based features

**Negative:**
- Requires data migration for existing work orders (will default to "Other")
- Single category per work order (no multi-category support)
- Fixed category list (no custom categories)

**Mitigations:**
- Default category ("Other") ensures no data loss
- Category can be updated at any time
- Future enhancement: custom categories if needed

---

## References

- Epic #8232: Work Order Categories
- Child Issues: #8272, #8273, #8274
- Related: ADR-0001 (Priority Levels), ADR-0002 (Recurring Schedules)
- Architecture: arch/arch-c4-component-project-dependencies.puml
