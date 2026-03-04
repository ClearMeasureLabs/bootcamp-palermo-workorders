## Why
Recurring work orders (weekly cleaning, monthly inspections) can be templated to reduce repetitive data entry and ensure consistency. Templates codify standard procedures and room assignments, enabling rapid creation of work orders for routine tasks.

## What Changes
- Add `WorkOrderTemplate` entity to `src/Core/Model/` with properties: Id (Guid), Title (string), Description (string), RoomNumber (string), IsActive (bool), CreatedById (Guid), CreatedDate (DateTime)
- Add `WorkOrderTemplatesQuery` to `src/Core/Queries/` to retrieve all active templates
- Add `WorkOrderTemplateByIdQuery` to `src/Core/Queries/` to retrieve a single template
- Add `CreateWorkOrderTemplateCommand` to `src/Core/Model/StateCommands/`
- Add `CreateWorkOrderFromTemplateCommand` to `src/Core/Model/StateCommands/` containing TemplateId and CreatorId
- Add EF Core mapping for `WorkOrderTemplate` in DataAccess
- Add handlers for template commands and queries in `src/DataAccess/Handlers/`
- Add a new DbUp migration script creating the `WorkOrderTemplate` table
- Add a template management page for creating and viewing templates
- Add a "Create from Template" dropdown on the new work order form that populates fields from a selected template

## Capabilities
### New Capabilities
- Users can create work order templates with pre-filled title, description, and room number
- Users can view and manage a list of active templates
- Users can create a new work order from a template, which pre-fills the form fields
- Templates can be deactivated to remove them from the available list

### Modified Capabilities
- WorkOrderManage new work order form includes a "Create from Template" option

## Impact
- **Core** — New `WorkOrderTemplate` entity; new queries and commands for template management
- **DataAccess** — EF Core mapping for `WorkOrderTemplate`; new MediatR handlers
- **UI.Shared** — New template management page; template selection integration on work order creation form
- **Database** — New migration script creating `WorkOrderTemplate` table

## Acceptance Criteria
### Unit Tests
- `WorkOrderTemplate_ShouldRequireTitle` — verify a template with empty title is rejected
- `CreateWorkOrderFromTemplateCommand_ShouldCopyFields` — verify new work order copies Title, Description, RoomNumber from template
- `CreateWorkOrderFromTemplateCommand_ShouldSetStatusToDraft` — verify the created work order starts in Draft status
- `WorkOrderManage_ShouldRenderTemplateDropdown_OnNewWorkOrder` — bUnit test verifying template dropdown appears on the new work order form

### Integration Tests
- `WorkOrderTemplate_ShouldPersistAndRetrieve` — create a template and verify it round-trips through the database
- `WorkOrderTemplatesQuery_ShouldReturnOnlyActiveTemplates` — create active and inactive templates, verify only active ones are returned
- `CreateWorkOrderFromTemplateCommand_ShouldPersistNewWorkOrder` — create a work order from a template and verify it exists in the database with correct fields

### Acceptance Tests
- Navigate to the template management page, create a template titled "Weekly Bathroom Cleaning" with description and room number, and verify it appears in the template list
- Navigate to create a new work order, select "Weekly Bathroom Cleaning" from the template dropdown, and verify the form fields are populated with the template values
- Save the templated work order and verify it exists as a Draft with the correct title, description, and room number
