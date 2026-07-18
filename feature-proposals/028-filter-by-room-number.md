## Why
Facility staff often need to see all work requests for a specific room to understand maintenance history and current issues in that location. A room number filter enables location-based work request management and helps prioritize room-specific maintenance.

## What Changes
- Add a `RoomNumber` filter property to `WorkRequestSpecificationQuery` in `src/Core/Queries/`
- Add a `DistinctRoomNumbersQuery` in `src/Core/Queries/` to retrieve all unique room numbers
- Add a MediatR handler in `src/DataAccess/Handlers/` for the distinct room numbers query
- Update the work request query handler in `src/DataAccess/Handlers/` to filter by RoomNumber
- Add a room number dropdown filter to the `WorkRequestSearch` page in `src/UI/Client/`, populated from the distinct room numbers query
- Add an API endpoint in `src/UI/Api/` for retrieving distinct room numbers

## Capabilities
### New Capabilities
- Room number dropdown filter on the WorkRequestSearch page
- Dropdown populated with distinct room numbers from existing work requests
- Filter search results to show only work requests for the selected room

### Modified Capabilities
- WorkRequestSearch page layout includes a room number dropdown filter
- WorkRequestSpecificationQuery supports an optional RoomNumber filter

## Impact
- `src/Core/Queries/WorkRequestSpecificationQuery.cs` — new RoomNumber property
- `src/Core/Queries/` — new DistinctRoomNumbersQuery
- `src/DataAccess/Handlers/` — new handler for DistinctRoomNumbersQuery, updated work request query handler
- `src/UI/Client/` — updated WorkRequestSearch page with room number dropdown
- `src/UI/Api/` — new endpoint for distinct room numbers
- No database migration required
- No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- DistinctRoomNumbersQuery handler returns unique room numbers sorted alphabetically
- Work request query handler filters results by the specified RoomNumber
- Null or empty RoomNumber filter returns all work requests

### Integration Tests
- DistinctRoomNumbersQuery returns correct unique room numbers from a seeded database
- Filtering by RoomNumber returns only work requests for that room

### Acceptance Tests
- User opens the room number dropdown and sees all distinct room numbers
- User selects a room number and search results filter to show only work requests for that room
- User clears the room number filter and all work requests are shown again
