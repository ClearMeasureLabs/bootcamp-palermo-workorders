# Module 7 — CI Integration (GitHub Actions)

Standards: Grafana `grafana/setup-k6-action`, GitHub Actions job summaries
(`$GITHUB_STEP_SUMMARY`), `actions/upload-artifact`, workflow exit-code gating.

When the skill runs inside a pipeline, the load test becomes a gate: thresholds
decide the job's pass/fail, the report is uploaded as an artifact, and a
human-readable summary is written to the Actions run page.

> **Repo rule:** do not auto-write files under `.github/workflows/`. Pipeline
> files require explicit approval (see `CLAUDE.md` Key Conventions). The workflow
> below is a **snippet the user opts to add**, surfaced in the report — not
> committed by the skill.

## Anti-patterns to hunt

| Pattern | Why it hurts | Evidence to look for |
|---|---|---|
| Swallowing k6's exit code (`k6 run … || true`) | A threshold breach no longer fails the job — the gate is fake | `|| true`, `continue-on-error: true` on the k6 step |
| No artifact upload | The report is lost when the runner is torn down | no `actions/upload-artifact` for `k6-load-report/` |
| Nothing written to the run summary | Reviewers must dig into raw logs to see results | no `>> "$GITHUB_STEP_SUMMARY"` |
| Load-testing a deployed env from CI without opt-in | Hits real infra/cost on every push | a workflow targeting the deployed URL on `push` with no guard |
| Running the app + k6 with no health gate in CI | Race: k6 starts before the app is ready → false failures | k6 step immediately after `dotnet run` with no wait |

## What "good" looks like

- **Thresholds are the gate** — let k6's non-zero exit fail the step; do not
  mask it. The job goes red exactly when an SLO is breached (Module 2).
- **Summary to the run page** — append a compact markdown table to
  `$GITHUB_STEP_SUMMARY` from the saved summary JSON (Module 6).
- **Report as artifact** — upload the whole `k6-load-report/` directory.
- **Detect CI** — the skill keys off `$GITHUB_ACTIONS` / `$CI` to switch on the
  summary + artifact behaviour; locally it just writes the report dir.
- **Local target in CI too** — spin the app up on the runner against throwaway
  SQLite (Module 5); target the deployed env only via an explicit, guarded
  `workflow_dispatch` input.

## Embedded workflow snippet (opt-in — do not auto-commit)

```yaml
# .github/workflows/k6-load-test.yml  (add manually after review)
name: k6 load test
on:
  workflow_dispatch:
  # pull_request:            # opt in once baselines are stable
jobs:
  load-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '10.0.x' }
      - name: Start app (throwaway SQLite, agent off)
        env:
          ConnectionStrings__SqlConnectionString: "Data Source=loadtest.db"
          DISABLE_AUTO_REFORMAT_AGENT: "true"
        run: |
          dotnet run --project src/UI/Server &
          for i in $(seq 1 60); do
            curl -sk https://localhost:7174/_healthcheck && break || sleep 2
          done
      - uses: grafana/setup-k6-action@v1
      - name: Run smoke + load (thresholds gate the job)
        run: |
          mkdir -p k6-load-report/metrics
          PROFILE=smoke k6 run --insecure-skip-tls-verify \
            --summary-export=k6-load-report/metrics/smoke-summary.json k6/baseline.js
          PROFILE=load  k6 run --insecure-skip-tls-verify \
            --summary-export=k6-load-report/metrics/load-summary.json  k6/baseline.js
      - name: Write job summary
        if: always()
        run: |
          {
            echo "## k6 load test results"
            echo "| profile | p95 (ms) | error rate | checks |"
            echo "|---|---|---|---|"
            # extract from metrics/*.json (jq or a .csx helper — see Module 6)
          } >> "$GITHUB_STEP_SUMMARY"
      - uses: actions/upload-artifact@v4
        if: always()
        with:
          name: k6-load-report
          path: k6-load-report/
```

## Detection commands

```
# Confirm the skill is running under CI:
echo "GITHUB_ACTIONS=$GITHUB_ACTIONS CI=$CI"

# Locally simulate the gate: a breached threshold must exit non-zero.
PROFILE=smoke k6 run --insecure-skip-tls-verify k6/baseline.js ; echo "exit=$?"
```

Cross-reference Module 2 (the thresholds doing the gating), Module 5 (bringing
the app up on the runner), and Module 6 (the summary JSON the summary step reads).
