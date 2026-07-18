## Why
Tracking material and labor costs per work request enables budget management and cost reporting across the organization. Cost visibility helps identify expensive recurring issues, supports budget forecasting, and provides accountability for expenditures.

## What Changes
- Add `EstimatedCost` nullable `decimal` property to the `WorkRequest` domain model in `src/Core/Model/`
- Add `ActualCost` nullable `decimal` property to the `WorkRequest` domain model in `src/Core/Model/`
- Update `DataContext` EF Core mapping to persist `EstimatedCost` and `ActualCost` as `decimal(10,2)` columns
- Add a new DbUp migration script to `src/Database/scripts/Update/` adding nullable `EstimatedCost` and `ActualCost` columns to the `WorkRequest` table
- Update `WorkRequestManage` Blazor form to include currency input fields for EstimatedCost (editable by creator in any status) and ActualCost (editable when InProgress or Complete)
- Update `WorkRequestSearch` results to display EstimatedCost and ActualCost columns
- Add domain validation: costs must be non-negative if provided

## Capabilities
### New Capabilities
- Users can enter an estimated cost when creating or editing a work request
- Users can record actual costs when the work request is InProgress or Complete
- Search results display cost information for each work request
- Cost values are formatted as currency in the UI

### Modified Capabilities
- WorkRequestManage form includes new EstimatedCost and ActualCost currency input fields
- WorkRequestSearch results table includes cost columns

## Impact
- **Core** — `WorkRequest` model gains nullable `EstimatedCost` and `ActualCost` decimal properties with non-negative validation
- **DataAccess** — EF Core mapping update for both cost columns
- **UI.Shared** — `WorkRequestManage` form updated with currency inputs; `WorkRequestSearch` results updated with cost columns
- **Database** — New migration script adding nullable `EstimatedCost` and `ActualCost` columns to `WorkRequest` table

## Acceptance Criteria
### Unit Tests
- `WorkRequest_EstimatedCost_ShouldDefaultToNull` — verify new work requests have no estimated cost by default
- `WorkRequest_ActualCost_ShouldDefaultToNull` — verify new work requests have no actual cost by default
- `WorkRequest_EstimatedCost_ShouldRejectNegativeValues` — verify validation rejects negative cost values
- `WorkRequest_ActualCost_ShouldRejectNegativeValues` — verify validation rejects negative cost values
- `WorkRequestManage_ShouldRenderEstimatedCostInput` — bUnit test verifying estimated cost input appears
- `WorkRequestManage_ShouldDisableActualCostInput_WhenDraft` — bUnit test verifying actual cost input is disabled for Draft work requests

### Integration Tests
- `WorkRequest_WithCosts_ShouldPersistAndRetrieve` — save a work request with EstimatedCost of 150.50 and ActualCost of 175.25, verify both round-trip through the database
- `WorkRequest_WithNullCosts_ShouldPersistAndRetrieve` — save a work request without costs and verify nulls are persisted

### Acceptance Tests
- Navigate to create work request form, enter $250.00 for estimated cost, save, and verify the value is displayed formatted as currency on the detail page
- Navigate to an InProgress work request, enter $310.75 for actual cost, save, and verify the value is displayed
- Navigate to work request search and verify both cost columns display formatted currency values
