# Lab 13: AI-Driven Development - Using Copilot with Architecture Guardrails

**Curriculum Section:** Section 08 (AI-Driven Development)
**Estimated Time:** 45 minutes
**Type:** Build + Experiment

---

## Objective

Use AI coding tools (GitHub Copilot, Claude Code, or similar) effectively within established architectural constraints. Experience how guardrails make AI more productive, not less.

---

## Context

The curriculum covers "Extreme Agile - AI-Driven Development" with emphasis on:
- Code forward with Copilot and productivity tools
- Architecture standards, design patterns, and consistency
- The importance of guardrails for AI-generated code

This project has two guardrail documents:
- `.github/copilot-instructions.md` — Copilot-specific rules
- `CLAUDE.md` — Claude Code-specific rules

---

## Steps

### Step 1: Read the AI Guardrails

Open `.github/copilot-instructions.md`. List the key constraints:

| Rule | Why It Exists |
|------|---------------|
| Do NOT add NuGet packages without approval | Prevents dependency bloat and license issues |
| Do NOT change .NET SDK version | Ensures team consistency |
| Use Shouldly for assertions | Single assertion library across all tests |
| Prefix test doubles with `Stub` | Consistent naming convention |
| Follow AAA pattern without section comments | Clean test structure |
| Maintain Onion Architecture | Prevent architectural erosion |

Open `CLAUDE.md`. Note the additional constraints for AI agents operating on this codebase.

### Step 2: Exercise 1 - AI-Assisted Test Writing

Use your AI coding tool to generate a unit test for `InProgressToCompleteCommand`.

**Prompt suggestion:**
> "Write a unit test for InProgressToCompleteCommand that verifies the command sets CompletedDate when executed. Follow the patterns in DraftToAssignedCommandTests.cs — use NUnit with Shouldly assertions."

**Evaluate the output:**
- [ ] Does it use `[TestFixture]` and `[Test]` attributes?
- [ ] Does it use Shouldly assertions (not FluentAssertions, not `Assert.That`)?
- [ ] Does it follow the naming convention?
- [ ] Does it test meaningful behavior (not just "does it compile")?
- [ ] Does it create test data correctly (Employee, WorkOrder with proper status)?

If the AI generated incorrect code, fix it to match the project conventions.

### Step 3: Exercise 2 - AI-Assisted Feature Development

Ask your AI tool to add a `Notes` field to `WorkOrder`. Evaluate the output against the architecture rules.

**Prompt suggestion:**
> "Add a Notes property (string, max 2000 characters) to WorkOrder. Include the database migration, EF mapping update, unit test, and integration test."

**Evaluate the output checklist:**
- [ ] `WorkOrder.cs` in Core — property added correctly?
- [ ] New migration SQL in `src/Database/scripts/Update/` — correct numbering?
- [ ] `WorkOrderMap.cs` in DataAccess — mapping added?
- [ ] Unit test in UnitTests — follows conventions?
- [ ] Integration test — follows patterns from existing handler tests?
- [ ] NO new NuGet packages added?
- [ ] NO references from Core to DataAccess?

### Step 4: Exercise 3 - AI Guardrail Violation Detection

Ask your AI tool to add input validation using FluentValidation:

**Prompt suggestion:**
> "Add FluentValidation to validate WorkOrder Title is not empty and Description is under 4000 characters."

**Evaluate:** This should trigger multiple guardrail violations:
1. FluentValidation is a **new NuGet package** — rejected by review rules
2. The domain model already handles truncation in the `Description` setter
3. The architecture may not support the validation library's DI patterns

**Question:** How would you achieve the same validation without adding a new package?

### Step 5: Validate AI-Generated Code

Take whatever code the AI generated in Steps 2-3 (after your corrections) and run:

```powershell
dotnet build src/ChurchBulletin.sln --configuration Release
dotnet test src/UnitTests --configuration Release
dotnet test src/IntegrationTests --configuration Release
```

Fix any failures.

### Step 6: Run Static Analysis on AI Output

```powershell
dotnet format style src/ChurchBulletin.sln --verify-no-changes
```

AI-generated code often violates style rules. Fix any formatting issues.

### Step 7: Full Build Verification

```powershell
.\privatebuild.ps1
```

---

## Expected Outcome

- Experience using AI within architectural constraints
- Understanding that guardrails make AI **more productive** (fewer rejected PRs, fewer rework cycles)
- At least one AI-generated test that passes all quality gates

---

## Discussion Questions

1. Did the AI follow the project conventions on its first attempt? What did you need to correct?
2. How do the guardrail documents (`.github/copilot-instructions.md`, `CLAUDE.md`) change the AI's output? Try the same prompt with and without context.
3. The curriculum mentions "Architecture standards, design patterns, and consistency" as key to AI-driven development. Why are these MORE important (not less) when using AI?
4. The AI generated code for a feature you could have written manually. When is AI-generated code faster? When is it slower? (Fast for boilerplate, slow when it violates conventions and requires rework)
5. How does the PR review process (Lab 10) serve as a safety net for AI-generated code?
