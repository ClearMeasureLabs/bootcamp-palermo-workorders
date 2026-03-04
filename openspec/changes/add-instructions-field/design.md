## Context

The WorkOrder domain model in `src/Core/Model/WorkOrder.cs` currently has Title, Description, RoomNumber, Status, Creator, Assignee, Number, dates, and Attachments. The Description property auto-uppercases its value and truncates to 4000 characters. The EF mapping in `WorkOrderMap.cs` maps these to the `[dbo].[WorkOrder]` table. The Blazor form in `WorkOrderManage.razor` renders fields with data-testid attributes and supports read-only mode.

## Goals / Non-Goals

**Goals:**
- Add an Instructions field to WorkOrder that stores actionable guidance for the assignee
- Follow existing patterns: nullable string, max 4000 characters, mapped in EF, rendered in the form
- Instructions should NOT be auto-uppercased (unlike Description)
- Instructions should respect the read-only mode in the UI

**Non-Goals:**
- Rich text or markdown support for instructions
- Validation rules beyond max length
- Changes to state commands or workflow transitions

## Decisions

### Decision 1: Instructions as a simple nullable string property

**Rationale:** Follows the same pattern as Description — a string field with max length. Unlike Description, Instructions will NOT be auto-uppercased since instructions benefit from mixed case readability.

### Decision 2: Place Instructions field after Description in the UI form

**Rationale:** Natural reading order — Title describes the work, Description provides context, Instructions tell the assignee what to do, Room tells where.

### Decision 3: DbUp migration adds a nullable nvarchar(4000) column

**Rationale:** Consistent with the existing Description column size. Nullable because existing work orders won't have instructions and new ones shouldn't require them.

## Risks / Trade-offs

- **[Backward compatibility]** Existing work orders will have NULL instructions. → Acceptable; the field is nullable and the UI handles null gracefully.
