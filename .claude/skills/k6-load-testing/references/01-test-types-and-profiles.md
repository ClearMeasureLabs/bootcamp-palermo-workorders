# Module 1 — Test Types & Load Profiles

Standards: Grafana k6 Test Types (smoke/average/stress/spike/breakpoint/soak),
k6 Scenarios & Executors reference, closed-vs-open workload modelling.

## The load profiles to run

| Profile | What it answers | Shape | k6 executor |
|---|---|---|---|
| Smoke | Does the system work at all under minimal load? (correctness gate) | 1–5 VUs, 30s–1m | `constant-vus` |
| Average load | How does it behave under normal expected traffic? | ramp to expected RPS, hold ~5–10m | `ramping-arrival-rate` |
| Stress | Where does it degrade beyond normal peaks? | ramp past expected to 2–4× | `ramping-arrival-rate` |
| Spike | Does a sudden surge break it, and does it recover? | jump to a high RPS for a short burst, drop back | `ramping-arrival-rate` (steep stages) |
| Breakpoint | At what exact RPS does it fall over? | slow open-ended ramp until thresholds fail (`abortOnFail`) | `ramping-arrival-rate` |
| Soak | Do leaks/degradation appear over hours? | modest RPS held 1h+ | `constant-arrival-rate` |

Always run **smoke first**; only proceed to load/stress/spike once smoke passes
its correctness checks. Soak and breakpoint are optional and opt-in (long/costly).

## Closed vs open workload model (pick deliberately, state which)

| Model | k6 executors | Behaviour | Use for |
|---|---|---|---|
| Closed | `constant-vus`, `ramping-vus`, `per-vu-iterations`, `shared-iterations` | A fixed number of VUs; each waits for the previous request before sending the next. Throughput is *bounded by response time* — if the server slows, offered load drops. | Reproducing a fixed-concurrency client (e.g. N browsers). |
| Open | `constant-arrival-rate`, `ramping-arrival-rate` | Requests arrive at a target *rate* regardless of how fast the server responds; k6 allocates VUs to keep the rate. | Any capacity/throughput/latency claim — this is what real internet traffic looks like. |

**Capacity and latency claims must use an open model.** Closed-model VUs suffer
*coordinated omission*: when the server stalls, the client stops sending, so the
slow requests are never counted and p99 looks artificially good.

## What "good" looks like

- One k6 script per logical target (baseline GETs; domain write path), with
  `scenarios` selected by an `env` var (`PROFILE=smoke|load|stress|spike`) so a
  single script drives every profile — no copy-paste per profile.
- `ramping-arrival-rate` with an explicit `preAllocatedVUs` and `maxVUs` sized
  above the target rate ÷ expected-latency, so k6 never starves itself of VUs
  (a "insufficient VUs" warning invalidates the run).
- Each profile's stages documented in the report so the run is reproducible.
- A short warm-up stage (or discarded first iterations) before measurement —
  see Module 5 for the .NET cold-start caveat.

## Detection commands

```
# List scenarios/executors available and validate a script without load:
k6 inspect k6/baseline.js

# Drive a profile by env var (script defines scenarios keyed on __ENV.PROFILE):
PROFILE=smoke k6 run k6/baseline.js
PROFILE=load  k6 run k6/baseline.js
```

Cross-reference Module 2 for the thresholds each profile is gated on, and
Module 5 for warm-up / environment setup that keeps the profile honest.
