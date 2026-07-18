# Lab 17: Capstone - End-to-End Feature Delivery

**Curriculum Section:** All Sections (Synthesis)
**Estimated Time:** 60 minutes
**Type:** Build

---

## Objective

Deliver a complete feature from design through pull request, exercising every practice from the course: architecture, domain modeling, database migration, testing at all levels, static analysis, and PR review.

---

## The Feature: "Cancelled" Work Request Status

A work request can be cancelled from **Draft** or **Assigned** status, but only by the **Creator**.

```
    [Draft] --------→ [Cancelled]
       |                    ↑
       v                    |
    [Assigned] --------→ [Cancelled]
```

---

## Steps

### Step 1: Add the Status (Core)

In `src/Core/Model/WorkRequestStatus.cs`, add:

```csharp
public static readonly WorkRequestStatus Cancelled = new("CNL", "Cancelled", "Cancelled", 5);
```

Update `GetAllItems()` to include it.

### Step 2: Create the State Commands (Core)

Since `StateCommandBase.IsValid()` is not virtual, cancellation from two different statuses requires two concrete commands with a shared abstract base. Create `src/Core/Model/StateCommands/CancelWorkRequestCommand.cs`:

```csharp
public abstract record CancelWorkRequestCommand(WorkRequest WorkRequest, Employee CurrentUser)
    : StateCommandBase(WorkRequest, CurrentUser)
{
    public const string Name = "Cancel";
    public override WorkRequestStatus GetEndStatus() => WorkRequestStatus.Cancelled;
    public override string TransitionVerbPresentTense => Name;
    public override string TransitionVerbPastTense => "Cancelled";

    protected override bool UserCanExecute(Employee currentUser)
        => currentUser == WorkRequest.Creator;
}

public sealed record CancelDraftWorkRequestCommand(WorkRequest WorkRequest, Employee CurrentUser)
    : CancelWorkRequestCommand(WorkRequest, CurrentUser)
{
    public override WorkRequestStatus GetBeginStatus() => WorkRequestStatus.Draft;
}

public sealed record CancelAssignedWorkRequestCommand(WorkRequest WorkRequest, Employee CurrentUser)
    : CancelWorkRequestCommand(WorkRequest, CurrentUser)
{
    public override WorkRequestStatus GetBeginStatus() => WorkRequestStatus.Assigned;
}
```

Note: `StateCommandBase.IsValid()` checks `WorkRequest.Status == GetBeginStatus()`, so each concrete command handles one transition. The base `IsValid()` logic works without modification.

### Step 3: Unit Tests

Create `src/UnitTests/Core/Model/StateCommands/CancelWorkRequestCommandTests.cs` with tests:
- `CancelDraftWorkRequestCommand` valid when Creator
- `CancelAssignedWorkRequestCommand` valid when Creator
- `CancelDraftWorkRequestCommand` invalid when not Creator
- `CancelAssignedWorkRequestCommand` invalid when not Creator
- Invalid from InProgress (neither cancel command's `IsValid()` returns true)
- Invalid from Complete
- Both commands transition to Cancelled status

### Step 4: Integration Test

Add a test that saves a Draft, cancels it via the handler, and verifies the persisted status is `Cancelled`.

### Step 5: Register in StateCommandList

Open `src/Core/Services/Impl/StateCommandList.cs`. Add both cancel commands to `GetAllStateCommands()`:

```csharp
commands.Add(new CancelDraftWorkRequestCommand(workRequest, currentUser));
commands.Add(new CancelAssignedWorkRequestCommand(workRequest, currentUser));
```

This makes the cancel commands discoverable by the UI dropdown and `GetValidStateCommands()`.

### Step 6: Register in MCP Tools

Open `src/McpServer/Tools/WorkRequestTools.cs`. In the `ExecuteWorkRequestCommand` method, add the new command names to the `switch` expression:

```csharp
"CancelDraftWorkRequestCommand" => new CancelDraftWorkRequestCommand(workRequest, user),
"CancelAssignedWorkRequestCommand" => new CancelAssignedWorkRequestCommand(workRequest, user),
```

Update the method's `[Description]` attribute to list the new commands. Without this step, the cancel feature would exist in the domain but be unreachable via MCP or AI agents.

### Step 7: Build and Verify

```powershell
dotnet format style src/ChurchBulletin.sln --verify-no-changes
.\PrivateBuild.ps1
```

### Step 8: Create Pull Request

Branch, commit, push, and create a PR with checklist.

---

