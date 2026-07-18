## Why
Maintenance staff and supervisors need to see assigned work requests alongside their other calendar commitments. Exporting work requests as iCalendar (.ics) files allows integration with Outlook, Google Calendar, and Apple Calendar without requiring users to manually track due dates.

## What Changes
- Add `ICalendarExportQuery` in `src/Core/Queries/` that returns assigned work requests for a given employee with due dates
- Add `ICalendarExportHandler` in `src/DataAccess/Handlers/` that queries work requests and generates iCalendar content
- Add `CalendarExportController` in `src/UI/Api/` with endpoint `GET /api/calendar/{username}.ics` returning `text/calendar` content type
- Generate RFC 5545-compliant VEVENT entries with SUMMARY (work request title), DESCRIPTION (work request description), DTSTART (assigned date), and UID (work request number)
- Add "Export to Calendar" button on the assigned work requests list page in `src/UI/Client/`

## Capabilities
### New Capabilities
- Export assigned work requests as downloadable .ics file per employee
- Each work request becomes a VEVENT entry with title, description, and assigned date
- Calendar subscription URL for automatic syncing with external calendar applications
- "Export to Calendar" button on the work request list page

### Modified Capabilities
- None

## Impact
- **src/Core/Queries/** - New `ICalendarExportQuery`
- **src/DataAccess/Handlers/** - New handler for calendar export query
- **src/UI/Api/** - New `CalendarExportController`
- **src/UI/Client/** - New "Export to Calendar" button on work request list page
- **Dependencies** - No new NuGet packages; iCalendar format generated via string building per RFC 5545

## Acceptance Criteria
### Unit Tests
- `CalendarExport_AssignedWorkRequests_GeneratesVeventsForEach` - Export produces one VEVENT per assigned work request
- `CalendarExport_NoAssignedWorkRequests_ReturnsEmptyCalendar` - Export for employee with no assignments returns valid VCALENDAR with zero VEVENTs
- `CalendarExport_VeventFields_ContainCorrectData` - SUMMARY matches work request title, DESCRIPTION matches description, DTSTART matches assigned date
- `CalendarExport_ContentType_IsTextCalendar` - Response content type is `text/calendar`
- `CalendarExport_Rfc5545Compliance_ContainsRequiredHeaders` - Output contains BEGIN:VCALENDAR, VERSION:2.0, PRODID, and END:VCALENDAR

### Integration Tests
- `CalendarExport_WithPersistedWorkRequests_ReturnsMatchingEvents` - Seed assigned work requests in database, call export endpoint, verify VEVENT count matches
- `CalendarExport_OnlyIncludesAssignedStatus_ExcludesDraftAndComplete` - Seed work requests in various statuses, verify only Assigned and InProgress appear in export

### Acceptance Tests
- `CalendarExport_ClickExportButton_DownloadsIcsFile` - Log in, navigate to work request list, click "Export to Calendar" button, verify .ics file downloads
- `CalendarExport_DownloadedFile_ContainsAssignedWorkRequests` - Download .ics file, parse content, verify it contains expected work request titles
