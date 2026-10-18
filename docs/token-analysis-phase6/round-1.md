# Phase 6 round 1 (v14g, 7a1cbd3b): 8/8 green, but input went UP

Measured with `measure_transcript.py` and `attribute_transcript_tokens.py` (bytes/4 estimates).

| Item | Issue | PR | Input | Output | Calls |
|---|---|---|---|---|---|
| r01 nosniff middleware | #10254 | #10266 | 468.5k | 2.53k | 35 |
| r02 WorkOrder.IsAssignedTo | #10255 | #10263 | 107.8k | 0.96k | 16 |
| r03 CSV title limit | #10256 | #10268 | 991.5k | 4.43k | 49 |
| r04 slugify endpoint | #10257 | #10269 | 393.4k | 1.41k | 31 |
| r05 aria-label | #10258 | #10264 | 24.4k | 0.42k | 10 |
| r06 employees by role | #10259 | #10267 | 275.6k | 2.23k | 25 |
| r07 translation guard | #10260 | #10262 | 35.3k | 0.77k | 11 |
| r08 worker reply log | #10261 | #10265 | 68.8k | 0.66k | 15 |
| **Total** | | | **2,365k** | **14.4k** | 192 |

Per run 296k input vs phase 4 rep 3 97k and phase 5 99.5k (about 3x worse). Without r01/r03/r04: 102k per run.

Attribution: reads/searches before first edit 1,190k (50%), assistant messages 372k (16%), edits 297k (13%), reads after
first edit 183k (8%), skill file 119k (5%, down from 18% with the 2.1 KB file), get_mcp_tools 98k (4%), wait turns 174k (7%).

## Diagnosis
- **Inline CI wait broke the output channel.** Submit-Change ran build + tests + publish + up to ~9 min of waiting in one
  call; several calls ended with no output (r03 got `""`; r01/r04/r06 got nothing usable). Agents then read terminal files,
  scripts (Submit-Change, Publish-AgentPr), `git diff`, `git show` and the PR via gh api. This was the largest regression.
- **r03 (992k):** scope creep: it also changed the bulk-import controller and processor (beyond the parser), causing a
  Qodana RED (UseWithExpressionToCopyRecord) and a NO_CHANGE/old-result confusion; 49 calls, 4 Get-Context calls.
- **r01 (469k):** one RISK=FIX hit, a **false positive**: `ConfigurePipeline` already had est. CC 9 and the change only
  added a middleware call. Then the missing-output problem led to script reads and a deleted/re-added file.
- **r04 (393k):** the local SQLite integration run caught a real failure (new anonymous tools route blocked by the API-key
  public-path list), saving a CI cycle, but finding the cause took 6 more Get-Context calls, 4 of them from `src/` so they
  matched nothing (the script assumed the repo root as cwd), then 10-12 KB outputs from broad regexes.
- **Get-Context outputs too big:** broad regexes returned 31 prod / 28 test files, 9-12 KB per call (r01, r04, r06).
- **get_mcp_tools:** 10 calls across all 8 runs (98k) despite the prompt and skill; platform-driven.
- RISK=FIX: 1 hit (false positive). No CRAP RED in CI.

## v15g changes
- Submit-Change: inline CI wait removed (returns PR + WAIT_CMD as in v11g); runs from the repo root; more IT failure lines.
- Get-Context: runs from the repo root; outlines only the first 3 prod + 3 test files (18 lines each), lists the rest by
  name, hard cap ~3.5 KB.
- Test-ChangeRisk: compares estimated CC with the base version; flags only methods that got more complex and are > 5.
  Tested: planted branch (5 -> 6) flagged; touching an already-complex method without new branches not flagged.
- GENERAL.md: missing output -> rerun once, never inspect scripts/terminal files/git/PR; Get-Context with exact type
  names, at most 2 calls; change only what the acceptance criteria need; trap: new anonymous API routes go in
  ApiPublicPathRules.PublicLeaves.
