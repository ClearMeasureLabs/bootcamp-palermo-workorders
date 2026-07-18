# Lab 08: Acceptance Testing with Playwright - L2 Testing

**Curriculum Section:** Section 06 (Operate/Execute - UX Testing After Deployment)
**Estimated Time:** 50 minutes
**Type:** Build

---

## Objective

Write an end-to-end acceptance test that exercises the full work request lifecycle through the browser UI using Playwright.

---

## Steps

### Step 1: Study the Test Base

Open `src/AcceptanceTests/AcceptanceTestBase.cs`. Understand helpers: `LoginAsCurrentUser()`, `Click()`, `Input()`, `Select()`, `Expect()`, `CreateAndSaveNewWorkRequest()`, `AssignExistingWorkRequest()`, `BeginExistingWorkRequest()`, `CompleteExistingWorkRequest()`.

### Step 2: Study Existing Tests

Open `src/AcceptanceTests/WorkRequests/WorkRequestSaveDraftTests.cs` and `WorkRequestAssignTests.cs`.

### Step 3: Write a Full Lifecycle Test

Create `src/AcceptanceTests/WorkRequests/WorkRequestFullLifecycleTests.cs`:

```csharp
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkRequests;

public class WorkRequestFullLifecycleTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldCompleteFullWorkRequestLifecycle()
    {
        await LoginAsCurrentUser();

        WorkRequest order = await CreateAndSaveNewWorkRequest();
        await ClickWorkRequestNumberFromSearchPage(order);
        order = await AssignExistingWorkRequest(order, CurrentUser.UserName);
        order.Status.ShouldBe(WorkRequestStatus.Assigned);

        await Page.WaitForURLAsync("**/workrequest/search");
        await ClickWorkRequestNumberFromSearchPage(order);
        order = await BeginExistingWorkRequest(order);
        order.Status.ShouldBe(WorkRequestStatus.InProgress);

        await Page.WaitForURLAsync("**/workrequest/search");
        await ClickWorkRequestNumberFromSearchPage(order);
        order = await CompleteExistingWorkRequest(order);
        order.Status.ShouldBe(WorkRequestStatus.Complete);

        var finalOrder = await Bus.Send(new WorkRequestByNumberQuery(order.Number!));
        finalOrder.ShouldNotBeNull();
        finalOrder!.CompletedDate.ShouldNotBeNull();
    }
}
```

### Step 4: Install Browsers and Run

```powershell
dotnet build src/AcceptanceTests --configuration Debug
pwsh src/AcceptanceTests/bin/Debug/net10.0/playwright.ps1 install
dotnet test src/AcceptanceTests --configuration Debug --filter "FullyQualifiedName~WorkRequestFullLifecycleTests"
```

---

## Expected Outcome

- A passing Playwright test exercising Draft → Assigned → InProgress → Complete
