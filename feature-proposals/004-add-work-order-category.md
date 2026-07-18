## Why
Categorizing work requests (Maintenance, Cleaning, Electrical, Plumbing, IT, Other) helps organize workload and enables category-based reporting and assignment. Categories provide a standard taxonomy for the types of work being performed across the organization.

## What Changes
- Add `WorkRequestCategory` smart enum to `src/Core/Model/` with values: Maintenance, Cleaning, Electrical, Plumbing, IT, Other (following the `WorkRequestStatus` pattern with Key/Code/Name)
- Add `Category` property of type `WorkRequestCategory` to the `WorkRequest` domain model
- Update `DataContext` EF Core mapping to persist `Category` as an integer column with a value converter
- Add a new DbUp migration script to `src/Database/scripts/Update/` adding a `Category` column (int, NOT NULL, default to Other) to the `WorkRequest` table
- Update `WorkRequestManage` Blazor form to include a Category dropdown selector
- Update `WorkRequestSearch` page to include a Category filter dropdown
- Update `WorkRequestSearchQuery` to support filtering by category

## Capabilities
### New Capabilities
- Users can assign a category (Maintenance, Cleaning, Electrical, Plumbing, IT, Other) when creating or editing a work request
- Users can filter work requests by category on the search page

### Modified Capabilities
- WorkRequestManage form includes a new Category dropdown field
- WorkRequestSearch results display the category and support category filtering

## Impact
- **Core** — New `WorkRequestCategory` smart enum class; `WorkRequest` model gains `Category` property
- **DataAccess** — EF Core mapping update with value converter for `Category` column; search handler updated for category filter
- **UI.Shared** — `WorkRequestManage` form updated with dropdown; `WorkRequestSearch` page updated with filter and display column
- **Database** — New migration script adding `Category` column to `WorkRequest` table

## Acceptance Criteria
### Unit Tests
- `WorkRequestCategory_FromCode_ShouldReturnCorrectEnum` — verify all six category codes resolve to correct enum values
- `WorkRequestCategory_FromKey_ShouldReturnCorrectEnum` — verify key-based lookup works
- `WorkRequest_ShouldDefaultCategory_ToOther` — verify new work requests default to Other category
- `WorkRequestManage_ShouldRenderCategoryDropdown` — bUnit test verifying dropdown appears with all category options

### Integration Tests
- `WorkRequest_WithCategory_ShouldPersistAndRetrieve` — save a work request with Electrical category and verify it round-trips through the database
- `WorkRequestSearchQuery_FilterByCategory_ShouldReturnMatchingResults` — verify search filtering returns only work requests with the specified category

### Acceptance Tests
- Navigate to create work request form, select "Plumbing" category, save, and verify the category is displayed on the work request detail page
- Navigate to work request search, filter by "Electrical" category, and verify only electrical work requests appear in results
