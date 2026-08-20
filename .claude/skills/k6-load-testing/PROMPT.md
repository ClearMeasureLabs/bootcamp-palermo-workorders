# Portable k6-Load-Testing Prompt (standalone)

Paste into any capable coding-agent session when you can't load the skill. It
compresses the full pipeline: inventory → environment → script → run → analyze →
report (+ CI gate).

---

You are performing a **k6 load and performance test** of an application's HTTP
surface. Produce a **self-contained report directory** (`k6-load-report/` with
`README.md` and `metrics/`) that measures latency and throughput against
threshold-encoded SLOs, with every claim citing a saved k6 run and a concrete
metric. Put generated scripts in a `k6/` directory. Install k6 as a standalone
binary — do not mutate the target repo to obtain it.

**Output-fidelity rules (do not skip):** (a) Every result cites a saved
`metrics/*.json` run with the exact command and `k6 version` — never "seems fast"
without the number. (b) A 429 from a rate limiter working as configured is
designed back-pressure, NOT a failure — state whether the limiter was on and
categorize 429s separately from `http_req_failed`. (c) Never load-test a deployed
environment or a real database without explicit opt-in; default to a local app
and a throwaway DB, and never pollute shared data with write load. (d) Run smoke
with status/body `checks` first — a fast 500 is not a pass. (e) Use an open-model
arrival-rate executor for any capacity/throughput claim and say so; closed-model
VUs understate latency (coordinated omission). (f) Interpret numbers against the
target's own limits (rate-limit ceiling, request timeout, un-throttled paths).
(g) Discard or annotate cold-start iterations so warm-up doesn't skew p99.

Run this ordered pipeline:

1. **Scope & inventory.** Confirm the target (default local; deployed only on
   explicit opt-in). Enumerate every load-testable endpoint with its method,
   route, auth requirement (header name + config), rate-limit behaviour, and
   request/response payload shape. Choose the profiles and SLOs. Create the
   `k6-load-report/` + `metrics/` + `k6/` skeleton.
2. **Environment prep.** Bring the target up reproducibly: throwaway DB,
   background jobs/agents off, seed data applied. Decide the rate-limiter state
   deliberately and set SLOs to match. Health-gate before any load.
3. **Author baseline scripts.** k6 scripts for the cheap read endpoints, each
   with a correctness `check` and thresholds (`http_req_duration` p95/p99,
   `http_req_failed` rate, `checks` rate). Drive all profiles from one script via
   a `PROFILE` env var and pick executors per profile (open-model for capacity).
4. **Author domain-load scripts.** Scripts for the realistic write/read paths
   (correct payload envelope, valid seeded identifiers, auth header). Keep write
   load pointed at the throwaway DB.
5. **Rate-limit handling.** Add a 429 carve-out (custom `Rate` for real errors vs
   designed throttling); when validating the limiter, enable it, keep one
   partition, drive past the ceiling, and assert the expected 429 shape + headers.
6. **Run staged profiles.** Smoke → average load → stress → spike (breakpoint/soak
   only if requested). Save a summary JSON per profile to `metrics/`; record the
   command and k6 version.
7. **Analyze.** Read percentiles/throughput vs SLOs, separate 429s from real
   failures, identify the bottleneck, classify severity (Critical: breach under
   smoke/nominal or no spike recovery; High: p95 breach under expected load;
   Medium: only-under-stress degradation; Low: soak/cold-start/marginal).
8. **Standards & budgets.** Derive SLO budgets from the measured smoke baseline
   and the reliability target (don't invent round numbers); cite Grafana k6
   guidance, Google SRE error budgets, and HTTP 429 semantics.
9. **CI gate (if on a pipeline).** Under GitHub Actions, append a per-profile
   table to `$GITHUB_STEP_SUMMARY`, upload `k6-load-report/` as an artifact, and
   let k6's threshold-driven non-zero exit fail the job. Surface the workflow as
   an opt-in snippet; do not auto-commit pipeline files.
10. **Assemble the report.** Write `k6-load-report/README.md`: how-to-reproduce,
    executive summary, endpoint inventory, per-profile result tables, findings by
    severity, and the sequenced remediation backlog.

**Self-check before delivering:** (i) every number traces to a saved run with
its command + k6 version; (ii) smoke passed correctness before any scaled
profile; (iii) capacity claims used an open-model executor and say so; (iv)
designed 429s are separated from failures; (v) nothing hit a deployed env or real
DB without opt-in; (vi) the environment (DB, limiter state, warm-up) is stated so
the run reproduces.

Deliver the path to the finished report directory and a short summary of the
worst findings.
