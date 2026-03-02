# Lab 06: Writing Your First Unit Test - L0 Testing

**Curriculum Section:** Section 05 (Team/Process Design - L0 Tests)
**Estimated Time:** 45 minutes
**Type:** Build

---

## Objective

Write unit tests for the domain model following project conventions: NUnit 4.x, Shouldly assertions, `Stub` prefix for test doubles, AAA pattern.

---

## Steps

### Step 1: Study Existing Unit Tests

Open `src/UnitTests/Core/Model/WorkOrderTests.cs`. Observe `[TestFixture]`, `[Test]`, AAA pattern, and assertion style.

### Step 2: Study Test Data Generation

Open `src/UnitTests/BogusOverrides.cs` and `src/UnitTests/ObjectMother.cs` for test data patterns.

### Step 3: Write WorkOrder Unit Tests

Add to `src/UnitTests/Core/Model/WorkOrderTests.cs`:

```csharp
[Test]
public void CanReassign_WhenStatusIsDraft_ShouldReturnTrue()
{
    var order = new WorkOrder();
    order.Status = WorkOrderStatus.Draft;
    order.CanReassign().ShouldBeTrue();
}

[Test]
public void CanReassign_WhenStatusIsAssigned_ShouldReturnFalse()
{
    var order = new WorkOrder();
    order.Status = WorkOrderStatus.Assigned;
    order.CanReassign().ShouldBeFalse();
}

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

### Step 4: Write Employee Unit Tests

Add to `src/UnitTests/Core/Model/EmployeeTests.cs`:

```csharp
[Test]
public void CanCreateWorkOrder_WhenHasRoleWithCreatePermission_ShouldReturnTrue()
{
    var employee = new Employee("testuser", "Test", "User", "test@test.com");
    employee.AddRole(new Role("Facility Lead", true, false));
    employee.CanCreateWorkOrder().ShouldBeTrue();
}

[Test]
public void CanFulfilWorkOrder_WhenNoFulfillmentRole_ShouldReturnFalse()
{
    var employee = new Employee("testuser", "Test", "User", "test@test.com");
    employee.AddRole(new Role("Facility Lead", true, false));
    employee.CanFulfilWorkOrder().ShouldBeFalse();
}
```

### Step 5: Run Tests

```powershell
dotnet test src/UnitTests --configuration Release
```

---

## Expected Outcome

- 5 new passing unit tests using Shouldly assertions
- Tests run in milliseconds with no database or I/O

---

## Discussion Questions

1. Why do these tests run in milliseconds? What makes them L0?
2. Why is the `Stub` prefix convention important for test doubles?
3. Can you read the test names and understand the business rules without reading the implementation?
