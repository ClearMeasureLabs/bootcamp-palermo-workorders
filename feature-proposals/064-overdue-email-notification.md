## Why
Work orders that pass their due date without completion represent service failures that need immediate attention. Automated overdue notifications ensure assignees and supervisors are alerted promptly, reducing the risk of forgotten or stalled work orders.

## What Changes
- Add `DueDate` property (nullable `DateOnly`) to `WorkOrder` entity in `src/Core/Model/`
- Add database migration script to add `DueDate` column to the `WorkOrder` table
- Add `OverdueCheckBackgroundService` as a hosted service in `src/UI/Server/` that runs daily at a configurable time
- Add `GetOverdueWorkOrdersQuery` in `src/Core/Queries/` returning work orders where DueDate < today and Status is not Complete or Cancelled
- Add handler for `GetOverdueWorkOrdersQuery` in `src/DataAccess/Handlers/`
- Add `OverdueNotificationService` in `src/Core/` defining the interface for sending overdue alerts
- Add `OverdueEmailSender` implementation in `src/UI/Server/` using the `IEmailSender` interface
- Add `OverdueEmailTemplate` Razor template in `src/UI/Server/EmailTemplates/`
- Modify `WorkOrderManage.razor` in `src/UI/Client/` to include a DueDate date picker field
- Update `DataContext` in `src/DataAccess/` to map the new `DueDate` property

## Capabilities
### New Capabilities
- DueDate field on work orders, editable during creation and editing
- Scheduled background service checks daily for overdue work orders
- Email notification sent to the assignee (and optionally the creator) when a work order is overdue
- Overdue email includes work order number, title, due date, days overdue, and a link to the work order

### Modified Capabilities
- WorkOrderManage page includes a new DueDate date picker
- Work order detail display shows DueDate when set

## Impact
- **Core**: Modified `WorkOrder` entity (new `DueDate` property), new `GetOverdueWorkOrdersQuery`, new `OverdueNotificationService` interface
- **DataAccess**: Updated `DataContext` mapping, new query handler
- **UI.Server**: New background service, email sender implementation, email template
- **UI.Client**: Modified `WorkOrderManage.razor` with DueDate picker
- **Database**: New migration script adding `DueDate` column (nullable `date`) to `WorkOrder` table
- **Dependencies**: No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `WorkOrder_WithDueDate_StoresDateCorrectly` - DueDate property getter/setter works
- `GetOverdueWorkOrdersQuery_Handler_ReturnsOnlyOverdue` - handler filters work orders with DueDate before today and non-terminal status
- `GetOverdueWorkOrdersQuery_Handler_ExcludesCompleteAndCancelled` - handler excludes Complete and Cancelled work orders even if past due
- `OverdueEmailTemplate_RendersAllFields` - template contains work order number, title, due date, and days overdue
- `OverdueCheckBackgroundService_InvokesQueryAndSendsEmails` - background service orchestrates query and notification

### Integration Tests
- `GetOverdueWorkOrdersQuery_WithOverdueRecords_ReturnsCorrectWorkOrders` - query against database returns only overdue records
- `GetOverdueWorkOrdersQuery_WithNoDueDate_ExcludesFromResults` - work orders without a DueDate are not returned
- `WorkOrder_DueDate_PersistsThroughEfCore` - DueDate value round-trips through save and load

### Acceptance Tests
- Navigate to WorkOrderManage page, set a DueDate using the date picker with `data-testid="due-date-input"`, save, reload, and verify the date persists
- Create a work order without a DueDate and verify the field displays as empty on reload
