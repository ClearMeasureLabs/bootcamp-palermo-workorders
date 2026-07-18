## Why
Tracking material and labor costs per work order enables budget management and cost reporting across the organization. Cost visibility helps identify expensive recurring issues, supports budget forecasting, and provides accountability for expenditures.

## What Changes
- Add `EstimatedCost` nullable `decimal` property to the `WorkOrder` domain model in `src/Core/Model/`
- Add `ActualCost` nullable `decimal` property to the `WorkOrder` domain model in `src/Core/Model/`
- Update `DataContext` EF Core mapping to persist `EstimatedCost` and `ActualCost` as `decimal(10,2)` columns
- Add a new DbUp migration script to `src/Database/scripts/Update/` adding nullable `EstimatedCost` and `ActualCost` columns to the `WorkOrder` table
- Update `WorkOrderManage` Blazor form to include currency input fields for EstimatedCost (editable by creator in any status) and ActualCost (editable when InProgress or Complete)
- Update `WorkOrderSearch` results to display EstimatedCost and ActualCost columns
- Add domain validation: costs must be non-negative if provided

## Capabilities
### New Capabilities
- Users can enter an estimated cost when creating or editing a work order
- Users can record actual costs when the work order is InProgress or Complete
- Search results display cost information for each work order
- Cost values are formatted as currency in the UI

### Modified Capabilities
- WorkOrderManage form includes new EstimatedCost and ActualCost currency input fields
- WorkOrderSearch results table includes cost columns

## Impact
- **Core** — `WorkOrder` model gains nullable `EstimatedCost` and `ActualCost` decimal properties with non-negative validation
- **DataAccess** — EF Core mapping update for both cost columns
- **UI.Shared** — `WorkOrderManage` form updated with currency inputs; `WorkOrderSearch` results updated with cost columns
- **Database** — New migration script adding nullable `EstimatedCost` and `ActualCost` columns to `WorkOrder` table

## Acceptance Criteria
### Unit Tests
- `WorkOrder_EstimatedCost_ShouldDefaultToNull` — verify new work orders have no estimated cost by default
- `WorkOrder_ActualCost_ShouldDefaultToNull` — verify new work orders have no actual cost by default
- `WorkOrder_EstimatedCost_ShouldRejectNegativeValues` — verify validation rejects negative cost values
- `WorkOrder_ActualCost_ShouldRejectNegativeValues` — verify validation rejects negative cost values
- `WorkOrderManage_ShouldRenderEstimatedCostInput` — bUnit test verifying estimated cost input appears
- `WorkOrderManage_ShouldDisableActualCostInput_WhenDraft` — bUnit test verifying actual cost input is disabled for Draft work orders

### Integration Tests
- `WorkOrder_WithCosts_ShouldPersistAndRetrieve` — save a work order with EstimatedCost of 150.50 and ActualCost of 175.25, verify both round-trip through the database
- `WorkOrder_WithNullCosts_ShouldPersistAndRetrieve` — save a work order without costs and verify nulls are persisted

### Acceptance Tests
- Navigate to create work order form, enter $250.00 for estimated cost, save, and verify the value is displayed formatted as currency on the detail page
- Navigate to an InProgress work order, enter $310.75 for actual cost, save, and verify the value is displayed
- Navigate to work order search and verify both cost columns display formatted currency values
