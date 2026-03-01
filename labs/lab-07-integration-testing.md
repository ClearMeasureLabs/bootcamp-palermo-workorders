# Lab 07: Integration Testing with a Real Database - L1 Testing

**Curriculum Section:** Section 05 (Team/Process Design - L1 Tests)
**Estimated Time:** 50 minutes
**Type:** Build

---

## Objective

Write integration tests that validate the full handler-to-database-to-response flow. Understand why L1 tests are necessary when mocking is not feasible and real persistence must be verified.

---

## Context

**L1 Tests** are integration tests that rely on lightweight dependencies like a database. They validate that the application code works correctly with real infrastructure. This project uses:

- Real database (SQL Server LocalDB, Docker SQL, or SQLite)
- `TestHost` for dependency injection
- `Faker<T>()` for test data generation
- `DatabaseTests().Clean()` for test isolation
- Shouldly assertions

---

## Steps

### Step 1: Study the Integration Test Infrastructure

Open `src/IntegrationTests/IntegratedTestBase.cs`. Note:
- `TestHost.GetRequiredService<T>()` — Resolves dependencies from the test DI container
- `Faker<T>()` — Generates random test data using AutoBogus
- The test host is shared across all tests for performance

### Step 2: Study an Existing Integration Test

Open `src/IntegrationTests/DataAccess/Handlers/StateCommandHandlerForSaveTests.cs`. Trace the pattern:

```csharp
[Test]
public async Task ShouldSaveWorkOrderBySavingDraft()
{
    // 1. Clean the database for isolation
    new DatabaseTests().Clean();

    // 2. Create test data
    var currentUser = Faker<Employee>();
    currentUser.Id = Guid.NewGuid();
    var context = TestHost.GetRequiredService<DbContext>();
    context.Add(currentUser);
    await context.SaveChangesAsync();

    // 3. Create and send the command
    var workOrder = Faker<WorkOrder>();
    workOrder.Id = Guid.Empty;
    workOrder.Creator = currentUser;
    var command = RemotableRequestTests.SimulateRemoteObject(
        new SaveDraftCommand(workOrder, currentUser));
    var handler = TestHost.GetRequiredService<StateCommandHandler>();
    var result = await handler.Handle(command);

    // 4. Assert the result
    result.WorkOrder.Creator.ShouldBe(currentUser);
    result.WorkOrder.Title.ShouldBe(workOrder.Title);

    // 5. Verify persistence by reading back
    var context3 = TestHost.GetRequiredService<DbContext>();
    var order = context3.Find<WorkOrder>(result.WorkOrder.Id);
    order.ShouldNotBeNull();
    order.Title.ShouldBe(workOrder.Title);
}
```

Key patterns:
- `DatabaseTests().Clean()` — Empties all tables for test isolation
- `RemotableRequestTests.SimulateRemoteObject()` — Simulates JSON serialization/deserialization (as happens over the wire)
- Separate DbContext instances for write and read (avoids EF caching issues)

### Step 3: Study the Query Handler Tests

Open `src/IntegrationTests/DataAccess/WorkOrderQueryHandlerTests.cs` (or similar handler test files). Note how queries are tested:

1. Seed data into the database
2. Execute the query through the handler
3. Assert the returned data matches

### Step 4: Write a New State Command Integration Test

Create a new test that verifies the DraftToAssigned flow persists correctly.

Add a new test to `src/IntegrationTests/DataAccess/Handlers/StateCommandHandlerForAssignTests.cs`:

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
    persisted.AssignedDate.ShouldNotBeNull();
}
```

### Step 5: Write a Query Integration Test

Add a test that verifies the `WorkOrderSpecificationQuery` filters correctly. In an existing or new test file under `src/IntegrationTests/DataAccess/`:

```csharp
[Test]
public async Task ShouldFilterWorkOrdersByStatus()
{
    new DatabaseTests().Clean();

    var creator = Faker<Employee>();
    var context = TestHost.GetRequiredService<DbContext>();
    context.Add(creator);

    var draftOrder = Faker<WorkOrder>();
    draftOrder.Creator = creator;
    draftOrder.Status = WorkOrderStatus.Draft;
    context.Add(draftOrder);

    var assignedOrder = Faker<WorkOrder>();
    assignedOrder.Creator = creator;
    assignedOrder.Status = WorkOrderStatus.Assigned;
    assignedOrder.Assignee = creator;
    context.Add(assignedOrder);

    await context.SaveChangesAsync();

    var query = new WorkOrderSpecificationQuery();
    query.MatchStatus(WorkOrderStatus.Draft);

    var bus = TestHost.GetRequiredService<IBus>();
    var results = await bus.Send(query);

    results.Length.ShouldBe(1);
    results[0].Status.ShouldBe(WorkOrderStatus.Draft);
}
```

### Step 6: Run the Integration Tests

```powershell
dotnet test src/IntegrationTests --configuration Release
```

All tests should pass. Note the execution time compared to unit tests.

### Step 7: Run the Full Build

```powershell
.\privatebuild.ps1
```

---

## Expected Outcome

- New integration tests that validate real database persistence
- Understanding of test isolation via `DatabaseTests().Clean()`
- Understanding of why separate DbContext instances are used for write and read

---

## Discussion Questions

1. Why does each test start with `new DatabaseTests().Clean()`? What would happen without it? (Test pollution — one test's data affects another)
2. Why does the test use `RemotableRequestTests.SimulateRemoteObject()`? What bug class does this catch? (Serialization/deserialization issues)
3. L1 tests are slower than L0. When is the tradeoff worth it? (When you need to verify ORM mappings, database constraints, query behavior)
4. The test creates separate `DbContext` instances for write and read. Why? (EF Core's change tracker caches entities — reading from the same context would return the cached object, not the persisted one)
5. How does the `TestHost` relate to the production `UIServiceRegistry`? Are they testing the same wiring?
