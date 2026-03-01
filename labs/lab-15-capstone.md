# Lab 15: Capstone - End-to-End Feature Delivery

**Curriculum Section:** All Sections (Synthesis)
**Estimated Time:** 60 minutes
**Type:** Build

---

## Objective

Deliver a complete feature from design through deployment, exercising every practice from the 3-day course: architecture, domain modeling, database migration, testing at all levels, static analysis, and pull request review.

---

## The Feature: "Cancelled" Work Order Status

A work order can be cancelled from **Draft** or **Assigned** status, but only by the **Creator**. Once cancelled, no further transitions are allowed.

State machine update:
```
    [Draft] --------→ [Cancelled]
       |                    ↑
       v                    |
    [Assigned] --------→ [Cancelled]
       |
       v
    [InProgress]
       |
       v
    [Complete]
```

---

## Steps

### Step 1: Domain Model - Add the Status (Core Layer)

Open `src/Core/Model/WorkOrderStatus.cs`.

**Add the Cancelled status** after the Complete definition:

```csharp
public static readonly WorkOrderStatus Cancelled = new("CXL", "Cancelled", "Cancelled", 5);
```

**Update `GetAllItems()`** to include the new status:

```csharp
public static WorkOrderStatus[] GetAllItems()
{
    return new[]
    {
        Draft,
        Assigned,
        InProgress,
        Complete,
        Cancelled
    };
}
```

### Step 2: Create the State Command (Core Layer)

Create `src/Core/Model/StateCommands/CancelWorkOrderCommand.cs`:

```csharp
namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record CancelWorkOrderCommand(WorkOrder WorkOrder, Employee CurrentUser)
    : StateCommandBase(WorkOrder, CurrentUser)
{
    public const string Name = "Cancel";

    public override WorkOrderStatus GetBeginStatus()
    {
        return WorkOrder.Status;
    }

    public override WorkOrderStatus GetEndStatus()
    {
        return WorkOrderStatus.Cancelled;
    }

    public override string TransitionVerbPresentTense => Name;

    public override string TransitionVerbPastTense => "Cancelled";

    public override bool IsValid()
    {
        var isInCancellableStatus = WorkOrder.Status == WorkOrderStatus.Draft
                                   || WorkOrder.Status == WorkOrderStatus.Assigned;
        var userIsCreator = UserCanExecute(CurrentUser);
        return isInCancellableStatus && userIsCreator;
    }

    protected override bool UserCanExecute(Employee currentUser)
    {
        return currentUser == WorkOrder.Creator;
    }
}
```

Note: This command overrides `IsValid()` because it accepts **multiple** begin statuses (Draft or Assigned), unlike standard commands that accept exactly one.

### Step 3: Database Migration

Create `src/Database/scripts/Update/024_EnsureCancelledStatusSupported.sql`:

```sql
-- No schema change needed: WorkOrder.Status stores the Code ("CXL")
-- as NVARCHAR(3), which already accommodates the new value.
-- This migration is a documentation marker for the Cancelled status addition.
PRINT 'Cancelled status (CXL) support confirmed - no schema changes required';
```

> **Why a no-op migration?** The status column stores 3-character codes. "CXL" fits the existing schema. The migration script documents the change for the deployment pipeline. Check the latest existing migration number and increment accordingly.

### Step 4: Unit Tests

Create `src/UnitTests/Core/Model/StateCommands/CancelWorkOrderCommandTests.cs`:

```csharp
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class CancelWorkOrderCommandTests
{
    [Test]
    public void ShouldBeValidWhenDraftAndCreator()
    {
        var creator = new Employee("creator", "Test", "Creator", "c@test.com");
        var order = new WorkOrder { Status = WorkOrderStatus.Draft, Creator = creator };

        var command = new CancelWorkOrderCommand(order, creator);

        command.IsValid().ShouldBeTrue();
    }

    [Test]
    public void ShouldBeValidWhenAssignedAndCreator()
    {
        var creator = new Employee("creator", "Test", "Creator", "c@test.com");
        var assignee = new Employee("assignee", "Test", "Assignee", "a@test.com");
        var order = new WorkOrder
        {
            Status = WorkOrderStatus.Assigned,
            Creator = creator,
            Assignee = assignee
        };

        var command = new CancelWorkOrderCommand(order, creator);

        command.IsValid().ShouldBeTrue();
    }

    [Test]
    public void ShouldNotBeValidWhenInProgress()
    {
        var creator = new Employee("creator", "Test", "Creator", "c@test.com");
        var order = new WorkOrder { Status = WorkOrderStatus.InProgress, Creator = creator };

        var command = new CancelWorkOrderCommand(order, creator);

        command.IsValid().ShouldBeFalse();
    }

    [Test]
    public void ShouldNotBeValidWhenComplete()
    {
        var creator = new Employee("creator", "Test", "Creator", "c@test.com");
        var order = new WorkOrder { Status = WorkOrderStatus.Complete, Creator = creator };

        var command = new CancelWorkOrderCommand(order, creator);

        command.IsValid().ShouldBeFalse();
    }

    [Test]
    public void ShouldNotBeValidWhenNotCreator()
    {
        var creator = new Employee("creator", "Test", "Creator", "c@test.com");
        var otherUser = new Employee("other", "Other", "User", "o@test.com");
        var order = new WorkOrder { Status = WorkOrderStatus.Draft, Creator = creator };

        var command = new CancelWorkOrderCommand(order, otherUser);

        command.IsValid().ShouldBeFalse();
    }

    [Test]
    public void ShouldTransitionToCancelled()
    {
        var creator = new Employee("creator", "Test", "Creator", "c@test.com");
        var order = new WorkOrder
        {
            Number = "TST1234",
            Status = WorkOrderStatus.Draft,
            Creator = creator
        };

        var command = new CancelWorkOrderCommand(order, creator);
        command.Execute(new StateCommandContext());

        order.Status.ShouldBe(WorkOrderStatus.Cancelled);
    }
}
```

### Step 5: Add WorkOrderStatus Unit Tests

Add to `src/UnitTests/Core/Model/WorkOrderStatusTests.cs`:

```csharp
[Test]
public void ShouldResolveCancelledFromCode()
{
    var status = WorkOrderStatus.FromCode("CXL");

    status.ShouldBe(WorkOrderStatus.Cancelled);
}

[Test]
public void ShouldResolveCancelledFromKey()
{
    var status = WorkOrderStatus.FromKey("Cancelled");

    status.ShouldBe(WorkOrderStatus.Cancelled);
}
```

### Step 6: Run Unit Tests

```powershell
dotnet test src/UnitTests --configuration Release
```

All tests should pass.

### Step 7: Integration Test

Add an integration test in `src/IntegrationTests/DataAccess/Handlers/` that verifies the cancel command persists:

```csharp
[Test]
public async Task ShouldCancelDraftWorkOrder()
{
    new DatabaseTests().Clean();

    var creator = Faker<Employee>();
    var context = TestHost.GetRequiredService<DbContext>();
    context.Add(creator);
    await context.SaveChangesAsync();

    var workOrder = Faker<WorkOrder>();
    workOrder.Id = Guid.Empty;
    workOrder.Creator = creator;
    workOrder.CreatedDate = null;

    var saveCommand = RemotableRequestTests.SimulateRemoteObject(
        new SaveDraftCommand(workOrder, creator));
    var handler = TestHost.GetRequiredService<StateCommandHandler>();
    var saveResult = await handler.Handle(saveCommand);

    var cancelCommand = RemotableRequestTests.SimulateRemoteObject(
        new CancelWorkOrderCommand(saveResult.WorkOrder, creator));
    var cancelResult = await handler.Handle(cancelCommand);

    cancelResult.WorkOrder.Status.ShouldBe(WorkOrderStatus.Cancelled);

    var verifyContext = TestHost.GetRequiredService<DbContext>();
    var persisted = verifyContext.Find<WorkOrder>(cancelResult.WorkOrder.Id);
    persisted.ShouldNotBeNull();
    persisted!.Status.ShouldBe(WorkOrderStatus.Cancelled);
}
```

### Step 8: Run Full Build

```powershell
.\privatebuild.ps1
```

### Step 9: Static Analysis

```powershell
dotnet format style src/ChurchBulletin.sln --verify-no-changes
```

### Step 10: Create Pull Request

```powershell
git checkout -b yourusername/add-cancelled-status
git add -A
git commit -m "Add Cancelled work order status with full test coverage"
git push -u origin yourusername/add-cancelled-status
gh pr create --title "Add Cancelled work order status" --body "## Summary
- Added WorkOrderStatus.Cancelled (Code: CXL, Key: Cancelled)
- Created CancelWorkOrderCommand accepting Draft or Assigned status
- Only the Creator can cancel a work order
- Unit tests for all valid/invalid transitions
- Integration test for persistence
- Database migration marker script

## Test Plan
- [x] Unit tests: 6 new tests for CancelWorkOrderCommand
- [x] Unit tests: 2 new tests for WorkOrderStatus
- [x] Integration test: Cancel a draft work order through handler
- [x] privatebuild.ps1 green
- [x] Static analysis passes"
```

---

## Expected Outcome

- A complete feature delivered through the full development lifecycle
- All quality gates passing (unit tests, integration tests, static analysis, build)
- A PR ready for review

---

## Synthesis: Trace Back Through the Five Pillars

| Pillar | How This Lab Demonstrated It |
|--------|------------------------------|
| **Create Clarity** | Clear requirements: cancel from Draft/Assigned, Creator only. State machine diagram updated. |
| **Establish Quality** | Tests at L0 (unit) and L1 (integration). Static analysis passing. |
| **Achieve Stability** | No regressions — existing tests still green. Migration is safe (no schema change). |
| **Increase Speed** | Patterns from earlier labs accelerated development. Command pattern provided a template. |
| **Optimize the Team** | PR review enables knowledge sharing. Consistent conventions reduce cognitive load. |

---

## Discussion Questions

1. How long did this feature take compared to what it would take without the patterns and conventions established in Days 1-2?
2. The `CancelWorkOrderCommand` overrides `IsValid()` to accept multiple begin statuses. Is this a design smell or a valid extension of the pattern? What alternatives exist?
3. No UI changes were made in this lab. If you had 30 more minutes, what UI changes would be needed? (Cancel button on the manage page, "Cancelled" in status filters, greyed-out display)
4. An MCP tool `cancel-work-order` could be added for AI agents. What would it look like? (Follow the pattern from Lab 14)
5. Reflect on the "Three Things" from the wrap-up: When you get back to work, what three practices from this course could create positive momentum?
