# Round 24 — v37g (21a26fde), change-type test c17-c24 (#10653-10660), 1 item/session

Method: `measure_transcript.py` (bytes/4 over the full input across turns). Real CI comes from `gh pr checks` (the copilot reviewer is ignored).

| id | category | transcript | PR | input | calls | Submits | real CI |
|---|---|---|---|---|---|---|---|
| c17 | UI + Playwright | bc-eadde05a | none | 79.8k | 11 | 0 | no PR: replied SCRIPT_FAIL on the LIMIT text |
| c18 | multi-layer (new MCP tool) | bc-7304d1ca | 10667 | 458.2k | 35 | 3 | GREEN (14 pass, 2 skipping) |
| c19 | symptom bug | bc-f245f34f | 10665 | 100.2k | 14 | 1 | GREEN |
| c20 | docs-only | bc-ed45e856 | 10662 | 18.5k | 6 | 1 | GREEN (3 pass, 12 skipping: the docs-only path skips most jobs) |
| c21 | quality (seal 12 handlers) | bc-abb64c09 | 10664 | 230.8k | 26 | 1 | GREEN |
| c22 | test-only (Playwright) | bc-9350e4a1 | 10663 | 38.7k | 8 | 1 | GREEN |
| c23 | vague | bc-ee2f418a | 10668 | 896.7k | 58 | 2 | GREEN |
| c24 | symptom bug | bc-37cd604f | 10666 | 305.6k | 29 | 3 | GREEN |

- **Total:** 2,128.5k
- **Average:** 266.1k/item (round 23: 520.8k; round 22: 286.5k)
- **Median:** 165.5k (round 23: 193.6k)
- **Results:** 7/8 GREEN. c17 stopped early.

**c04 workflow probe (separate; not in the average):** bc-61b815c0, PR 10661, 18.3k, 7 calls, GREEN (15 pass, 2 skipping). Cloud agents **can** push `.github/workflows` changes, and v36's non-code publish path works end to end. c20 (docs-only) also went through NON_CODE: 6 calls, 18.5k.

## Waste
- Attribution: reads/search before the first edit 35.9%, assistant args 25.0%, edits 19.0%, skill file 7.8%, get_mcp_tools 5.6% (still 8/8), wait-turn replay 8.1%.
- **v37g read-budget bug:** the line counter was stored ×10, so the 800-line budget ran out after about 80 lines. Agents got the LIMIT text after their first batched read:
  - c17 got it 4 times and then replied `SCRIPT_FAIL: LIMIT: ...`, so no PR was made.
  - c18 got it 14 times and kept retrying (`sleep 2; ...`), which added about 30 turns to a 458k run.
- Counter tampering stopped (0 runs touched /tmp/agent-*), as did grep/read_file fallbacks (0 besides GENERAL.md).
- c23 (vague): 58 calls and 9 failed edits. An under-specified item is the most expensive kind.

## v38g
- **Get-Context:** the line budget is stored as plain lines (bug fixed; tested: 4 batched calls of 160 lines, then the budget message).
- **Budget message:** now `NEXT: read budget used up (normal, not an error; never reply SCRIPT_FAIL for this). Edit now ...`, exit 0. There is no `LIMIT:` text any more.
- **GENERAL.md v38g (3,288 bytes):** the SCRIPT_FAIL rule says "a `NEXT:` line is an instruction, never a failure", and the read rule says "then the NEXT line means edit".
