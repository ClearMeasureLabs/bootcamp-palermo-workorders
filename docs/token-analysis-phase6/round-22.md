# Round 22 — v35g, change-type test c01-c08 (#10621-10628), 1 item/session

Method: `measure_transcript.py` (bytes/4, cumulative input per tool-call turn). Real CI comes from `gh pr checks`.

| id | category | transcript | PR | input | out | calls | Submits | real CI |
|---|---|---|---|---|---|---|---|---|
| c01 | UI + Playwright | bc-fd3116e1 | 10635 | 810.9k | 4.00k | 71 | 5 | GREEN (14 pass, 2 skipping) |
| c02 | multi-layer | bc-4f4a65a5 | 10632 | 258.6k | 2.50k | 28 | 3 | GREEN |
| c03 | symptom bug | bc-3201f4cc | 10633 | 334.7k | 3.53k | 37 | 1 | GREEN (the agent got RESULT=GREEN, then its final reply was empty) |
| c04 | CI workflow | bc-5b086e25 | none | 44.8k | 1.65k | 12 | 2 | no PR: SCRIPT_FAIL NO_CHANGES (script bug) |
| c05 | quality batch | bc-08568961 | 10631 | 382.7k | 2.39k | 39 | 2 | GREEN |
| c06 | test-only | bc-f519a7b2 | 10629 | 85.4k | 1.50k | 11 | 2 | GREEN |
| c07 | CRAP trap | bc-b9c8e326 | 10634 | 297.1k | 3.19k | 29 | 6 | GREEN |
| c08 | vague | bc-21af88ad | 10630 | 77.9k | 2.80k | 14 | 2 | GREEN |

- **Total:** 2,292.2k
- **Average:** 286.5k/item
- **Median:** 277.9k
- **Output:** 2.70k/item
- **Comparison:** round 21 was 52.0k (43.6k without the false RED); round 20 arm A was 34.5k.

The c-set is intentionally larger and less specified than the q-u sets, so the averages are not comparable one to one. The waste causes below are fixable, though.

## Waste (attribute_transcript_tokens.py over all 8)
| bucket | share |
|---|---|
| assistant messages / tool args replayed (mostly big search_replace payloads) | 29.7% |
| reads/search before the first edit | 27.7% |
| edits | 21.2% |
| skill file | 8.8% |
| get_mcp_tools (7/8 agents called it despite the Never rule) | 4.2% |
| wait-call turn replay | 7.9% |

0. *Correction (round 23): the failed-edit counts below were inverted; the real counts are c01 43, c05 14, c03 13, c02 5 failed edits (735k).*
1. **c04 NO_CHANGES was a script bug, not the agent.** The agent correctly edited both workflows (verified with Select-String), but `Publish-AgentPr.ps1` only diffs `src/`, so it printed "no src changes". Submit had also run the full .NET risk/build/test cycle for a YAML-only change.
2. **c01 LIMIT → edit probing.** After the 3-call Get-Context LIMIT ("never read files via shell"), the agent said "probing edit targets via StrReplace fuzzy matches" and made 59 search_replace calls. 16 of them failed, and those turns alone replayed 237k. Two Submits then failed on build errors in the broken test file, and one on a namespace-qualified name.
3. **c03 read via python.** After 5 Get-Context calls it read 8 files with `python -c` (about 35KB, re-sent every later turn), and 6 of its edits failed (68k). Its final reply was empty although Wait printed RESULT=GREEN, so the parent saw no RESULT line.
4. **c05:** 14 failed edits (147k), which is expected friction for a 10-site batch.
5. **c07:** 6 Submits. The trap worked: the agent fixed a FromCode exception on unknown statuses and an existing CSS assertion test, then went GREEN.

## By category
| category | item | input | verdict |
|---|---|---|---|
| UI + Playwright | c01 | 810.9k | worst; LIMIT-driven probing |
| multi-layer | c02 | 258.6k | 5 layers, 3 Submits (a repeated risk hint) |
| symptom bug | c03 | 334.7k | found the cause; python reads |
| CI workflow | c04 | 44.8k | cheap but blocked by the script |
| quality batch | c05 | 382.7k | batch edit friction |
| test-only | c06 | 85.4k | fine |
| trap | c07 | 297.1k | bounded RED→fix loop |
| vague | c08 | 77.9k | cheap and green |

## v36g
- **Publish-AgentPr:** publishes `src`, `.github`, `docs` (excluding docs/token-analysis*) and root `*.ps1`; the guard allows the same set.
- **Submit:** when src/ is unchanged and only .github/docs/root ps1 changed (NON_CODE), it skips risk/build/unit/IT. It parse-checks .ps1 with the PowerShell parser and YAML with python3 yaml (if available), then publishes. Tested: broken YAML gives SYNTAX_FAIL; a valid yml+ps1 change gives NON_CODE, VERIFY=OK.
- **Get-Context:**
  - The issue call prints named non-src .yml/.ps1/.md files in full and says NON_CODE.
  - `-Find` also searches css/yml/ps1, .github and root ps1.
  - After the 3-call LIMIT it allows 3 ranged reads (`-File path:a-b`, max 60 lines) instead of nothing, so agents have a sanctioned alternative to edit-probing and shell reads.
- **GENERAL.md v36g (3,129 bytes, +156):** docs/ is allowed when the issue names it; it states the ranged-read rule, forbids probing with edits, and adds the non-code line.
- **Not fixed by script:** c03's empty final reply; the existing GREEN-stop rule already tells the agent to reply with the RESULT line.
