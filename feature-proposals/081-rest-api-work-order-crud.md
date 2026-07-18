## Why
External systems and mobile clients need programmatic access to work requests beyond what the Blazor UI provides. A standard RESTful API with GET/POST/PUT endpoints enables third-party integrations and supports future native app development without coupling to the existing single-endpoint pattern.

## What Changes
- Add `WorkRequestApiController` in `src/UI/Api/` with REST routes: `GET /api/workrequests`, `GET /api/workrequests/{number}`, `POST /api/workrequests`, `PUT /api/workrequests/{number}`
- Add `WorkRequestDto` and `CreateWorkRequestRequest`/`UpdateWorkRequestRequest` DTOs in `src/UI/Api/Models/`
- Add mapping logic between DTOs and domain commands/queries
- Wire GET endpoints to existing `AllWorkRequestsQuery` and `WorkRequestByNumberQuery` via `IBus`
- Wire POST to `SaveDraftCommand` and PUT to relevant state commands via `IBus`
- Return standard HTTP status codes: 200 OK, 201 Created, 400 Bad Request, 404 Not Found

## Capabilities
### New Capabilities
- Retrieve all work requests via `GET /api/workrequests` with JSON response
- Retrieve a single work request by number via `GET /api/workrequests/{number}`
- Create a new draft work request via `POST /api/workrequests`
- Update an existing work request via `PUT /api/workrequests/{number}`

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
- `Get_AllWorkRequests_ReturnsOkWithList` - GET returns 200 with list of work request DTOs
- `Get_ByNumber_ExistingWorkRequest_ReturnsOkWithDto` - GET by number returns 200 with correct DTO
- `Get_ByNumber_NonExistent_ReturnsNotFound` - GET by number returns 404 when not found
- `Post_ValidRequest_ReturnsCreatedWithLocation` - POST returns 201 with Location header
- `Post_InvalidRequest_ReturnsBadRequest` - POST with missing Title returns 400
- `Put_ExistingWorkRequest_ReturnsOk` - PUT with valid update returns 200
- `Put_NonExistent_ReturnsNotFound` - PUT for unknown number returns 404

### Integration Tests
- `CreateAndRetrieveWorkRequest_RoundTrips` - POST a work request, then GET it back and verify all fields match
- `UpdateWorkRequest_PersistsChanges` - PUT updated fields, then GET and verify persistence
- `GetAllWorkRequests_ReturnsPersistedRecords` - Seed multiple work requests, GET all, verify count and content

### Acceptance Tests
- `RestApi_CreateWorkRequest_ReturnsCreatedStatus` - Send POST via Playwright API context, verify 201 response and JSON body
- `RestApi_GetWorkRequestByNumber_ReturnsCorrectData` - Create a work request, then GET by number and verify response fields
- `RestApi_UpdateWorkRequest_ReflectsChangesInUI` - Update via PUT, navigate to work request in Blazor UI, verify changes appear
