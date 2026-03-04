## Why
External systems and mobile clients need programmatic access to work orders beyond what the Blazor UI provides. A standard RESTful API with GET/POST/PUT endpoints enables third-party integrations and supports future native app development without coupling to the existing single-endpoint pattern.

## What Changes
- Add `WorkOrderApiController` in `src/UI/Api/` with REST routes: `GET /api/workorders`, `GET /api/workorders/{number}`, `POST /api/workorders`, `PUT /api/workorders/{number}`
- Add `WorkOrderDto` and `CreateWorkOrderRequest`/`UpdateWorkOrderRequest` DTOs in `src/UI/Api/Models/`
- Add mapping logic between DTOs and domain commands/queries
- Wire GET endpoints to existing `AllWorkOrdersQuery` and `WorkOrderByNumberQuery` via `IBus`
- Wire POST to `SaveDraftCommand` and PUT to relevant state commands via `IBus`
- Return standard HTTP status codes: 200 OK, 201 Created, 400 Bad Request, 404 Not Found

## Capabilities
### New Capabilities
- Retrieve all work orders via `GET /api/workorders` with JSON response
- Retrieve a single work order by number via `GET /api/workorders/{number}`
- Create a new draft work order via `POST /api/workorders`
- Update an existing work order via `PUT /api/workorders/{number}`

### Modified Capabilities
- None

## Impact
- **src/UI/Api/** - New controller and DTO classes added
- **src/UI/Server/** - Register new API routes in startup pipeline
- **src/Core/** - No changes; reuses existing queries and commands
- **src/DataAccess/** - No changes; reuses existing handlers
- **Dependencies** - No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `Get_AllWorkOrders_ReturnsOkWithList` - GET returns 200 with list of work order DTOs
- `Get_ByNumber_ExistingWorkOrder_ReturnsOkWithDto` - GET by number returns 200 with correct DTO
- `Get_ByNumber_NonExistent_ReturnsNotFound` - GET by number returns 404 when not found
- `Post_ValidRequest_ReturnsCreatedWithLocation` - POST returns 201 with Location header
- `Post_InvalidRequest_ReturnsBadRequest` - POST with missing Title returns 400
- `Put_ExistingWorkOrder_ReturnsOk` - PUT with valid update returns 200
- `Put_NonExistent_ReturnsNotFound` - PUT for unknown number returns 404

### Integration Tests
- `CreateAndRetrieveWorkOrder_RoundTrips` - POST a work order, then GET it back and verify all fields match
- `UpdateWorkOrder_PersistsChanges` - PUT updated fields, then GET and verify persistence
- `GetAllWorkOrders_ReturnsPersistedRecords` - Seed multiple work orders, GET all, verify count and content

### Acceptance Tests
- `RestApi_CreateWorkOrder_ReturnsCreatedStatus` - Send POST via Playwright API context, verify 201 response and JSON body
- `RestApi_GetWorkOrderByNumber_ReturnsCorrectData` - Create a work order, then GET by number and verify response fields
- `RestApi_UpdateWorkOrder_ReflectsChangesInUI` - Update via PUT, navigate to work order in Blazor UI, verify changes appear
