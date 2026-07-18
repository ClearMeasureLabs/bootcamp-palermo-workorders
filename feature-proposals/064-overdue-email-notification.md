## Why
Work requests that pass their due date without completion represent service failures that need immediate attention. Automated overdue notifications ensure assignees and supervisors are alerted promptly, reducing the risk of forgotten or stalled work requests.

## What Changes
- Add `DueDate` property (nullable `DateOnly`) to `WorkRequest` entity in `src/Core/Model/`
- Add database migration script to add `DueDate` column to the `WorkRequest` table
- Add `OverdueCheckBackgroundService` as a hosted service in `src/UI/Server/` that runs daily at a configurable time
- Add `GetOverdueWorkRequestsQuery` in `src/Core/Queries/` returning work requests where DueDate < today and Status is not Complete or Cancelled
- Add handler for `GetOverdueWorkRequestsQuery` in `src/DataAccess/Handlers/`
- Add `OverdueNotificationService` in `src/Core/` defining the interface for sending overdue alerts
- Add `OverdueEmailSender` implementation in `src/UI/Server/` using the `IEmailSender` interface
- Add `OverdueEmailTemplate` Razor template in `src/UI/Server/EmailTemplates/`
- Modify `WorkRequestManage.razor` in `src/UI/Client/` to include a DueDate date picker field
- Update `DataContext` in `src/DataAccess/` to map the new `DueDate` property

## Capabilities
### New Capabilities
- DueDate field on work requests, editable during creation and editing
- Scheduled background service checks daily for overdue work requests
- Email notification sent to the assignee (and optionally the creator) when a work request is overdue
- Overdue email includes work request number, title, due date, days overdue, and a link to the work request

### Modified Capabilities
- WorkRequestManage page includes a new DueDate date picker
- Work request detail display shows DueDate when set

## Impact
- **Core**: Modified `WorkRequest` entity (new `DueDate` property), new `GetOverdueWorkRequestsQuery`, new `OverdueNotificationService` interface
- **DataAccess**: Updated `DataContext` mapping, new query handler
- **UI.Server**: New background service, email sender implementation, email template
- **UI.Client**: Modified `WorkRequestManage.razor` with DueDate picker
- **Database**: New migration script adding `DueDate` column (nullable `date`) to `WorkRequest` table
- **Dependencies**: No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `WorkRequest_WithDueDate_StoresDateCorrectly` - DueDate property getter/setter works
- `GetOverdueWorkRequestsQuery_Handler_ReturnsOnlyOverdue` - handler filters work requests with DueDate before today and non-terminal status
- `GetOverdueWorkRequestsQuery_Handler_ExcludesCompleteAndCancelled` - handler excludes Complete and Cancelled work requests even if past due
- `OverdueEmailTemplate_RendersAllFields` - template contains work request number, title, due date, and days overdue
- `OverdueCheckBackgroundService_InvokesQueryAndSendsEmails` - background service orchestrates query and notification

### Integration Tests
- `GetOverdueWorkRequestsQuery_WithOverdueRecords_ReturnsCorrectWorkRequests` - query against database returns only overdue records
- `GetOverdueWorkRequestsQuery_WithNoDueDate_ExcludesFromResults` - work requests without a DueDate are not returned
- `WorkRequest_DueDate_PersistsThroughEfCore` - DueDate value round-trips through save and load

### Acceptance Tests
- Navigate to WorkRequestManage page, set a DueDate using the date picker with `data-testid="due-date-input"`, save, reload, and verify the date persists
- Create a work request without a DueDate and verify the field displays as empty on reload
