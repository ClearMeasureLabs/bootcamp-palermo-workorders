# Lab 06: Writing Your First Unit Test - L0 Testing

**Curriculum Section:** Section 05 (Team/Process Design - L0 Tests)
**Estimated Time:** 45 minutes
**Type:** Build

---

## Objective

Write unit tests for the domain model following the project's established conventions: NUnit 4.x, Shouldly assertions, `Stub` prefix for test doubles, AAA pattern, and descriptive test naming.

---

## Context

**L0 Tests** are fast, in-memory unit tests with no external dependencies. They validate isolated code logic and run in milliseconds. This project uses:

- **Framework:** NUnit 4.3.2
- **Assertions:** Shouldly 4.3.0 (preferred) and NUnit `Assert.That`
- **Test Data:** AutoBogus for random generation
- **Test Doubles:** Prefixed with `Stub` (never `Mock`)
- **Naming:** `[MethodName]_[Scenario]_[ExpectedResult]` or prefixed with `Should`/`When`

---

## Steps

### Step 1: Study Existing Unit Tests

Open `src/UnitTests/Core/Model/WorkOrderTests.cs`. Observe the patterns:

```csharp
[TestFixture]
public class WorkOrderTests
{
    [Test]
    public void ShouldTruncateTo4000CharactersOnDescription()
    {
        var longText = new string('x', 4001);
        var order = new WorkOrder();
        order.Description = longText;
        Assert.That(order.Description.Length, Is.EqualTo(4000));
    }
}
```

Key observations:
- `[TestFixture]` on the class, `[Test]` on each method
- AAA pattern (Arrange, Act, Assert) without section comments
- Direct assertion on the result

### Step 2: Study Test Data Generation

Open `src/UnitTests/BogusOverrides.cs` — this configures AutoBogus for random test data.
Open `src/UnitTests/ObjectMother.cs` — this provides static factory methods for test objects.

### Step 3: Write WorkOrder Unit Tests

Add these tests to `src/UnitTests/Core/Model/WorkOrderTests.cs`:

**Test 1: CanReassign returns true for Draft status**

```csharp
[Test]
public void CanReassign_WhenStatusIsDraft_ShouldReturnTrue()
{
    var order = new WorkOrder();
    order.Status = WorkOrderStatus.Draft;

    order.CanReassign().ShouldBeTrue();
}
```

**Test 2: CanReassign returns false for Assigned status**

```csharp
[Test]
public void CanReassign_WhenStatusIsAssigned_ShouldReturnFalse()
{
    var order = new WorkOrder();
    order.Status = WorkOrderStatus.Assigned;

    order.CanReassign().ShouldBeFalse();
}
```

**Test 3: GetMessage includes the work order number and status**

```csharp
[Test]
public void GetMessage_WhenNumberAndStatusSet_ShouldContainBoth()
{
    var order = new WorkOrder();
    order.Number = "ABC1234";
    order.Status = WorkOrderStatus.InProgress;

    var message = order.GetMessage();

    message.ShouldContain("ABC1234");
    message.ShouldContain("In Progress");
}
```

### Step 4: Run Only Your New Tests

```powershell
dotnet test src/UnitTests --configuration Release --filter "FullyQualifiedName~WorkOrderTests.CanReassign"
```

Verify both CanReassign tests pass.

```powershell
dotnet test src/UnitTests --configuration Release --filter "FullyQualifiedName~WorkOrderTests.GetMessage"
```

Verify the GetMessage test passes.

### Step 5: Write Employee Unit Tests

Open `src/UnitTests/Core/Model/EmployeeTests.cs`. Add these tests:

**Test 4: CanCreateWorkOrder with Facility Lead role**

```csharp
[Test]
public void CanCreateWorkOrder_WhenHasRoleWithCreatePermission_ShouldReturnTrue()
{
    var employee = new Employee("testuser", "Test", "User", "test@test.com");
    employee.AddRole(new Role("Facility Lead", true, false));

    employee.CanCreateWorkOrder().ShouldBeTrue();
}
```

**Test 5: CanFulfilWorkOrder with no fulfillment role**

```csharp
[Test]
public void CanFulfilWorkOrder_WhenNoFulfillmentRole_ShouldReturnFalse()
{
    var employee = new Employee("testuser", "Test", "User", "test@test.com");
    employee.AddRole(new Role("Facility Lead", true, false));

    employee.CanFulfilWorkOrder().ShouldBeFalse();
}
```

### Step 6: Run All Unit Tests

```powershell
dotnet test src/UnitTests --configuration Release
```

All tests should pass. Note the execution time — L0 tests typically complete in seconds.

### Step 7: Verify with the Full Build

```powershell
.\privatebuild.ps1
```

---

## Expected Outcome

- 5 new passing unit tests following project conventions
- Tests use Shouldly assertions (`ShouldBeTrue()`, `ShouldBeFalse()`, `ShouldContain()`)
- Tests run in milliseconds with no database or I/O dependencies

---

## Discussion Questions

1. Why do these tests run in milliseconds? What makes them L0? (No I/O, no database, no network)
2. Compare `Shouldly` assertions vs `Assert.That`. Which reads more naturally? Why does the project prefer Shouldly?
3. Why is the `Stub` prefix convention important for test doubles? What confusion does the `Mock` prefix cause?
4. These tests validate **domain logic** in the Core layer. Why is it important that Core has no dependencies for testability?
5. The test naming convention `[Method]_[Scenario]_[Result]` serves as documentation. Can you read the test names and understand the business rules without reading the implementation?
