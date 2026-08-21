# Deploy Workflow Failure Window — 2026-08-21 (work item #9008)

## Summary

The Deploy GitHub Action failed repeatedly on master between 05:32Z and 15:08Z on
2026-08-21 (commits `9614eff0` through `6f577f22`), due to THREE distinct, unrelated
root causes that overlapped in the same time window. All three are now fixed; Deploy
has been green on every master run since 15:21Z (commit `2b66f8204a`), including
current master HEAD.

## Failure Window

- First failing run: `32450953882` (05:32:57Z, headSha `9614eff0`, TDD job)
- Last failing run: `32495982631` (15:08:30Z, headSha `6f577f22`, UAT job)
- Last known-good before the window: run `32447607246` (04:37:41Z, headSha `f68aad70`)
- First recovered run: `32497148168` (15:21:27Z, headSha `2b66f8204a`)

## Failing Deploy Runs on Master During the Window

| Run ID | Time (UTC) | headSha | Job / Failure |
|---|---|---|---|
| 32450953882 | 05:32:57Z | `9614eff0` | TDD job failed (mode 1: Playwright ARM64 cache) |
| 32452767476 | 06:01:51Z | `dd8b6172` | TDD/Octopus job failed (mode 2: Octopus version collision) |
| 32487627650 | 13:35:19Z | `d1a35c2a` | Octopus release job failed (mode 2 recurring) |
| 32489463066 | 13:56:11Z | `dd14d8fe` | Octopus release job failed (mode 2 recurring) |
| 32493694397 | 14:43:41Z | `d96dd27d` | UAT job failed (mode 3: Container App FQDN AuthorizationFailed) — TDD now passing, confirms mode 1 fixed |
| 32495982631 | 15:08:30Z | `6f577f22` | UAT job failed (mode 3 recurring) |

## Three Distinct Root-Cause Failure Modes

(Identified via `gh run view <id> --log-failed`)

### Mode 1 — TDD Playwright ARM64/x64 browser-cache mismatch

Error: `headless_shell: 1: Syntax error: "&" unexpected` when launching the cached
browser binary — a wrong-architecture binary was served from the Playwright browser
cache, causing `Microsoft.Playwright.TargetClosedException` during
`BlazorWasmWarmUp.ExecuteAsync()` / `ServerFixture.OneTimeSetUp()`, failing all 96
acceptance tests in the TDD job.

**Fixed by:** commit `f622d6a0` (PR #8996, "Fix Deploy Playwright cache restoring
ARM64 browsers on x64", merged as `d96dd27d`) — no ARM64 cache error appears in any
run after this commit.

### Mode 2 — Octopus release version collision

Error from the "Create Octopus Release" step: `Release '1.4.2540' already exists for
this project. Please use a different version, or look at using a mask to
auto-increment the number.` Root cause: version numbers under the `1.4.x` scheme
collided across separate CI runs creating releases faster than the auto-increment
mask could keep unique values clear.

**Fixed by:** commit `1ff3080f` ("Bump release major version from 1.4.x to 2.4.x") —
moving the major version series eliminated collisions with already-registered
`1.4.x` releases in Octopus. No version-collision error appears in any run after
this commit.

### Mode 3 — UAT/Prod Container App FQDN lookup failing the whole job

Error from the "Get Container App FQDN" step:
`ERROR: (AuthorizationFailed) The client '...' does not have authorization to
perform action 'Microsoft.App/containerApps/read' over scope
'.../resourceGroups/bootcamp-uat/.../containerApps/ui-gh'`, followed by
`Write-Error "Error: Could not retrieve container app FQDN"; exit 1` — the step
aborted the whole UAT job even though the underlying Octopus deployment had already
succeeded; the FQDN lookup was a non-essential diagnostic step wired as a hard gate.

**Fixed by:** commit `8268a930` (PR #9002, "Do not fail UAT/Prod on Azure Container
App FQDN lookup", merged as headSha `2b66f8204a` — the first green run) — the lookup
is now non-blocking since Octopus already gates real deploy success.

## Commit Classification, `f68aad70..6f577f22` (oldest to newest)

All commits not listed below are UNRELATED — pure test-coverage/hardening additions
in PRs #8975 (Worker coverage), #8976 (UI.Server/Api coverage), #8977
(LlmGateway/McpServer coverage), #8979 (Copilot test hardening), plus assorted
Qodana findings fixes — none touch Deploy workflow YAML or
Playwright/Octopus/Container-App configuration.

- `9614eff0` (PR #8977, LlmGateway/McpServer coverage) — UNRELATED to root cause;
  merely the first commit whose Deploy run exposed the pre-existing Playwright
  ARM64 cache bug (mode 1). Evidence: run `32450953882`.
- `dd8b6172`, `d1a35c2a`, `dd14d8fe` — headShas of runs that failed on the Octopus
  version collision (mode 2 recurring). UNRELATED as causes (the collision is an
  Octopus-side release-numbering issue, not introduced by these specific commits);
  each is evidence the bug was still present.
- `f622d6a0` (PR #8996) — FIXED mode 1 (Playwright ARM64 cache). Evidence: no ARM64
  cache failure in any run after `d96dd27d`.
- `d96dd27d`, `6f577f22` — headShas of runs where TDD passed (confirming mode 1
  fixed) but UAT failed on the FQDN AuthorizationFailed error (mode 3). Evidence:
  runs `32493694397` and `32495982631`.
- `1ff3080f` — FIXED mode 2 (Octopus version collision), by bumping major version
  1.4.x → 2.4.x.
- Commits after `6f577f22`, before first green run `2b66f8204a`: `8268a930`
  (PR #9002) — FIXED mode 3 (Container App FQDN AuthorizationFailed made
  non-blocking). This commit's merge (`2b66f8204a`) is the headSha of the first
  green Deploy run (`32497148168`, 15:21:27Z).
- `6adbc92d` (PR #9003, "Run Prod after UAT even when TDD was skipped") — landed
  AFTER the window closed (strictly after `2b66f8204a`, confirmed via
  `git merge-base --is-ancestor`). Follow-up hardening, not part of what fixed this
  incident; it addresses Prod-stage gating when TDD is skipped by path filters,
  unrelated to modes 1-3.
- "skipped" job conclusions seen throughout the window (e.g. UAT/Prod skipped when
  TDD fails) are the workflow's normal cascading stage-dependency gating (UAT only
  runs if TDD succeeds, Prod only if UAT succeeds) — expected behavior, not a
  defect.

## Verification of Current State

Current master HEAD (`60f1cf10`) Deploy run `32511996076`
(2026-08-21T18:10:45Z) is fully green — TDD: success, UAT: success, Prod: success
(confirmed via `gh run view 32511996076 --json jobs`). No fresh trigger or revert
was required; the window is fully closed with all three root causes fixed by
commits already on master (`f622d6a0`, `1ff3080f`, `8268a930`).

## Residual Defects

None found. All 6 failing runs in the window are fully explained by the 3 root
causes above, and each root cause has a confirmed fixing commit whose merge
coincides with or precedes the run that stopped exhibiting that error. No child
work items filed for this issue.
