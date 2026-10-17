# Work-type catalog

Each type gets one run with the generic lean skill (no type script), then a canned type recipe/script and a rerun.

| Type | Recipe / script | Generic-lean est. tokens | Canned est. tokens | Runs |
|---|---|---|---|---|
| Null guard + unit test | `scripts/agent/Invoke-GuardItem.ps1` | ~47k (run 2) | ~0.3k (run 3) | 1-3 |
| Unit tests for an untested class | `Get-TestContext.ps1` + `Complete-AgentItem.ps1` (3 calls) | ~37k (run 4, 10 calls) | ~3.3k (run 5, 3 calls) | 4-5 |
| Qodana P1 mechanical fix (real finding) | _pending_ | | | |
| Visibility: seal non-inherited class | `Invoke-SealItem.ps1` (1 call) | ~10k (run 6, 6 calls) | ~0.27k (run 7, 1 call) | 6-7 |
| Validation rule + unit test | `Invoke-ValidatorRuleItem.ps1` (1 call) | ~50k (run 8, 13 calls) | ~0.31k (run 9, 1 call) | 8-9 |
| Bug fix + regression test | general v3 + `-Focus` | ~17k (run 10, 6 calls) | run 11 | 10-11 |
| Mixed / uncanned (general skill) | general v3: `Get-ItemContext` + `Complete-AgentItem` | ~63k (run 12, 14 calls) | runs 13-16 | 12-16 |

Baseline (stock feature loop, guard type): ~2.90M (run 1).
