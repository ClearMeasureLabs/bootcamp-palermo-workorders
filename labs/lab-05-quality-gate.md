# Lab 05: Quality Gate - Static Analysis & Build Pipeline

**Curriculum Section:** Sections 04-05 (Project Design Strategy / Team Process Design)
**Estimated Time:** 35 minutes
**Type:** Analyze

---

## Objective

Explore the automated quality enforcement in the build pipeline. Understand how static analysis, CI/CD, and formal checklists implement the three proven defect reduction methods.

---

## Steps

### Step 1: Explore Static Analysis Rules

Open `src/.editorconfig`. Identify naming rules: interfaces must start with `I`, types must be PascalCase, non-field members PascalCase.

### Step 2: Trigger a Static Analysis Failure

Add a snake_case method to any Core file, then run:

```powershell
dotnet format style src/ChurchBulletin.sln --verify-no-changes
```

Observe the failure. **Revert immediately.**

### Step 3: Trace the Local Build Pipeline

Open `build.ps1`. Trace: `Init` → `Compile` → `UnitTests` → `IntegrationTest` → `Package-Everything`.

### Step 4: Examine the CI/CD Pipeline

Open `.github/workflows/build.yml`. Identify the parallel build matrix (Linux SQL, SQLite, Windows LocalDB, ARM) and sequential quality gates (code-analysis → security-scan → acceptance-tests).

### Step 5: Review the PR Template

Open `.github/pull_request_template.md`. Study both the submitter and approver checklists.

### Step 6: Review Automated Code Review Rules

Open `.github/copilot-code-review-instructions.md`. List the automatic rejection criteria: new NuGet packages, FluentAssertions, Mock prefix, architecture violations, secrets.

---

## Expected Outcome

- Understanding of how automated quality gates prevent defects before they escape
- Knowledge of the three defect reduction methods as implemented in this codebase

---

## Discussion Questions

1. How do these gates implement the Three Ways of DevOps?
2. Why does CI run 4 parallel build jobs across different OS/database combinations?
3. The PR template has both submitter AND approver checklists. Why is the dual-checklist important?
