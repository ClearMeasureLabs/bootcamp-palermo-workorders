# Phase 4 rep 1 (v9g general skill, no templates): 8/8 green

Method: `scripts/agent/measure_transcript.py`. Input = transcript bytes replayed at each tool-call turn (including that
turn), summed, /4; it reproduces phase 3's published inputs exactly (rep 5: t04 58.7k, t07 1.9k; rep 2: t04 1,107k, t16 2,136k).
Output = thinking + text + tool args, /4. **Caveat:** this output rule reads lower than phase 3's published outputs for the
same transcripts (rep 5: 5.4k here vs 12.0k published), so output is compared only with rep 5 re-measured by this script.

| Item | Issue | PR | Kind | Input | Output | Tool calls |
|---|---|---|---|---|---|---|
| g01 | #10206 | #10216 | domain method | 64.0k | 0.62k | 14 |
| g02 | #10207 | #10220 | search filter (spec/query/handler) | 271.9k | 1.73k | 30 |
| g03 | #10208 | #10214 | UI component | 36.4k | 0.42k | 11 |
| g04 | #10209 | #10217 | MCP tool param | 526.9k | 2.41k | 35 |
| g05 | #10210 | #10219 | DB index migration | 50.1k | 0.68k | 14 |
| g06 | #10211 | #10221 | structured logging | 425.7k | 1.86k | 35 |
| g07 | #10212 | #10215 | bug from symptom | 37.1k | 0.52k | 10 |
| g08 | #10213 | #10218 | refactor | 87.2k | 1.01k | 15 |
| **Total** | | | | **1,499k** | **9.26k** | 164 |

Per run: input 187k, output 1.16k.
- vs phase 3 rep 2 (general techniques, published 7,445k / 19 = 392k per run): **input -52%**.
- vs phase 3 rep 5 (templates, 106k / 19 = 5.6k per run): input ~33x higher; output 1.16k vs 0.29k per run (same script).
- Different change kinds than phase 3, so per-run averages are the only comparison.

## Waste per transcript
| Item | Exploration calls (KB) | Submits | Wait calls (empty) | Stray calls |
|---|---|---|---|---|
| g01 | 3 (8.9) | 1 | 3 (2) | 3: get_mcp_tools x2, read terminal file |
| g02 | 16 (30.2) | 2 | 4 (3) | git branch/status inside rg calls |
| g03 | 3 (6.2) | 1 | 2 (1) | get_mcp_tools |
| g04 | 8 (44.1) | 2 | 12 (11) | 7: git status, read Wait-PrChecks.ps1 (6 KB), 5x gh api check-runs |
| g05 | 3 (3.2) | 1 | 3 (2) | 4: get_mcp_tools x2, terminal file, rg in scripts |
| g06 | 26 (55.8) | 1 | 2 (1) | git status |
| g07 | 3 (9.5) | 1 | 2 (1) | 0 |
| g08 | 7 (19.3) | 1 | 2 (1) | get_mcp_tools |

Top causes (input is driven by how big the context is when the many later turns replay it):
1. **Exploration before the first edit** (g06: 26 reads/rg sweeps, about 56 KB, mostly hunting for a logger test helper that
   doesn't exist; g02: 16, tracing spec -> query -> filter -> tests and BogusOverrides). This bloats every later turn.
2. **CRAP gate RED -> second CI cycle** (g02 `WorkOrderQueryFilters.Apply`, g04 `ListWorkOrders` at CC 8): methods grew
   past cyclomatic complexity 6 (threshold 6; 100% coverage still fails above CC 6). Each cost a resubmit + more waits.
3. **Wait handling:** 8-min WAIT_CMD calls came back empty in every run (22 of 32 wait calls); some agents then read terminal
   files. g04: a `QDNETC` check sat `queued` >1 h after everything else finished; the wait script reported PENDING forever,
   so the agent read the script and polled check-runs by hand (12 waits, 5 gh api calls).
4. **Stray calls:** get_mcp_tools in 5 of 8 runs (1.9-4.6 KB each); git status/branch checks because the workspace branch
   looked wrong.

## v10g changes (general only)
- GENERAL.md: explicit never-list (get_mcp_tools, git status/branch, reading scripts/terminal files, check-run digging),
  states the workspace branch is expected; aim <= 12 calls; one sibling test, no repo-wide sweeps, no re-reads; richer map
  (search-filter chain, test folder per layer, MCP shared parsing, no logger test package exists, BogusOverrides cases);
  CRAP formula and CC <= 5 rule with "extract a helper"; Qodana `new[]` note.
- Wait-PrChecks.ps1 v10: a check still `queued` >= 15 min with nothing else running prints `IGNORED_QUEUED: <name>` and no
  longer blocks the verdict.
- Submit-Change.ps1: WAIT_CMD uses 5-min calls (8-min calls came back empty). This is a hypothesis to be confirmed in rep 2.
- measure_transcript.py added (the measurement script).
