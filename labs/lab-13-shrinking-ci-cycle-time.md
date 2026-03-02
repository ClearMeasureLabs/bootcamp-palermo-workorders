# Lab 13: Shrinking Continuous Integration Cycle Time

**Curriculum Section:** Sections 04-06 (Project Design Strategy / Team Process Design / Operate-Execute)
**Estimated Time:** 40 minutes
**Type:** Analyze + Experiment

---

## Objective

Understand how the CI pipeline uses multiple parallel build checks on every commit and push to shrink cycle time while maintaining comprehensive quality coverage.

---

## Context

The curriculum emphasizes: "Defining Speed: Theory of Constraints & DevOps" and "The Goal: Projects should accelerate over time." The CI pipeline demonstrates this by running **11 checks in parallel** on every push, catching different defect classes simultaneously rather than sequentially.

---

## Steps

### Step 1: Map the Build Trigger

Open `.github/workflows/build.yml`. Note the trigger:

```yaml
on:
  push:
```

**Every push to any branch** triggers the full pipeline — not just PRs, not just master. This is the "shift left" principle in action.

### Step 2: Map the Parallel Build Matrix

Identify all jobs that run concurrently:

| Job | OS | Database | What It Catches |
|-----|-------|----------|-----------------|
| `build-linux` | Ubuntu | SQL Container | Core functionality on Linux |
| `build-sqlite` | Ubuntu | SQLite | DB-engine-specific bugs |
| `build-windows` | Windows | LocalDB | Windows-specific issues |
| `integration-build-arm` | Ubuntu ARM64 | SQLite | Architecture-specific bugs |
| `code-analysis` | Ubuntu | — | Style violations, analyzer warnings |
| `security-scan` | Ubuntu | — | Vulnerabilities, secrets, deprecated packages |

These 6 jobs start **simultaneously** on every push.

### Step 3: Map the Sequential Dependencies

After the parallel jobs, these run in sequence:

```
[All parallel builds complete]
        ↓
acceptance-tests (Playwright E2E on Linux)
        ↓
acceptance-tests-arm (Playwright on ARM)
        ↓
docker-build-image (only after build-linux succeeds)
        ↓
publish-github-packages → publish-octopus (deployment artifacts)
```

### Step 4: Study Concurrency Groups

Find this in the workflow:

```yaml
concurrency:
  group: build-${{ github.ref }}
  cancel-in-progress: true
```

This means: when a new commit arrives on a branch, **cancel any in-progress build** for that branch. This prevents wasting CI resources on outdated commits.

### Step 5: Calculate Cycle Time Savings

**Sequential approach** (one job at a time):
- Build Linux: ~5 min
- Build SQLite: ~5 min
- Build Windows: ~5 min
- Build ARM: ~5 min
- Code analysis: ~2 min
- Security scan: ~2 min
- Acceptance tests: ~10 min
- **Total: ~34 minutes**

**Parallel approach** (current pipeline):
- Parallel builds: ~5 min (all run simultaneously)
- Acceptance tests: ~10 min (after builds)
- **Total: ~15 minutes**

That's a **55% reduction** in cycle time from parallelization alone.

### Step 6: Study the Security Scan

Find the `security-scan` job. It runs:
1. **Gitleaks** — scans for secrets/credentials in code
2. **NuGet vulnerability check** — `dotnet list package --vulnerable`
3. **Deprecated package check** — `dotnet list package --deprecated`
4. **Hardcoded password/API key detection** — grep patterns

These run in parallel with builds, so security feedback arrives at the same time as build feedback.

### Step 7: Study Test Reporting

Find `dorny/test-reporter@v2.1.1` in the workflow. Note `if: always()` — the test reporter runs even if tests fail, ensuring results are always visible in the PR. This is the "Amplify Feedback Loops" principle.

### Step 8: Design an Improvement

**Exercise:** If you added a new quality check (e.g., database migration validation, or API contract testing), where would you insert it in the pipeline?

- Should it run in parallel with builds or sequentially after?
- What concurrency group should it use?
- Should it block the acceptance tests?

---

## Expected Outcome

- Understanding of how parallel CI checks shrink cycle time
- Knowledge of the concurrency cancellation pattern
- Ability to calculate the time savings of parallelization

---

## Discussion Questions

1. Why test on 4 different OS/database combinations? Isn't one enough?
2. The concurrency group cancels in-progress builds when new commits arrive. What is the tradeoff? (Speed vs. wasted work on intermediate commits)
3. How does `cancel-in-progress: true` relate to the Theory of Constraints? (The constraint is CI machine time; canceling outdated builds frees the constraint)
4. Security scanning runs in parallel with builds. Why not make it a prerequisite? (Speed — you want security feedback at the same time as build feedback, not before)
5. What would happen to team velocity if the CI pipeline took 60 minutes instead of 15?
