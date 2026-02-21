# Plan: Parallel Integration Builds in Feature Branch Pipeline

## Current State

`.github/workflows/integration-build-feature-branches.yml` has a **single sequential chain**:
```
build-linux → docker-build → deploy-to-tdd
```

The `build-linux` job runs `Invoke-CIBuild` which auto-detects the database engine (typically Docker SQL Server on GitHub Actions Linux runners). All compilation, unit tests, integration tests, packaging, and publishing happen in this one job.

## Proposed Changes

Add **two new parallel jobs** alongside the existing `build-linux` job:

```
┌─────────────────────┐
│  build-linux        │  (existing - Docker SQL Server)
│  Compile + Test     │
│  + Package + Publish│
└─────────┬───────────┘
          │
┌─────────────────────┐
│  build-sqlite       │  (NEW - SQLite pathway)
│  Compile + Test     │  runs in parallel
└─────────┬───────────┘
          │         ──► all 3 must pass ──► docker-build → deploy-to-tdd
┌─────────────────────┐
│  code-analysis      │  (NEW - dotnet format + analyzers)
│  Lint + Analyze     │  runs in parallel
└─────────┬───────────┘
```

### Job 1: `build-linux` (existing, unchanged)
- Runs `Invoke-CIBuild` with Docker SQL Server (current behavior)
- Produces NuGet packages, publishes artifacts
- Remains the "primary" build that feeds downstream jobs

### Job 2: `build-sqlite` (new)
- **Runner:** `ubuntu-latest`
- **Purpose:** Validates the SQLite code pathway used in local development and constrained environments
- **Environment:** Sets `DATABASE_ENGINE=SQLite` to force SQLite mode
- **Build command:** `Invoke-PrivateBuild` (compile + unit tests + integration tests, no packaging needed)
- **Artifacts:** Test results only (no NuGet packages — the primary build handles packaging)
- **Key difference from build-linux:** Exercises `TestDatabaseConfiguration`, SQLite EF Core provider, `EnsureCreated()` pathway, and SQLite-specific `DatabaseEmptier` logic

### Job 3: `code-analysis` (new)
- **Runner:** `ubuntu-latest`
- **Purpose:** Static analysis and code style enforcement
- **Steps:**
  1. `dotnet restore` the solution
  2. `dotnet format --verify-no-changes --verbosity diagnostic` — checks code style and formatting against `.editorconfig` rules without modifying files. Fails the job if any formatting violations are found.
  3. `dotnet build` with `/p:TreatWarningsAsErrors=true /p:EnforceCodeStyleInBuild=true /p:AnalysisLevel=latest` — runs Roslyn analyzers with code style enforcement enabled at build time
- **Artifacts:** Build warnings/errors log
- **No test execution** — this job is purely static analysis

### Downstream Impact

- `docker-build-image-for-churchbulletin-ui` changes from `needs: [build-linux]` to `needs: [build-linux, build-sqlite, code-analysis]`
- This ensures the Docker image is only built when all three quality gates pass
- `deploy-to-tdd` remains unchanged (still depends on `docker-build`)

## Files to Modify

1. **`.github/workflows/integration-build-feature-branches.yml`**
   - Add `build-sqlite` job (~40 lines) after the `build-linux` job
   - Add `code-analysis` job (~35 lines) after `build-sqlite`
   - Update `docker-build-image-for-churchbulletin-ui.needs` to include all three jobs
   - Unique artifact names for each job's test results (`test-results-sqlite`, `code-analysis-results`)

2. **No changes to build scripts** — `Invoke-PrivateBuild` already supports `-UseSqlite` switch, and `dotnet format` / analyzer flags are CLI arguments

## Risk Assessment

- **Low risk:** New jobs are additive — they don't modify the existing `build-linux` job or its artifacts
- **SQLite job** uses the same `Invoke-PrivateBuild` function already validated locally
- **Code analysis job** may initially surface formatting violations that need fixing — the plan is to add the job and fix any violations it finds
- **Build time impact:** No increase to critical path — new jobs run in parallel. Total pipeline time stays the same as the slowest job (likely `build-linux`)
