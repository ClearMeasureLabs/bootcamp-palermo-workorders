# Phase 5 rep 1: v12g on 8 new change kinds, 8/8 green

Skill frozen at v12g (no map lines added for these items). Measured with `scripts/agent/measure_transcript.py` exactly as
phase 4 (bytes/4 estimates; input = context replayed at each tool-call turn, summed).

| Item | Issue | PR | Kind | Input | Output | Calls | Reads before 1st edit | Submits | Waits | get_mcp_tools |
|---|---|---|---|---|---|---|---|---|---|---|
| v01 | #10238 | #10248 | validation rule | 30.7k | 0.51k | 10 | 3 | 1 | 2 | 0 |
| v02 | #10239 | #10253 | new query + handler | 329.6k | 1.85k | 33 | 18 | 2 | 4 | 0 |
| v03 | #10240 | #10252 | UI page change | 103.5k | 0.85k | 16 | 6 | 1 | 2 | 2 |
| v04 | #10241 | #10250 | component tweak | 30.2k | 0.43k | 10 | 3 | 1 | 2 | 0 |
| v05 | #10242 | #10247 | DB + mapping | 62.5k | 0.66k | 14 | 5 | 1 | 2 | 1 |
| v06 | #10243 | #10251 | branch in existing method | 73.8k | 0.70k | 15 | 4 | 1 | 2 | 0 |
| v07 | #10244 | #10249 | config/option | 136.3k | 1.17k | 22 | 6 | 2 | 5 | 2 |
| v08 | #10245 | #10246 | bug from symptom | 29.8k | 0.56k | 10 | 3 | 1 | 2 | 0 |
| **Total** | | | | **796k** | **6.74k** | 130 | | | | |

Per run: **99.5k input** vs phase 4 rep 3 97k (+3%, i.e. about the same on items the skill was never tuned on) and vs
phase 3 rep 2 general baseline 392k (**-75%**). Output 0.84k per run (phase 4 rep 3: 0.98k).
Median run 68k; five of eight runs were 30-74k.

## Helper rule and CI failures
- **v06 helper rule fired:** the agent moved the today/overdue checks plus the new due-soon branch into a new private
  `ForOpenDueDate` helper instead of growing `Calculate`. No CRAP failure in any run.
- **Two CI REDs, each one extra submit + CI cycle:**
  - v02: new integration test data used work order numbers longer than the 7-character `Number` column.
  - v07: Qodana flagged the new options property as could-be-get-only (AutoPropertyCanBeMadeGetOnly); the existing
    properties carry a ReSharper suppression comment because options binding needs setters.

## Why v02, v03, v07 cost more
- **v02 (330k):** cross-layer new query. 18 reads/searches before the first edit (two sibling query stacks, their tests,
  RemotableRequestTests, BogusOverrides, validator-coverage test, filters), then the RED above and a second CI cycle
  (4 waits). Every later turn replays all of that.
- **v03 (104k):** the page and its test file are large (6.2 KB razor, 8.3 KB test), and the test file was read twice
  (once whole, once by range). Plus 2 `get_mcp_tools` calls.
- **v07 (136k):** the Qodana RED above: one extra edit, submit and 3 more waits. Plus 2 `get_mcp_tools` calls.

## Recommended skill fixes (NOT applied)
1. Two CI traps in GENERAL.md: WorkOrder `Number` is at most 7 characters (test data too); options classes keep setters, so
   copy the existing `// ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global` line onto new option properties.
2. `get_mcp_tools` still appeared in 3 of 8 runs despite the rule in the skill and prompt; it likely comes from platform
   instructions, so further wording probably won't remove it (about 2-6 KB per run).
3. Large files: allow reading a large test file by range only (rg the test names, then read one test), never whole then again.
