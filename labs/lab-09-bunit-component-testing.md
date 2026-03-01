# Lab 09: Blazor Component Testing with bUnit

**Curriculum Section:** Section 05 (Team/Process Design - L0 Tests for UI)
**Estimated Time:** 40 minutes
**Type:** Build

---

## Objective

Write bUnit tests for Blazor components using the project's stub pattern. Understand how bUnit achieves L0 speed for UI component tests by rendering in-memory without a browser.

---

## Context

bUnit is a testing library for Blazor components that renders them in-memory. This means:
- **No browser needed** — tests run in the same process as the test runner
- **L0 speed** — milliseconds, not seconds
- **Full component lifecycle** — `OnInitialized`, `OnParametersSet`, event handlers all execute
- **Dependency injection** — stubs can be injected to isolate the component

---

## Steps

### Step 1: Study the Existing bUnit Tests

Open `src/UnitTests/UI.Shared/Components/MyWorkOrdersTests.cs`. Study the pattern:

```csharp
[Test]
public void ShouldInitializeWithZeroCount()
{
    using var ctx = new TestContext();

    var stubBus = new StubBusWithNoWorkOrders();
    var stubUserSession = new StubUserSession();
    var stubUiBus = new StubUiBus();

    ctx.Services.AddSingleton<IBus>(stubBus);
    ctx.Services.AddSingleton<IUserSession>(stubUserSession);
    ctx.Services.AddSingleton<IUiBus>(stubUiBus);

    var component = ctx.RenderComponent<MyWorkOrders>();

    component.Instance.Count.ShouldBe(0);
}
```

Key patterns:
1. **`TestContext`** — bUnit's test container (note: `Bunit.TestContext`, not NUnit's)
2. **Stub injection** — Replace real services with stubs via `ctx.Services.AddSingleton`
3. **`RenderComponent<T>()`** — Renders the Blazor component in-memory
4. **`component.Instance`** — Access the component's public properties and methods

### Step 2: Study the Stub Implementations

In the same file, examine the private stub classes:

```csharp
private class StubBusWithWorkOrders(Employee creator) : Bus(null!)
{
    public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        if (request is WorkOrderSpecificationQuery query)
        {
            var workOrders = new[] { /* test data */ };
            return Task.FromResult<TResponse>((TResponse)(object)workOrders);
        }
        throw new NotImplementedException();
    }
}
```

Note:
- Stubs are prefixed with `Stub` (never `Mock`)
- They extend real classes and override specific methods
- They return predetermined data for specific request types

### Step 3: Study the Component Under Test

Open `src/UI.Shared/Components/MyWorkOrders.razor`. Understand:
- What services it injects (`IBus`, `IUserSession`, `IUiBus`)
- What it does on initialization (loads work orders for the current user)
- What public properties it exposes (`Count`)
- How it handles the `WorkOrderChangedEvent`

### Step 4: Study the Event Handling Test

In `MyWorkOrdersTests.cs`, study `ShouldHandleWorkOrderChangedEventAndIncrementCount`:

```csharp
var workOrderChangedEvent = new WorkOrderChangedEvent(
    new StateCommandResult(newWorkOrder)
);
component.Instance.Handle(workOrderChangedEvent);
component.Instance.Count.ShouldBe(3);
```

This tests that the component correctly updates its internal state when a domain event fires.

### Step 5: Write a New bUnit Test

Add a test to `src/UnitTests/UI.Shared/Components/MyWorkOrdersTests.cs` that verifies the component handles a completed work order event:

```csharp
[Test]
public void ShouldHandleCompletedWorkOrderEvent()
{
    using var ctx = new TestContext();

    var currentUser = new Employee("jpalermo", "Jeffrey", "Palermo", "jeffrey@example.com");
    var stubBus = new StubBusWithWorkOrders(currentUser);
    var stubUserSession = new StubUserSession(currentUser);
    var stubUiBus = new StubUiBus();

    ctx.Services.AddSingleton<IBus>(stubBus);
    ctx.Services.AddSingleton<IUserSession>(stubUserSession);
    ctx.Services.AddSingleton<IUiBus>(stubUiBus);

    var component = ctx.RenderComponent<MyWorkOrders>();
    var initialCount = component.Instance.Count;

    var completedOrder = new WorkOrder
    {
        Id = Guid.NewGuid(),
        Number = "WO-DONE",
        Title = "Completed work order",
        Status = WorkOrderStatus.Complete,
        Creator = currentUser
    };

    component.Instance.Handle(new WorkOrderChangedEvent(
        new StateCommandResult(completedOrder)));

    component.Instance.Count.ShouldBe(initialCount + 1);
}
```

### Step 6: Study the Login Page Tests

Open `src/UnitTests/UI.Shared/Pages/LoginPageTests.cs`. Note how page-level components are tested:
- Pages may have routing parameters
- Pages may load data on initialization
- The test pattern remains the same: inject stubs, render, assert

### Step 7: Run the bUnit Tests

```powershell
dotnet test src/UnitTests --configuration Release --filter "FullyQualifiedName~MyWorkOrdersTests"
```

Verify all tests pass.

```powershell
dotnet test src/UnitTests --configuration Release
```

Verify the full unit test suite passes.

---

## Expected Outcome

- Understanding of the bUnit testing pattern for Blazor components
- A new passing bUnit test
- Understanding of how stubs replace real services in component tests

---

## Discussion Questions

1. How does bUnit achieve L0 speed for UI tests? (In-memory rendering, no browser, no HTTP)
2. Why use `Stub` classes instead of a mocking framework like Moq? (Explicit behavior, no magic, easier to debug, project convention)
3. The component tests access `component.Instance.Count` directly. Is this testing the **rendered output** or the **component logic**? When would you need to test rendered HTML instead?
4. Compare bUnit (L0) to Playwright (L2) for testing the same component. What does each level catch that the other cannot?
5. The `StubBusWithWorkOrders` throws `NotImplementedException` for unexpected request types. Is this a good pattern? Why or why not?
