## Why
Sales demonstrations, developer onboarding, and testing environments all require realistic sample data. A seeding command that populates the database with diverse work requests and employees eliminates manual data entry, ensures consistent demo environments, and accelerates new developer setup.

## What Changes
- Add `DatabaseSeeder` class in `src/DataAccess/Services/` with method `SeedDemoData()` that creates 50 work requests and 10 employees
- Add `IDatabaseSeeder` interface in `src/Core/Interfaces/`
- Generate 10 employees with varied roles: 3 with CanCreateWorkRequest, 5 with CanFulfillWorkRequest, 2 with both
- Generate 50 work requests distributed across statuses: 10 Draft, 12 Assigned, 13 InProgress, 10 Complete, 5 Cancelled
- Use realistic titles (e.g., "Replace fluorescent lights in Room 204", "Fix leaking faucet in restroom B2")
- Assign work requests to employees with appropriate roles
- Set realistic dates: CreatedDate spread over last 60 days, AssignedDate 1-3 days after creation, CompletedDate 2-7 days after assignment
- Add `SeedDemoDataCommand` in `src/Core/Commands/` and handler in `src/DataAccess/Handlers/`
- Add `--seed` command-line argument support in `src/UI/Server/Program.cs` to trigger seeding on startup
- Add idempotency check to prevent duplicate seeding

## Capabilities
### New Capabilities
- Command-line triggered database seeding with `--seed` argument
- 50 realistic work requests spanning all status values with plausible date progressions
- 10 employees with varied role assignments reflecting real organizational structure
- Idempotent seeding that skips if demo data already exists

### Modified Capabilities
- None

## Impact
- **src/Core/Interfaces/** - New `IDatabaseSeeder` interface
- **src/Core/Commands/** - New `SeedDemoDataCommand`
- **src/DataAccess/Services/** - New `DatabaseSeeder` implementation
- **src/DataAccess/Handlers/** - New seed command handler
- **src/UI/Server/Program.cs** - `--seed` command-line argument handling
- **Dependencies** - No new NuGet packages required; uses hardcoded realistic sample data or standard `Random` for variety (note: AutoBogus is test-only in `src/UnitTests/` and must NOT be used in production seeding code)
- **Database** - No schema changes; data only

## Acceptance Criteria
### Unit Tests
- `DatabaseSeeder_Creates50WorkRequests` - Seeder generates exactly 50 work requests
- `DatabaseSeeder_Creates10Employees` - Seeder generates exactly 10 employees
- `DatabaseSeeder_WorkRequestStatusDistribution_CoverAllStatuses` - Generated work requests include at least one of each status
- `DatabaseSeeder_EmployeeRoles_IncludeCreatorsAndFulfillers` - Generated employees include both CanCreateWorkRequest and CanFulfillWorkRequest roles
- `DatabaseSeeder_Dates_AreRealistic` - CreatedDate is within last 60 days, AssignedDate is after CreatedDate, CompletedDate is after AssignedDate
- `DatabaseSeeder_Idempotent_DoesNotDuplicate` - Running seeder twice produces same record count

### Integration Tests
- `SeedDemoData_PersistsAllRecords` - Run seeder against test database, verify 50 work requests and 10 employees persisted
- `SeedDemoData_WorkRequestsHaveValidAssignees` - All assigned/in-progress/complete work requests reference valid employee records
- `SeedDemoData_SecondRun_NoNewRecords` - Run seeder twice, verify record count remains 50/10

### Acceptance Tests
- `DemoSeed_AfterSeeding_WorkRequestListPopulated` - Start application with `--seed`, navigate to work request list, verify work requests are displayed
- `DemoSeed_AfterSeeding_VariousStatusesVisible` - Navigate to work request list after seeding, verify work requests in multiple statuses appear
