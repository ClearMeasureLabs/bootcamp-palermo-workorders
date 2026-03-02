# Lab 21: Capstone - End-to-End Feature Delivery

**Curriculum Section:** All Sections (Synthesis)
**Estimated Time:** 60 minutes
**Type:** Build

---

## Objective

Deliver a complete feature from design through pull request, exercising every practice from the course: architecture, domain modeling, database migration, testing at all levels, static analysis, and PR review.

---

## The Feature: "Cancelled" Work Order Status

A work order can be cancelled from **Draft** or **Assigned** status, but only by the **Creator**.

```
    [Draft] --------→ [Cancelled]
       |                    ↑
       v                    |
    [Assigned] --------→ [Cancelled]
```

---

## Steps

### Step 1: Add the Status (Core)

In `src/Core/Model/WorkOrderStatus.cs`, add:

```csharp
public static readonly WorkOrderStatus Cancelled = new("CXL", "Cancelled", "Cancelled", 5);
```

Update `GetAllItems()` to include it.

### Step 2: Create the State Command (Core)

Create `src/Core/Model/StateCommands/CancelWorkOrderCommand.cs`:

```csharp
public record CancelWorkOrderCommand(WorkOrder WorkOrder, Employee CurrentUser)
    : StateCommandBase(WorkOrder, CurrentUser)
{
    public const string Name = "Cancel";
    public override WorkOrderStatus GetBeginStatus() => WorkOrder.Status;
    public override WorkOrderStatus GetEndStatus() => WorkOrderStatus.Cancelled;
    public override string TransitionVerbPresentTense => Name;
    public override string TransitionVerbPastTense => "Cancelled";

    public override bool IsValid()
    {
        var cancellable = WorkOrder.Status == WorkOrderStatus.Draft
                       || WorkOrder.Status == WorkOrderStatus.Assigned;
        return cancellable && UserCanExecute(CurrentUser);
    }

    protected override bool UserCanExecute(Employee currentUser)
        => currentUser == WorkOrder.Creator;
}
```

### Step 3: Unit Tests

Create `src/UnitTests/Core/Model/StateCommands/CancelWorkOrderCommandTests.cs` with tests:
- Valid from Draft when Creator
- Valid from Assigned when Creator
- Invalid from InProgress
- Invalid from Complete
- Invalid when not Creator
- Transitions to Cancelled status

### Step 4: Integration Test

Add a test that saves a Draft, cancels it via the handler, and verifies the persisted status is `Cancelled`.

### Step 5: Build and Verify

```powershell
dotnet format style src/ChurchBulletin.sln --verify-no-changes
.\privatebuild.ps1
```

### Step 6: Create Pull Request

Branch, commit, push, and create a PR with checklist.

---

## Synthesis: The Five Pillars

| Pillar | How This Lab Demonstrates It |
|--------|------------------------------|
| **Create Clarity** | Clear requirements: cancel from Draft/Assigned, Creator only |
| **Establish Quality** | Tests at L0 and L1, static analysis passing |
| **Achieve Stability** | No regressions — existing tests green |
| **Increase Speed** | Patterns accelerated development |
| **Optimize the Team** | PR review enables knowledge sharing |

---

## Discussion Questions

1. How long did this take compared to without established patterns?
2. The command overrides `IsValid()` for multiple begin statuses. Design smell or valid extension?
3. What UI and MCP tool changes would complete this feature?
4. Reflect: what three practices from this course could create positive momentum at work?
