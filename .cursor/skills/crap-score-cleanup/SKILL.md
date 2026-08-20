---
name: crap-score-cleanup
description: >
  Computes Bob Martin CRAP (Change Risk Anti-Patterns) scores for every source file
  in the .NET solution by combining cyclomatic complexity with Cobertura test coverage.
  Produces method-level and file-level ranked reports for cleanup subagents. Use when
  the user asks for CRAP scores, risky untested code, change-risk hotspots, or wants
  a codebase-wide cleanup prioritized by complexity × lack of coverage.
---

# CRAP Score Cleanup

Quantify change risk across the entire codebase, rank files and methods, and drive
focused cleanup (refactor complex methods, add tests, or both).

## Background

CRAP was introduced by Alberto Savoia and Bob Evans (2007), popularized in Uncle Bob's
Clean Code writing. It combines **cyclomatic complexity** with **automated test coverage**:

```
CRAP(m) = CC(m)² × (1 - cov(m)/100)³ + CC(m)
```

| CRAP | Risk | Typical action |
|------|------|----------------|
| ≤ 5 | Low | None |
| 5–15 | Moderate | Monitor |
| 15–30 | High | Add tests or simplify |
| > 30 | **CRAPpy** | Refactor and/or test urgently |

**Threshold 30** is the standard "CRAPpy" cutoff. At 100% coverage, CRAP equals complexity
(the minimum). At 0% coverage, CRAP = CC² + CC.

Complexity alone can exceed the threshold: a method with CC ≥ 14 cannot reach CRAP < 14
through testing alone — it must be refactored.

## Quick start

From the repo root:

```powershell
pwsh .cursor/skills/crap-score-cleanup/scripts/run-crap-audit.ps1
```

**CI / private-build gate (threshold 14, production only):** after unit + integration coverage exists under `build/test`, fail the build when any in-scope production method has CRAP > 14 (test projects and generated code are excluded):

```powershell
pwsh .cursor/skills/crap-score-cleanup/scripts/run-crap-audit.ps1 -Threshold 14 -SkipTests -FailOnViolations
```

`PrivateBuild.ps1` and the Linux integration-build job in `.github/workflows/build.yml` run this automatically. Coverlet records async methods on compiler-generated state-machine types; the audit flattens those hits onto the original methods (`flatten-cobertura.csx`) before `dotnet-crap`, then overlays line coverage in `rollup-file-scores.csx`. `dotnet-crap` may still exit non-zero because of CRAPpy *test* methods; the gate uses `crap-metrics/crap-production-violations.json` (from `assert-crap-gate.ps1`) and only fails on production violations.

Outputs land in `crap-metrics/`:

| File | Contents |
|------|----------|
| `crap-report.json` | Every method: complexity, coverage, CRAP, file path |
| `crap-by-file.json` | Every `.cs`/`.razor` file rolled up |
| `crap-by-file.csv` | Same data, spreadsheet-friendly |
| `crap-summary.md` | Human-readable top offenders + cleanup queue |

**Coverage scope:** crap4dotnet walks every `.cs` file referenced by the solution (~314 files /
~1,428 methods in this repo). Files with no methods (e.g. assembly attributes only) are omitted.
Coverage comes from UnitTests + IntegrationTests + AcceptanceTests Cobertura merge.

## Workflow (full audit)

### Step 1 — Install tools (once per environment)

```powershell
dotnet tool install -g crap4dotnet --version 0.1.1
dotnet tool install -g dotnet-script --version 2.0.0
```

Optional HTML report:

```powershell
dotnet tool install -g dotnet-reportgenerator-globaltool
```

### Step 2 — Collect coverage (Cobertura)

This repo uses `coverlet.collector` on UnitTests, IntegrationTests, and AcceptanceTests.
The script delegates to **`build.ps1`** — the same pipeline as `AcceptanceTests.ps1`
(Init → Compile → UnitTests → Setup-DatabaseForBuild → IntegrationTest → AcceptanceTests).
Coverage Cobertura files land under `build/test/{UnitTests,IntegrationTests,AcceptanceTests}/`.

Do not invoke bare `dotnet test` on the `.sln` or on AcceptanceTests in isolation; that skips
database setup and server lifecycle and produces false failures.

**All tests must pass.** The audit script fails if `dotnet test` exits non-zero. Acceptance
failures usually indicate stale server/worker processes or a dirty worktree — not bad CRAP
data to paper over.

Recommended preflight on a clean checkout:

```powershell
git worktree add ../clean-audit origin/master
cd ../clean-audit
.\AcceptanceTests.ps1   # must report Passed with 0 Failed
pwsh .cursor/skills/crap-score-cleanup/scripts/run-crap-audit.ps1
```

Do not use `dotnet-crap --run-tests` directly — it aborts on any test failure. The script
collects coverage via `dotnet test` and passes Cobertura paths to `dotnet-crap analyze`.

Fallback when in-proc collector fails:

```powershell
dotnet tool install -g dotnet-coverage
dotnet-coverage collect -f cobertura -o crap-metrics/coverage.cobertura.xml `
  "dotnet test src/UnitTests/UnitTests.csproj --configuration Release"
```

### Step 3 — Analyze CRAP scores

```powershell
$coverage = Get-ChildItem crap-metrics/TestResults -Recurse -Filter coverage.cobertura.xml
$args = @("analyze", "src/ChurchBulletin.sln", "--threshold", "30", "--output", "crap-metrics/crap-report.json")
foreach ($f in $coverage) { $args += @("--coverage", $f.FullName) }
dotnet-crap @args
```

### Step 4 — Roll up to file scores

```powershell
dotnet script .cursor/skills/crap-score-cleanup/scripts/rollup-file-scores.csx `
  crap-metrics/crap-report.json crap-metrics
```

### Step 5 — Review and prioritize

Read `crap-metrics/crap-summary.md`. Sort `crap-by-file.csv` by `MaxCrap` descending.

**Exclude from cleanup targets** (still scored, but deprioritize):

- `**/Generated/**`, `*.g.cs`, `*.Designer.cs`
- Test projects when the goal is production-code risk (`AcceptanceTests`, `UnitTests`, `IntegrationTests`)
- `Program.cs` top-level statements unless explicitly in scope

## File-level score semantics

Per file, the rollup computes:

| Metric | Meaning |
|--------|---------|
| `MaxCrap` | Worst method in the file (primary sort key) |
| `TotalCrapLoad` | Sum of `(CRAP - threshold)` for CRAPpy methods |
| `CrappyMethodCount` | Methods with CRAP > 30 |
| `MethodCount` | Methods analyzed in file |
| `AvgCrap` | Mean CRAP across methods |
| `AvgCoverage` | Mean line coverage % across methods |

A file with `MaxCrap = 420` and one god-method is a refactor candidate. A file with
`MaxCrap = 35` but ten methods at 32 is a test-coverage candidate.

## Cleanup subagent protocol

When dispatching a cleanup subagent, pass a **bounded scope**:

```
Target: crap-metrics/crap-by-file.csv rows 1–5 (production code only)
Goal:   reduce MaxCrap below 30 for each file
Rules:
  - Prefer extract-method refactoring when CC ≥ 15
  - Prefer characterization tests when CC < 15 and coverage is low
  - Follow onion architecture; no new NuGet packages
  - Run .\privatebuild.ps1 before commit
Acceptance: re-run run-crap-audit.ps1; targeted files no longer CRAPpy
```

### Subagent task template

1. Read `crap-metrics/crap-report.json` methods where `filePath` matches the target file.
2. For each CRAPpy method (`isCrappy: true`), choose:
   - **Refactor** if `complexity >= 15` or nested logic / long method
   - **Test** if `complexity < 15` and `coverage < 70%`
   - **Both** if `complexity >= 10` and `coverage == 0`
3. Implement smallest change that drops CRAP below 30.
4. Re-run audit on changed files only:

```powershell
pwsh .cursor/skills/crap-score-cleanup/scripts/run-crap-audit.ps1 -SkipTests
dotnet-crap diff crap-metrics/crap-report-before.json crap-metrics/crap-report.json
```

### Coverage needed formula

To bring method with complexity `CC` below CRAP 14 (production gate):

```
cov_needed = 1 - ((14 - CC) / CC²)^(1/3)
```

Only valid when `CC < 14`.

## Validation checklist

- [ ] Cobertura XML exists and is non-empty
- [ ] No coverage percentages were guessed
- [ ] 100%-covered method has CRAP == complexity (spot-check one)
- [ ] File rollup count matches distinct `filePath` values in method report
- [ ] Generated/test-only files flagged in summary exclusions

## Common pitfalls

| Pitfall | Fix |
|---------|-----|
| `dotnet-crap --run-tests` fails on acceptance failures | Collect coverage separately; pass `--coverage` paths |
| All production methods show 0% coverage | Run IntegrationTests + UnitTests, not UnitTests alone |
| Stale coverage | Always regenerate before comparing diffs |
| Method name mismatch in Cobertura | crap4dotnet matches by line range; trust its output |
| CRAP on test helpers | Filter to `src/Core`, `src/DataAccess`, `src/UI`, `src/LlmGateway`, `src/McpServer`, `src/Worker` for product cleanup |

## Related skills

- `roslynator-analysis` — static analyzer findings (orthogonal to CRAP)
- `codebase-cartography-audit` — LOC + McCabe CC without coverage
- Official dotnet targeted CRAP skill — single method/class deep-dive (not whole-repo)
