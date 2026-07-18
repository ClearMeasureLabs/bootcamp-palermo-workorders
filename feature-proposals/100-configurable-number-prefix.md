## Why
Different organizations and departments use distinct work request prefixes to identify request types and origins (e.g., "WO-" for general work requests, "MNT-" for maintenance, "EMR-" for emergencies). A configurable prefix enables each deployment to match its organizational naming convention without code changes.

## What Changes
- Add `WorkRequestPrefix` setting in `appsettings.json` under a `WorkRequestSettings` section (default value: "WO-")
- Add `WorkRequestSettings` configuration class in `src/Core/` with `Prefix` property
- Update `WorkRequestNumberGenerator` in `src/Core/Services/Impl/` (currently generates 7-character uppercase GUID substring via `GenerateNumber()`) to prepend the configured prefix when generating new work request numbers
- Register `WorkRequestSettings` in `UIServiceRegistry.cs` via `IOptions<WorkRequestSettings>` pattern
- Ensure existing work requests without prefix remain valid and displayable
- Update work request search to handle prefix in search queries (strip prefix for comparison when needed)
- Add configuration validation: prefix must be 0-10 characters, alphanumeric and hyphens only

## Capabilities
### New Capabilities
- Configurable work request number prefix via application settings
- Prefix applied automatically to all newly generated work request numbers
- Prefix validation enforcing 0-10 character limit with alphanumeric and hyphen characters only
- Backward compatibility with existing work requests that lack a prefix

### Modified Capabilities
- Work request number generation updated to prepend configured prefix
- Work request search updated to handle prefixed numbers correctly

## Impact
- **src/Core/** - New `WorkRequestSettings` configuration class
- **src/Core/Services/Impl/WorkRequestNumberGenerator.cs** - Updated to prepend configured prefix to the 7-char GUID substring
- **src/UI/Server/appsettings.json** - New `WorkRequestSettings` section with `Prefix` property
- **src/UI/Server/UIServiceRegistry.cs** - Registration of `IOptions<WorkRequestSettings>`
- **src/DataAccess/Handlers/** - Search handler updated for prefix-aware querying
- **Dependencies** - No new NuGet packages required
- **Database** - No schema changes required; prefix is part of the generated number string (note: DB column length may need review since current Number is 7 chars and prefix adds length)

## Acceptance Criteria
### Unit Tests
- `NumberGenerator_DefaultPrefix_PrependsWO` - Default configuration generates numbers like "WO-A1B2C3D" (prefix + 7-char GUID substring)
- `NumberGenerator_CustomPrefix_PrependsMNT` - Configuration with prefix "MNT-" generates "MNT-" followed by 7-char GUID substring
- `NumberGenerator_EmptyPrefix_GeneratesGuidOnly` - Empty prefix generates 7-char GUID substring without leading characters
- `NumberGenerator_PrefixValidation_RejectsSpecialCharacters` - Prefix "WO@#" throws configuration validation error
- `NumberGenerator_PrefixValidation_RejectsOverLength` - Prefix longer than 10 characters throws validation error
- `NumberGenerator_TwoCalls_ProducesDifferentNumbers` - Two consecutive generations produce unique GUID-based numbers with prefix
- `WorkRequestSearch_PrefixedNumber_FindsWorkRequest` - Search for "WO-A1B2C3D" returns the correct work request

### Integration Tests
- `NumberPrefix_ConfiguredInSettings_AppliedToNewWorkRequests` - Configure prefix in settings, create work request, verify persisted number contains prefix
- `NumberPrefix_ExistingWorkRequests_StillAccessible` - Work requests created before prefix configuration remain queryable and displayable
- `NumberPrefix_ChangePrefix_NextWorkRequestUsesNewPrefix` - Change prefix in settings, create new work request, verify new prefix applied while old work requests retain original numbers

### Acceptance Tests
- `CreateWorkRequest_WithConfiguredPrefix_NumberShowsPrefix` - Create work request through UI, verify displayed work request number starts with configured prefix
- `SearchWorkRequest_ByPrefixedNumber_FindsResult` - Create work request, search by full prefixed number in UI, verify work request found
- `WorkRequestList_AllNumbers_DisplayWithPrefix` - Navigate to work request list, verify all newly created work request numbers display with the configured prefix
