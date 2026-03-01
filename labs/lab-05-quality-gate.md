# Lab 05: Quality Gate - Static Analysis & Build Pipeline

**Curriculum Section:** Sections 04-05 (Project Design Strategy / Team Process Design)
**Estimated Time:** 35 minutes
**Type:** Analyze

---

## Objective

Explore the automated quality enforcement in the build pipeline. Understand how static analysis, CI/CD, and formal checklists implement the "Three Proven Defect Reduction Methods" from the lecture: static analysis, testing, and formal inspections.

---

## Context

The curriculum identifies three proven defect reduction methods:
1. **Static analysis** — Treat warnings as errors, linters, Roslyn analyzers
2. **Testing** — L0, L1, L2 test levels
3. **Formal inspections** — Pull request checklists

This lab traces how each is implemented in the codebase.

---

## Steps

### Step 1: Explore Static Analysis Rules

Open `src/.editorconfig`. Identify at least 3 naming rules:

| Rule | Enforcement |
|------|------------|
| Interfaces must start with `I` | `dotnet_naming_rule.interface_should_be_begins_with_i` |
| Types must be PascalCase | `dotnet_naming_rule.types_should_be_pascal_case` |
| Non-field members must be PascalCase | `dotnet_naming_rule.non_field_members_should_be_pascal_case` |

Also note code style rules:
- File-scoped namespaces preferred
- Primary constructors preferred
- Expression-bodied properties allowed

### Step 2: Trigger a Static Analysis Failure

Create a temporary file or add a method to any Core file with a snake_case name:

```csharp
public void get_work_orders() { }
```

Run the style checker:

```powershell
dotnet format style src/ChurchBulletin.sln --verify-no-changes
```

**Expected result:** The command exits with a non-zero code, reporting the naming violation.

**Revert the change immediately.**

### Step 3: Trace the Local Build Pipeline

Open `build.ps1` and trace the quality gates in execution order:

```
Init → Compile → UnitTests → IntegrationTest → Package-Everything
```

For each stage, find the function in `build.ps1` and note:

| Stage | What It Does | Failure = ? |
|-------|-------------|-------------|
| `Init` | Clean, restore NuGet packages | Missing dependencies |
| `Compile` | `dotnet build --configuration Release` with warnings-as-errors | Compilation errors block everything |
| `UnitTests` | `dotnet test src/UnitTests` with code coverage | Logic errors caught early |
| `IntegrationTest` | `dotnet test src/IntegrationTests` against real DB | Data/persistence errors caught |
| `Package-Everything` | Create deployment packages | Only runs if all tests pass |

### Step 4: Examine the CI/CD Pipeline

Open `.github/workflows/build.yml`. Identify the build matrix:

| Job | OS | Database | Purpose |
|-----|-------|----------|---------|
| `build-linux` | Ubuntu | SQL Container | Primary integration build |
| `build-sqlite` | Ubuntu | SQLite | Alternative DB engine test |
| `build-windows` | Windows | LocalDB | Windows environment validation |
| `integration-build-arm` | Ubuntu ARM64 | SQLite | ARM architecture support |

Note the **sequential quality gates** that run after the parallel builds:
1. `code-analysis` — `dotnet format` style and analyzers
2. `security-scan` — Gitleaks, NuGet vulnerabilities, credential scanning
3. `acceptance-tests` — Playwright E2E tests (depends on successful build-linux)

### Step 5: Review the Pull Request Template

Open `.github/pull_request_template.md`. Study both checklists:

**Submitter Checklist:**
- [ ] Issue clearly tagged
- [ ] Branch status narrated
- [ ] Expect approval checklist satisfied

**Approver Checklist:**
- [ ] Build and all test suites passing
- [ ] Static analysis ran and passed
- [ ] All changes with accompanying tests
- [ ] Dependencies pre-approved
- [ ] Team coding standard adhered to

### Step 6: Review the Automated Code Review Rules

Open `.github/copilot-code-review-instructions.md`. List the **automatic rejection** criteria:

1. New NuGet packages without approval
2. .NET SDK version changes without approval
3. FluentAssertions instead of Shouldly
4. Test doubles named with "Mock" prefix (should be "Stub")
5. Modifications to `.octopus/`, build scripts, or pipeline files
6. Secrets or credentials in code
7. Onion Architecture violations

### Step 7: Trace the Deployment Pipeline

Open `.github/workflows/deploy.yml` (if present) or review the build workflow for deployment stages:

```
Build Success → TDD Environment (automatic)
                    ↓
                UAT Environment (manual approval)
                    ↓
                Production (manual approval)
```

---

## Expected Outcome

- Understanding of how automated quality gates prevent defects before they escape
- Knowledge of the three defect reduction methods as implemented in this codebase
- Awareness of what triggers automatic PR rejection

---

## Discussion Questions

1. How do these gates implement the **Three Ways of DevOps**?
   - **First Way (Flow):** Pipeline stages optimize the flow from code to production
   - **Second Way (Feedback):** Test results and static analysis provide fast, actionable feedback
   - **Third Way (Experimentation):** Branch-based development enables safe experimentation
2. Why does the CI run **4 parallel build jobs** across different OS/database combinations?
3. The PR template has both submitter AND approver checklists. Why is the dual-checklist important?
4. What is the cost of adding a quality gate that takes 10 minutes to run? When is that cost justified?
5. The curriculum states "only 40% of defects are caused by problems with code." What non-code defects could the PR checklist catch that static analysis cannot?
