## Why
Facility managers and administrators need to export work order data for offline reporting, sharing with stakeholders, and analysis in spreadsheet tools. A CSV export provides a simple, universally compatible format for this purpose.

## What Changes
- Add an `ExportWorkOrdersQuery` in `src/Core/Queries/` that returns a CSV-formatted byte array
- Add a MediatR handler in `src/DataAccess/Handlers/` that queries work orders using the current filters and formats them as CSV
- Add an API endpoint in `src/UI/Api/` that returns the CSV file as a downloadable response with appropriate content headers
- Add an "Export CSV" button to the `WorkOrderSearch` page in `src/UI/Client/`
- Use JavaScript interop in the Blazor client to trigger the file download

## Capabilities
### New Capabilities
- Export current search results to a CSV file
- CSV includes all displayed columns: Number, Title, Description, RoomNumber, Status, Creator, Assignee, CreatedDate, AssignedDate, CompletedDate
- Export respects the currently applied search filters
- File download triggered from the browser

### Modified Capabilities
- WorkOrderSearch page includes an "Export CSV" button in the toolbar area

## Impact
- `src/Core/Queries/` — new ExportWorkOrdersQuery
- `src/DataAccess/Handlers/` — new handler for CSV generation
- `src/UI/Api/` — new API endpoint returning file download response
- `src/UI/Client/` — updated WorkOrderSearch page with Export button and JS interop for download
- No database migration required
- No new NuGet packages required — CSV generation uses manual string building with proper escaping

## Acceptance Criteria
### Unit Tests
- CSV handler produces correct header row with all column names
- CSV handler produces correct data rows matching work order fields
- Special characters (commas, quotes, newlines) in fields are properly escaped
- Empty search results produce a CSV with only the header row
- Export respects the same filters as the current search

### Integration Tests
- Export query returns CSV bytes containing all matching work orders from a seeded database
- CSV content matches the expected format and column order

### Acceptance Tests
- User clicks "Export CSV" and a file downloads with a .csv extension
- Downloaded CSV contains the same work orders displayed on the search page
- CSV opens correctly in a spreadsheet application with proper column alignment
