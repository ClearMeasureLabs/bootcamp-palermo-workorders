# Analysis: Consolidating Deploy Workflows

## Current State

Three GitHub Actions workflows handle deployment:

| Workflow | File | Trigger | Branches |
|---|---|---|---|
| **Integration Build** | `integration-build-feature-branches.yml` | `push`, `workflow_dispatch` | All |
| **Deploy to TDD** | `deploy-tdd.yml` | `workflow_run` (Integration Build) | All |
| **Deploy to Environments** | `deploy-to-environments.yml` | `workflow_run` (Integration Build) | `master`/`main` only |

### How TDD status reaches PRs today

`workflow_run`-triggered workflows do **not** natively appear as check runs on pull requests.
`deploy-tdd.yml` works around this by explicitly calling the GitHub Statuses API:

```yaml
gh api "repos/${{ github.repository }}/statuses/$sha" \
  -f state="pending" \
  -f context="Deploy to TDD"
```

This is the **only** mechanism reporting TDD results back to the PR.

## Assumption Validation

> "Every feature branch would run the TDD job and that check would be reported for pull requests,
> but because the UAT job would require manual approval, the check would never be reported to a pull request."

**This assumption is correct.** Detailed reasoning:

1. **TDD job** completes and uses the Statuses API to report success/failure to the commit SHA.
   Branch protection can require the `"Deploy to TDD"` status context.

2. **UAT/Prod jobs** with environment protection rules (required reviewers) enter a "Waiting" state.
   On feature branches nobody approves them, so they wait indefinitely
   (GitHub auto-rejects after 30 days).

3. **The waiting UAT/Prod jobs do not block TDD reporting.**
   The TDD job runs independently. Its Statuses API call executes regardless of sibling job states.
   UAT/Prod never post any status to the PR commit, so they are invisible to branch protection.

## Advantages of Consolidation

- **Simpler maintenance** — two workflow files become one
- **Unified view** — all deployments visible in a single Actions workflow run
- **Consistent trigger logic** — no risk of TDD and environment deploy triggers diverging
- **Shared Octopus release** — TDD creates the release; UAT/Prod reuse it via `needs:` dependency

## Concerns

### Perpetually waiting workflow runs on feature branches

If UAT/Prod jobs rely solely on environment approval gates, every feature branch push
creates a workflow run stuck in "Waiting" for up to 30 days. This clutters the Actions tab.

### Recommended mitigation

Use `if:` conditions on UAT/Prod jobs to skip them on non-default branches:

```yaml
deploy-to-uat:
  needs: [deploy-to-tdd]
  if: >-
    github.event.workflow_run.head_branch == 'master' ||
    github.event.workflow_run.head_branch == 'main'
  environment:
    name: UAT
```

This way:
- Feature branches: only TDD runs; UAT/Prod are skipped (not "waiting")
- `master`/`main`: TDD runs first, then UAT requires manual approval, then Prod requires manual approval
- The workflow run completes cleanly on feature branches after TDD finishes

### `workflow_run` branch filtering

Currently `deploy-to-environments.yml` uses `branches: ['master', 'main']` at the trigger level
to avoid starting at all on feature branches. In a consolidated workflow, the trigger must omit
the branch filter (so TDD runs on all branches), and the branch restriction moves to `if:` on UAT/Prod jobs.

## Proposed Consolidated Structure

```
Integration Build (all branches)
        |
        v
  workflow_run trigger
        |
        v
+-------------------+
| Deploy Workflow   |
|                   |
| Job: TDD          |  <-- runs on ALL branches
|   - Create release|
|   - Deploy to TDD |
|   - Run acceptance|
|   - Report status |
|                   |
| Job: UAT          |  <-- if: master/main only
|   needs: TDD      |
|   environment: UAT|  <-- requires manual approval
|   - Deploy to UAT |
|                   |
| Job: Prod         |  <-- if: master/main only
|   needs: UAT      |
|   environment:Prod|  <-- requires manual approval
|   - Deploy to Prod|
+-------------------+
```

## Conclusion

Consolidation is feasible and the core assumption holds: TDD status is independently reported to PRs
via the Statuses API, and UAT/Prod checks never appear on PRs. The `if:` condition approach
for branch filtering is preferred over relying on environment approval stalling.
