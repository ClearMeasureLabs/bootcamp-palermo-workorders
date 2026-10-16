# Repeatable work-item template (guard clause + unit test)

**Title:** `Token-exp iter {N}: guard null StateCommandContext in {Target}.Execute`

**Body:**
> Part of the token-reduction experiment (analysis/token-reduction). The PR will be closed without merging.
>
> **Change:** at the top of `{Target}.Execute(StateCommandContext context)` in `src/Core/Model/StateCommands/{Target}.cs`, add `ArgumentNullException.ThrowIfNull(context);`.
> **Test:** in `src/UnitTests/Core/Model/StateCommands/{TestFile}`, add one NUnit test asserting `Execute(null!)` throws `ArgumentNullException` (ParamName == "context").
> **Done when:** the build passes with -warnaserror, the new test plus the existing tests in that file pass, and a PR referencing this issue is open. Do not merge.

**Target rotation** (each iteration gets a distinct sibling; master never gets the change because PRs are closed, so targets can be reused):
1 DraftToAssignedCommand · 2 SaveDraftCommand · 3 AssignedToCancelledCommand · 4 InProgressToCompleteCommand · 5 StateCommandBase (via a concrete subclass with no override, e.g. AssignedToInProgressCommand) · 6+ cycle again from 1.
