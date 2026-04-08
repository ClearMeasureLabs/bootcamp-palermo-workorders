## Context

The ChurchBulletin system follows Onion Architecture with a CQRS pattern via MediatR. The `WorkOrder` entity currently has `Title` (NVARCHAR 300), `Description` (NVARCHAR 4000, with domain-level truncation), `RoomNumber` (NVARCHAR 50), and several other properties. The Description field uses a private backing field with a `getTruncatedString` helper that caps values at 4000 characters. The EF Core mapping in `WorkOrderMap.cs` declares max lengths that align with the database column definitions.

The Blazor UI uses a `WorkOrderManageModel` view model with `[Required]` on Title and Description. The code-behind (`WorkOrderManage.razor.cs`) maps between the view model and domain entity in `CreateViewModel()` and `HandleSubmit()`. The MCP server's `WorkOrderTools.cs` exposes `CreateWorkOrder` and `FormatWorkOrderDetail` methods that would need updating.

The most recent database migration is `027_UpdateWorkOrderDFTDRT.sql`. All migrations follow a `BEGIN TRANSACTION` / `PRINT` / DDL / error check / `COMMIT TRANSACTION` pattern with TAB indentation.

## Goals / Non-Goals

**Goals:**
- Add an optional Instructions field to the WorkOrder entity with a 4000 character limit, matching the Description pattern
- Display the field on the create/edit screen below Description with an `InputTextArea`
- Persist the field through the full stack: domain model, EF Core mapping, database column, UI, and MCP tools
- Maintain all existing tests passing and add new test coverage for the Instructions property

**Non-Goals:**
- Making Instructions a required field (the issue explicitly states it is optional)
- Adding Instructions to the search results table (it is a detail field, not a summary field)
- Adding speech synthesis support for Instructions (can be added later if needed)
- Rich text or markdown formatting for Instructions (plain text only)

## Decisions

### Decision 1: Use NVARCHAR(4000) with domain-level truncation, matching Description

**Rationale:** The issue says "make it varchar 4000." The Description field already uses this exact pattern: `NVARCHAR(4000)` in the database, `HasMaxLength(4000)` in EF Core, and a `getTruncatedString` helper in the domain model. Reusing the same approach ensures consistency and predictability.

**Alternatives considered:**
- NVARCHAR(MAX): Would allow unlimited text but deviates from the explicit requirement and the established pattern
- A separate truncation method: Unnecessary since the existing `getTruncatedString` method can be reused

### Decision 2: Optional field with no `[Required]` annotation

**Rationale:** The issue explicitly states "it's an optional field." The view model will not have a `[Required]` attribute on Instructions. The database column will allow NULLs (or default to empty string, following the Description pattern).

### Decision 3: Position below Description on the create/edit form

**Rationale:** The issue explicitly says "put it underneath description." The `InputTextArea` will be placed in the form grid immediately after the Description field and before the RoomNumber field.

### Decision 4: Reuse existing `getTruncatedString` helper for truncation

**Rationale:** The `WorkOrder` class already has a private `getTruncatedString(string? value)` method that truncates to 4000 characters. The Instructions property should use the same method for its setter, ensuring consistent behavior.

## Risks / Trade-offs

- **[Migration ordering]** The new migration `028_AddInstructionsToWorkOrder.sql` must run after all prior migrations. Since DbUp processes scripts in alphabetical order by convention, the `028_` prefix ensures correct ordering.
- **[Null vs empty string]** The Description property defaults to `""` and its truncation helper converts `null` to `string.Empty`. Instructions will follow the same pattern, defaulting to `""`, so downstream code doesn't need null checks.

## Open Questions

- Should the Instructions field be included in the speech synthesis feature (megaphone button) like Title and Description? This could be added as a follow-up.
