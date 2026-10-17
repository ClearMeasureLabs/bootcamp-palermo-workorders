# Work-type catalog

Each type gets one run with the generic lean skill (no type script), then a canned type recipe/script and a rerun.

| Type | Recipe / script | Generic-lean est. tokens | Canned est. tokens | Runs |
|---|---|---|---|---|
| Null guard + unit test | `scripts/agent/Invoke-GuardItem.ps1` | ~47k (run 2) | ~0.3k (run 3) | 1-3 |
| Unit tests for an untested class | `Get-TestContext.ps1` + `Complete-AgentItem.ps1` (3 calls) | ~37k (run 4, 10 calls) | ~3.3k (run 5, 3 calls) | 4-5 |
| Qodana P1 mechanical fix (real finding) | _pending_ | | | |
| Visibility: seal non-inherited class | `Invoke-SealItem.ps1` (1 call) | ~10k (run 6, 6 calls) | run 7 | 6-7 |
| Validation rule + unit test | _pending (after run 8)_ | run 8 | run 9 | 8-9 |

Baseline (stock feature loop, guard type): ~2.90M (run 1).
