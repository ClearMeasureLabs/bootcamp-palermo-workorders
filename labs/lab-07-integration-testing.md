# Lab 07: Integration Testing with a Real Database - L1 Testing

**Curriculum Section:** Section 05 (Team/Process Design - L1 Tests)
**Estimated Time:** 50 minutes
**Type:** Build

---

## Objective

Write integration tests that validate the full handler-to-database-to-response flow using real persistence.

---

## Steps

### Step 1: Study the Test Infrastructure

Open `src/IntegrationTests/IntegratedTestBase.cs`. Note `TestHost.GetRequiredService<T>()`, `Faker<T>()`, and `DatabaseTests().Clean()` for isolation.

### Step 2: Study an Existing Test

Open `src/IntegrationTests/DataAccess/Handlers/StateCommandHandlerForSaveTests.cs`. Trace the pattern: clean DB → create test data → send command through handler → assert result → verify persistence with a fresh DbContext.

### Step 3: Write a State Command Integration Test

Add to `src/IntegrationTests/DataAccess/Handlers/StateCommandHandlerForAssignTests.cs`:

```csharp
[Test]
public async Task ShouldPersistAssignedDateWhenAssigning()
{
    new DatabaseTests().Clean();
    var creator = Faker<Employee>();
    var assignee = Faker<Employee>();
    var context = TestHost.GetRequiredService<DbContext>();
    context.Add(creator);
    context.Add(assignee);
    await context.SaveChangesAsync();

    var workOrder = Faker<WorkOrder>();
    workOrder.Id = Guid.Empty;
    workOrder.Creator = creator;
    workOrder.Assignee = assignee;
    workOrder.CreatedDate = null;

    var saveCommand = RemotableRequestTests.SimulateRemoteObject(
        new SaveDraftCommand(workOrder, creator));
    var handler = TestHost.GetRequiredService<StateCommandHandler>();
    var saveResult = await handler.Handle(saveCommand);

    var assignCommand = RemotableRequestTests.SimulateRemoteObject(
        new DraftToAssignedCommand(saveResult.WorkOrder, creator));
    var assignResult = await handler.Handle(assignCommand);

    assignResult.WorkOrder.Status.ShouldBe(WorkOrderStatus.Assigned);
    assignResult.WorkOrder.AssignedDate.ShouldNotBeNull();

    var verifyContext = TestHost.GetRequiredService<DbContext>();
    var persisted = verifyContext.Find<WorkOrder>(assignResult.WorkOrder.Id);
    persisted.ShouldNotBeNull();
    persisted!.Status.ShouldBe(WorkOrderStatus.Assigned);
}
```

### Step 4: Run Tests

```powershell
dotnet test src/IntegrationTests --configuration Release
```

---

## Expected Outcome

- New integration tests validating real database persistence
- Understanding of test isolation via `DatabaseTests().Clean()`

---

## Discussion Questions

1. Why does each test start with `DatabaseTests().Clean()`? What happens without it?
2. Why use `RemotableRequestTests.SimulateRemoteObject()`? What bug class does this catch?
3. Why use separate DbContext instances for write and read?
