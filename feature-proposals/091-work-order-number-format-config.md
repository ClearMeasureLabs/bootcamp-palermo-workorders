## Why
Different facilities use different numbering conventions for tracking maintenance requests. Making the work order number format configurable allows each deployment to match its existing numbering scheme, improving consistency with legacy systems and reducing confusion during migration.

## What Changes
- Add `NumberFormatConfiguration` entity in `src/Core/Model/` with properties: Prefix (string), Pattern (string template, e.g., "{Prefix}-{GuidSegment}")
- Add `NumberFormatSettings` configuration class in `src/Core/` bound to `appsettings.json` section `WorkOrderNumberFormat`
- Extend existing `IWorkOrderNumberGenerator` interface in `src/Core/Services/` (currently has `GenerateNumber()`) with an overload or update to accept format configuration
- Update existing `WorkOrderNumberGenerator` implementation in `src/Core/Services/Impl/` (currently generates 7-character uppercase GUID substring) to support configurable prefix prepended to the GUID-based number
- Update `SaveDraftCommand` handler to pass format configuration to the existing number generator
- Add database migration script in `src/Database/scripts/Update/` to add `NumberFormatConfiguration` table with default row
- Add admin UI section in `src/UI/Client/Pages/` for configuring number format

## Capabilities
### New Capabilities
- Configurable work order number prefix (e.g., "WO-", "MNT-", "REQ-") prepended to the existing 7-character GUID-based number
- Pattern template supporting prefix and GUID-based number combination (e.g., "WO-A1B2C3D")
- Admin UI page for viewing and updating number format configuration

### Modified Capabilities
- `SaveDraftCommand` handler updated to pass format configuration to the existing number generator

## Impact
- **src/Core/Model/** - New `NumberFormatConfiguration` entity
- **src/Core/Services/IWorkOrderNumberGenerator.cs** - Extended existing interface (currently has `GenerateNumber()`)
- **src/Core/Services/Impl/WorkOrderNumberGenerator.cs** - Updated existing implementation (currently generates 7-char GUID substring)
- **src/DataAccess/Handlers/** - Updated `SaveDraftCommand` handler
- **src/Database/** - New migration script for `NumberFormatConfiguration` table
- **src/UI/Client/** - New admin configuration page
- **src/UI/Server/appsettings.json** - New `WorkOrderNumberFormat` configuration section
- **Dependencies** - No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `NumberGenerator_DefaultConfig_GeneratesExpectedFormat` - Default configuration produces numbers like "WO-A1B2C3D" (prefix + 7-char GUID substring)
- `NumberGenerator_CustomPrefix_AppliesPrefix` - Configuration with prefix "MNT-" generates "MNT-" followed by 7-character GUID substring
- `NumberGenerator_EmptyPrefix_GeneratesGuidOnly` - Empty prefix configuration produces 7-character GUID substring without prefix
- `NumberGenerator_TwoCalls_ProducesUniqueNumbers` - Two consecutive calls produce different GUID-based numbers
- `NumberGenerator_GeneratedNumber_MatchesExpectedLength` - Output length matches prefix length + 7

### Integration Tests
- `NumberFormat_ConfigurationPersisted_GeneratesMatchingNumbers` - Save configuration to database, generate number, verify format matches
- `NumberFormat_UpdateConfiguration_NextNumberUsesNewFormat` - Change prefix in database, generate next number, verify new prefix applied
- `NumberFormat_ConcurrentGeneration_NoCollisions` - Generate numbers concurrently, verify all are unique

### Acceptance Tests
- `NumberFormat_AdminPage_DisplaysCurrentConfiguration` - Log in as admin, navigate to configuration page, verify current format settings displayed
- `NumberFormat_UpdateAndCreate_NewWorkOrderUsesNewFormat` - Update number format via admin page, create new work order, verify work order number matches new format
