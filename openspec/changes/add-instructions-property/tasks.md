## 1. Domain Model

- [ ] 1.1 Add `Instructions` property to `WorkOrder` in `src/Core/Model/WorkOrder.cs` using the existing `getTruncatedString` helper for null normalization and 4000-char truncation

## 2. Data Access

- [ ] 2.1 Add EF Core mapping for `Instructions` in `src/DataAccess/Mappings/WorkOrderMap.cs` with `HasMaxLength(4000)`

## 3. Database Migration

- [ ] 3.1 Create DbUp script `src/Database/scripts/Update/028_AddInstructionsToWorkOrder.sql` adding `Instructions NVARCHAR(4000) NULL` to the `WorkOrder` table

## 4. UI Updates

- [ ] 4.1 Add Instructions text field to the work order create/edit Blazor form components

## 5. Unit Tests

- [ ] 5.1 Add unit test verifying `Instructions` truncates values exceeding 4000 characters
- [ ] 5.2 Add unit test verifying `Instructions` normalizes null to empty string

## 6. Integration Tests

- [ ] 6.1 Add integration test verifying `Instructions` persists and round-trips through EF Core

## 7. Validation

- [ ] 7.1 Run `.\privatebuild.ps1` to confirm all unit and integration tests pass
