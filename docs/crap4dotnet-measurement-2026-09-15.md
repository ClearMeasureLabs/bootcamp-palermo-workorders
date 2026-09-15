# crap4dotnet — Plan and One-Time Baseline Measurement

**Date:** 2026-09-15
**Commit measured:** `1747660` (master head at time of measurement)
**Branch:** `claude/crap4dotnet-measurement-l00sf3`

## 1. Reference: Clear-Measure-Intelligence-Scorecard

The requested reference repository (`ClearMeasure/Clear-Measure-Intelligence-Scorecard`) lives in a
different GitHub organization from this session's authorized scope (`ClearMeasureLabs`), so its
implementation could not be read directly. The plan below is built on the crap4dotnet
implementation already present in this repo, which follows the same tool chain the Scorecard
approach is based on: **coverlet Cobertura → `dotnet-crap analyze` → threshold gate → CI job
summary.** Re-validate against the Scorecard repo once it is added to a session's sources.

## 2. What CRAP measures

```
CRAP(m) = CC(m)² × (1 − cov(m)/100)³ + CC(m)
```

| CRAP | Risk | Action |
|------|------|--------|
| ≤ 5 | Low | None |
| 5–15 | Moderate | Monitor |
| 15–30 | High | Add tests or simplify |
| > 30 | CRAPpy (classic threshold) | Refactor and/or test |

At 100% coverage CRAP equals cyclomatic complexity, so a method whose CC exceeds the gate can
only be fixed by refactoring.

## 3. Plan — existing implementation in this repo

| Concern | Where |
|---------|-------|
| Tool | `crap4dotnet` 0.1.1 (`dotnet-crap`), `dotnet-script` 2.0.0 |
| Coverage collection | `coverlet.collector` via `build.ps1` (`UnitTests`, `IntegrationTest`, `AcceptanceTests`) with `coverlet.runsettings` (Include `[ClearMeasure.Bootcamp.*]*,[Worker]*,[ChurchBulletin.ServiceDefaults]*`) |
| Async state-machine flattening | `scripts/crap/flatten-cobertura.csx` |
| Analysis + rollup | `scripts/crap/run-crap-audit.ps1` → `dotnet-crap analyze` → `rollup-file-scores.csx` (line-range coverage overlay, production scoping, file rollups) |
| Gate | `assert-crap-gate.ps1`; threshold in `scripts/crap/crap-gate-threshold.json` (`productionThreshold: 6`) |
| Local gate | `PrivateBuild.ps1` runs the audit with `-SkipTests -FailOnViolations -Quiet` |
| CI gate | `.github/workflows/build.yml`, job **Integration Build (SQL container)**: step *Enforce CRAP (production)*, then *Publish CRAP summary to job summary* (`if: always()`) |
| Outputs | `crap-metrics/` (gitignored locally); published in CI as artifact `crap-metrics-linux` and the job summary |
| Scope | Production only. Excluded: `UnitTests`, `IntegrationTests`, `AcceptanceTests`, `**/Generated/**`, `*.g.cs`, `*.Designer.cs` |

### Run it

```powershell
# full pipeline (compile, unit, integration, acceptance, then audit)
pwsh scripts/crap/run-crap-audit.ps1

# reuse coverage already under build/test and enforce the gate
pwsh scripts/crap/run-crap-audit.ps1 -SkipTests -FailOnViolations
```

Prerequisites on Linux: .NET 10 SDK **and** the .NET 8 runtime (`dotnet-crap` 0.1.1 targets
`net8.0`), plus `pwsh`.

### Follow-ups

Done in this change:

- .NET 8 runtime prerequisite documented in `docs/crap-score-audit.md` (the tool fails to launch on an SDK-10-only machine).
- `crap-metrics/` published as the `crap-metrics-linux` build artifact in addition to the job summary, so baselines can be diffed with `dotnet-crap diff`.

Remaining:

1. Add a trend line: store `crap-summary.md` per run and compare average CRAP and CRAPpy count.
2. Compare with the Scorecard repo's thresholds and scoping once that repo is reachable.

## 4. Baseline measurement

**How:** SDK 10.0.401, `build.ps1` `Init → Compile → UnitTests → Setup-DatabaseForBuild → IntegrationTest`
on Linux with the SQLite fallback, then `run-crap-audit.ps1 -SkipTests`.
Acceptance tests (Playwright) were **not** run, so UI coverage is understated relative to CI.

| Tests | Result |
|-------|--------|
| UnitTests | 843 passed |
| IntegrationTests | 278 passed, 15 skipped (SQLite) |

### Production scope (gate threshold 6)

| Metric | Value |
|--------|-------|
| Production files analyzed | 196 |
| Production methods analyzed | 713 |
| Methods over threshold 6 | **0** |
| Methods over classic threshold 30 | **0** |
| Max CRAP | 6.0 |
| Max cyclomatic complexity | 6 |
| Average CRAP | 1.9 |
| Median CRAP | 2 |
| Mean file coverage | 89.5% |

**Gate result: PASS** — no crappy production code at either threshold.

### Production files at the ceiling (MaxCrap = 6.0)

| File | AvgCov% | Worst method |
|------|---------|--------------|
| `src/Database/Console/QuietLog.cs` | 17 | `QuietLog.LogInformation` |
| `src/Database/Console/ScriptBaselineMarker.cs` | 12 | `ScriptBaselineMarker.*` |
| `src/Database/Console/AbstractDatabaseCommand.cs` | 50 | `AbstractDatabaseCommand.*` |
| `src/UI/Server/WorkOrderReformatAgent.cs` | 65 | `ReformatWorkOrderRunner.*` |
| `src/UI/Server/AutoReformatAgentService.cs` | 79 | `AutoReformatAgentService.*` |
| `src/DataAccess/Handlers/CreateDatedWorkOrdersHandler.cs` | 100 | CC 6 at full coverage |
| `src/McpServer/Tools/WorkOrderTools.cs` | 94 | `WorkOrderTools.List*` |

These are one added branch away from failing the gate. The first three (Database console) and
the two Server agents are coverage problems; the last two are complexity problems.

### Production files with < 50% coverage (all CC ≤ 3, so CRAP stays low)

`Database/Program.cs`, `McpServer/Program.cs`, `Worker/Program.cs`, `ChurchBulletin.AppHost/AppHost.cs`,
`UI/Api/Program.cs`, `UI/Client/Program.cs`, `UI/Client/UIClientServiceRegistry.cs`,
`UI/Client/WasmHostEnvironment.cs` (0%); `Database/Console/ScriptBaselineMarker.cs` (12%);
`Database/Console/QuietLog.cs` (17%); `UI/Server/Is64BitProcessHealthCheck.cs` (38%);
`DataAccess/CanConnectToDatabaseHealthCheck.cs` (47%).

### Out of scope (informational only)

| Category | Files | Methods | Over threshold 6 | Worst |
|----------|-------|---------|------------------|-------|
| Tests + generated | 286 | 2,075 | 147 | `UI/Server/Generated/Protos/Workorders.cs` — CRAP 306 (gRPC generated `InternalMergeFrom`, CC 17, 0% cov) |

Worst test files: `IntegrationTests/LlmGateway/ApplicationChatHandlerTests.cs` (72),
`AcceptanceTests/AIAgents/SaturdayMowSchedulingAgentTests.cs` (56),
`UnitTests/UI.Client/Authentication/LocalStorageUserSessionStoreTests.cs` (56). Test-method
CRAP is expected to be high (tests are not covered by tests) and is intentionally excluded from
the gate.

### Caveats

- Raw `dotnet-crap` output reports 0% coverage for many production methods it cannot match by
  name; the rollup's line-range overlay from the flattened Cobertura corrects this. Use
  `crap-summary.md` / `crap-production-violations.json`, not the tool's console table, as the
  gate source.
- No acceptance coverage in this run. CI numbers will be equal or better.
