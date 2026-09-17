# CI: single merge gate (`build-result`)

## Decision

`build-result` is the only required status check for merging a PR in this repository. Individual
job-level `if:` conditions must not also gate independent jobs against each other's outcomes —
that duplicates gating, and when an independent job (e.g. `qodana`) fails, it silently prevents an
unrelated job (e.g. `acceptance-tests`) from producing a result at all, destroying evidence that a
later factory/review step needs to read.

`build-result` (`.github/workflows/build.yml`) already implements the single gate correctly: it
`needs:` every gated job, reads each job's `.result`, and fails the PR if any required job did not
succeed. It runs with `if: always() && !cancelled()` so it always evaluates and reports, even when
upstream jobs fail.

## Before

`acceptance-tests` and `acceptance-tests-arm` were declared:

```yaml
if: success() && needs.changes.outputs.code == 'true'
```

`success()` here couples the job to the outcome of *every other job already completed in the
run* — including jobs like `qodana` and `code-analysis` that `acceptance-tests` has no data
dependency on. A single Qodana style warning would cause `acceptance-tests` to be skipped/cancelled
instead of running and reporting a result, even though `acceptance-tests` does its own independent
build (`. ./build.ps1; Invoke-AcceptanceTests`) and never consumes Qodana's or code-analysis's
output.

Job dependency graph (before), showing the incidental coupling:

```
changes -> acceptance-tests      (gated on success() of qodana, code-analysis, build-windows, ...)
changes -> acceptance-tests-arm  (same incidental coupling)
```

## After

```yaml
if: needs.changes.outputs.code == 'true'
```

`acceptance-tests` and `acceptance-tests-arm` now run whenever there is code to test, regardless of
any other independent job's outcome. `build-result` remains unchanged and still fails the PR if
either job does not succeed — merge safety is unaffected; only evidence completeness improves.

```
changes -> acceptance-tests      (runs whenever code == 'true')
changes -> acceptance-tests-arm  (runs whenever code == 'true')

build-result needs: [changes, build-linux, build-sqlite, integration-build-arm,
                      code-analysis, qodana, build-windows,
                      acceptance-tests, acceptance-tests-arm]
build-result: if: always() && !cancelled()   <- single required merge gate
```

## Jobs intentionally left unchanged

`docker-build-image-for-churchbulletin-ui`, `publish-github-packages`, and `publish-octopus` keep
their `success() && needs.changes.outputs.code == 'true'` job-level `if`. Unlike the acceptance
jobs, they `needs: [changes, build-linux]` and consume `build-linux`'s published artifact — their
`success()` reflects a real data dependency, not an incidental one, so they are out of scope for
this decision.

## Related

- Issue: ClearMeasureLabs/bootcamp-palermo-workorders#9518
- Evidence: ClearMeasureLabs/AISoftwareFactory#384
