# Lab 04: Tracing the State Machine - Domain Model Deep Dive

**Curriculum Section:** Section 03 (System Architecture - Vision)
**Estimated Time:** 45 minutes
**Type:** Analyze

---

## Objective

Understand the State Command pattern and work order lifecycle by tracing every transition and its validation rules.

---

## Steps

### Step 1: Map the Status Values

Open `src/Core/Model/WorkOrderStatus.cs`. Document all statuses with their Code, Key, and FriendlyName.

### Step 2: Read the State Command Base

Open `src/Core/Model/StateCommands/StateCommandBase.cs`. Study `IsValid()` — it checks both `WorkOrder.Status == GetBeginStatus()` AND `UserCanExecute(CurrentUser)`.

### Step 3: Document Each State Command

Read each command in `src/Core/Model/StateCommands/`:

| Command | Begin Status | End Status | Who Can Execute | Dates Set |
|---------|-------------|------------|-----------------|-----------|
| `SaveDraftCommand` | Draft | Draft | Creator | CreatedDate (first save) |
| `DraftToAssignedCommand` | Draft | Assigned | Creator | AssignedDate |
| `AssignedToInProgressCommand` | Assigned | InProgress | Assignee | (none) |
| `InProgressToCompleteCommand` | InProgress | Complete | Assignee | CompletedDate |
| `UpdateDescriptionCommand` | (current) | (current) | Creator OR Assignee | (none) |

### Step 4: Draw the State Machine

Draw nodes for each status and arrows for each transition, labeled with command name and actor.

### Step 5: Study the Unit Tests

Read tests in `src/UnitTests/Core/Model/StateCommands/`. Observe naming conventions, AAA pattern, and `Stub` prefix.

### Step 6: Trace the Handler

Open `src/DataAccess/Handlers/StateCommandHandler.cs`. Follow: `command.Execute()` → Attach/Update → `SaveChangesAsync()` → Publish events → Return result.

### Step 7: Challenge Question

What files would need to change to add a "Cancelled" state from Draft or Assigned, executable only by the Creator?

---

## Expected Outcome

- Complete state machine diagram with all transitions
- Understanding of the command pattern and two-part validation

---

## Discussion Questions

1. Why is each state transition a separate class instead of a switch/case?
2. How does this relate to "structuring services by volatility"?
3. The `WorkOrderAssignedToBotEvent` in `DraftToAssignedCommand` shows event-driven behavior. How does this enable extensibility?
