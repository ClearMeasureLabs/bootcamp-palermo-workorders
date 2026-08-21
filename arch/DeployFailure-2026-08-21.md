# Deploy Workflow Failure Window — 2026-08-21 (work item #9008)

## Summary

The Deploy GitHub Action failed repeatedly on master between 05:32Z and 15:08Z on
2026-08-21 (commits `9614eff0` through `6f577f22`), due to THREE distinct, unrelated
root causes that overlapped in the same time window. All three are now fixed; Deploy
has been green on every master run since 15:21Z (commit `2b66f8204a`), including
current master HEAD.

## Historical Context (episode 21 of 21)

A separate full-history audit of the Deploy workflow (all 502 runs, 2026-02-22 to
2026-08-21, posted as issue #9008 comment
[5373938083](https://github.com/ClearMeasureLabs/bootcamp-palermo-workorders/issues/9008#issuecomment-5373938083))
found 289 completed master runs (207 green, 82 red) across 21 distinct green→red
episodes. Today's 05:32Z–15:08Z outage is **episode 21**, not the first Deploy
failure this project has had:

- The first-ever green→red was 2026-02-22 (TDD job, exit code 1); recovered same
  night.
- A separate, earlier episode today — **EP20, 01:16Z–01:54Z** — was an OctopusDeploy
  create-release-action error that recovered before EP21 (this window) began at
  05:32Z. EP20 is out of scope for this doc's `f68aad70..6f577f22` window.
- The longest-ever outage was **EP18**, 41 consecutive red runs over ~13 days
  (2026-07-16 to 07-29), caused by an invalid/rotated Octopus API key.
- Recurring themes across all 21 episodes: TDD acceptance-test failures (most
  common), Octopus service errors (503s, gateway timeouts, failed deploy tasks,
  release-version collisions — the same collision class seen as mode 2 below), and
  one GitHub environment approval-gate rejection.

The audit's summary of EP21 groups all 6 red runs under "TDD Run Acceptance tests"
with cached-Playwright-binary corruption as the cause and the Container App FQDN
error as secondary. Direct log inspection of each of the 6 failing runs (below,
via `gh run view <id> --log-failed`, re-verified against raw log text) shows EP21
is actually **three distinct proximate failure signatures**, not one: the
Playwright ARM64 cache corruption is real and is the cause of the very first red
run (`32450953882`), but the three middle reds (`dd8b6172`, `d1a35c2a`, `dd14d8fe`)
fail specifically on an Octopus release-version collision
(`Release '1.4.25xx' already exists`) inside the TDD job's Octopus release-creation
step — a different error class from the Playwright/browser-cache symptom, and
consistent with the "release-version collision" theme the historical audit calls
out as recurring across other episodes. The final two reds (`d96dd27d`, `6f577f22`)
show TDD passing (Playwright fixed) but UAT failing on the Container App FQDN
`AuthorizationFailed` lookup. This doc's per-run table and root-cause sections below
reflect the raw-log evidence; the mode numbering (1/2/3) is used consistently
throughout.

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

**Live probe correction (2026-08-21 ~19:00Z):** a direct `/_healthcheck` probe of all
three environments (independent of the Deploy workflow's own health-check step)
found the green run's status only partially trustworthy. All three environments do
serve the correct deployed image (`2.4.2589`) and are online. TDD is Healthy. UAT
and Prod are **Degraded** — the `LlmGateway` health check reports the
`AI_OpenAI_ApiKey` / `AI_OpenAI_Url` / `AI_OpenAI_Model` chat client as
unconfigured in those two environments (TDD has this configuration; UAT/Prod do
not). More significantly, run `32511996076`'s own logs show UAT and Prod were
**never actually health-checked at all**: both "Get Container App FQDN" steps
failed with `AuthorizationFailed` — the deploy service principal lacks
`Microsoft.App/containerApps/read` on `bootcamp-uat` and `bootcamp-prod` — and
`continue-on-error: true` on that step (added by the mode-3 fix, `8268a930`) left
`CONTAINER_APP_URL` empty, which the health-check step's
`if: env.CONTAINER_APP_URL != ''` guard then silently skipped. This corrects the
earlier hypothesis in #9011 (below) that the FQDN failures were purely a missing
repo-vars problem — the real gap is service-principal RBAC, not repo variable
configuration; #9011 has been updated with this correction. Separately, even TDD's
"blocking" health loop treats a `Degraded` response as passing by design
(`deploy.yml` ~226), so `Degraded` currently never blocks promotion anywhere in the
pipeline (tracked as #9017 below).

## Residual Defects

All 6 failing runs in the window are fully explained by the 3 root causes above,
and each root cause has a confirmed fixing commit whose merge coincides with or
precedes the run that stopped exhibiting that error — the failure *window itself*
is closed with no unexplained red run remaining.

However, a follow-up code review of the fixes that closed the window
(`deploy.yml` / `build.yml` / `.octopus/deployment_process.ocl`, posted as issue
#9008 comment
[5373961642](https://github.com/ClearMeasureLabs/bootcamp-palermo-workorders/issues/9008#issuecomment-5373961642))
found that several of the fixes muted symptoms rather than closing gaps, and
surfaced one latent bug of the same class as mode 1. Filed as child work items of
#9008 (leftmost board column), not fixed in this PR:

- **#9011** — UAT/Prod health checks are non-blocking (regression from the mode-3
  fix, commit `8268a930`): the FQDN lookup AND the health-check step are
  `continue-on-error: true`, so a crash-looping revision can promote to Prod with
  zero post-deploy verification. **Updated by live-probe correction** (comment on
  #9011): the FQDN failures are a service-principal RBAC gap
  (`Microsoft.App/containerApps/read` missing on `bootcamp-uat`/`bootcamp-prod`),
  not a repo-vars misconfiguration as first hypothesized — grant the role, then
  remove `continue-on-error` and make the health checks blocking.
- **#9012** — Prod gating weakened by the mode-3-adjacent commit `6adbc92d`: on
  `workflow_dispatch`, Prod can be reached with an unvalidated `release_number` and
  no test gate; also no concurrency group on the Prod job.
- **#9013** — the NuGet cache in `build.yml` has the same cross-architecture
  poisoning mechanism that caused mode 1 (Playwright), unfixed: ARM and x64 jobs
  share a restore-key prefix.
- **#9014** — misc CI hardening: `cancel-in-progress` silently drops superseded
  master Deploy runs, `.trx` upload lacks `if: always()`, SQL connection string is
  unmasked in logs, hardcoded `2.4.` version literal in `run-name`.
- **#9016** — UAT and Prod are currently serving Degraded (LlmGateway chat client
  unconfigured); configure `AI_OpenAI_ApiKey`/`AI_OpenAI_Url`/`AI_OpenAI_Model` in
  those environments or explicitly mark that health check optional there.
- **#9017** — decide whether a `Degraded` health response should gate promotion;
  currently it never does anywhere in the pipeline (`deploy.yml` ~226 treats
  `Degraded` as passing by design).

None of #9011–#9014, #9016, or #9017 block Deploy being green (in workflow-run
terms) today; they are hardening/defect work for the fixes already on master and
for the environment state uncovered by direct probing, scoped as separate PRs per
the discovered-work rule (this PR is the analysis/catalog deliverable only).
