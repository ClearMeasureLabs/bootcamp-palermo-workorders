# Module 2 — Thresholds & SLOs

Standards: Grafana k6 Thresholds reference, Google SRE Workbook (SLIs/SLOs,
latency & error budgets), k6 exit-code semantics (thresholds → non-zero exit).

## Anti-patterns to hunt

| Pattern | Why it hurts | Evidence to look for |
|---|---|---|
| No `thresholds` block at all | The run has no pass/fail verdict — it can't gate CI, and "looks fine" is not a result | `options` with `scenarios` but no `thresholds` key |
| Threshold only on the average (`avg`), not a percentile | Averages hide tail latency; a p99 of 5s can sit behind a 200ms average | `http_req_duration: ['avg<...']` with no `p(95)`/`p(99)` |
| `http_req_failed` counting designed 429s as failures | Rate-limiter back-pressure (429) inflates the error rate and fails a healthy run | a failure threshold with no carve-out for expected 429 (see Module 4) |
| Thresholds present but not gating (results ignored, exit code unchecked) | CI goes green while SLOs are breached | `k6 run` whose exit code is discarded; no `abortOnFail` on the breakpoint run |
| One global SLO reused for every profile | Smoke and spike have different acceptable latencies; a single budget is either too loose for smoke or too strict for spike | identical `thresholds` regardless of `PROFILE` |

## What "good" looks like

- Thresholds expressed as the SLO contract, gating the run's exit code:
  ```js
  export const options = {
    thresholds: {
      http_req_duration: ['p(95)<500', 'p(99)<1000'],   // latency budget
      http_req_failed:   ['rate<0.01'],                  // <1% real errors
      checks:            ['rate>0.99'],                  // correctness
      // abortOnFail for breakpoint runs so k6 stops at the breaking point:
      // http_req_duration: [{ threshold: 'p(95)<500', abortOnFail: true }],
    },
  };
  ```
- **Per-profile budgets**, tightest on smoke (system idle, no excuse for slow),
  looser under stress/spike (degradation is expected, collapse is not):

  | Profile | p95 budget | error budget | note |
  |---|---|---|---|
  | Smoke | p95 < 300ms | rate < 0.001 | correctness gate; a fast 500 still fails `checks` |
  | Average load | p95 < 500ms, p99 < 1s | rate < 0.01 | the real SLO |
  | Stress | p95 < 2s | rate < 0.05 | degrade, don't collapse |
  | Spike | recovers within N s after burst | rate < 0.10 during burst | recovery matters more than peak |

  (Tune the numbers to the target's measured baseline — see Module 8 for how to
  set them; do not copy these verbatim as if measured.)
- A dedicated `Rate` metric for **real** failures that excludes expected 429s
  (Module 4), thresholded separately from `http_req_failed`.
- k6's non-zero exit on a failed threshold is the CI gate (Module 7) — never
  swallow it.

## Detection commands

```
# A failed threshold makes k6 exit non-zero — this is the gate:
k6 run k6/baseline.js ; echo "exit=$?"     # expect non-zero when a threshold breaches

# Emit the machine-readable summary the report + CI consume:
k6 run --summary-export=k6-load-report/metrics/smoke-summary.json k6/baseline.js
```

Cross-reference Module 6 for turning the summary JSON into the report tables,
Module 4 for the 429 carve-out, and Module 8 for choosing the actual budgets.
