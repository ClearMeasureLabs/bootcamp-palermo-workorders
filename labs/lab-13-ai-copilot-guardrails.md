# Lab 13: AI-Driven Development - Using Copilot with Architecture Guardrails

**Curriculum Section:** Section 08 (AI-Driven Development)
**Estimated Time:** 45 minutes
**Type:** Build + Experiment

---

## Objective

Use AI coding tools (GitHub Copilot, Claude Code, or similar) effectively within established architectural constraints. Experience how guardrails make AI more productive, not less.

---

## Steps

### Step 1: Read the AI Guardrails

Open `.github/copilot-instructions.md` and `CLAUDE.md`. List key constraints: no new NuGet packages, Shouldly assertions, Stub prefix, onion architecture, no SDK changes.

### Step 2: AI-Assisted Test Writing

Use your AI tool to generate a unit test for `InProgressToCompleteCommand`. Evaluate: Does it follow naming conventions? Use Shouldly? Create proper test data?

### Step 3: AI-Assisted Feature Development

Ask AI to add a `Notes` property to `WorkOrder`. Evaluate against architecture rules: correct Core placement? Migration created? Tests included? No forbidden packages?

### Step 4: AI Guardrail Violation Detection

Ask AI to add FluentValidation. Observe: this requires a new NuGet package (rejected), the domain model already handles validation, and the PR review rules would catch it.

### Step 5: Validate and Build

```powershell
dotnet format style src/ChurchBulletin.sln --verify-no-changes
.\PrivateBuild.ps1
```

---

## Expected Outcome

- Experience using AI within architectural constraints
- Understanding that guardrails make AI more productive (fewer rejected PRs)
