## Why
Categorizing work orders (Maintenance, Cleaning, Electrical, Plumbing, IT, Other) helps organize workload and enables category-based reporting and assignment. Categories provide a standard taxonomy for the types of work being performed across the organization.

## What Changes
- Add `WorkOrderCategory` smart enum to `src/Core/Model/` with values: Maintenance, Cleaning, Electrical, Plumbing, IT, Other (following the `WorkOrderStatus` pattern with Key/Code/Name)
- Add `Category` property of type `WorkOrderCategory` to the `WorkOrder` domain model
- Update `DataContext` EF Core mapping to persist `Category` as an integer column with a value converter
- Add a new DbUp migration script to `src/Database/scripts/Update/` adding a `Category` column (int, NOT NULL, default to Other) to the `WorkOrder` table
- Update `WorkOrderManage` Blazor form to include a Category dropdown selector
- Update `WorkOrderSearch` page to include a Category filter dropdown
- Update `WorkOrderSearchQuery` to support filtering by category

## Capabilities
### New Capabilities
- Users can assign a category (Maintenance, Cleaning, Electrical, Plumbing, IT, Other) when creating or editing a work order
- Users can filter work orders by category on the search page

### Modified Capabilities
- WorkOrderManage form includes a new Category dropdown field
- WorkOrderSearch results display the category and support category filtering

## Impact
- **Core** — New `WorkOrderCategory` smart enum class; `WorkOrder` model gains `Category` property
- **DataAccess** — EF Core mapping update with value converter for `Category` column; search handler updated for category filter
- **UI.Shared** — `WorkOrderManage` form updated with dropdown; `WorkOrderSearch` page updated with filter and display column
- **Database** — New migration script adding `Category` column to `WorkOrder` table

## Acceptance Criteria
### Unit Tests
- `WorkOrderCategory_FromCode_ShouldReturnCorrectEnum` — verify all six category codes resolve to correct enum values
- `WorkOrderCategory_FromKey_ShouldReturnCorrectEnum` — verify key-based lookup works
- `WorkOrder_ShouldDefaultCategory_ToOther` — verify new work orders default to Other category
- `WorkOrderManage_ShouldRenderCategoryDropdown` — bUnit test verifying dropdown appears with all category options

### Integration Tests
- `WorkOrder_WithCategory_ShouldPersistAndRetrieve` — save a work order with Electrical category and verify it round-trips through the database
- `WorkOrderSearchQuery_FilterByCategory_ShouldReturnMatchingResults` — verify search filtering returns only work orders with the specified category

### Acceptance Tests
- Navigate to create work order form, select "Plumbing" category, save, and verify the category is displayed on the work order detail page
- Navigate to work order search, filter by "Electrical" category, and verify only electrical work orders appear in results
