# Phase 4 rep 3 (v11g, 02dfe0ec): 8/8 green

| Item | PR | Input | Output | Calls | Reads before 1st edit | Submits | Wait calls | get_mcp_tools |
|---|---|---|---|---|---|---|---|---|
| g01 | #10235 | 58.2k | 0.63k | 12 | 3 | 1 | 2 | 2 |
| g02 | #10233 | 149.0k | 1.25k | 21 | 10 | 1 | 2 | 0 |
| g03 | #10231 | 36.7k | 0.42k | 10 | 3 | 1 | 2 | 0 |
| g04 | #10236 | 181.3k | 1.42k | 22 | 8 | 2 | 4 | 0 |
| g05 | #10232 | 21.7k | 0.48k | 9 | 3 | 1 | 2 | 0 |
| g06 | #10237 | 212.8k | 1.96k | 23 | 15 | 1 | 2 | 0 |
| g07 | #10230 | 45.1k | 0.56k | 12 | 3 | 1 | 2 | 0 |
| g08 | #10234 | 73.9k | 1.09k | 15 | 7 | 1 | 2 | 0 |
| **Total** | | **779k** | **7.83k** | 124 | | | | |

Per run 97k input (rep 2: 118k, rep 1: 187k).

## v11g effects
- 9-min waits: every green-first-time run needed exactly 2 wait calls (rep 2: 3). Worked.
- get_mcp_tools: 1 run (rep 2: 4).
- Reads before first edit: g02 15 -> 10, g08 7 -> 7, g06 10 -> 15 (logger/test-host hunting again).
- g04: CRAP RED again (adding the limit branch took the existing `ListWorkOrders` from CC 6 to 7), so it took a second CI cycle.

## Final edit (v12g)
- GENERAL.md CRAP trap: before adding a branch to an existing method, count its branches; 4+ -> put the new logic in a helper.
  This is the only failure that recurred across reps (rep 1 g02/g04, rep 3 g04). No other changes; v12g is not measured.
