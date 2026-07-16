# Module 6 — Metrics, Results & Reporting

Standards: Grafana k6 built-in metrics, `handleSummary()` / `--summary-export`,
custom metrics (`Trend`/`Counter`/`Rate`/`Gauge`).

Every claim in the report must trace to a saved k6 run. This module covers
capturing the raw numbers and turning them into the report tables.

## k6 built-in metrics worth reporting

| Metric | Meaning | Report as |
|---|---|---|
| `http_req_duration` | End-to-end request time | p50 / p95 / p99 latency |
| `http_reqs` | Total requests + rate | throughput (RPS) |
| `http_req_failed` | Fraction of failed requests | error rate (see Module 4 for the 429 carve-out) |
| `iterations` / `iteration_duration` | VU loop completions | per-scenario work rate |
| `vus` / `vus_max` | Concurrent/allocated VUs | concurrency reached |
| `data_sent` / `data_received` | Bytes over the wire | bandwidth (useful for payload-heavy routes) |
| `checks` | Fraction of passing checks | correctness % (a fast 500 shows up here) |

## Anti-patterns to hunt

| Pattern | Why it hurts | Evidence to look for |
|---|---|---|
| Reporting only the k6 terminal blurb, no saved JSON | Not reproducible; can't diff runs; fidelity rule violated | no `metrics/*.json` for a cited number |
| Quoting `avg` latency | Hides the tail; SLOs are percentile-based | "average response 120ms" with no p95/p99 |
| No k6 version recorded | Metric semantics shift across k6 versions | report without `k6 version` output |
| Mixing profiles in one summary file | Can't attribute a number to a profile | one JSON overwritten across smoke/load/stress |
| Ignoring `checks` rate | Latency looks great because errors returned instantly | thresholds pass but `checks` < 100% unremarked |

## What "good" looks like

- Persist a machine-readable summary **per profile** into `metrics/`:
  ```
  PROFILE=smoke k6 run --summary-export=k6-load-report/metrics/smoke-summary.json \
    --insecure-skip-tls-verify k6/baseline.js
  ```
- Or emit a custom, report-shaped JSON via `handleSummary` (also keep the text
  summary for humans):
  ```js
  export function handleSummary(data) {
    return {
      'k6-load-report/metrics/summary.json': JSON.stringify(data, null, 2),
      'stdout': textSummary(data, { indent: ' ', enableColors: false }),
    };
  }
  // import { textSummary } from 'https://jslib.k6.io/k6-summary/0.0.2/index.js';
  ```
  (If the environment blocks the jslib import, use `--summary-export` instead.)
- Record `k6 version` and the exact command alongside each result.
- Build the report's per-profile table straight from the summary JSON fields
  (`metrics.http_req_duration.values["p(95)"]`, `.http_reqs.values.rate`,
  `.http_req_failed.values.rate`, `.checks.values.rate`). If post-processing the
  JSON into rollup tables needs code, write a single-file **C# script (`.csx`
  via `dotnet-script`)** — matching the repo's .NET-only helper norm — not
  Python/PowerShell.
- Keep the raw JSON in `metrics/` so every number in `README.md` is one grep away.

## Report tables to produce (per run)

- **Per-profile summary**: profile | executor/stages | p50 | p95 | p99 | RPS |
  error rate | checks% | threshold verdict.
- **Threshold verdict list**: each threshold → pass/fail (from k6's
  `metrics.<name>.thresholds`).
- **429 vs real-error split** when the limiter was exercised (Module 4).

## Detection commands

```
k6 version

# Pull the headline numbers back out of a saved summary:
cat k6-load-report/metrics/smoke-summary.json | \
  grep -oE '"p\(95\)":[0-9.]+|"rate":[0-9.]+' | head
```

Cross-reference Module 2 (thresholds → verdict), Module 4 (429 categorization),
and Module 7 (surfacing these into a GitHub Actions summary).
