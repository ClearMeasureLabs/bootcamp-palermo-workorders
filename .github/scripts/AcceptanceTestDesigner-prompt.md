Generate acceptance test specifications for this GitHub issue.

ACCEPTANCE TEST CONTEXT:
- Framework: NUnit + Playwright for browser automation
- Base class: AcceptanceTestBase (provides Page, Bus, CurrentUser, helper methods)
- Location: src/AcceptanceTests/
- Existing fixtures: App/, Authentication/, WorkOrders/, AIAgents/

AVAILABLE HELPER METHODS:
- LoginAsCurrentUser() - logs in with test user
- CreateAndSaveNewWorkOrder() - creates a new work order
- ClickWorkOrderNumberFromSearchPage(order) - navigates to work order
- AssignExistingWorkOrder(order, username) - assigns work order
- BeginExistingWorkOrder(order) - starts work on order
- CompleteExistingWorkOrder(order) - completes work order
- Click(testId) - clicks element by test ID
- Input(testId, value) - fills input by test ID
- Select(testId, value) - selects dropdown option
- Expect(locator) - Playwright assertion

TEST PATTERN:
```csharp
[Test]
public async Task ShouldDoSomething()
{
    await LoginAsCurrentUser();
    // test steps...
    // assertions using Expect() and Shouldly
}
```

OUTPUT FORMAT:
For each test, provide:
TEST: [TestMethodName]
FIXTURE: [ExistingOrNewFixtureFileName.cs]
STEPS:
- step 1
- step 2
- step 3

Generate tests that fully cover the feature from a user's perspective.

ISSUE: {title}

{body}
