# Module 5 — Environment & Data Setup

Standards: repeatable-environment / test-data-isolation practice; .NET
configuration precedence (env vars via `__`); DbUp migration seeding.

A load number is only meaningful if you know exactly what it ran against. This
module pins the target so runs are reproducible and never touch real data.

## Anti-patterns to hunt

| Pattern | Why it hurts | Evidence to look for |
|---|---|---|
| Load-testing against a real/shared DB | `bulk-import` and single-api commands write rows; you pollute or corrupt shared data | connection string pointing at a shared SQL Server |
| Measuring the first (cold) iterations | .NET JIT + EF model build + first-connection cost skews p99 badly | no warm-up; p99 wildly above p95 on the first run only |
| Not stating the environment/DB/limiter state | Numbers can't be compared run-to-run or trusted | report with no "environment" section |
| Leaving the auto-reformat agent on | The background AI agent mutates Draft work requests and makes LLM calls mid-test, adding noise and cost | `DISABLE_AUTO_REFORMAT_AGENT` unset |
| Running smoke against a not-yet-ready app | Startup/migration still in progress → spurious 500s | no `/_healthcheck` gate before load |

## What "good" looks like

- **Throwaway SQLite DB** — set the connection string to a `Data Source=` value;
  the app auto-switches to SQLite + NServiceBus LearningTransport (no SQL Server
  needed) and connection-pool clearing follows suit:
  ```
  ConnectionStrings__SqlConnectionString="Data Source=loadtest.db"
  ```
  Delete `loadtest.db` between runs for a clean slate.
- **Background AI off** — `DISABLE_AUTO_REFORMAT_AGENT=true` (already the default
  in `appsettings.json`, but set it explicitly so a config override can't
  re-enable it during a run).
- **Decide the limiter state deliberately** (Module 4): default `dotnet run` is
  Development → limiter OFF (no ceiling). To exercise it, run in a non-Development
  environment and/or set `ApiRateLimiting__Enabled=true`.
- **Seed data** so payloads are valid — run the repo `Build` (applies DbUp
  migrations incl. the employee seed) against the throwaway DB, or point at a DB
  already seeded by `ZDataLoader`. Valid can-create username: `jpalermo`.
- **Warm-up** — either a short warm-up scenario before the measured one, or
  discard the first N iterations in analysis; note which in the report.
- **Health-gate the start** — poll `/_healthcheck` until 200 before launching k6.

## Reference launch (local, throwaway SQLite)

```
# 1. Seed a throwaway DB + build (applies DbUp migrations):
ConnectionStrings__SqlConnectionString="Data Source=loadtest.db" . .\build.ps1 ; Build

# 2. Run the app against the throwaway DB, agent off:
$env:ConnectionStrings__SqlConnectionString="Data Source=loadtest.db"
$env:DISABLE_AUTO_REFORMAT_AGENT="true"
dotnet run --project src/UI/Server            # → https://localhost:7174

# 3. Wait for health, then load-test (see Modules 3/6):
curl -sk https://localhost:7174/_healthcheck  # expect Healthy
```

## Config keys a load test may set (env-var form uses `__`)

| Key | Effect |
|---|---|
| `ConnectionStrings__SqlConnectionString` | `Data Source=…` → throwaway SQLite + LearningTransport |
| `DISABLE_AUTO_REFORMAT_AGENT` | `true` stops the background AI reformat agent |
| `ASPNETCORE_ENVIRONMENT` | non-`Development` enables the base-config limiter |
| `ApiRateLimiting__Enabled` / `__PermitLimit` / `__WindowSeconds` | toggle/tune the limiter (Module 4) |
| `ApiKeyAuthentication__Enabled` / `__ValidationKey` | enable + supply the `X-Api-Key` (Module 3) |
| `ApiRequestTimeouts__TimeoutSeconds` | per-request timeout (default 120) on `/api/*` |

## Detection commands

```
# Confirm SQLite mode (LearningTransport) was selected, not SQL Server:
grep -niE "Data Source=|LearningTransport|LocalDb" src/UI/Server/Program.cs src/UI/Server/DatabaseConfiguration.cs

# Confirm the app is healthy and seeded before load:
curl -sk https://localhost:7174/_healthcheck
curl -sk https://localhost:7174/api/blazor-wasm-single-api -H "Content-Type: application/json" \
  -d '{"TypeName":"ClearMeasure.Bootcamp.Core.Queries.EmployeeGetAllQuery, Core","Body":"{}"}'
```

Cross-reference Module 3 (payloads that need the seed usernames) and Module 4
(limiter/environment interplay).
