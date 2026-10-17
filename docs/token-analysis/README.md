# Token-reduction experiment (feature-loop dispatch)

Goal: cut Cursor cloud agent token usage per work item by 80% vs baseline. Each iteration uses one sibling issue built from [work-item-template.md](work-item-template.md). PRs are closed without merging. Maximum 20 iterations.

Cursor transcripts carry no token usage fields. Proxy = cumulative replayed context (sum over turns of prefix bytes / 4), plus turns and tool calls.

| Run | Item | PR | Agent | Turns | Tool calls | Est. cumulative tokens | Δ vs baseline | Notes |
|---|---|---|---|---|---|---|---|---|
| 0 | #9936 | — | bc-34ba5471 | 99 | 99 | ~3.1M | invalid | no diff (finding already fixed) |
| 1 | #9937 | #9938 | bc-3101faa3 | 90 | 90 | ~2.90M | **baseline** | stock skill, ~35 min |
| 2 | #9939 | #9940 | bc-aea9f32c | 13 | 13 | ~47k | **-98.4%** | lean v1, ~4 min |
| 3 | #9941 | #9942 | bc-06208a2f | 1 | 1 | ~0.3k | **-99.99%** | lean v2 one-shot; target confirmed |
| 4 | #9943 | #9944 | bc-23f09c58 | 10 | 10 | ~37k | -98.7% | type B (unit tests), generic lean |
| 5 | #9945 | #9946 | bc-caaf07fd | 3 | 3 | ~3.3k | -99.9% | type B, canned recipe |
| 6 | #9947 | #9948 | bc-54d95a8d | 6 | 6 | ~10k | -99.7% | type C (seal), generic lean |
| 7 | #9949 | #9951 | bc-43a2211a | 1 | 1 | ~0.27k | -99.99% | type C canned |
| 8 | #9950 | #9952 | bc-c199a536 | 13 | 13 | ~50k | -98.3% | type D generic lean |
| 9 | #9953 | #9956 | bc-30f95531 | 1 | 1 | ~0.31k | -99.99% | type D canned |
| 10 | #9954 | #9957 | bc-57c1aa8b | 6 | 6 | ~17k | -99.4% | type E generic lean |
| 11 | #9959 | | | | | | | type E via general path v3 (-Focus) |
| 12 | #9955 | #9958 | bc-5761780f | 14 | 14 | ~63k | -97.8% | mixed, generic lean |
| 13 | #9960 | | | | | | | small feature, general v3 |
| 14 | #9961 | | | | | | | guard on factory method, general v3 |
| 15 | #9962 | | | | | | | ToString override, general v3 |
| 16 | #9963 | | | | | | | name trimming bug, general v3 |

## Progress notes
- 2026-10-16: Run 0 analysed. It is an invalid baseline. A repeatable template was created and issue #9937 filed for baseline run 1. Board writes were not possible: the gh token lacks the project scope.
- 2026-10-16: Run 1 baseline recorded (~2.90M est.). Optimization v1 added (scripts/agent + feature-loop-lean). Iteration 2 filed.
- 2026-10-16: Run 2 came in at -98.4% vs baseline (est.). v2 is a one-shot canned script. Iteration 3 filed; it is the confirmation run for the >=80% target.
- 2026-10-16: Run 3 came in at -99.99% (est.), confirming >=80% over 2 runs. Runs 4-20 vary the type of work; see work-type-catalog.md.
- 2026-10-16: Run 4 (type B, generic lean): ~37k. Canned unit-test recipe added. Note: branch deletion is blocked by a repo rule, so experiment branches remain.
- 2026-10-16: Run 5 (type B canned): 3 calls, ~3.3k. Type C = seal a non-inherited class; the Qodana backlog is already fixed on master.
- 2026-10-16: Run 6 (type C generic lean): 6 calls, ~10k. Seal recipe added. Runs 7 and 8 pre-filed; run 9 (type D canned) will be filed after run 8 so its recipe is based on the measured run.

## Finding: the Qodana checklists are already fixed on master
Checklist items in #9436 (P1 mechanical) and #9437 (P2 naming/nullability), and a sample from #9439 (P4 dead symbols), were spot-checked on master on 2026-10-16; all of them are already fixed (#9437-#9439 are CLOSED; #9438 has no file-level checklist). The run-0 agent independently found its P1 target already fixed. The committed `qodana.sarif.json` holds a single result (`Html.AttributeValueNotResolved`, Settings.razor:16), which is not a mechanical fix. That is why run 0 found nothing to change. Experiment work types are therefore drawn from verified live code (missing tests, sealable classes, validation gaps) instead of the stale checklists. The issues still show unchecked boxes and should be reconciled.
- 2026-10-16: Runs 7 and 8 recorded. Validator recipe added. Runs 9, 10 and 12 pre-filed; run 11 (type E canned) waits for run 10.
- 2026-10-16: Runs 9, 10 and 12 recorded. General path v3 (Get-ItemContext) built. Runs 11 and 13-16 test it without type scripts.
