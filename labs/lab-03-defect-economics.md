# Lab 03: Defect Economics - The Cost of Escaped Defects

**Curriculum Section:** Sections 02-04 (Structure of a Software Project / Project Design Strategy)
**Estimated Time:** 40 minutes
**Type:** Analyze + Experiment

---

## Objective

Experience the test pyramid (L0/L1/L2) firsthand by introducing a deliberate bug and measuring the cost difference of catching defects at each level.

---

## Steps

### Step 1: Understand the Target Code

Open `src/Core/Model/WorkOrderStatus.cs` and read the `FromCode` method. This converts database codes (`"DFT"`, `"ASD"`, `"IPG"`, `"CMP"`) into `WorkOrderStatus` objects via `WorkOrderStatusConverter` in `src/DataAccess/Mappings/WorkOrderStatusConverter.cs`.

### Step 2: Introduce a Deliberate Bug

Change `FromCode` to compare against `Key` instead of `Code`:

```csharp
// DELIBERATE BUG: Change instance.Code to instance.Key
var match = Array.Find(items, instance => instance.Key == code)!;
```

### Step 3: L0 — Unit Tests (seconds)

```powershell
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
dotnet test src/UnitTests --configuration Release
$stopwatch.Stop()
Write-Host "Unit tests took: $($stopwatch.Elapsed.TotalSeconds) seconds"
```

Record which test class catches the bug and how long it took.

### Step 4: L1 — Integration Tests (minutes)

```powershell
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
dotnet test src/IntegrationTests --configuration Release
$stopwatch.Stop()
Write-Host "Integration tests took: $($stopwatch.Elapsed.TotalSeconds) seconds"
```

Record which handler tests fail and how long it took.

### Step 5: Analyze L2 Impact

Trace the impact without running acceptance tests: `WorkOrderStatusConverter` calls `FromCode()` on every database read — every query would return work orders with null status, causing `NullReferenceException` cascades.

### Step 6: Revert the Bug

Restore the original code and run `.\privatebuild.ps1` to confirm green.

### Step 7: Calculate the Cost Multiplier

| Feedback Level | Time to Detect | Clarity of Error | Cost to Fix |
|----------------|----------------|------------------|-------------|
| L0 (Unit Test) | ___ seconds | High | Immediate |
| L1 (Integration) | ___ seconds | Medium | Minutes |
| L2 (Acceptance) | ___ minutes | Low | 10+ minutes |
| Production escape | Hours/Days | Very Low | Hours + reputation |

---

## Expected Outcome

- Visceral understanding of why fast tests matter
- A completed cost multiplier table

---

## Discussion Questions

1. This bug was in a pure logic function. Why is it particularly well-suited for L0 testing?
2. The curriculum states "9% of defect fixes introduce a new defect." If you fixed this by changing database codes, what new defects would that introduce?
3. Map this to the Three Ways of DevOps: Flow (test pyramid), Feedback (fastest actionable signal), Experimentation (deliberate bug introduction).
