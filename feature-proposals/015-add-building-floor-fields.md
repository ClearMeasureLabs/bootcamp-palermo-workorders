## Why
The current RoomNumber field is insufficient for multi-building campuses. Separate Building and Floor fields enable better location tracking, filtering, and reporting across facilities with multiple structures and levels.

## What Changes
- Add `Building` nullable string property to the `WorkRequest` domain model in `src/Core/Model/`
- Add `Floor` nullable string property to the `WorkRequest` domain model in `src/Core/Model/`
- Update `DataContext` EF Core mapping to persist `Building` and `Floor` as nvarchar columns
- Add a new DbUp migration script to `src/Database/scripts/Update/` adding nullable `Building` and `Floor` columns to the `WorkRequest` table
- Update `WorkRequestManage` Blazor form to include text inputs for Building and Floor
- Update `WorkRequestSearch` page to include Building and Floor filter dropdowns (populated from distinct values in the database)
- Update `WorkRequestSearchQuery` to support filtering by building and floor

## Capabilities
### New Capabilities
- Users can specify a building name and floor when creating or editing a work request
- Users can filter work requests by building and floor on the search page
- Filter dropdowns are dynamically populated from existing work request data

### Modified Capabilities
- WorkRequestManage form includes new Building and Floor text input fields
- WorkRequestSearch results display Building and Floor columns and support filtering

## Impact
- **Core** — `WorkRequest` model gains nullable `Building` and `Floor` string properties
- **DataAccess** — EF Core mapping update for `Building` and `Floor` columns; search handler updated for new filters
- **UI.Shared** — `WorkRequestManage` form updated with two new fields; `WorkRequestSearch` page updated with filters and columns
- **Database** — New migration script adding nullable `Building` and `Floor` columns to `WorkRequest` table

## Acceptance Criteria
### Unit Tests
- `WorkRequest_Building_ShouldDefaultToNull` — verify new work requests have no building by default
- `WorkRequest_Floor_ShouldDefaultToNull` — verify new work requests have no floor by default
- `WorkRequestManage_ShouldRenderBuildingInput` — bUnit test verifying Building text input appears on the form
- `WorkRequestManage_ShouldRenderFloorInput` — bUnit test verifying Floor text input appears on the form

### Integration Tests
- `WorkRequest_WithBuildingAndFloor_ShouldPersistAndRetrieve` — save a work request with Building "Main Hall" and Floor "2nd" and verify both round-trip through the database
- `WorkRequestSearchQuery_FilterByBuilding_ShouldReturnMatchingResults` — verify filtering by building returns only matching work requests
- `WorkRequestSearchQuery_FilterByFloor_ShouldReturnMatchingResults` — verify filtering by floor returns only matching work requests

### Acceptance Tests
- Navigate to create work request form, enter "Science Building" for Building and "3rd Floor" for Floor, save, and verify both values are displayed on the work request detail page
- Navigate to work request search, filter by Building "Science Building", and verify only work requests in that building appear
