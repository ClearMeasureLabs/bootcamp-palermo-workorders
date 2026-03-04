## Why
Free-text room number entry leads to inconsistent data (e.g., "101", "Room 101", "Rm-101" all referring to the same location). A dropdown populated from a managed Room reference table ensures data consistency, enables accurate reporting by location, and reduces errors during work order creation.

## What Changes
- Add `Room` entity in `src/Core/Model/` with properties: Id (int), Number (string), Building (string), Floor (int)
- Add `RoomConfiguration` EF Core entity configuration in `src/DataAccess/`
- Add `DbSet<Room>` to `DataContext`
- Add database migration script in `src/Database/scripts/Update/` to create `Room` table and seed initial room data
- Add `AllRoomsQuery` in `src/Core/Queries/` and `AllRoomsHandler` in `src/DataAccess/Handlers/`
- Replace free-text `RoomNumber` input with a `<select>` dropdown on the work order create and edit forms in `src/UI/Client/Pages/`
- Populate dropdown from `AllRoomsQuery` results, displaying "Building - Floor - Room Number"
- Update `WorkOrder.RoomNumber` to reference `Room.Number` (keep as string for backward compatibility)
- Add `RoomApiController` in `src/UI/Api/` with `GET /api/rooms` endpoint

## Capabilities
### New Capabilities
- Managed Room reference table with Building, Floor, and Room Number
- Room dropdown on work order create and edit forms replacing free-text input
- Rooms API endpoint for programmatic access to room list
- Room display format: "Building - Floor N - Room Number"

### Modified Capabilities
- Work order create and edit forms updated to use dropdown instead of text input for RoomNumber

## Impact
- **src/Core/Model/** - New `Room` entity
- **src/Core/Queries/** - New `AllRoomsQuery`
- **src/DataAccess/** - New entity configuration, new handler, updated `DataContext`
- **src/Database/** - New migration script creating `Room` table with seed data
- **src/UI/Client/Pages/** - Work order forms updated to use room dropdown
- **src/UI/Api/** - New `RoomApiController`
- **Dependencies** - No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `AllRoomsQuery_ReturnsAllRooms` - Query returns complete list of rooms from database
- `Room_DisplayFormat_ShowsBuildingFloorNumber` - Room display string formatted as "Building A - Floor 2 - 201"
- `CreateForm_RoomDropdown_PopulatedWithRooms` - bUnit render verifies dropdown contains room options
- `CreateForm_RoomDropdown_NoFreeTextAllowed` - bUnit render verifies input is select element, not text input
- `WorkOrder_RoomNumber_MatchesSelectedRoom` - Selecting room from dropdown sets correct RoomNumber value on work order

### Integration Tests
- `Room_SeededData_AccessibleViaQuery` - After migration, AllRoomsQuery returns seeded rooms
- `WorkOrder_WithRoomFromDropdown_PersistsCorrectRoomNumber` - Create work order selecting room from dropdown, verify persisted RoomNumber matches
- `RoomApi_ReturnsAllRooms_AsJson` - GET `/api/rooms` returns complete room list as JSON

### Acceptance Tests
- `CreateWorkOrder_RoomDropdown_ShowsAvailableRooms` - Log in, navigate to create form, click room dropdown, verify room options are displayed
- `CreateWorkOrder_SelectRoom_WorkOrderShowsCorrectRoom` - Select a room from dropdown, submit form, navigate to work order detail, verify room number displayed
- `EditWorkOrder_RoomDropdown_ShowsCurrentSelection` - Navigate to edit form for existing work order, verify dropdown shows currently assigned room as selected
