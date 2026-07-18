## Why
Work requests often need supporting documents such as photos of damage, invoices, or floor plans. Tracking attachment metadata enables future file storage integration and provides immediate visibility into what documents are associated with each work request.

## What Changes
- Add `WorkRequestAttachment` entity to `src/Core/Model/` with properties: Id (Guid), WorkRequestId (Guid), FileName (string), ContentType (string), FileSize (long), UploadedById (Guid), UploadedDate (DateTime)
- Add navigation property `Attachments` (ICollection<WorkRequestAttachment>) to `WorkRequest` domain model
- Add `WorkRequestAttachmentsQuery` to `src/Core/Queries/` to retrieve attachments for a given work request
- Add `AddAttachmentMetadataCommand` to `src/Core/Model/StateCommands/` for recording new attachment metadata
- Add EF Core mapping for `WorkRequestAttachment` in DataAccess
- Add handlers for the attachment command and query in `src/DataAccess/Handlers/`
- Add a new DbUp migration script creating the `WorkRequestAttachment` table with foreign keys
- Add an attachment list display on the `WorkRequestManage` page showing file name, size, uploader, and date
- Add MCP tool for listing attachments associated with a work request

## Capabilities
### New Capabilities
- Users can record attachment metadata (file name, type, size) for a work request
- Users can view a list of all attachments on the work request manage page
- MCP tools can query attachment metadata for a given work request
- Attachment metadata captures who uploaded each file and when

### Modified Capabilities
- WorkRequestManage page includes a new attachments section displaying metadata records

## Impact
- **Core** — New `WorkRequestAttachment` entity; new `AddAttachmentMetadataCommand`; new `WorkRequestAttachmentsQuery`
- **DataAccess** — EF Core mapping for `WorkRequestAttachment`; new MediatR handlers
- **UI.Shared** — Attachment list component on `WorkRequestManage` page
- **Database** — New migration script creating `WorkRequestAttachment` table
- **McpServer** — New MCP tool for listing work request attachments

## Acceptance Criteria
### Unit Tests
- `WorkRequestAttachment_ShouldRequireFileName` — verify that an attachment with empty file name is rejected
- `WorkRequestAttachment_ShouldSetUploadedDate` — verify UploadedDate is set on creation
- `AddAttachmentMetadataCommand_ShouldAddAttachment` — verify command adds attachment metadata to the work request
- `WorkRequestManage_ShouldRenderAttachmentsSection` — bUnit test verifying attachments list renders with existing records

### Integration Tests
- `AddAttachmentMetadataCommand_ShouldPersistAttachment` — add attachment metadata and verify it persists in the database
- `WorkRequestAttachmentsQuery_ShouldReturnAttachmentsForWorkRequest` — add multiple attachments and verify query returns them all
- `WorkRequestAttachmentsQuery_ShouldReturnEmptyForWorkRequestWithNoAttachments` — verify empty collection for work requests without attachments

### Acceptance Tests
- Navigate to an existing work request, add attachment metadata with file name "damage-photo.jpg", and verify the attachment appears in the attachments list with correct details
- Verify the attachments section displays file name, content type, file size, uploader name, and upload date for each entry
