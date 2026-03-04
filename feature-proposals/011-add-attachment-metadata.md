## Why
Work orders often need supporting documents such as photos of damage, invoices, or floor plans. Tracking attachment metadata enables future file storage integration and provides immediate visibility into what documents are associated with each work order.

## What Changes
- Add `WorkOrderAttachment` entity to `src/Core/Model/` with properties: Id (Guid), WorkOrderId (Guid), FileName (string), ContentType (string), FileSize (long), UploadedById (Guid), UploadedDate (DateTime)
- Add navigation property `Attachments` (ICollection<WorkOrderAttachment>) to `WorkOrder` domain model
- Add `WorkOrderAttachmentsQuery` to `src/Core/Queries/` to retrieve attachments for a given work order
- Add `AddAttachmentMetadataCommand` to `src/Core/Model/StateCommands/` for recording new attachment metadata
- Add EF Core mapping for `WorkOrderAttachment` in DataAccess
- Add handlers for the attachment command and query in `src/DataAccess/Handlers/`
- Add a new DbUp migration script creating the `WorkOrderAttachment` table with foreign keys
- Add an attachment list display on the `WorkOrderManage` page showing file name, size, uploader, and date
- Add MCP tool for listing attachments associated with a work order

## Capabilities
### New Capabilities
- Users can record attachment metadata (file name, type, size) for a work order
- Users can view a list of all attachments on the work order manage page
- MCP tools can query attachment metadata for a given work order
- Attachment metadata captures who uploaded each file and when

### Modified Capabilities
- WorkOrderManage page includes a new attachments section displaying metadata records

## Impact
- **Core** — New `WorkOrderAttachment` entity; new `AddAttachmentMetadataCommand`; new `WorkOrderAttachmentsQuery`
- **DataAccess** — EF Core mapping for `WorkOrderAttachment`; new MediatR handlers
- **UI.Shared** — Attachment list component on `WorkOrderManage` page
- **Database** — New migration script creating `WorkOrderAttachment` table
- **McpServer** — New MCP tool for listing work order attachments

## Acceptance Criteria
### Unit Tests
- `WorkOrderAttachment_ShouldRequireFileName` — verify that an attachment with empty file name is rejected
- `WorkOrderAttachment_ShouldSetUploadedDate` — verify UploadedDate is set on creation
- `AddAttachmentMetadataCommand_ShouldAddAttachment` — verify command adds attachment metadata to the work order
- `WorkOrderManage_ShouldRenderAttachmentsSection` — bUnit test verifying attachments list renders with existing records

### Integration Tests
- `AddAttachmentMetadataCommand_ShouldPersistAttachment` — add attachment metadata and verify it persists in the database
- `WorkOrderAttachmentsQuery_ShouldReturnAttachmentsForWorkOrder` — add multiple attachments and verify query returns them all
- `WorkOrderAttachmentsQuery_ShouldReturnEmptyForWorkOrderWithNoAttachments` — verify empty collection for work orders without attachments

### Acceptance Tests
- Navigate to an existing work order, add attachment metadata with file name "damage-photo.jpg", and verify the attachment appears in the attachments list with correct details
- Verify the attachments section displays file name, content type, file size, uploader name, and upload date for each entry
