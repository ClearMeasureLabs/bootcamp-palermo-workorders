# Lab 04: Tracing the State Machine - Domain Model Deep Dive

**Curriculum Section:** Section 03 (System Architecture - Vision)
**Estimated Time:** 45 minutes
**Type:** Analyze

---

## Objective

Understand the State Command pattern and the work order lifecycle by tracing code and documenting every transition. Map the domain model to architectural concepts from the lecture: designing software that solves real problems, structured by volatility.

---

## Context

The work order system uses a **State Machine** implemented through the **Command Pattern** with MediatR CQRS. Each state transition is a separate command class that validates preconditions and executes the transition.

---

## Steps

### Step 1: Map the Status Values

Open `src/Core/Model/WorkOrderStatus.cs`. Document all statuses:

| Status | Code | Key | FriendlyName | SortBy |
|--------|------|-----|--------------|--------|
| None | `""` | `""` | `" "` | 0 |
| Draft | `"DFT"` | `"Draft"` | `"Draft"` | 1 |
| Assigned | `"ASD"` | `"Assigned"` | `"Assigned"` | 2 |
| InProgress | `"IPG"` | `"InProgress"` | `"In Progress"` | 3 |
| Complete | `"CMP"` | `"Complete"` | `"Complete"` | 4 |

Note the **smart enum pattern**: `WorkOrderStatus` is a class with static readonly instances, not a C# `enum`. Factory methods `FromCode()` and `FromKey()` convert between representations.

### Step 2: Read the State Command Base

Open `src/Core/Model/StateCommands/StateCommandBase.cs`:

```csharp
public abstract record StateCommandBase(WorkOrder WorkOrder, Employee CurrentUser) : IStateCommand
```

Key methods:
- `IsValid()` — checks `WorkOrder.Status == GetBeginStatus()` AND `UserCanExecute(CurrentUser)`
- `Execute(StateCommandContext context)` — calls `WorkOrder.ChangeStatus(CurrentUser, date, GetEndStatus())`
- `Matches(string commandName)` — matches by `TransitionVerbPresentTense`

### Step 3: Document Each State Command

Read each command file in `src/Core/Model/StateCommands/` and fill in this table:

| Command | File | Begin Status | End Status | Who Can Execute | Dates Set | Special Behavior |
|---------|------|-------------|------------|-----------------|-----------|------------------|
| `SaveDraftCommand` | `SaveDraftCommand.cs` | Draft | Draft | Creator | CreatedDate (first save only) | No status change |
| `DraftToAssignedCommand` | `DraftToAssignedCommand.cs` | Draft | Assigned | Creator | AssignedDate | Emits `WorkOrderAssignedToBotEvent` if assignee has Bot role |
| `AssignedToInProgressCommand` | `AssignedToInProgressCommand.cs` | Assigned | InProgress | Assignee | (none) | |
| `InProgressToCompleteCommand` | `InProgressToCompleteCommand.cs` | InProgress | Complete | Assignee | CompletedDate | |
| `UpdateDescriptionCommand` | `UpdateDescriptionCommand.cs` | (current) | (current) | Creator OR Assignee | (none) | No status change; allows updates at any state |

### Step 4: Draw the State Machine

On paper, draw nodes for each status and arrows for each transition:

```
                    SaveDraft
                   +---------+
                   |         |
                   v         |
    [Draft] ------+----------+
       |
       | DraftToAssigned (Creator)
       v
    [Assigned]
       |
       | AssignedToInProgress (Assignee)
       v
    [InProgress]
       |
       | InProgressToComplete (Assignee)
       v
    [Complete]
```

Label each arrow with:
- Command name
- Who can execute (Creator vs Assignee)
- Which dates are set

### Step 5: Trace the Validation Logic

Open `src/Core/Model/StateCommands/StateCommandBase.cs` and study `IsValid()`:

```csharp
public bool IsValid()
{
    var beginStatusMatches = WorkOrder.Status == GetBeginStatus();
    var currentUserIsCorrectRole = UserCanExecute(CurrentUser);
    return beginStatusMatches && currentUserIsCorrectRole;
}
```

This means a transition is only valid when BOTH conditions are true:
1. The work order is in the correct starting status
2. The current user has the right relationship (Creator or Assignee)

**Trace example:** If a work order is in `Assigned` status and the **Creator** tries to call `AssignedToInProgressCommand`:
- `GetBeginStatus()` returns `Assigned` — matches
- `UserCanExecute(Creator)` checks `currentUser == WorkOrder.Assignee` — **fails** (Creator != Assignee)
- `IsValid()` returns `false`

### Step 6: Study the Unit Tests

Read the test files in `src/UnitTests/Core/Model/StateCommands/`:

- `DraftToAssignedCommandTests.cs` — Tests invalid status, wrong employee, valid case, and state transition
- `SaveDraftCommandTests.cs` — Tests save behavior
- `AssignedToInProgressCommandTests.cs` — Tests assignee requirement
- `InProgressToCompleteCommandTests.cs` — Tests completion

Note the test patterns:
- **Naming:** `ShouldNotBeValidInWrongStatus`, `ShouldBeValid`, `ShouldTransitionStateProperly`
- **Assertions:** `Assert.That(command.IsValid(), Is.False)` — NUnit syntax
- **No mocking:** Tests use real domain objects, not mocks
- **Base class:** `StateCommandBaseTests` provides shared test infrastructure

### Step 7: Trace the Handler

Open `src/DataAccess/Handlers/StateCommandHandler.cs`. Understand the flow:

1. `command.Execute(new StateCommandContext { CurrentDateTime = time })` — Mutates the work order
2. Attach/Add or Update the work order in `DbContext`
3. `SaveChangesAsync()` — Persist to database
4. Publish `StateTransitionEvent` if present (e.g., `WorkOrderAssignedToBotEvent`)
5. Return `StateCommandResult`

### Step 8: Challenge Question

What would be needed to add a **"Cancelled"** state that any participant (Creator or Assignee) can trigger from Draft or Assigned status?

List every file that would need to change:

1. `WorkOrderStatus.cs` — Add `Cancelled` static field
2. `WorkOrderStatus.cs` — Add to `GetAllItems()` array
3. New file: `CancelWorkOrderCommand.cs` in `StateCommands/`
4. `src/Database/scripts/Update/` — New migration script (if schema changes needed)
5. `src/DataAccess/Mappings/WorkOrderMap.cs` — (if new column)
6. Unit tests for the new command
7. Integration tests for the handler
8. UI changes (button, status display)

---

## Expected Outcome

- A complete state machine diagram with all transitions labeled
- Understanding of the command pattern and two-part validation (status + user role)
- A list of files needed for the "Cancelled" state challenge

---

## Discussion Questions

1. Why is each state transition a **separate class** instead of a single method with switch/case?
2. How does the command pattern relate to "structuring services by volatility"? What changes when business rules evolve?
3. The `UpdateDescriptionCommand` doesn't change status — why does it still exist as a command instead of a direct save?
4. The `WorkOrderAssignedToBotEvent` in `DraftToAssignedCommand` shows event-driven behavior. How does this enable extensibility without modifying the core workflow?
5. How does the **4+1 Architectural View Model** apply here? Which view does the state machine diagram represent?
