# Module 8 — Standards & Targets Mapping

Use this module to turn each run into the citation line the finding format
requires, to pick defensible SLO budgets, and to classify severity consistently
rather than by gut feel.

## References this skill leans on

- **Grafana k6**: Test Types, Scenarios & Executors, Thresholds, Metrics,
  `handleSummary` / results output — the tool's own guidance is the primary
  authority for *how* to load-test.
- **Google SRE (SRE Book / Workbook)**: SLIs, SLOs, and error budgets — the
  framework for *what* latency/error target to hold and why breaching it matters.
- **RFC 6585 §4 / RFC 9110**: HTTP 429 & `Retry-After` semantics — the contract
  the rate limiter (Module 4) implements; a load test validates against it.
- **This app's own config as the target of record**: the rate limit
  (100 req/60 s), request timeout (120 s), and the un-throttled single-api path
  are *the* constraints numbers must be read against — cite the config file, not
  a generic "the API".

## Choosing SLO budgets (don't invent them)

1. Run **smoke** first to establish the idle-system baseline latency.
2. Set the average-load p95 budget as a modest multiple of the smoke baseline
   (e.g. baseline p95 × 2–3), not an arbitrary round number.
3. Set the error budget from the intended reliability target (e.g. 99.9%
   availability → `http_req_failed rate < 0.001` for the SLO tier), excluding
   designed 429s (Module 4).
4. Record the derivation in the report so the budget is auditable, not asserted.

## Severity guidance specific to load-test findings

- **Critical**: correctness or error-rate breach under **smoke or nominal load**
  (5xx/timeouts, `checks` < target at low RPS), or the app failing to recover
  after a spike. The system is not meeting its SLO under expected conditions.
- **High**: **p95/p99 latency budget breached under expected (average) load**,
  or throughput plateauing well below the expected peak for a non-rate-limited
  reason (DB, contention).
- **Medium**: degradation that appears **only under stress/spike** beyond
  expected peaks — the system bends but doesn't break; a capacity-planning
  concern, not a live SLO breach.
- **Low**: marginal soak drift, cold-start-only tail latency, or defense-in-depth
  observations (e.g. missing `Retry-After` nicety) with no SLO impact.

Rate-limiter 429s returned *as designed* are **not** a finding — they are the
control working (Module 4). Only report them as a finding if the limiter behaves
incorrectly (wrong count, missing headers, limiting the wrong routes).

## Finding-citation convention

Every finding's **Threshold verdict** and **Evidence** lines must name the
concrete k6 run and metric — e.g. `p(95)=812ms vs SLO p95<500ms (fail),
metrics/load-summary.json` — plus the governing reference where a general
principle is invoked (e.g. "open-model executor per k6 Scenarios guidance", or
"error budget per Google SRE 99.9% target"). Never write "slow under load"
without the metric, the profile, and the saved run.

## Detection commands

```
# Re-derive a cited number from its saved run (reproducibility check):
PROFILE=load k6 run --insecure-skip-tls-verify \
  --summary-export=k6-load-report/metrics/load-summary.json k6/baseline.js

# Confirm the target's own limits that numbers must be read against:
grep -niE "PermitLimit|WindowSeconds|TimeoutSeconds" src/UI/Server/*Options.cs src/UI/Server/appsettings*.json
```

Cross-reference Module 2 (thresholds encode these SLOs) and Module 4 (429
categorization feeds the severity call).
