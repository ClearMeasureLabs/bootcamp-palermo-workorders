## Context

The ChurchBulletin system manages work orders through an Onion Architecture with CQRS via MediatR. The `WorkOrder` entity in `src/Core/Model/WorkOrder.cs` currently has `Title` (max 300 chars), `Description` (max 4000 chars, auto-truncated), and `RoomNumber` (max 50 chars) as text fields. The work order create/manage screen (`WorkOrderManage.razor`) renders these fields with `InputText` and `InputTextArea` controls, and each text field has a megaphone button for speech synthesis via `ITranslationService` and `SpeechSynthesis`.

The `Instructions` field follows the same pattern as `Description` — a large optional text field — but serves a different purpose: Description captures _what_ work needs to be done, while Instructions captures _how_ to do it.

## Goals / Non-Goals

**Goals:**
- Add an `Instructions` property to the `WorkOrder` domain entity as an optional field with 4000-character max length
- Persist Instructions through the existing EF Core / SQL Server data path
- Display Instructions on the work order manage screen below the Description field, following the same UI patterns (InputTextArea, megaphone button, disabled when read-only)
- Include Instructions in MCP tool responses and accept it during work order creation via MCP
- Full test coverage: unit, integration, and acceptance tests

**Non-Goals:**
- Rich text or markdown support for Instructions (plain text only, same as Description)
- Separate search/filter by Instructions content
- Instructions field on the work order search results table
- Any changes to the state command workflow or authorization model

## Decisions

### Decision 1: Instructions is nullable in the database, defaults to empty string in the domain

**Rationale:** Following the exact pattern of `Description`, the database column is `NVARCHAR(4000) NULL` (no default constraint needed since it is nullable). In the domain entity, the backing field initializes to `""` and the setter truncates to 4000 characters, matching the `Description` property implementation. This keeps the field optional — existing work orders will have `NULL` Instructions in the database, which EF Core will map to `null`, and the domain setter will normalize to `""`.

**Alternatives considered:**
- `NOT NULL` with a default constraint: Unnecessary since the field is optional and the domain handles null normalization
- No truncation: Inconsistent with Description behavior and could cause database errors

### Decision 2: Place Instructions below Description in the form layout

**Rationale:** The issue requests "put it underneath description." This follows logical reading order: Title (what is it), Description (what needs to be done), Instructions (how to do it), Room Number (where).

### Decision 3: Include a megaphone/speak button for Instructions

**Rationale:** Title and Description both have megaphone buttons for speech synthesis. Instructions should follow the same UX pattern for consistency. The button calls the same `SpeakTextAsync` method used by the other fields.

### Decision 4: Max length of 4000 characters, matching Description

**Rationale:** The issue specifies "make it in our char 4000." This matches the existing Description field constraint, providing ample space for detailed instructions while preventing unbounded input.

### Decision 5: Update MCP tools to include Instructions

**Rationale:** The MCP server's `WorkOrderTools` returns work order details and accepts creation parameters. Instructions should be included in the `FormatWorkOrderDetail` response and accepted as an optional parameter in the `create-work-order` tool, keeping the MCP interface complete and consistent.

## Risks / Trade-offs

- **[Backward compatibility]** Existing work orders will have `NULL` Instructions. → Mitigation: The column is nullable, and the domain entity defaults to `""`. UI displays an empty textarea. No data migration needed.
- **[Form length]** Adding another large textarea increases the form height. → Mitigation: The field is optional and the textarea can be the same size as Description. The form already scrolls on smaller viewports.
- **[MCP breaking change]** Adding a new optional parameter to `create-work-order` is backward-compatible. Adding Instructions to the detail response is additive. No breaking changes.

## Open Questions

- None. The scope is straightforward and the patterns are well-established in the codebase.
