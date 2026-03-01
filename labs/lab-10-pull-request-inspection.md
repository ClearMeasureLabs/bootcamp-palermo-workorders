# Lab 10: Pull Request as a Formal Inspection

**Curriculum Section:** Section 06 (Operate/Execute - Pull Requests as Formal Inspections)
**Estimated Time:** 35 minutes
**Type:** Build + Process

---

## Objective

Practice the pull request review process as a quality gate. Experience formal inspections by creating a PR for your Day 2 work and peer-reviewing another student's PR.

---

## Context

The curriculum positions pull requests as **formal inspections** — a proven defect reduction method. The project enforces this through:
- A PR template with dual checklists (submitter + approver)
- Automated code review rules (`.github/copilot-code-review-instructions.md`)
- CI/CD pipeline gates that must pass before merge

---

## Steps

### Step 1: Prepare Your Changes

Ensure all changes from Labs 06-09 are on your feature branch. Run the full quality check:

```powershell
.\privatebuild.ps1
```

Verify all tests pass.

### Step 2: Run Static Analysis

```powershell
dotnet format style src/ChurchBulletin.sln --verify-no-changes
dotnet format analyzers src/ChurchBulletin.sln --verify-no-changes
```

Fix any violations before proceeding.

### Step 3: Review the PR Template

Open `.github/pull_request_template.md`. Mentally check each item:

**Submitter Checklist:**
- [ ] Issue clearly tagged
- [ ] Branch status narrated (is this feature complete or incremental?)
- [ ] All approval criteria satisfied

**Approver Checklist:**
- [ ] Build and all test suites passing
- [ ] Static analysis ran and passed
- [ ] All changes with accompanying tests
- [ ] Dependencies pre-approved
- [ ] Team coding standard adhered to

### Step 4: Create the Pull Request

```powershell
git add -A
git commit -m "Add unit tests, integration tests, and database migration from Labs 06-09"
git push -u origin yourusername/day2-lab-exercises
```

Create the PR:

```powershell
gh pr create --title "Day 2 lab exercises: tests and migration" --body "## Summary
- Added unit tests for WorkOrder and Employee domain models (Lab 06)
- Added integration tests for state command handler and query filtering (Lab 07)
- Added database migration for Priority column (Lab 08)
- Added bUnit component tests (Lab 09)

## Submitter Checklist
- [x] privatebuild.ps1 passes
- [x] Static analysis passes
- [x] All changes have accompanying tests

## Test Plan
- [ ] Run privatebuild.ps1
- [ ] Verify new unit tests in WorkOrderTests and EmployeeTests
- [ ] Verify new integration tests for state commands and queries
- [ ] Verify database migration applies cleanly"
```

### Step 5: Peer Review Exercise

**Swap PRs with a partner.** Review their PR using the automated review rules as your checklist.

Open `.github/copilot-code-review-instructions.md` and check for each rejection criterion:

| Check | Pass/Fail |
|-------|-----------|
| No new NuGet packages added | |
| No .NET SDK version changes | |
| Uses Shouldly (not FluentAssertions) | |
| Test doubles prefixed with `Stub` (not `Mock`) | |
| No modifications to build scripts or `.octopus/` | |
| No secrets or credentials | |
| Onion Architecture respected | |

### Step 6: Review for Quality

Beyond the automated checks, review for:

1. **Test naming:** Do test names follow `[Method]_[Scenario]_[Result]` or `Should*` convention?
2. **Test isolation:** Does each test clean up after itself or use `DatabaseTests().Clean()`?
3. **Assertions:** Are assertions specific and meaningful? (Not just `ShouldNotBeNull()`)
4. **Code style:** PascalCase for methods, camelCase for variables?
5. **Architecture:** Do changes in Core avoid referencing DataAccess?

### Step 7: Leave Review Comments

On your partner's PR, leave at least:
- **1 positive comment** — something they did well
- **1 constructive suggestion** — an improvement opportunity
- **1 question** — something you want them to explain

### Step 8: Address Review Feedback

Review the comments on your own PR. Address any feedback by pushing additional commits.

---

## Expected Outcome

- A properly created PR with submitter checklist completed
- Experience reviewing another developer's code against established standards
- At least one round of review feedback given and received

---

## Discussion Questions

1. Why does the PR template have BOTH a submitter AND approver checklist? What does each prevent?
2. The automated review rules reject `FluentAssertions` in favor of `Shouldly`. Why enforce a single assertion library across the team?
3. How does PR review implement the "Second Way of DevOps" (Amplify Feedback Loops)?
4. The curriculum mentions "an escaped defect is a process killer." How does a thorough PR review prevent defects from escaping to production?
5. What is the cost/benefit tradeoff of formal PR reviews? When might they slow the team down, and how can that be mitigated?
