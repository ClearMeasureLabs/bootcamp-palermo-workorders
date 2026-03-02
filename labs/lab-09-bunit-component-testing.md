# Lab 09: Blazor Component Testing with bUnit

**Curriculum Section:** Section 05 (Team/Process Design - L0 Tests for UI)
**Estimated Time:** 40 minutes
**Type:** Build

---

## Objective

Write bUnit tests for Blazor components using the project's stub pattern. Understand how bUnit achieves L0 speed by rendering in-memory without a browser.

---

## Steps

### Step 1: Study Existing bUnit Tests

Open `src/UnitTests/UI.Shared/Components/MyWorkOrdersTests.cs`. Study the pattern: `TestContext` → stub injection via `ctx.Services.AddSingleton` → `ctx.RenderComponent<T>()` → assert on `component.Instance`.

### Step 2: Study the Stub Implementations

In the same file, examine `StubBusWithWorkOrders` and `StubBusWithNoWorkOrders`. Note the `Stub` prefix convention and how they extend `Bus(null!)` to override `Send<TResponse>`.

### Step 3: Study the Component Under Test

Open `src/UI.Shared/Components/MyWorkOrders.razor`. Understand its injected services (`IBus`, `IUserSession`, `IUiBus`), initialization behavior, and `Handle(WorkOrderChangedEvent)` method.

### Step 4: Write a New bUnit Test

Add to `MyWorkOrdersTests.cs` a test that verifies the component handles a completed work order event and increments its count.

### Step 5: Run Tests

```powershell
dotnet test src/UnitTests --configuration Release --filter "FullyQualifiedName~MyWorkOrdersTests"
```

---

## Expected Outcome

- New passing bUnit tests using the stub pattern
- Understanding of in-memory Blazor component rendering

---

## Discussion Questions

1. How does bUnit achieve L0 speed for UI tests?
2. Why use `Stub` classes instead of a mocking framework?
3. Compare bUnit (L0) to Playwright (L2). What does each catch that the other cannot?
