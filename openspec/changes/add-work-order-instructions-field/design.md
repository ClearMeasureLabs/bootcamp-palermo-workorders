## Context

The system already stores mutable work order text fields (`Title`, `Description`) on the `WorkOrder` entity and edits them in `WorkOrderManage`. `Description` is optional and capped at 4000 characters. The new `Instructions` field follows the same storage and editing pattern while remaining optional.

## Goals / Non-Goals

**Goals:**
- Add optional instructions for work order creators
- Keep instructions length capped at 4000 characters
- Show instructions directly below description in the manage form
- Persist and return instructions in all normal work order retrieval paths

**Non-Goals:**
- No workflow/state changes
- No required-field validation for instructions
- No new role or authorization logic

## Decisions

### Decision 1: Model-level truncation to 4000 characters

`WorkOrder.Instructions` uses the same truncation helper as `Description` so oversized values are safely constrained before persistence.

### Decision 2: Optional nullable database column

Migration adds `[Instructions] NVARCHAR(4000) NULL` to `dbo.WorkOrder`, preserving backward compatibility and allowing existing records to remain valid.

### Decision 3: UI placement under Description on WorkOrderManage

`WorkOrderManage.razor` adds an `InputTextArea` bound to `Model.Instructions` immediately below the Description group, matching requested placement.

### Decision 4: Include instructions in MCP detail/create paths

MCP `create-work-order` accepts optional instructions and `get-work-order` detail output includes instructions so AI-based and programmatic consumers retain parity with UI data.

## Risks / Trade-offs

- Existing tests asserting complete work order detail payloads require updates
- Additional nullable text column slightly increases payload size for detail retrieval
