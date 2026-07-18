## Why
Different organizations and departments use distinct work order prefixes to identify request types and origins (e.g., "WO-" for general work orders, "MNT-" for maintenance, "EMR-" for emergencies). A configurable prefix enables each deployment to match its organizational naming convention without code changes.

## What Changes
- Add `WorkOrderPrefix` setting in `appsettings.json` under a `WorkOrderSettings` section (default value: "WO-")
- Add `WorkOrderSettings` configuration class in `src/Core/` with `Prefix` property
- Update `WorkOrderNumberGenerator` in `src/Core/Services/Impl/` (currently generates 7-character uppercase GUID substring via `GenerateNumber()`) to prepend the configured prefix when generating new work order numbers
- Register `WorkOrderSettings` in `UIServiceRegistry.cs` via `IOptions<WorkOrderSettings>` pattern
- Ensure existing work orders without prefix remain valid and displayable
- Update work order search to handle prefix in search queries (strip prefix for comparison when needed)
- Add configuration validation: prefix must be 0-10 characters, alphanumeric and hyphens only

## Capabilities
### New Capabilities
- Configurable work order number prefix via application settings
- Prefix applied automatically to all newly generated work order numbers
- Prefix validation enforcing 0-10 character limit with alphanumeric and hyphen characters only
- Backward compatibility with existing work orders that lack a prefix

### Modified Capabilities
- Work order number generation updated to prepend configured prefix
- Work order search updated to handle prefixed numbers correctly

## Impact
- **src/Core/** - New `WorkOrderSettings` configuration class
- **src/Core/Services/Impl/WorkOrderNumberGenerator.cs** - Updated to prepend configured prefix to the 7-char GUID substring
- **src/UI/Server/appsettings.json** - New `WorkOrderSettings` section with `Prefix` property
- **src/UI/Server/UIServiceRegistry.cs** - Registration of `IOptions<WorkOrderSettings>`
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
- `WorkOrderSearch_PrefixedNumber_FindsWorkOrder` - Search for "WO-A1B2C3D" returns the correct work order

### Integration Tests
- `NumberPrefix_ConfiguredInSettings_AppliedToNewWorkOrders` - Configure prefix in settings, create work order, verify persisted number contains prefix
- `NumberPrefix_ExistingWorkOrders_StillAccessible` - Work orders created before prefix configuration remain queryable and displayable
- `NumberPrefix_ChangePrefix_NextWorkOrderUsesNewPrefix` - Change prefix in settings, create new work order, verify new prefix applied while old work orders retain original numbers

### Acceptance Tests
- `CreateWorkOrder_WithConfiguredPrefix_NumberShowsPrefix` - Create work order through UI, verify displayed work order number starts with configured prefix
- `SearchWorkOrder_ByPrefixedNumber_FindsResult` - Create work order, search by full prefixed number in UI, verify work order found
- `WorkOrderList_AllNumbers_DisplayWithPrefix` - Navigate to work order list, verify all newly created work order numbers display with the configured prefix
