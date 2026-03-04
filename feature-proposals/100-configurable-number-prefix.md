## Why
Different organizations and departments use distinct work order prefixes to identify request types and origins (e.g., "WO-" for general work orders, "MNT-" for maintenance, "EMR-" for emergencies). A configurable prefix enables each deployment to match its organizational naming convention without code changes.

## What Changes
- Add `WorkOrderPrefix` setting in `appsettings.json` under a `WorkOrderSettings` section (default value: "WO-")
- Add `WorkOrderSettings` configuration class in `src/Core/` with `Prefix` property
- Update `WorkOrderNumberGenerator` (or equivalent number assignment logic) in `src/DataAccess/` to prepend the configured prefix when generating new work order numbers
- Register `WorkOrderSettings` in `UIServiceRegistry.cs` via `IOptions<WorkOrderSettings>` pattern
- Ensure existing work orders without prefix remain valid and displayable
- Update work order search to handle prefix in search queries (strip prefix for numeric comparison when needed)
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
- **src/DataAccess/** - `WorkOrderNumberGenerator` updated to use configured prefix
- **src/UI/Server/appsettings.json** - New `WorkOrderSettings` section with `Prefix` property
- **src/UI/Server/UIServiceRegistry.cs** - Registration of `IOptions<WorkOrderSettings>`
- **src/DataAccess/Handlers/** - Search handler updated for prefix-aware querying
- **Dependencies** - No new NuGet packages required
- **Database** - No schema changes required; prefix is part of the generated number string

## Acceptance Criteria
### Unit Tests
- `NumberGenerator_DefaultPrefix_PrependsWO` - Default configuration generates numbers like "WO-00001"
- `NumberGenerator_CustomPrefix_PrependsMNT` - Configuration with prefix "MNT-" generates "MNT-00001"
- `NumberGenerator_EmptyPrefix_GeneratesNumberWithoutPrefix` - Empty prefix generates "00001" without leading characters
- `NumberGenerator_PrefixValidation_RejectsSpecialCharacters` - Prefix "WO@#" throws configuration validation error
- `NumberGenerator_PrefixValidation_RejectsOverLength` - Prefix longer than 10 characters throws validation error
- `NumberGenerator_Sequential_MaintainsIncrementWithPrefix` - Two consecutive generations produce "WO-00001" and "WO-00002"
- `WorkOrderSearch_PrefixedNumber_FindsWorkOrder` - Search for "WO-00001" returns the correct work order

### Integration Tests
- `NumberPrefix_ConfiguredInSettings_AppliedToNewWorkOrders` - Configure prefix in settings, create work order, verify persisted number contains prefix
- `NumberPrefix_ExistingWorkOrders_StillAccessible` - Work orders created before prefix configuration remain queryable and displayable
- `NumberPrefix_ChangePrefix_NextWorkOrderUsesNewPrefix` - Change prefix in settings, create new work order, verify new prefix applied while old work orders retain original numbers

### Acceptance Tests
- `CreateWorkOrder_WithConfiguredPrefix_NumberShowsPrefix` - Create work order through UI, verify displayed work order number starts with configured prefix
- `SearchWorkOrder_ByPrefixedNumber_FindsResult` - Create work order, search by full prefixed number in UI, verify work order found
- `WorkOrderList_AllNumbers_DisplayWithPrefix` - Navigate to work order list, verify all newly created work order numbers display with the configured prefix
