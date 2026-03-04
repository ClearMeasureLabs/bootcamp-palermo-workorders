## Why
Different facilities use different numbering conventions for tracking maintenance requests. Making the work order number format configurable allows each deployment to match its existing numbering scheme, improving consistency with legacy systems and reducing confusion during migration.

## What Changes
- Add `NumberFormatConfiguration` entity in `src/Core/Model/` with properties: Prefix (string), Length (int), Pattern (string template, e.g., "{Prefix}{Number:D{Length}}")
- Add `NumberFormatSettings` configuration class in `src/Core/` bound to `appsettings.json` section `WorkOrderNumberFormat`
- Add `IWorkOrderNumberGenerator` interface in `src/Core/Interfaces/` with method `GenerateNext()`
- Add `WorkOrderNumberGenerator` implementation in `src/DataAccess/Services/` that reads configuration and generates formatted numbers
- Update `SaveDraftCommand` handler to use `IWorkOrderNumberGenerator` instead of hardcoded format
- Add database migration script in `src/Database/scripts/Update/` to add `NumberFormatConfiguration` table with default row
- Add admin UI section in `src/UI/Client/Pages/` for configuring number format

## Capabilities
### New Capabilities
- Configurable work order number prefix (e.g., "WO-", "MNT-", "REQ-")
- Configurable numeric portion length with zero-padding (e.g., 5 digits produces "00001")
- Pattern template supporting prefix and padded number combination
- Admin UI page for viewing and updating number format configuration

### Modified Capabilities
- `SaveDraftCommand` handler updated to use configurable number generator instead of hardcoded format

## Impact
- **src/Core/Model/** - New `NumberFormatConfiguration` entity
- **src/Core/Interfaces/** - New `IWorkOrderNumberGenerator` interface
- **src/DataAccess/Services/** - New `WorkOrderNumberGenerator` implementation
- **src/DataAccess/Handlers/** - Updated `SaveDraftCommand` handler
- **src/Database/** - New migration script for `NumberFormatConfiguration` table
- **src/UI/Client/** - New admin configuration page
- **src/UI/Server/appsettings.json** - New `WorkOrderNumberFormat` configuration section
- **Dependencies** - No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `NumberGenerator_DefaultConfig_GeneratesExpectedFormat` - Default configuration produces numbers like "WO-00001"
- `NumberGenerator_CustomPrefix_AppliesPrefix` - Configuration with prefix "MNT-" generates "MNT-00001"
- `NumberGenerator_CustomLength_PadsCorrectly` - Configuration with length 8 produces "WO-00000001"
- `NumberGenerator_Sequential_IncrementsCorrectly` - Two consecutive calls produce incrementing numbers
- `NumberGenerator_EmptyPrefix_GeneratesNumberOnly` - Empty prefix configuration produces "00001"

### Integration Tests
- `NumberFormat_ConfigurationPersisted_GeneratesMatchingNumbers` - Save configuration to database, generate number, verify format matches
- `NumberFormat_UpdateConfiguration_NextNumberUsesNewFormat` - Change prefix in database, generate next number, verify new prefix applied
- `NumberFormat_ConcurrentGeneration_NoCollisions` - Generate numbers concurrently, verify all are unique

### Acceptance Tests
- `NumberFormat_AdminPage_DisplaysCurrentConfiguration` - Log in as admin, navigate to configuration page, verify current format settings displayed
- `NumberFormat_UpdateAndCreate_NewWorkOrderUsesNewFormat` - Update number format via admin page, create new work order, verify work order number matches new format
