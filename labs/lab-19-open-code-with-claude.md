# Lab 19: Open Code with Claude Opus - Driving a Feature to Pull Request

**Curriculum Section:** Section 08 (AI-Driven Development)
**Estimated Time:** 50 minutes
**Type:** Build + Experiment

---

## Objective

Use Claude Code (or equivalent AI coding agent) to drive a complete feature from specification to pull request, observing how the agent navigates architecture constraints, writes tests, and follows project conventions.

---

## Context

"Open code" means working with an AI coding agent in a collaborative, conversational mode — giving it a feature specification and watching it plan, implement, test, and deliver. The agent reads `CLAUDE.md` and `.github/copilot-instructions.md` for project conventions, explores the codebase, and executes the full development workflow.

---

## Steps

### Step 1: Review the Guardrail Files

Before engaging the AI agent, review what it will read:
- `CLAUDE.md` — Architecture rules, build commands, testing conventions, dependency constraints
- `.github/copilot-instructions.md` — Copilot-specific rules (same principles)
- `.github/copilot-code-review-instructions.md` — What the PR review will check

### Step 2: Define the Feature

Give the AI agent this specification:

> Add a `RoomNumber` validation rule to the `WorkOrder` domain model. Room numbers must be non-empty when transitioning from Draft to Assigned (the `DraftToAssignedCommand`). If a work order has no room number, the assign command should be invalid.
>
> Requirements:
> - Modify `DraftToAssignedCommand.IsValid()` or `UserCanExecute()` to check for room number
> - Add unit tests covering: valid with room number, invalid without room number
> - Add an integration test that verifies the handler rejects assignment without a room number
> - Run privatebuild.ps1 to confirm all tests pass
> - Create a feature branch and commit

### Step 3: Observe the Agent's Approach

Watch how the agent:
1. **Explores** — reads existing command files, tests, and patterns
2. **Plans** — identifies which files to modify
3. **Implements** — writes code following discovered conventions
4. **Tests** — creates tests matching the naming pattern
5. **Validates** — runs the build to confirm green
6. **Commits** — creates a branch and commit

### Step 4: Evaluate the Output

Check the agent's work against the project standards:

| Criterion | Pass/Fail |
|-----------|-----------|
| Modification stays within Core layer (no DataAccess changes for validation) | |
| Unit tests use Shouldly assertions | |
| Test naming follows `[Method]_[Scenario]_[Result]` convention | |
| No new NuGet packages added | |
| Integration test uses `DatabaseTests().Clean()` pattern | |
| `privatebuild.ps1` passes | |
| Commit message is clear and descriptive | |

### Step 5: Review the PR

If the agent created a PR, review it using the same criteria from Lab 11 (Pull Request as Formal Inspection).

### Step 6: Iterate

If the agent made mistakes:
1. Give it feedback referencing the specific convention violated
2. Watch how it corrects itself
3. Note whether the correction follows the right pattern

### Step 7: Compare Approaches

**Time yourself** on two approaches:
1. How long did the AI agent take (including your review time)?
2. Estimate how long the same feature would take manually

---

## Expected Outcome

- A complete feature implemented by an AI agent
- All quality gates passing
- Understanding of when AI coding is faster vs. slower than manual coding

---

## Discussion Questions

1. Did the agent find and follow the project conventions without being told? What did it get right immediately? What did it miss?
2. How important is the `CLAUDE.md` file for the agent's success? Try the same task on a repo without one.
3. The agent reads existing code to learn patterns. Is it "copying" or "understanding"? Does it matter for the output quality?
4. What types of features are best suited for AI-driven development? What types are poorly suited?
5. How does this change the role of the developer? Is the developer now a "reviewer" or still a "writer"?
6. The curriculum mentions "Code forward" — generating code from specifications. How does this compare to traditional test-first development?
