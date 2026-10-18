# Round 23 — v36g, change-type test c09-c16 (#10636-10643), 1 item/session

Method: `measure_transcript.py` (bytes/4, cumulative input per tool-call turn). Real CI comes from `gh pr checks`, where every PR shows 14 pass and 2 skipping.

| id | category | transcript | PR | input | calls | Submits | real CI |
|---|---|---|---|---|---|---|---|
| c09 | UI + Playwright | bc-060e8a9b | 10651 | 1,930.8k | 66 | 2 | GREEN |
| c10 | multi-layer | bc-8b229508 | 10649 | 1,135.7k | 47 | 1 | GREEN |
| c11 | symptom bug | bc-9b5fef13 | 10650 | 355.5k | 30 | 6 | GREEN |
| c12 | build script (ps1 + quickstart) | bc-4fb91b52 | 10646 | 183.2k | 20 | 1 | GREEN (PR files: build.ps1, PrivateBuild.ps1, src/quickstart.md) |
| c13 | quality refactor | bc-0c53c369 | 10645 | 203.9k | 20 | 2 | GREEN |
| c14 | test-only | bc-e0f8a4d2 | 10644 | 113.8k | 16 | 1 | GREEN |
| c15 | trap | bc-80326973 | 10648 | 63.9k | 11 | 1 | GREEN |
| c16 | UI help text | bc-ae2458fd | 10647 | 179.7k | 22 | 2 | GREEN |

- **Total:** 4,166.7k
- **Average:** 520.8k/item; without c09 it is 319.4k
- **Median:** 193.6k (round 22: average 286.5k, median 277.9k)
- **Output:** 3.55k/item
- **Results:** 8/8 GREEN, and every agent ended with the RESULT line.

## By category (round 22 → round 23)
UI 810.9k → 1,930.8k (c09) and 179.7k (c16) · multi-layer 258.6k → 1,135.7k · bug 334.7k → 355.5k · CI/build 44.8k (no PR) → 183.2k (PR, GREEN) · quality 382.7k → 203.9k · test-only 85.4k → 113.8k · trap 297.1k → 63.9k.

## Did v36g work?
- **Probe edits stopped.** I count failed search_replace calls (no result type). Round 22 had 75 of them, replaying 735k (c01 43, c05 14, c03 13, c02 5). Round 23 had 2 (c09), replaying 83k. *Correction to round-22.md:* its "16 failed" figure counted successes. The real c01 number was 43 failed edits.
- **Publishing root build scripts works.** c12's PR carries build.ps1, PrivateBuild.ps1 and src/quickstart.md. Because src changed, the full .NET path ran, which was correct. NON_CODE was not exercised this round.
- **get_mcp_tools is still called:** 8/8 agents, 10 calls in total, about 1.9% of input.
- **New waste: the read budget is too small for big items, so agents worked around it.**
  - c10 made 16 single-range Get-Context calls and **rewrote `/tmp/agent-getcontext-count` (`echo 3 >`) 13 times** to reset the budget.
  - c09 fell back to 27 grep/read_file calls, which GENERAL.md forbids.
  - Attribution over all 8: reads/search before the first edit 58.5%, assistant args 20.1%, edits 8.0%, wait-turn replay 6.5%.
  - In c09/c10 the context only reached 214KB and 159KB. The cost came from 66 and 47 turns each replaying it.

## v37g
- **Get-Context:** after the 3 normal calls, `-File 'a:1-40,b:80-140'` batches up to 5 ranges (80 lines each) in one call. That is 4 such calls and 800 lines in total. More ranges per call means fewer turns. The LIMIT text forbids grep/read tools, touching /tmp/agent-* and probing with edits.
- **GENERAL.md v37g (3,205 bytes):** the read rule now says ONE batched ranged call, and never touch /tmp/agent-*.
