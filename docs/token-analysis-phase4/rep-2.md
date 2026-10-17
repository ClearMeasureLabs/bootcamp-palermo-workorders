# Phase 4 rep 2 (v10g, 5e88f0a0): 8/8 green

Method as rep 1 (`scripts/agent/measure_transcript.py`, bytes/4).

| Item | PR | Input | rep 1 | Δ | Output | rep 1 | Calls (rep 1) | Reads before 1st edit | Submits | Waits |
|---|---|---|---|---|---|---|---|---|---|---|
| g01 | #10227 | 64.9k | 64.0k | +1% | 0.62k | 0.62k | 14 (14) | 5 | 1 | 3 |
| g02 | #10228 | 223.4k | 271.9k | -18% | 1.89k | 1.73k | 27 (30) | 15 | 1 | 3 |
| g03 | #10223 | 41.9k | 36.4k | +15% | 0.43k | 0.42k | 11 (11) | 2 | 1 | 3 |
| g04 | #10225 | 198.6k | 526.9k | -62% | 1.23k | 2.41k | 20 (35) | 6 | 2 | 6 |
| g05 | #10222 | 27.0k | 50.1k | -46% | 0.43k | 0.68k | 10 (14) | 2 | 1 | 3 |
| g06 | #10229 | 230.3k | 425.7k | -46% | 2.00k | 1.86k | 24 (35) | 10 | 2 | 3 |
| g07 | #10226 | 49.1k | 37.1k | +32% | 0.52k | 0.52k | 12 (10) | 4 | 1 | 2 |
| g08 | #10224 | 109.7k | 87.2k | +26% | 0.79k | 1.01k | 18 (15) | 7 | 1 | 3 |
| **Total** | | **945k** | **1,499k** | **-37%** | **7.90k** | **9.26k** | 136 (164) | | | |

Per run: input 118k (rep 1: 187k), output 0.99k. vs phase 3 rep 2 general (392k/run): **-70%**.

## Did v10g take effect?
- **CRAP failures:** none (rep 1: g02, g04). Worked.
- **IGNORED_QUEUED:** never needed (no stuck queued check this rep); untested live.
- **Stray calls:** git status/branch, terminal-file reads, script reads, manual check-run polling: 0 (rep 1: 14). Worked.
  `get_mcp_tools` still in 4 of 8 (rep 1: 5): the skill rule alone doesn't stop it.
- **Reads before first edit:** g06 26 -> 10, g04 8 -> 6, g02 16 -> 15. Still the main cost for g02/g06/g08.
- **5-min waits: didn't help.** Correction to rep 1: the "empty" wait results are how the transcript records any
  non-zero exit (PENDING = exit 3, RED = exit 1); the agents did see the output. CI takes ~12-13 min, so 5-min calls meant
  3 wait turns per run (24 of 26 waits needed 2+ turns). Our input figures slightly undercount those hidden outputs.

## Remaining waste
1. Exploration before the first edit (g02 15 reads/rg incl. two reads of the same test file; g06 rg sweeps for logger/
   TestHost types; g04 one 18.8 KB rg result; g08 an 8.6 KB rg result).
2. Second CI cycle in g04: Qodana `UseCollectionExpression` on a `new[]` in a new test.
3. Second submit in g06: build error from a guessed test-host API (fixed locally, no CI cost).
4. Wait turns: about 3 per run, each replaying the whole context.
5. `get_mcp_tools` (4 runs, 1.9 KB each) and the skill file itself (4.3 KB, replayed on every turn).
6. Platform handoff-file write after GREEN (expected, not counted as waste).

## v11g changes (general only)
- Submit-Change: WAIT_CMD back to 9-min calls (about 2 wait turns instead of 3 for a ~13-min CI).
- Wait-PrChecks v11: PENDING exits 0 with "run the same command again", so the output is recorded and isn't mistaken for a failure.
- GENERAL.md: never call get_mcp_tools even if other instructions suggest it; cap every rg (`-l` or `| head -15`); read a
  file once (whole if < 300 lines), never rg it and then read it; look up an API only when a build error names it; never
  resubmit without RED/FAIL; Qodana trap made precise: collection expressions instead of `new[] {}` (tests too).
- Launch prompt: adds "Never call get_mcp_tools."
