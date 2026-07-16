---
name: k6-load-testing
description: >-
  Full load and performance test of an application's HTTP surface using Grafana
  k6. Produces a self-contained report directory covering: an inventory of every
  load-testable endpoint (open diagnostics GETs, auth-gated reads, and the domain
  write paths) with the auth header, rate-limit ceiling, and payload envelope each
  requires; generated k6 scripts with correctness checks and threshold-encoded
  SLOs, run across staged profiles (smoke, average load, stress, spike, and
  optionally breakpoint/soak) using the right open- or closed-model executor;
  results captured as raw k6 summary JSON and rolled up into per-profile latency
  (p50/p95/p99), throughput (RPS), error-rate and checks tables; a severity-ranked
  set of findings each citing the exact k6 run, metric, and governing standard
  (Grafana k6 guidance, Google SRE error budgets, HTTP 429 semantics); explicit
  separation of designed rate-limiter back-pressure (429) from real failures; and,
  when run inside a GitHub Actions pipeline, a job-summary table, an uploaded
  report artifact, and a threshold-gated non-zero exit that fails the job on an
  SLO breach. Use when performance-testing an API before a release or capacity
  review, validating a rate limiter or autoscaling behaviour, establishing a
  latency/throughput baseline, or wiring load testing into CI.
---

# k6 Load Testing

A focused sibling of `codebase-cartography-audit` and `cryptography-audit`:
instead of mapping architecture or auditing crypto, this skill exercises a
running application under load with Grafana k6, measures latency and throughput
against threshold-encoded SLOs, and produces a sequenced performance backlog.

Deliverable: a new directory (default `./k6-load-report/`) containing
`README.md` (the report) and `metrics/` (raw k6 summary JSON per profile, plus
the `k6 version` used). A working `k6/` directory holds the generated scripts.
Every claim cites a saved k6 run and a concrete metric — no "seems fast" without
the p95 and the run it came from.

## When to use this

- Performance-testing an application's API before a release, a capacity review,
  or a customer load/SLA commitment.
- Validating a rate limiter, request-timeout, or autoscaling policy behaves as
  designed under sustained and bursty traffic.
- Establishing a latency/throughput baseline for an endpoint so future
  regressions are measurable (especially AI-generated code, which rarely has any
  performance characterization).
- Wiring load testing into CI so an SLO breach fails the build rather than being
  discovered in production.

## The inspection catalogue

| # | Module | File | Focus |
|---|--------|------|-------|
| 1 | Test types & load profiles | `references/01-test-types-and-profiles.md` | Grafana k6 test types; executors; open vs closed |
| 2 | Thresholds & SLOs | `references/02-thresholds-and-slos.md` | k6 thresholds; SRE error/latency budgets; CI gate |
| 3 | Scripting this app | `references/03-scripting-this-app.md` | endpoint inventory; X-Api-Key; WebServiceMessage; multipart |
| 4 | Rate limiting & back-pressure | `references/04-rate-limiting-and-backpressure.md` | 100/60s window; 429 vs real failure |
| 5 | Environment & data setup | `references/05-environment-and-data-setup.md` | throwaway SQLite; seed data; warm-up |
| 6 | Metrics, results & reporting | `references/06-metrics-results-reporting.md` | built-in/custom metrics; summary JSON |
| 7 | CI integration (GitHub Actions) | `references/07-ci-github-actions.md` | job summary; artifact; threshold-gated exit |
| 8 | Standards & targets mapping | `references/08-standards-and-targets-mapping.md` | SLO budgets; k6/SRE citations; severity |

## How to run the load test (ordered pipeline)

1. **Scope & inventory.** Confirm the target (default the local app at
   `https://localhost:7174`; the deployed environment is an explicit opt-in
   only). Enumerate the load-testable endpoints and their auth/rate-limit/payload
   shape (Module 3), and choose the profiles and SLOs to run (Modules 1–2, 8).
   Install k6 as a standalone binary — **never mutate the target repo** to get
   it. Create the **self-contained** report directory skeleton
   (`k6-load-report/` with `metrics/`) plus a working `k6/` scripts directory.

2. **Environment prep.** Bring the target up reproducibly (Module 5): local app
   against a **throwaway SQLite DB** (`ConnectionStrings__SqlConnectionString="Data Source=loadtest.db"`),
   background AI agent off (`DISABLE_AUTO_REFORMAT_AGENT=true`), seed data applied
   via `Build`. Decide deliberately whether the rate limiter is on and set SLOs to
   match (Module 4). Health-gate on `/_healthcheck` and confirm `/api/ping` → `pong`
   before any load.

3. **Author scripts.** Generate parameterized k6 scripts (Module 3): baseline
   GETs for latency/throughput, and the domain write path (`bulk-import` /
   `blazor-wasm-single-api`) for realistic DB load. Every request carries a
   correctness `check`; every script carries threshold-encoded SLOs (Module 2)
   and a 429 carve-out where the limiter applies (Module 4). Drive all profiles
   from one script via a `PROFILE` env var.

4. **Run staged profiles.** Smoke first (correctness gate) → average load →
   stress → spike (→ breakpoint/soak only if explicitly requested). Use the
   open-model arrival-rate executor for any capacity/throughput claim (Module 1).
   Save a raw summary JSON per profile into `metrics/` and record the `k6 version`
   and exact command (Module 6). Run stress/spike against a deployed environment
   only with explicit opt-in.

5. **Analyze results.** Separate real failures from designed 429s (Module 4),
   read latency percentiles and throughput against the SLOs, identify the
   bottleneck (DB, limiter, cold start, contention), and classify each finding's
   severity per Module 8. Distinguish "the limiter did its job" from "the app
   fell over."

6. **CI integration (when on a pipeline).** When running under GitHub Actions
   (`$GITHUB_ACTIONS`/`$CI`), append a per-profile results table to
   `$GITHUB_STEP_SUMMARY`, upload `k6-load-report/` as an artifact, and let k6's
   threshold-driven non-zero exit fail the job (Module 7). The workflow file is
   surfaced as an opt-in snippet — **not auto-committed** (pipeline files need
   approval per `CLAUDE.md`).

7. **Assemble the report.** Write `k6-load-report/README.md`: how-to-reproduce,
   executive summary, endpoint inventory, per-profile results tables, findings
   sorted by severity, and the sequenced remediation backlog. **Self-check before
   delivering** — any missing item means the run is not done: (i) every reported
   number traces to a saved `metrics/*.json` run with the command and k6 version;
   (ii) smoke passed its correctness checks before any scaled profile ran; (iii)
   capacity/throughput claims used an open-model executor and say so; (iv)
   designed 429s are categorized separately and not counted as failures; (v)
   nothing was run against a deployed environment or a real DB without explicit
   opt-in; (vi) the environment (DB, limiter state, warm-up) is stated so the run
   is reproducible.

## Finding format (required for every result)

```
### [SEVERITY] <profile> — <endpoint(s)>
- **Module:** <e.g. Module 4 — Rate Limiting & Back-Pressure>
- **Profile / executor:** <e.g. ramping-arrival-rate, 0→200 RPS over 2m>
- **Command:** <the exact `k6 run …` invocation — reproducible>
- **Metrics:** <p50/p95/p99 http_req_duration; RPS; error rate; checks%>
- **Threshold verdict:** <pass/fail against the stated SLO, with the numbers>
- **Bottleneck / why:** <what the numbers indicate — DB, rate limiter, cold start…>
- **Recommendation:** <the specific fix or follow-up>
- **Evidence:** metrics/<run>.json
```

## Report structure (`k6-load-report/README.md`)

1. **Title + how-to-reproduce** — target, k6 version, environment (DB, limiter
   state, warm-up), and the exact commands, so every result can be regenerated.
2. **Executive summary** — overall performance posture, the headline
   latency/throughput/error numbers, 3–5 systemic issues, single
   highest-leverage fix.
3. **Inventory** — the load-testable endpoints and the auth/rate-limit/payload
   shape of each (Module 3).
4. **Per-profile results** — smoke/load/stress/spike tables (p50/p95/p99, RPS,
   error rate, checks%, threshold verdict).
5. **Prioritized findings** — sorted by severity, using the finding format.
6. **Standards & severity mapping** — the SLO derivation and citations from
   Module 8, annotated with which findings map to which control/budget.
7. **Remediation backlog** — sequenced, flagging fixes that need a config change,
   an infra change, or coordinated capacity planning.

## Output fidelity rules (avoid these common LLM failure modes)

1. **Every result cites a saved k6 run**, with the real metric numbers from
   `metrics/*.json` — never "seems fast" or a number with no run behind it.
2. **Distinguish designed back-pressure from real failure.** A 429 from the rate
   limiter working as configured is not a defect; state whether the limiter was
   on and categorize 429s separately from `http_req_failed`.
3. **Never load-test a deployed environment or a real database without explicit
   opt-in.** Default to the local app + a throwaway SQLite DB; never pollute a
   shared/real DB with `bulk-import` or command writes.
4. **Smoke and correctness before scale.** Run smoke with status/body `checks`
   first — a fast 500 is not a pass, and scaling a broken endpoint measures
   nothing.
5. **Use an open-model executor for capacity/throughput claims and say which.**
   Closed-model VUs understate latency under load (coordinated omission).
6. **Interpret numbers against the target's own limits** — the 100/60s rate-limit
   ceiling, the request timeout, and the un-throttled single-api path — so a
   plateau isn't mistaken for max capacity.
7. **Discard or annotate cold-start iterations** (.NET JIT + first-request cost)
   so a warm-up spike doesn't distort p99.

## Guiding stance

- **Evidence over vibes.** Every number comes from a saved k6 run; every SLO
  claim cites the governing k6/SRE guidance and the metric.
- **Smoke before scale.** Correctness checks gate the run before any latency or
  throughput claim.
- **Model the client honestly.** Open vs closed executor is a deliberate choice,
  stated in the report — capacity claims are open-model only.
- **Respect the target.** Local app + throwaway DB by default; the deployed
  environment and destructive write load are explicit opt-ins.
- **Thresholds are the contract.** A run's verdict is threshold pass/fail with a
  non-zero exit gating CI, not prose.
- **k6 JS is tool input, not a repo helper.** Any results post-processing is a
  single-file C# script (`.csx` via `dotnet-script`) to match the repo's
  .NET-only toolchain — never Python/PowerShell for analysis logic.

See `PROMPT.md` for a portable single-paste version of this whole pipeline.
