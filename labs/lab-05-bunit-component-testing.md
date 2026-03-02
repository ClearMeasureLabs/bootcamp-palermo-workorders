# Lab 05: Blazor Component Testing with bUnit

**Curriculum Section:** Section 05 (Team/Process Design - L0 Tests for UI)
**Estimated Time:** 40 minutes
**Type:** Build

---

## Objective

Write bUnit tests for Blazor components using the project's stub pattern. Understand how bUnit achieves L0 speed by rendering in-memory without a browser.

---

## Steps

### Step 1: Study Existing bUnit Tests

Open `src/UnitTests/UI.Shared/Pages/WorkOrderSearchTests.cs`. Study the pattern: `TestContext` → stub injection via `ctx.Services.AddSingleton` → `ctx.RenderComponent<WorkOrderSearch>()` → find HTML elements → assert on rendered output.

### Step 2: Study the Stub Implementations

Open `src/UnitTests/UI.Shared/Pages/StubBus.cs`. Note the `Stub` prefix convention and how `StubBus` extends `Bus(null!)` to override `Send<TResponse>`. It handles `EmployeeGetAllQuery`, `EmployeeByUserNameQuery`, and `WorkOrderSpecificationQuery` with canned test data.

Open `src/UnitTests/UI.Shared/Pages/StubUiBus.cs`. This is a no-op implementation of `IUiBus` for test isolation.

### Step 3: Study the Component Under Test

Open `src/UI.Shared/Pages/WorkOrderSearch.razor` and its code-behind `WorkOrderSearch.razor.cs`. Understand:
- The `Elements` enum that provides stable test IDs for `CreatorSelect`, `AssigneeSelect`, `StatusSelect`, `SearchButton`, and `WorkOrderLink`
- How the component loads dropdown options from `EmployeeGetAllQuery`
- How `SearchWorkOrders()` builds a `WorkOrderSpecificationQuery` from the filter model
- How query string parameters (`Creator`, `Assignee`, `Status`) are applied on initialization

### Step 4: Study the Existing Tests

Read through the six tests in `WorkOrderSearchTests.cs`:
- `ShouldLoadDropDownsInitiallyOnLoad` — verifies dropdowns render with correct options
- `ShouldLoadWorkOrderTableWithAllFiltersSetToAllOnInitialLoad` — verifies the results table renders
- Three tests for individual query string filters (`Creator`, `Assignee`, `Status`)
- `AfterInitialLoadSelectingAllThreeOptionsShouldLoadWorkOrders` — simulates user interaction (selecting filters and clicking Search)

Notice how `component.Find()` uses the `Elements` enum for stable selectors, and how `creatorSelect.Change("jpalermo")` simulates user input.

### Step 5: Write a New bUnit Test

Add a new test to `WorkOrderSearchTests.cs` that verifies the search results table displays the correct work order data. For example, assert that the rendered table rows contain the expected work order numbers (`WO-001`, `WO-002`) from the `StubBus` test data.

### Step 6: Run Tests

```powershell
dotnet test src/UnitTests --configuration Release --filter "FullyQualifiedName~WorkOrderSearchTests"
```

---

## Expected Outcome

- New passing bUnit test using the stub pattern
- Understanding of in-memory Blazor component rendering
- Understanding of how `Elements` enum provides stable test selectors
