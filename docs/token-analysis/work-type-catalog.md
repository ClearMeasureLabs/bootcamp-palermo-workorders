# Work-type catalog

Each type gets one run with the generic lean skill (no type script), then a canned type recipe/script and a rerun.

| Type | Recipe / script | Generic-lean est. tokens | Canned est. tokens | Runs |
|---|---|---|---|---|
| Null guard + unit test | `scripts/agent/Invoke-GuardItem.ps1` | ~47k (run 2) | ~0.3k (run 3) | 1-3 |
| Unit tests for an untested class | `Get-TestContext.ps1` + `Complete-AgentItem.ps1` (3 calls) | ~37k (run 4, 10 calls) | ~3.3k (run 5, 3 calls) | 4-5 |
| Qodana P1 mechanical fix (real finding) | n/a: checklist findings already fixed on master | run 0 (invalid, ~3.1M) | — | 0 |
| Visibility: seal non-inherited class | `Invoke-SealItem.ps1` (1 call) | ~10k (run 6, 6 calls) | ~0.27k (run 7, 1 call) | 6-7 |
| Validation rule + unit test | `Invoke-ValidatorRuleItem.ps1` (1 call) | ~50k (run 8, 13 calls) | ~0.31k (run 9, 1 call) | 8-9 |
| Bug fix + regression test | general v3 + `-Focus` | ~17k (run 10, 6 calls) | ~5.7k (run 11, 5 calls) | 10-11 |
| Mixed / uncanned (general skill) | general v3: `Get-ItemContext` + `Complete-AgentItem` | ~63k (run 12, 14 calls) | ~4.3k-11.2k (runs 13-16, avg ~8.2k) | 12-16 |
| Larger realistic items (2-3 files, Blazor/bUnit, parser) | general v4 | — | 7.5k-251k (runs 17-20, median ~28.5k) | 17-20 |

Baseline (stock feature loop, guard type): ~2.90M (run 1).
