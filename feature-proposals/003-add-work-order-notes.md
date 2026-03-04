## Why
Work orders lack a way to capture ongoing commentary. A notes/comments feature lets creators and assignees document progress, decisions, and issues over the lifecycle of a work order. This creates a communication trail that reduces miscommunication and provides context for future reference.

## What Changes
- Add `WorkOrderNote` entity to `src/Core/Model/` with properties: Id (Guid), WorkOrderId (Guid), AuthorId (Guid), Text (string), CreatedDate (DateTime)
- Add `IWorkOrderNote` interface if needed for abstraction
- Add navigation property `Notes` (ICollection<WorkOrderNote>) to `WorkOrder` domain model
- Add `AddNoteCommand` to `src/Core/Model/StateCommands/` containing WorkOrderId, AuthorId, and Text
- Add `WorkOrderNotesQuery` to `src/Core/Queries/` to retrieve notes for a given work order
- Add EF Core mapping for `WorkOrderNote` in DataAccess
- Add handler for `AddNoteCommand` in `src/DataAccess/Handlers/`
- Add handler for `WorkOrderNotesQuery` in `src/DataAccess/Handlers/`
- Add a new DbUp migration script creating the `WorkOrderNote` table with foreign keys
- Add a notes section to the `WorkOrderManage` page displaying existing notes and a text input for adding new ones

## Capabilities
### New Capabilities
- Users can add text notes to any work order regardless of status
- Users can view the chronological history of all notes on a work order
- Each note records the author and timestamp automatically

### Modified Capabilities
- WorkOrderManage page includes a new notes section below the main form

## Impact
- **Core** — New `WorkOrderNote` entity; new `AddNoteCommand`; new `WorkOrderNotesQuery`
- **DataAccess** — EF Core mapping for `WorkOrderNote` table; two new MediatR handlers
- **UI.Shared** — Notes section component on `WorkOrderManage` page
- **Database** — New migration script creating `WorkOrderNote` table with FK to `WorkOrder` and `Employee`

## Acceptance Criteria
### Unit Tests
- `WorkOrderNote_ShouldRequireText` — verify that a note with empty text is rejected
- `WorkOrderNote_ShouldSetCreatedDate` — verify CreatedDate is set on creation
- `AddNoteCommand_ShouldAddNoteToWorkOrder` — verify command execution adds a note to the work order's collection
- `WorkOrderManage_ShouldRenderNotesSection` — bUnit test verifying the notes section renders with existing notes

### Integration Tests
- `AddNoteCommand_ShouldPersistNote` — add a note via the command and verify it is persisted in the database
- `WorkOrderNotesQuery_ShouldReturnNotesInChronologicalOrder` — add multiple notes and verify they are returned oldest-first
- `WorkOrderNotesQuery_ShouldReturnEmptyForWorkOrderWithNoNotes` — verify empty collection for work orders without notes

### Acceptance Tests
- Navigate to an existing work order, type a note in the text input, submit, and verify the note appears in the notes list with author name and timestamp
- Add multiple notes to a work order and verify they display in chronological order
