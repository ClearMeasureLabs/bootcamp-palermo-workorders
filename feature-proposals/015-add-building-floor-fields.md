## Why
The current RoomNumber field is insufficient for multi-building campuses. Separate Building and Floor fields enable better location tracking, filtering, and reporting across facilities with multiple structures and levels.

## What Changes
- Add `Building` nullable string property to the `WorkOrder` domain model in `src/Core/Model/`
- Add `Floor` nullable string property to the `WorkOrder` domain model in `src/Core/Model/`
- Update `DataContext` EF Core mapping to persist `Building` and `Floor` as nvarchar columns
- Add a new DbUp migration script to `src/Database/scripts/Update/` adding nullable `Building` and `Floor` columns to the `WorkOrder` table
- Update `WorkOrderManage` Blazor form to include text inputs for Building and Floor
- Update `WorkOrderSearch` page to include Building and Floor filter dropdowns (populated from distinct values in the database)
- Update `WorkOrderSearchQuery` to support filtering by building and floor

## Capabilities
### New Capabilities
- Users can specify a building name and floor when creating or editing a work order
- Users can filter work orders by building and floor on the search page
- Filter dropdowns are dynamically populated from existing work order data

### Modified Capabilities
- WorkOrderManage form includes new Building and Floor text input fields
- WorkOrderSearch results display Building and Floor columns and support filtering

## Impact
- **Core** — `WorkOrder` model gains nullable `Building` and `Floor` string properties
- **DataAccess** — EF Core mapping update for `Building` and `Floor` columns; search handler updated for new filters
- **UI.Shared** — `WorkOrderManage` form updated with two new fields; `WorkOrderSearch` page updated with filters and columns
- **Database** — New migration script adding nullable `Building` and `Floor` columns to `WorkOrder` table

## Acceptance Criteria
### Unit Tests
- `WorkOrder_Building_ShouldDefaultToNull` — verify new work orders have no building by default
- `WorkOrder_Floor_ShouldDefaultToNull` — verify new work orders have no floor by default
- `WorkOrderManage_ShouldRenderBuildingInput` — bUnit test verifying Building text input appears on the form
- `WorkOrderManage_ShouldRenderFloorInput` — bUnit test verifying Floor text input appears on the form

### Integration Tests
- `WorkOrder_WithBuildingAndFloor_ShouldPersistAndRetrieve` — save a work order with Building "Main Hall" and Floor "2nd" and verify both round-trip through the database
- `WorkOrderSearchQuery_FilterByBuilding_ShouldReturnMatchingResults` — verify filtering by building returns only matching work orders
- `WorkOrderSearchQuery_FilterByFloor_ShouldReturnMatchingResults` — verify filtering by floor returns only matching work orders

### Acceptance Tests
- Navigate to create work order form, enter "Science Building" for Building and "3rd Floor" for Floor, save, and verify both values are displayed on the work order detail page
- Navigate to work order search, filter by Building "Science Building", and verify only work orders in that building appear
