# Lab 11: Acceptance Testing with Playwright - L2 Testing

**Curriculum Section:** Section 06 (Operate/Execute - UX Testing After Deployment)
**Estimated Time:** 50 minutes
**Type:** Build

---

## Objective

Write an end-to-end acceptance test that exercises the full work order lifecycle through the browser UI using Playwright. Understand when L2 tests are worth the tradeoff of slower execution.

---

## Context

**L2 Tests** are functional tests that involve multiple components — the UI, API, database, and browser. They verify the complete user experience. This project uses:

- **Playwright** for browser automation
- **AcceptanceTestBase** with helpers: `LoginAsCurrentUser()`, `Click()`, `Input()`, `Select()`, `Expect()`
- **Parallel execution** with isolated test users (`CreateTestUser(testTag)`)
- **Playwright tracing** for debugging failed tests

---

## Steps

### Step 1: Study the Test Base

Open `src/AcceptanceTests/AcceptanceTestBase.cs`. Understand the key helpers:

| Helper | Purpose |
|--------|---------|
| `LoginAsCurrentUser()` | Navigates to login page, selects the test user, submits login |
| `Click(testId)` | Clicks element by `data-testid` using `EvaluateAsync` (avoids Blazor WASM timeout) |
| `Input(testId, value)` | Fills an input field, waits for editability, verifies the value |
| `Select(testId, value)` | Selects a dropdown option |
| `Expect(locator)` | Playwright assertion wrapper |
| `CreateAndSaveNewWorkOrder()` | Creates a draft work order through the UI |
| `AssignExistingWorkOrder(order, username)` | Assigns a work order through the UI |
| `BeginExistingWorkOrder(order)` | Begins a work order through the UI |
| `CompleteExistingWorkOrder(order)` | Completes a work order through the UI |
| `TakeScreenshotAsync()` | Captures a screenshot for debugging |

### Step 2: Study an Existing Acceptance Test

Open `src/AcceptanceTests/WorkOrders/WorkOrderSaveDraftTests.cs`:

```csharp
[Test, Retry(2)]
public async Task ShouldCreateNewWorkOrderAndVerifyOnSearchScreen()
{
    await LoginAsCurrentUser();
    WorkOrder order = await CreateAndSaveNewWorkOrder();
    await Page.WaitForURLAsync("**/workorder/search");
    // ... verify fields on search page
}
```

Note:
- `[Retry(2)]` — Retries flaky tests up to 2 times (browser tests can be timing-sensitive)
- Test starts by logging in, then exercises the UI flow
- Assertions verify both the UI state and the persisted data (via `Bus.Send(query)`)

### Step 3: Study the Assign and Complete Tests

Open `src/AcceptanceTests/WorkOrders/WorkOrderAssignTests.cs` — see how Save → Assign is chained.
Open `src/AcceptanceTests/WorkOrders/WorkOrderCompleteTests.cs` — see the full lifecycle.

### Step 4: Write a Full Lifecycle Acceptance Test

Create a new file `src/AcceptanceTests/WorkOrders/WorkOrderFullLifecycleTests.cs`:

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
        // Step 1: Login
        await LoginAsCurrentUser();

        // Step 2: Create a draft work order
        WorkOrder order = await CreateAndSaveNewWorkOrder();
        order.Number.ShouldNotBeNullOrEmpty();
        await TakeScreenshotAsync(1, "DraftCreated");

        // Step 3: Navigate to the work order from search
        await ClickWorkOrderNumberFromSearchPage(order);

        // Step 4: Assign the work order to ourselves
        order = await AssignExistingWorkOrder(order, CurrentUser.UserName);
        order.Status.ShouldBe(WorkOrderStatus.Assigned);
        await TakeScreenshotAsync(2, "Assigned");

        // Step 5: Navigate back and begin work
        await Page.WaitForURLAsync("**/workorder/search");
        await ClickWorkOrderNumberFromSearchPage(order);
        order = await BeginExistingWorkOrder(order);
        order.Status.ShouldBe(WorkOrderStatus.InProgress);
        await TakeScreenshotAsync(3, "InProgress");

        // Step 6: Navigate back and complete
        await Page.WaitForURLAsync("**/workorder/search");
        await ClickWorkOrderNumberFromSearchPage(order);
        order = await CompleteExistingWorkOrder(order);
        order.Status.ShouldBe(WorkOrderStatus.Complete);
        await TakeScreenshotAsync(4, "Completed");

        // Step 7: Verify final state in database
        var finalOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
        finalOrder.ShouldNotBeNull();
        finalOrder!.Status.ShouldBe(WorkOrderStatus.Complete);
        finalOrder.CreatedDate.ShouldNotBeNull();
        finalOrder.AssignedDate.ShouldNotBeNull();
        finalOrder.CompletedDate.ShouldNotBeNull();
    }
}
```

### Step 5: Install Playwright Browsers

```powershell
dotnet build src/AcceptanceTests --configuration Debug
pwsh src/AcceptanceTests/bin/Debug/net10.0/playwright.ps1 install
```

### Step 6: Run Your Test

```powershell
dotnet test src/AcceptanceTests --configuration Debug --filter "FullyQualifiedName~WorkOrderFullLifecycleTests"
```

### Step 7: Review Traces on Failure

If the test fails, find the Playwright trace file in the `playwright-traces/` directory. Open it with:

```powershell
pwsh src/AcceptanceTests/bin/Debug/net10.0/playwright.ps1 show-trace playwright-traces/WorkOrderFullLifecycleTests.*.zip
```

The trace shows every network request, DOM snapshot, and screenshot — invaluable for debugging.

---

## Expected Outcome

- A passing Playwright test that exercises Draft → Assigned → InProgress → Complete
- Understanding of the `AcceptanceTestBase` helper methods
- Ability to debug failures using Playwright traces and screenshots

---

## Discussion Questions

1. This test takes seconds to minutes. The same state transitions are tested in milliseconds by unit tests. Why is the L2 test still valuable?
2. The test uses `[Retry(2)]`. Is this a code smell or a pragmatic necessity? What causes flakiness in browser tests?
3. The base class uses `EvaluateAsync("el => el.click()")` instead of Playwright's native `ClickAsync()`. Why? (Blazor WASM client-side routing doesn't trigger Playwright's navigation detection)
4. Each test gets its own browser, page, and user via `TestState`. Why is this isolation important for parallel execution?
5. How does this test implement "User Experience Testing After Deployment" from the curriculum?
