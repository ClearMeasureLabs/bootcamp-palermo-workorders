## Why
Loading spinners provide no sense of what content is coming, leaving users staring at a blank page. Skeleton placeholders that match the shape of the actual content give users a preview of the page structure, reducing perceived load time and creating a smoother experience.

## What Changes
- Add `SkeletonLine.razor` component in `src/UI.Shared/Components/` rendering a rectangular animated placeholder matching the width and height of a text line
- Add `SkeletonTable.razor` component in `src/UI.Shared/Components/` rendering a table-shaped skeleton with header row and configurable number of body rows
- Add `SkeletonForm.razor` component in `src/UI.Shared/Components/` rendering form-shaped skeleton with label and input placeholders
- Modify `WorkOrderSearch.razor` in `src/UI/Client/Pages/` to show `SkeletonTable` while search results are loading instead of a spinner
- Modify `WorkOrderManage.razor` in `src/UI/Client/Pages/` to show `SkeletonForm` while work order data is loading instead of a spinner
- Add CSS keyframe animation for the skeleton shimmer effect (light gradient moving left to right)
- Add CSS classes for skeleton widths: `skeleton-short` (30%), `skeleton-medium` (60%), `skeleton-full` (100%)
- Remove existing spinner/loading indicators from WorkOrderSearch and WorkOrderManage pages

## Capabilities
### New Capabilities
- Skeleton placeholder animations on WorkOrderSearch page during data load
- Skeleton placeholder animations on WorkOrderManage page during data load
- Shimmer animation effect on skeleton elements
- Content-shaped placeholders matching the layout of tables and forms
- Configurable skeleton row count and field count

### Modified Capabilities
- WorkOrderSearch replaces spinner with skeleton table during loading
- WorkOrderManage replaces spinner with skeleton form during loading

## Impact
- **UI.Shared**: New `SkeletonLine.razor`, `SkeletonTable.razor`, and `SkeletonForm.razor` components
- **UI.Client**: Modified `WorkOrderSearch.razor` and `WorkOrderManage.razor`, new CSS animation styles
- **Database**: No schema changes required
- **Dependencies**: No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `SkeletonTable_RendersCorrectNumberOfRows` - bUnit test confirming skeleton renders the specified number of placeholder rows
- `SkeletonForm_RendersCorrectNumberOfFields` - bUnit test confirming skeleton renders the specified number of label/input pairs
- `SkeletonLine_RendersWithCorrectWidthClass` - bUnit test confirming skeleton line applies the requested width CSS class

### Integration Tests
- None (client-side UI component with no server/database interaction)

### Acceptance Tests
- Navigate to WorkOrderSearch and verify the skeleton table appears with `data-testid="skeleton-table"` before results load, then is replaced by actual data
- Navigate to WorkOrderManage for an existing work order and verify the skeleton form appears with `data-testid="skeleton-form"` before data loads, then is replaced by the actual form
- Verify the skeleton elements have the shimmer animation CSS class with `data-testid="skeleton-line"`
