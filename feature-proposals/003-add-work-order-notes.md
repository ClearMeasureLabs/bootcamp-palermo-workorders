## Why
Work requests lack a way to capture ongoing commentary. A notes/comments feature lets creators and assignees document progress, decisions, and issues over the lifecycle of a work request. This creates a communication trail that reduces miscommunication and provides context for future reference.

## What Changes
- Add `WorkRequestNote` entity to `src/Core/Model/` with properties: Id (Guid), WorkRequestId (Guid), AuthorId (Guid), Text (string), CreatedDate (DateTime)
- Add `IWorkRequestNote` interface if needed for abstraction
- Add navigation property `Notes` (ICollection<WorkRequestNote>) to `WorkRequest` domain model
- Add `AddNoteCommand` to `src/Core/Model/StateCommands/` containing WorkRequestId, AuthorId, and Text
- Add `WorkRequestNotesQuery` to `src/Core/Queries/` to retrieve notes for a given work request
- Add EF Core mapping for `WorkRequestNote` in DataAccess
- Add handler for `AddNoteCommand` in `src/DataAccess/Handlers/`
- Add handler for `WorkRequestNotesQuery` in `src/DataAccess/Handlers/`
- Add a new DbUp migration script creating the `WorkRequestNote` table with foreign keys
- Add a notes section to the `WorkRequestManage` page displaying existing notes and a text input for adding new ones

## Capabilities
### New Capabilities
- Users can add text notes to any work request regardless of status
- Users can view the chronological history of all notes on a work request
- Each note records the author and timestamp automatically

### Modified Capabilities
- WorkRequestManage page includes a new notes section below the main form

## Impact
- **Core** — New `WorkRequestNote` entity; new `AddNoteCommand`; new `WorkRequestNotesQuery`
- **DataAccess** — EF Core mapping for `WorkRequestNote` table; two new MediatR handlers
- **UI.Shared** — Notes section component on `WorkRequestManage` page
- **Database** — New migration script creating `WorkRequestNote` table with FK to `WorkRequest` and `Employee`

## Acceptance Criteria
### Unit Tests
- `WorkRequestNote_ShouldRequireText` — verify that a note with empty text is rejected
- `WorkRequestNote_ShouldSetCreatedDate` — verify CreatedDate is set on creation
- `AddNoteCommand_ShouldAddNoteToWorkRequest` — verify command execution adds a note to the work request's collection
- `WorkRequestManage_ShouldRenderNotesSection` — bUnit test verifying the notes section renders with existing notes

### Integration Tests
- `AddNoteCommand_ShouldPersistNote` — add a note via the command and verify it is persisted in the database
- `WorkRequestNotesQuery_ShouldReturnNotesInChronologicalOrder` — add multiple notes and verify they are returned oldest-first
- `WorkRequestNotesQuery_ShouldReturnEmptyForWorkRequestWithNoNotes` — verify empty collection for work requests without notes

### Acceptance Tests
- Navigate to an existing work request, type a note in the text input, submit, and verify the note appears in the notes list with author name and timestamp
- Add multiple notes to a work request and verify they display in chronological order
