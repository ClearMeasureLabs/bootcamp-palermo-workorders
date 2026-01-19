Generate full system acceptance test specifications for this GitHub issue.

ACCEPTANCE TEST CONTEXT:
- Framework: NUnit + Playwright for browser automation
- Location: src/AcceptanceTests/

PURPOSE:
- Given the feature and the development task list, there are multiple
    things that one must test in order to know that the application is
    working correctly. With the application running, how many scenarios
    are needed to ensure that the various behaviors of this feature are working?  Give a name for each test and the purpose and the few, high
    level steps. Be brief. 

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
