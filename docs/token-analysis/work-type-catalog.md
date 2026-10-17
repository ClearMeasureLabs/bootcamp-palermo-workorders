# Work-type catalog

Each type gets one run with the generic lean skill (no type script), then a canned type recipe/script and a rerun.

| Type | Recipe / script | Generic-lean est. tokens | Canned est. tokens | Runs |
|---|---|---|---|---|
| Null guard + unit test | `scripts/agent/Invoke-GuardItem.ps1` | ~47k (run 2) | ~0.3k (run 3) | 1-3 |
| Unit tests for an untested class | _pending_ | run 4 | run 5 | 4-5 |
| Qodana P1 mechanical fix (real finding) | _pending_ | | | |
| Rename / visibility change | _pending_ | | | |
| Small validation rule + test | _pending_ | | | |

Baseline (stock feature loop, guard type): ~2.90M (run 1).
