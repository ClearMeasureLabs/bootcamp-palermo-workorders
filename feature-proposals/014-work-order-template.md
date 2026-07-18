## Why
Recurring work requests (weekly cleaning, monthly inspections) can be templated to reduce repetitive data entry and ensure consistency. Templates codify standard procedures and room assignments, enabling rapid creation of work requests for routine tasks.

## What Changes
- Add `WorkRequestTemplate` entity to `src/Core/Model/` with properties: Id (Guid), Title (string), Description (string), RoomNumber (string), IsActive (bool), CreatedById (Guid), CreatedDate (DateTime)
- Add `WorkRequestTemplatesQuery` to `src/Core/Queries/` to retrieve all active templates
- Add `WorkRequestTemplateByIdQuery` to `src/Core/Queries/` to retrieve a single template
- Add `CreateWorkRequestTemplateCommand` to `src/Core/Model/StateCommands/`
- Add `CreateWorkRequestFromTemplateCommand` to `src/Core/Model/StateCommands/` containing TemplateId and CreatorId
- Add EF Core mapping for `WorkRequestTemplate` in DataAccess
- Add handlers for template commands and queries in `src/DataAccess/Handlers/`
- Add a new DbUp migration script creating the `WorkRequestTemplate` table
- Add a template management page for creating and viewing templates
- Add a "Create from Template" dropdown on the new work request form that populates fields from a selected template

## Capabilities
### New Capabilities
- Users can create work request templates with pre-filled title, description, and room number
- Users can view and manage a list of active templates
- Users can create a new work request from a template, which pre-fills the form fields
- Templates can be deactivated to remove them from the available list

### Modified Capabilities
- WorkRequestManage new work request form includes a "Create from Template" option

## Impact
- **Core** — New `WorkRequestTemplate` entity; new queries and commands for template management
- **DataAccess** — EF Core mapping for `WorkRequestTemplate`; new MediatR handlers
- **UI.Shared** — New template management page; template selection integration on work request creation form
- **Database** — New migration script creating `WorkRequestTemplate` table

## Acceptance Criteria
### Unit Tests
- `WorkRequestTemplate_ShouldRequireTitle` — verify a template with empty title is rejected
- `CreateWorkRequestFromTemplateCommand_ShouldCopyFields` — verify new work request copies Title, Description, RoomNumber from template
- `CreateWorkRequestFromTemplateCommand_ShouldSetStatusToDraft` — verify the created work request starts in Draft status
- `WorkRequestManage_ShouldRenderTemplateDropdown_OnNewWorkRequest` — bUnit test verifying template dropdown appears on the new work request form

### Integration Tests
- `WorkRequestTemplate_ShouldPersistAndRetrieve` — create a template and verify it round-trips through the database
- `WorkRequestTemplatesQuery_ShouldReturnOnlyActiveTemplates` — create active and inactive templates, verify only active ones are returned
- `CreateWorkRequestFromTemplateCommand_ShouldPersistNewWorkRequest` — create a work request from a template and verify it exists in the database with correct fields

### Acceptance Tests
- Navigate to the template management page, create a template titled "Weekly Bathroom Cleaning" with description and room number, and verify it appears in the template list
- Navigate to create a new work request, select "Weekly Bathroom Cleaning" from the template dropdown, and verify the form fields are populated with the template values
- Save the templated work request and verify it exists as a Draft with the correct title, description, and room number
