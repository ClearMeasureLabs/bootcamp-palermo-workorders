# Round 25 — v38g (8149b76f), change-type test c25-c32 (#10669-10676), 1 item/session

Method: `measure_transcript.py` (bytes/4 over the full input across turns). Real CI comes from `gh pr checks` (the copilot reviewer is ignored).

| id | category | transcript | PR | input | calls | Submits | real CI |
|---|---|---|---|---|---|---|---|
| c25 | UI + Playwright | bc-2dcadc20 | 10677 | 1,291.2k | 52 | 4 | GREEN (14 pass, 2 skipping) |
| c26 | multi-layer LLM | bc-d781f521 | 10684 | 1,228.0k | 50 | 5 | GREEN |
| c27 | symptom bug | bc-23dace90 | 10680 | 42.0k | 9 | 1 | GREEN |
| c28 | CI workflow | bc-0a7e179e | 10678 | 13.7k | 6 | 1 | GREEN (15 pass, 2 skipping) |
| c29 | quality (cancellation tokens) | bc-32b17589 | 10682 | 302.0k | 26 | 3 | GREEN |
| c30 | trap (options) | bc-e3fe929f | 10683 | 274.2k | 24 | 3 | GREEN |
| c31 | vague | bc-63ee80af | 10681 | 151.5k | 17 | 1 | GREEN |
| c32 | build script | bc-839d09c3 | 10679 | 97.3k | 12 | 1 | GREEN |

- **Total:** 3,400.0k
- **Average:** 425.0k/item
- **Median:** 212.9k
- **Results:** 8/8 GREEN, and every agent ended with the RESULT line.

| round | skill | average | median |
|---|---|---|---|
| 22 | v35g | 286.5k | 277.9k |
| 23 | v36g | 520.8k | 193.6k |
| 24 | v37g | 266.1k | 165.5k |
| 25 | v38g | 425.0k | 212.9k |

c25 and c26 are 74% of the round. Without them, the other 6 average 146.8k.

## Evidence
- **Attribution:** reads/search before the first edit 55.3%, assistant args 20.4%, edits 12.3%, wait-turn replay 12.6%, get_mcp_tools 2.5%.
- **Refused reads (main driver).** After the 3 normal calls v38g only accepted ranged `-File`, so `-Find` got the NEXT message.
  - c25 hit NEXT 8 times, c26 9 times, plus c29 3, c30 2 and c31 1.
  - c25 then fell back to 8 shell reads (Select-String/Get-Content) and wrote a python edit script.
  - Every refused call still replays the whole context.
- **Failed edits:** 0 in all 8 runs, including the vague c31 (151.5k, 1 Submit). The v36/v37 read changes held.
- **get_mcp_tools:** 9 calls in 8 runs, always the second call, right after GENERAL.md is read. A skill rule doesn't fix it (it is already in the Never list), so it looks driven by the harness/system prompt. It costs about 10k per run.
- **CI RED loops:** c25 and c26 went RED after publish (a Qodana primary-constructor finding, then an existing integration test). Each RED adds a Wait turn plus a Submit, which accounts for the 12.6% wait replay.
- **Plan error (mine):** c25 said the DB Title column is nvarchar(200), but `024_ExtendWorkOrderTitleLength.sql` widened it to 300. The agent implemented 200 as written, then had to fix the integration test that asserts 300. Lesson for prep: check every later migration, not only the CREATE script.

## v39g
- **Get-Context:** the 4 extra calls accept `-Find` as well as batched ranges. The NEXT message now says "Do not call Get-Context again". Tested: call 4 `-Find` is served, an unranged `-File` is refused, calls 5-7 are served and call 8 gets NEXT.
- **GENERAL.md v39g (3,266 bytes):** the read rule is "2 extra calls, then 4 more as -Find or batched ranges; after the NEXT line never call it again: edit".
