# Lab 12: Acceptance Testing with Playwright - L2 Testing

**Curriculum Section:** Section 06 (Operate/Execute - UX Testing After Deployment)
**Estimated Time:** 50 minutes
**Type:** Build

---

## Objective

Write an end-to-end acceptance test that exercises the full work order lifecycle through the browser UI using Playwright.

---

## Steps

### Step 1: Study the Test Base

Open `src/AcceptanceTests/AcceptanceTestBase.cs`. Understand helpers: `LoginAsCurrentUser()`, `Click()`, `Input()`, `Select()`, `Expect()`, `CreateAndSaveNewWorkOrder()`, `AssignExistingWorkOrder()`, `BeginExistingWorkOrder()`, `CompleteExistingWorkOrder()`.

### Step 2: Study Existing Tests

Open `src/AcceptanceTests/WorkOrders/WorkOrderSaveDraftTests.cs` and `WorkOrderAssignTests.cs`.

### Step 3: Write a Full Lifecycle Test

Create `src/AcceptanceTests/WorkOrders/WorkOrderFullLifecycleTests.cs`:

```csharp
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderFullLifecycleTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldCompleteFullWorkOrderLifecycle()
    {
        await LoginAsCurrentUser();

        WorkOrder order = await CreateAndSaveNewWorkOrder();
        await ClickWorkOrderNumberFromSearchPage(order);
        order = await AssignExistingWorkOrder(order, CurrentUser.UserName);
        order.Status.ShouldBe(WorkOrderStatus.Assigned);

        await Page.WaitForURLAsync("**/workorder/search");
        await ClickWorkOrderNumberFromSearchPage(order);
        order = await BeginExistingWorkOrder(order);
        order.Status.ShouldBe(WorkOrderStatus.InProgress);

        await Page.WaitForURLAsync("**/workorder/search");
        await ClickWorkOrderNumberFromSearchPage(order);
        order = await CompleteExistingWorkOrder(order);
        order.Status.ShouldBe(WorkOrderStatus.Complete);

        var finalOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
        finalOrder.ShouldNotBeNull();
        finalOrder!.CompletedDate.ShouldNotBeNull();
    }
}
```

### Step 4: Install Browsers and Run

```powershell
dotnet build src/AcceptanceTests --configuration Debug
pwsh src/AcceptanceTests/bin/Debug/net10.0/playwright.ps1 install
dotnet test src/AcceptanceTests --configuration Debug --filter "FullyQualifiedName~WorkOrderFullLifecycleTests"
```

---

## Expected Outcome

- A passing Playwright test exercising Draft → Assigned → InProgress → Complete

---

## Discussion Questions

1. This test takes seconds/minutes. Unit tests cover the same transitions in milliseconds. Why is the L2 test still valuable?
2. Each test gets its own browser and user via `TestState`. Why is this isolation important for parallel execution?
