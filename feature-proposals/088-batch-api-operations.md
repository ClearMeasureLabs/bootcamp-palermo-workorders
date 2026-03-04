## Why
Integration partners often need to create or update multiple work orders in a single transaction, such as importing from an external maintenance system or bulk-reassigning during shift changes. A batch API endpoint reduces HTTP overhead, simplifies client code, and enables atomic multi-operation requests.

## What Changes
- Add `BatchOperationRequest` record in `src/UI/Api/Models/` containing an array of `BatchOperationItem` (each with OperationType, WorkOrderNumber, and payload)
- Add `BatchOperationResult` record with per-item results: Index, Success, StatusCode, ErrorMessage, WorkOrderNumber
- Add `BatchController` in `src/UI/Api/` with endpoint `POST /api/batch` accepting `BatchOperationRequest`
- Support operation types: `Create` (creates draft work orders) and `UpdateStatus` (transitions work order status)
- Execute each operation via `IBus` using existing commands; collect individual results
- Return 200 with array of per-item results even if some operations fail (partial success model)
- Add validation for maximum batch size (configurable, default 50 operations per request)

## Capabilities
### New Capabilities
- Batch create multiple draft work orders in a single API call
- Batch update statuses of multiple work orders in a single API call
- Per-item success/failure reporting with individual HTTP status codes and error messages
- Configurable maximum batch size to prevent abuse

### Modified Capabilities
- None

## Impact
- **src/UI/Api/** - New `BatchController`, new request/result DTOs
- **src/UI/Api/Models/** - New `BatchOperationRequest`, `BatchOperationItem`, `BatchOperationResult`
- **src/Core/** - No changes; reuses existing commands
- **src/DataAccess/** - No changes; reuses existing handlers
- **src/UI/Server/appsettings.json** - New `BatchOperations` configuration section with MaxBatchSize
- **Dependencies** - No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `Batch_CreateMultiple_ReturnsSuccessForEachItem` - Batch of 3 create operations returns 3 success results
- `Batch_PartialFailure_ReturnsIndividualStatuses` - Batch with one valid and one invalid operation returns mixed results
- `Batch_ExceedsMaxSize_Returns400` - Batch with more than configured max operations returns 400 Bad Request
- `Batch_EmptyArray_Returns400` - Empty operations array returns 400 Bad Request
- `Batch_UpdateStatus_ValidTransitions_ReturnsSuccess` - Batch status updates with valid transitions return success for each
- `BatchOperationResult_ContainsCorrectIndex` - Each result item's Index matches its position in the request array

### Integration Tests
- `Batch_CreateWorkOrders_AllPersistedInDatabase` - Send batch create, query database, verify all work orders exist
- `Batch_UpdateStatuses_AllTransitionsPersisted` - Send batch status updates, verify each work order has correct new status
- `Batch_PartialFailure_SuccessfulItemsStillPersisted` - Batch with mix of valid and invalid items persists valid items

### Acceptance Tests
- `BatchApi_CreateMultipleWorkOrders_AllVisibleInUI` - Send batch create via Playwright API context, navigate to work order list, verify all created work orders appear
- `BatchApi_UpdateStatuses_ReflectedInUI` - Batch update work order statuses, navigate to each work order, verify new statuses displayed
