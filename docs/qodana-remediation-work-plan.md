# Qodana Remediation Work Plan

**Tracking issue:** [#8839](https://github.com/ClearMeasureLabs/bootcamp-palermo-workorders/issues/8839)  
**Source run:** [Build 32329753510](https://github.com/ClearMeasureLabs/bootcamp-palermo-workorders/actions/runs/32329753510) (`cursor/crap-metrics-artifacts-b36a`, Qodana Community .NET 2026.2.672)  
**Artifact:** `qodana-report`  
**Baseline total:** **973** findings (463 High, 510 Info)  
**CI today:** warnings-only (`continue-on-error: true` on the Qodana job in `.github/workflows/build.yml`)  
**Goal:** reduce High findings enough to turn Qodana into a **build failure** gate (remove `continue-on-error`, set `fail-threshold` / baseline in `qodana.yaml`)

---

## 1. Executive summary

Qodana Community for .NET is producing a large volume of findings, but severity and risk are uneven:

| Bucket | Count | Share | Gate relevance |
|--------|------:|------:|----------------|
| High — true defect / correctness risk | ~52 | ~5% | **Must fix** before fail-on-findings |
| High — dead / unused symbols | ~107 | ~11% | Fix or suppress with justification |
| High — bulk noise (usings, `!` suppressions) | ~225 | ~23% | Mechanical cleanup; high ROI for counts |
| High — other redundancies / naming / docs | ~79 | ~8% | Batch cleanup |
| Info — style / modernization | 510 | ~52% | Optional; exclude or baseline for gate |

**Recommended gate strategy:** do not fail the build on all 973 items. Remediate P0–P2 High defect/dead-code cohorts, mechanically clear bulk High noise (P3), then enable a **fail-threshold of 0 new High** against a committed baseline that either excludes Info-only inspections or baselines remaining Info.

---

## 2. Categorization

### 2.1 By severity

| Severity | Count | Notes |
|----------|------:|-------|
| High | 463 | Treated as gate candidates when fail mode is enabled |
| Info | 510 | Style / modernization; not suitable as a hard fail set without triage |
| Critical / Error | 0 | None in this Community profile run |

### 2.2 By Qodana category

| Category | Count | Dominant severity |
|----------|------:|-------------------|
| Redundancies in Code | 314 | Mostly High (usings, `!`, assignments) |
| Common Practices and Code Improvements | 217 | Info |
| Language Usage Opportunities | 180 | Info |
| Potential Code Quality Issues | 128 | Mostly High |
| Redundancies in Symbol Declarations | 97 | Mix |
| Constraints Violations | 32 | Mix (namespaces, naming) |
| Syntax Style | 3 | Info |
| Compiler Warnings | 2 | High (obsolete API, fire-and-forget) |

### 2.3 By project area

| Area | Total | High | Notes |
|------|------:|-----:|-------|
| IntegrationTests | 185 | 74 | Heavy test noise |
| UnitTests | 174 | 74 | Heavy test noise |
| AcceptanceTests | 137 | 89 | HttpClient, usings, assignments |
| UI/Server | 123 | 82 | Large production surface |
| Core | 89 | 16 | Domain; prioritize reference-comparison fixes |
| ChurchBulletin.ServiceDefaults | 80 | 43 | Namespace mismatches dominate High |
| UI.Shared | 64 | 20 | Blazor HTML id resolution |
| UI/Api | 26 | 21 | Error.cshtml path errors |
| Remaining (Client, LlmGateway, McpServer, DataAccess, Database, Worker) | 95 | ~44 | Smaller slices |

**Production High:** 226 · **Test High:** 237

### 2.4 Top inspection types (all severities)

| Count | Inspection |
|------:|------------|
| 145 | Redundant using directive |
| 89 | Use object or collection initializer |
| 79 | Redundant nullable warning suppression (`!`) |
| 64 | Member can be made private |
| 54 | Property can be made init-only |
| 51 | Method has async overload |
| 41 | Convert into `await using` |
| 36 | Unused auto-property accessor (non-private) |
| 27 | Redundant type declaration body |
| 25 | Convert constructor into primary constructor |

---

## 3. Priority taxonomy (for remediation)

### P0 — Correctness / reliability (fix first) — **42 High**

Real defect or resource-lifecycle risk. These should be zero (or explicitly accepted) before Qodana fails the build.

| Inspection | Count | Where | Risk |
|------------|------:|-------|------|
| Possible unintended reference comparison | 14 | Core (2), IntegrationTests (12) | Wrong equality semantics (`==` on entities / values) |
| Using-statement resource initialization | 9 | AcceptanceTests, UI/Client, UnitTests | Dispose skipped if property init throws |
| Short-lived `HttpClient` | 6 | AcceptanceTests | Socket exhaustion under load |
| Possible multiple enumeration | 6 | LlmGateway (4), UnitTests (2) | Double enumeration / side effects |
| Empty general catch clause | 4 | AcceptanceTests, ServiceDefaults | Swallows failures |
| Obsolete API (`CS0618`) | 1 | UI/Client `App.razor` | Router `NotFound` → `NotFoundPage` |
| Inconsistent field synchronization | 1 | UnitTests `ObjectMother` | Thread-safety smell |
| Suspicious type conversion | 1 | UnitTests | Meaningless type check |

**Production hotspots for P0:**

- `Core/Model/WorkOrder.cs`, `Core/Model/StateCommands/StateCommandBase.cs` — reference comparison
- `LlmGateway/TracingChatClient.cs` — multiple enumeration
- `UI/Client/PublisherGateway.cs` — using init
- `ChurchBulletin.ServiceDefaults/LocalTelemetryFileWriter.cs` — empty catch
- `UI/Client/App.razor` — obsolete Router API

### P1 — UI / contract noise that still fails High — **25 High**

| Cohort | Count | Action |
|--------|------:|--------|
| HTML path / id / attribute resolution | 10 | Fix real Blazor/`asp-*` mismatches **or** exclude Razor HTML inspections if false positives |
| Nullable API contract mismatches | 15 | Remove redundant `?.` / `??` / dead branches per annotations |

Notable files: `UI/Api/Pages/Error.cshtml`, `UI.Shared/Pages/WorkOrderSearch.razor`, `Login.razor`, `Settings.razor`, plus scattered Server/Shared/Worker nullable annotations.

### P2 — Dead code & structure — **~123 High**

| Cohort | Count | Action |
|--------|------:|--------|
| Unused auto-properties / positional properties | ~60 | Narrow accessibility, delete, or mark intentionally public API |
| Unused locals / parameters / private members / redundant assignments | ~47 | Delete or use; many in tests |
| Namespace ≠ file location | 16 | Almost all `ChurchBulletin.ServiceDefaults` (15) + 1 Core |

### P3 — Bulk High count reduction — **~225+ High**

Mechanical, safe, highest count impact:

| Cohort | Count | Action |
|--------|------:|--------|
| Redundant using directives | 146 | IDE / `dotnet format` / Rider cleanup |
| Redundant nullable suppressions (`!`) | 79 | Remove where annotations already non-null |
| Remaining High redundancies (qualifiers, default args, casts, etc.) | ~48 | Batch PR after P0–P2 |

### P4 — Info modernization (gate-optional) — **510 Info**

Do **not** block the fail-gate on these unless product owners want a style mandate:

- Use object/collection initializer (89)
- MemberCanBePrivate (64)
- Init-only / get-only properties (66+)
- MethodHasAsyncOverload (51)
- UseAwaitUsing (41)
- Primary constructors / collection expressions / etc.

**Gate options for P4:**

1. Leave as Info and set fail only on High severity, **or**
2. Exclude selected inspections in `qodana.yaml`, **or**
3. Commit a baseline SARIF after P0–P3 so Info does not fail until deliberately adopted.

---

## 4. Phased work plan

### Phase 0 — Gate design (no code fixes)

**Outcome:** agreed fail policy before large cleanup PRs.

1. Decide fail mode:
   - **Preferred:** `fail-threshold: 0` for **new** High problems vs committed baseline; Info allowed or excluded.
   - **Aggressive:** fail on any High remaining (requires P0–P3 complete).
2. Confirm exclusions already in `qodana.yaml` (`src/UI/Server/Generated`) and whether to add:
   - acceptance/integration test paths (optional — not recommended; prefer fix)
   - specific HTML inspections if confirmed false positive for Blazor
3. Document in `qodana.yaml` comments + this plan when `continue-on-error` will be removed.

**Exit criteria:** written gate policy approved (High-only vs all severities; baseline vs absolute zero).

### Phase 1 — P0 defect remediation (highest priority)

**Scope:** 42 High findings listed in §3 P0.  
**Suggested PR split:**

| PR | Scope | Approx. findings |
|----|-------|-----------------:|
| 1a | Core reference comparisons + related Core unused locals | ~3–5 |
| 1b | LlmGateway multiple enumeration (`TracingChatClient`) | 4 |
| 1c | AcceptanceTests HttpClient + using-init + empty catches | ~20 |
| 1d | UI/Client obsolete Router + PublisherGateway using-init | ~2 |
| 1e | ServiceDefaults empty catch + UnitTests sync/suspicious | ~3 |

**Exit criteria:** P0 inspection counts = 0 (or waived with linked ADR/comment in `qodana.yaml` exclude).

### Phase 2 — P1 UI & nullable contracts

| PR | Scope | Approx. findings |
|----|-------|-----------------:|
| 2a | Fix or exclude `Html.PathError` / `IdNotResolved` / attribute | 10 |
| 2b | Remove redundant null-forgiveness patterns (nullable contract High) | 15 |

**Exit criteria:** no High in HTML path/id or nullable-contract inspections (unless excluded intentionally).

### Phase 3 — P2 dead code & ServiceDefaults namespaces

| PR | Scope | Approx. findings |
|----|-------|-----------------:|
| 3a | Align `ChurchBulletin.ServiceDefaults` namespaces to folder/project | 15 |
| 3b | Production unused accessors / positional properties (Server, Shared, Api, Core) | ~50 |
| 3c | Test unused variables / assignments / private members | ~50 |

**Caution:** public API surface on DTOs / Blazor parameters — verify before narrowing accessibility.

**Exit criteria:** P2 High under ~20 remaining (or zero).

### Phase 4 — P3 mechanical High cleanup

| PR | Scope | Approx. findings |
|----|-------|-----------------:|
| 4a | Remove redundant usings solution-wide | 146 |
| 4b | Remove redundant `!` suppressions | 79 |
| 4c | Remaining High redundancies / naming / XML doc | ~48 |

Prefer automated cleanup (Rider Code Cleanup, `dotnet format`, or IDE bulk fix) with review focused on behavioral diffs only.

**Exit criteria:** High findings dominated by intentional leftovers only; total High ideally **&lt; 50** (stretch: **&lt; 20**).

### Phase 5 — Enable Qodana as build failure

1. Commit baseline (if using incremental gate):  
   `qodana.yaml` → `baseline: qodana.sarif.json` (or JetBrains baseline path used by the action).
2. Set `fail-threshold: 0` (new High problems) **or** absolute threshold matching remaining accepted High.
3. Remove `continue-on-error: true` from the Qodana step in `.github/workflows/build.yml`.
4. Keep SARIF upload to GitHub code scanning.
5. Optionally add job summary script (see experimental worktrees’ `Publish-QodanaJobSummary.ps1`) for visibility.

**Exit criteria:** Qodana job red on new High regressions; green on master after Phase 1–4.

### Phase 6 — Optional Info / style adoption (post-gate)

Tackle P4 in theme PRs (async overloads, `await using`, init-only, private members) only if the team wants stricter style enforcement. Otherwise keep Info out of the fail threshold.

---

## 5. Suggested issue / PR breakdown (tracking)

Create GitHub issues (or board cards) roughly as:

1. **Epic:** Qodana fail-gate readiness  
2. Phase 0 — Gate policy decision  
3. Phase 1 — P0 correctness / reliability  
4. Phase 2 — P1 UI HTML + nullable contracts  
5. Phase 3 — P2 dead code + ServiceDefaults namespaces  
6. Phase 4 — P3 bulk usings / `!` cleanup  
7. Phase 5 — Flip CI to fail (`continue-on-error` off + threshold/baseline)

Each child issue should list the inspection names from §3 and link back to this document.

---

## 6. Verification checklist (per remediation PR)

1. Re-download latest `qodana-report` artifact (or run Qodana locally if licensed/tooling available).
2. Confirm targeted inspection counts dropped; no unexpected new High.
3. Run `.\PrivateBuild.ps1` (or at least unit + affected integration tests) before merge.
4. Do not loosen onion dependency rules or add packages to silence inspections.

---

## 7. Snapshot metrics (for progress tracking)

| Metric | Baseline (run 32329753510) | Target before fail-gate |
|--------|---------------------------:|------------------------:|
| Total findings | 973 | n/a (Info optional) |
| High findings | 463 | &lt; 50 (stretch &lt; 20) |
| P0 defect inspections | 42 | 0 |
| Redundant usings | 145 | 0 |
| Redundant `!` | 79 | 0 |
| CI Qodana mode | warnings-only | **fail on High regression** |

Recompute after each phase from `result-allProblems.json` / SARIF in the `qodana-report` artifact:

- Group by `severity`, `category`, `attributes.inspectionName`, and first path segment under `src/`.

---

## 8. References

- Config: `qodana.yaml` (linter `jetbrains/qodana-cdnet:latest`, solution `src/ChurchBulletin.sln`)
- Workflow: `.github/workflows/build.yml` → job `qodana`
- Latest analyzed artifact: run `32329753510` / artifact `qodana-report`
