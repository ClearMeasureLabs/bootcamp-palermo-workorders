# Round 27 — v40g (a63f4718), c41-c48 (#10701-10708), expensive-type extension, 1 item/session

Method: `measure_transcript.py` (bytes/4 over the full input across turns). Real CI comes from `gh pr checks` (the copilot reviewer is ignored).

| id | category | transcript | PR | input | calls | Submits | real CI | CI REDs before green |
|---|---|---|---|---|---|---|---|---|
| c41 | UI + Playwright | bc-db451a7e | 10714 | 422.7k | 27 | 4 | GREEN (14 pass, 2 skipping) | 0 |
| c42 | UI + Playwright | bc-28abc65f | 10713 | 622.4k | 34 | 2 | GREEN | 1 (its new acceptance test) |
| c43 | UI + Playwright | bc-2a924ca3 | 10712 | 1,114.5k | 55 | 6 | GREEN | 3 (same new acceptance test ×3) |
| c44 | multi-layer (IsActive) | bc-6187eebd | 10715 | 5,973.9k | 178 | 15 | GREEN | 3 (existing acceptance tests ×2, integration ×1) |
| c45 | multi-layer (priority) | bc-d9870491 | 10711 | 2,877.1k | 89 | 4 | GREEN | 1 (integration) |
| c46 | multi-layer (CSV export) | bc-7b5aa68c | 10716 | 617.2k | 34 | 4 | GREEN | 0 |
| c47 | vague | bc-83d47d42 | 10709 | 79.0k | 15 | 2 | GREEN | 0 |
| c48 | symptom bug | bc-2ce511ac | 10710 | 236.0k | 25 | 3 | GREEN | 0 |

- **Total:** 11,942.9k
- **Average:** 1,492.9k/item; 852.7k without c44
- **Median:** 619.8k
- **Results:** 8/8 GREEN, and all 8 replied with the RESULT line.

## Against the same categories in the 40-item test (rounds 22-26)
| category | 40-item average | round 27 | items |
|---|---|---|---|
| UI with tests | 930.4k | **719.9k** (median 622.4k) | c41 422.7k, c42 622.4k, c43 1,114.5k |
| multi-layer | 692.0k | **3,156.1k** (median 2,877.1k) | c44 5,973.9k, c45 2,877.1k, c46 617.2k |
| vague | 347.5k | 79.0k | c47 |
| symptom bug | 211.3k | 236.0k | c48 |

The c4x multi-layer items are deliberately bigger than c02/c10/c18/c26/c34/c40. c44 and c45 each touch DB + EF + entity + manage page + search/MCP + tests, which puts them at the catalog's p90 size.

## Did the v40g changes work?
- **UI recipes:**
  - `ui-bunit` and `ui-playwright` fired on all of c41-c43.
  - `ui-bunit` also fired on c44, c45, c46 and c48 (they mention the manage/search page).
  - UI items fell from 930.4k to 719.9k on average.
  - Budget NEXT hits were 1 each on c41/c42 (5 on c43), against 5-9 for c25/c33.
- **Invoke-LocalInspect:** ran on every agent VM. There were 0 INSPECT=SKIPPED; the first Submit there installed the tool, and passing runs print nothing.
  - It printed `INSPECT=FIX` 3 times:
    - c41.
    - c43: `.CSharpErrors` in a new acceptance test. Submit does not build AcceptanceTests, so InspectCode caught compile errors there.
    - c44: ConditionalAccessQualifierIsNonNullableAccordingToAPIContract on WorkOrderManage.razor.cs:140.
  - **No CI RED came from Qodana in round 27.** Rounds 25-26 had 5+ Qodana CI REDs. The remaining CI REDs are all acceptance or integration tests.
  - A successful Submit now takes about 400-450 s, which includes InspectCode.

## Remaining cost drivers
- **CI acceptance REDs.** Submit never runs AcceptanceTests locally, so every Playwright failure costs a full CI cycle: about 20-25 min and 3 Wait calls, each replaying the context.
  - c43 failed the same new test 3 times.
  - c44 broke 2 existing acceptance flows (Complete, Shelve) through the active-employee filter.
- **c44 alone is 50% of the round:** 178 calls, 15 Submits. Before publishing it had 5 local BUILD/IT_BUILD fails, then RemotableRequestTests serialization of the new Employee property, plus the CI REDs above.
- **Wait polling:** each CI cycle is 2-3 Wait calls of up to 9 min.

## Next steps (ranked)
1. **Run affected acceptance tests locally in Submit.** When AcceptanceTests/UI.Shared/UI.Server changes, build AcceptanceTests and run the changed and new acceptance test classes, plus any classes mentioning changed pages, on SQLite with Playwright. This targets every CI RED in round 27: c42, c43 ×3, c44 ×2.
2. **Build AcceptanceTests in Submit** (cheap, ~20 s). This catches compile errors locally instead of via InspectCode/CI (c43).
3. **A single long Wait** (e.g. 25 min) instead of 3 polling calls, if the terminal cap allows. Otherwise make WAIT_CMD print nothing until it reaches a verdict, which cuts the replayed Wait turns per CI cycle.
