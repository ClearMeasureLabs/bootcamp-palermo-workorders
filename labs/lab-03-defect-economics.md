# Lab 03: Defect Economics - The Cost of Escaped Defects

**Curriculum Section:** Sections 02-04 (Structure of a Software Project / Project Design Strategy)
**Estimated Time:** 40 minutes
**Type:** Analyze + Experiment

---

## Objective

Experience the test pyramid (L0/L1/L2) firsthand by introducing a deliberate bug and measuring the cost difference of catching defects at each level. Connect the results to the "Defects Kill Projects" principle.

---

## Context

The curriculum states:
- Defects escaping a feedback cycle are **10x more expensive** to remove
- Only 40% of defects are caused by problems with code
- 9% of defect fixes introduce a new defect

This lab makes those numbers tangible.

---

## Steps

### Step 1: Understand the Target Code

Open `src/Core/Model/WorkOrderStatus.cs` and read the `FromCode` method (line 84-91):

```csharp
public static WorkOrderStatus FromCode(string code)
{
    var items = GetAllItems();
    var match =
        Array.Find(items, instance => instance.Code == code)!;
    return match;
}
```

This method converts a database code (e.g., `"DFT"`, `"ASD"`, `"IPG"`, `"CMP"`) into a `WorkOrderStatus` object. It is called by `WorkOrderStatusConverter` in `src/DataAccess/Mappings/WorkOrderStatusConverter.cs` every time a work order is read from the database.

### Step 2: Introduce a Deliberate Bug

In `src/Core/Model/WorkOrderStatus.cs`, change `FromCode` to compare against `Key` instead of `Code`:

```csharp
// DELIBERATE BUG: Change instance.Code to instance.Key
var match =
    Array.Find(items, instance => instance.Key == code)!;
```

This is subtle — `Code` values are `"DFT"`, `"ASD"`, etc., while `Key` values are `"Draft"`, `"Assigned"`, etc. The comparison will never match database values.

### Step 3: L0 - Catch It with Unit Tests

Run unit tests and **time the execution**:

```powershell
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
dotnet test src/UnitTests --configuration Release
$stopwatch.Stop()
Write-Host "Unit tests took: $($stopwatch.Elapsed.TotalSeconds) seconds"
```

**Record:**
- Which test class caught the bug? (Check `WorkOrderStatusTests.cs`)
- How many seconds did it take to get feedback?
- Was the error message clear enough to identify the root cause?

### Step 4: L1 - Catch It with Integration Tests

Run integration tests and time them:

```powershell
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
dotnet test src/IntegrationTests --configuration Release
$stopwatch.Stop()
Write-Host "Integration tests took: $($stopwatch.Elapsed.TotalSeconds) seconds"
```

**Record:**
- Which handler tests fail? (Check `StateCommandHandlerFor*.cs` tests and `WorkOrderMappingTests.cs`)
- How many seconds did it take?
- Was the error message more or less clear than the unit test failure?

### Step 5: Analyze L2 Impact

Without running acceptance tests (they require browser setup), trace the impact:

1. Open `src/DataAccess/Mappings/WorkOrderStatusConverter.cs` — it calls `FromCode()` on every database read
2. Open `src/AcceptanceTests/WorkOrders/WorkOrderSaveDraftTests.cs` — after saving, it reads back via `WorkOrderByNumberQuery`
3. The query would return a work order with `null` status (match fails), causing a `NullReferenceException`

**Question:** Would a manual tester find this bug? How long would diagnosis take?

### Step 6: Revert the Bug

Restore the original code:

```csharp
var match =
    Array.Find(items, instance => instance.Code == code)!;
```

Run the full private build to confirm green:

```powershell
.\privatebuild.ps1
```

### Step 7: Calculate the Cost Multiplier

Fill in this table with your recorded times:

| Feedback Level | Time to Detect | Clarity of Error | Cost to Fix |
|----------------|----------------|------------------|-------------|
| L0 (Unit Test) | ___ seconds | High - exact method | Immediate |
| L1 (Integration Test) | ___ seconds | Medium - handler context | Minutes |
| L2 (Acceptance Test) | ___ minutes | Low - browser crash | 10+ minutes |
| Production escape | Hours/Days | Very Low - user report | Hours + reputation |

Apply the 10x multiplier: If L0 costs 1 unit, L1 costs 10, L2 costs 100, production costs 1000.

---

## Expected Outcome

- Visceral understanding of why fast tests matter
- The bug is caught, analyzed at each level, and reverted
- A completed cost multiplier table

---

## Discussion Questions

1. This bug was in a **factory method** — a pure logic function with no I/O. Why is it particularly well-suited for L0 testing?
2. The `WorkOrderStatusConverter` is infrastructure code in DataAccess that calls a Core method. How does the test pyramid distribution reflect this boundary?
3. The curriculum states "9% of defect fixes introduce a new defect." If you fixed this bug by changing the database codes to match Key values, what new defects would that introduce?
4. Map this exercise to the "Three Ways of DevOps":
   - **First Way (Flow):** How did the test pyramid optimize the flow of detecting this issue?
   - **Second Way (Feedback):** Which test level gave the fastest, most actionable feedback?
   - **Third Way (Experimentation):** What did you learn by deliberately introducing the bug?
5. If your team has zero unit tests and relies only on manual testing, what is the true cost multiplier on escaped defects?
