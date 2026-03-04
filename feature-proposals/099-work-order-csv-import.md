## Why
Facility managers migrating from spreadsheet-based tracking or receiving bulk maintenance requests from building inspections need to import multiple work orders at once. CSV import eliminates tedious one-by-one data entry and enables batch onboarding of historical or external work order data.

## What Changes
- Add `CsvImportCommand` in `src/Core/Commands/` accepting a stream of CSV data
- Add `CsvImportResult` record in `src/Core/Commands/` with properties: TotalRows, SuccessCount, FailureCount, Errors (list of row-level error details)
- Add `CsvImportHandler` in `src/DataAccess/Handlers/` that parses CSV, validates each row, and creates draft work orders for valid rows
- Expected CSV columns: Title, Description, RoomNumber, CreatorUsername (header row required)
- Validate that CreatorUsername references an existing Employee with CanCreateWorkOrder permission
- Add file upload component on the work order list page in `src/UI/Client/Pages/` with "Import CSV" button
- Add `CsvImportController` in `src/UI/Api/` with `POST /api/workorders/import` accepting multipart file upload
- Return import results showing success count, failure count, and per-row error details
- Limit file size to 5MB and maximum 500 rows per import

## Capabilities
### New Capabilities
- CSV file upload via UI "Import CSV" button on work order list page
- API endpoint for programmatic CSV import via multipart file upload
- Row-by-row validation with detailed error reporting per failed row
- Bulk creation of draft work orders from valid CSV rows
- File size limit (5MB) and row count limit (500) enforcement

### Modified Capabilities
- None

## Impact
- **src/Core/Commands/** - New `CsvImportCommand` and `CsvImportResult`
- **src/DataAccess/Handlers/** - New `CsvImportHandler` with CSV parsing and validation logic
- **src/UI/Client/Pages/** - File upload component and "Import CSV" button added to work order list page
- **src/UI/Api/** - New `CsvImportController`
- **Dependencies** - No new NuGet packages required; CSV parsing should use `Microsoft.VisualBasic.FileIO.TextFieldParser` (included in .NET runtime) for robust handling of quoted fields, embedded commas, and newlines — naive string splitting is insufficient for production use
- **Database** - No schema changes; creates work orders using existing schema

## Acceptance Criteria
### Unit Tests
- `CsvImport_ValidFile_CreatesWorkOrdersForEachRow` - CSV with 5 valid rows creates 5 draft work orders
- `CsvImport_MissingTitle_ReportsRowError` - Row without Title field is skipped with error detail
- `CsvImport_InvalidCreatorUsername_ReportsRowError` - Row with non-existent CreatorUsername reports validation error
- `CsvImport_CreatorWithoutPermission_ReportsRowError` - Row with creator lacking CanCreateWorkOrder role reports error
- `CsvImport_EmptyFile_ReturnsZeroCounts` - Empty CSV (header only) returns TotalRows=0, SuccessCount=0
- `CsvImport_ExceedsRowLimit_ReturnsError` - CSV with 501 rows returns error indicating row limit exceeded
- `CsvImport_MissingHeaders_ReturnsError` - CSV without required header columns returns format error
- `CsvImportResult_PartialSuccess_ReportsCorrectCounts` - CSV with 3 valid and 2 invalid rows returns SuccessCount=3, FailureCount=2

### Integration Tests
- `CsvImport_ValidFile_WorkOrdersPersistedInDatabase` - Import valid CSV, query database, verify all work orders exist with correct data
- `CsvImport_AllCreatedAsDraft_StatusIsDraft` - Import CSV, verify all created work orders have Draft status
- `CsvImport_PartiallyValid_OnlyValidRowsPersisted` - Import CSV with mix of valid and invalid rows, verify only valid rows create records

### Acceptance Tests
- `CsvImport_UploadFile_ShowsImportResults` - Log in, navigate to work order list, click "Import CSV", upload valid file, verify success count displayed
- `CsvImport_InvalidRows_ShowsErrorDetails` - Upload CSV with invalid rows, verify error details displayed per failed row
- `CsvImport_ImportedWorkOrders_AppearInList` - Upload valid CSV, verify imported work orders appear in the work order list
