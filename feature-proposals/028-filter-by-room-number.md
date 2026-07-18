## Why
Facility staff often need to see all work orders for a specific room to understand maintenance history and current issues in that location. A room number filter enables location-based work order management and helps prioritize room-specific maintenance.

## What Changes
- Add a `RoomNumber` filter property to `WorkOrderSpecificationQuery` in `src/Core/Queries/`
- Add a `DistinctRoomNumbersQuery` in `src/Core/Queries/` to retrieve all unique room numbers
- Add a MediatR handler in `src/DataAccess/Handlers/` for the distinct room numbers query
- Update the work order query handler in `src/DataAccess/Handlers/` to filter by RoomNumber
- Add a room number dropdown filter to the `WorkOrderSearch` page in `src/UI/Client/`, populated from the distinct room numbers query
- Add an API endpoint in `src/UI/Api/` for retrieving distinct room numbers

## Capabilities
### New Capabilities
- Room number dropdown filter on the WorkOrderSearch page
- Dropdown populated with distinct room numbers from existing work orders
- Filter search results to show only work orders for the selected room

### Modified Capabilities
- WorkOrderSearch page layout includes a room number dropdown filter
- WorkOrderSpecificationQuery supports an optional RoomNumber filter

## Impact
- `src/Core/Queries/WorkOrderSpecificationQuery.cs` — new RoomNumber property
- `src/Core/Queries/` — new DistinctRoomNumbersQuery
- `src/DataAccess/Handlers/` — new handler for DistinctRoomNumbersQuery, updated work order query handler
- `src/UI/Client/` — updated WorkOrderSearch page with room number dropdown
- `src/UI/Api/` — new endpoint for distinct room numbers
- No database migration required
- No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- DistinctRoomNumbersQuery handler returns unique room numbers sorted alphabetically
- Work order query handler filters results by the specified RoomNumber
- Null or empty RoomNumber filter returns all work orders

### Integration Tests
- DistinctRoomNumbersQuery returns correct unique room numbers from a seeded database
- Filtering by RoomNumber returns only work orders for that room

### Acceptance Tests
- User opens the room number dropdown and sees all distinct room numbers
- User selects a room number and search results filter to show only work orders for that room
- User clears the room number filter and all work orders are shown again
