# Round 26 — v39g (cdcc41a4), change-type test c33-c40 (#10685-10692), 1 item/session

Method: `measure_transcript.py` (bytes/4 over the full input across turns). Real CI comes from `gh pr checks` (the copilot reviewer is ignored).

| id | category | transcript | PR | input | calls | Submits | real CI |
|---|---|---|---|---|---|---|---|
| c33 | UI (nav badge, bUnit) | bc-cd93cc16 | 10700 | 1,290.3k | 56 | 5 | GREEN (14 pass, 2 skipping) |
| c34 | multi-layer gRPC | bc-7484031b | 10698 | 175.0k | 19 | 3 | GREEN; no RESULT line (see below) |
| c35 | symptom bug | bc-fca75556 | 10697 | 129.8k | 20 | 2 | GREEN |
| c36 | docs-only | bc-c8d74180 | 10693 | 71.1k | 10 | 1 | GREEN (3 pass, 12 skipping: docs path); no RESULT line |
| c37 | test-only stabilization | bc-045a2175 | 10694 | 331.9k | 27 | 3 | GREEN |
| c38 | trap (correlation header) | bc-0809edd3 | 10696 | 216.5k | 21 | 2 | GREEN |
| c39 | vague | bc-790e7173 | 10695 | 264.0k | 28 | 4 | GREEN |
| c40 | multi-layer (language setting) | bc-636ef8bf | 10699 | 896.8k | 46 | 10 | GREEN |

- **Total:** 3,375.4k
- **Average:** 421.9k/item (round 25: 425.0k)
- **Median:** 240.2k (round 25: 212.9k)
- **Results:** 8/8 real GREEN; 6/8 replied with the RESULT line.

## c34 / c36: no RESULT line — harness/model, not a v39 regression
In both transcripts the last tool output is Wait/Submit printing `CHECKS=GREEN`, then `DONE. Your final reply is exactly the next line ...`, then `RESULT=GREEN PR=...`.
The final assistant message is **thinking-only with no text**, so the parent sees the previous narration ("Fixing the Qodana..." / "Editing the docs..."). The skill and scripts behaved correctly. This is the same pattern as round 22 c03 (v35g).
It happened in 3 of 40 runs across different versions, so it is not tied to v39g. The fix belongs in the harness: when the final message is empty, take the RESULT line from the last tool output.

## Evidence
- **Attribution:** reads before the first edit 33.8%, assistant args 27.3%, edits 15.7%, reads after the first edit 7.0%, submit 6.0%, wait-turn replay 10.2%, get_mcp_tools 2.5%.
- **NEXT budget hits:** 17 (round 25: 23).
- **Failed edits:** 1.
- **Shell reads:** c33 still made 15, hunting bUnit stubs (StubBus, IListener, IUserSession, RemotableRequestTests). c37 made 7.
- **c40 (10 Submits):** 2 local BUILD=FAIL and 5 local TEST=FAIL on new Settings/bUnit tests, then **2 CI REDs on Qodana RedundantTypeArgumentsOfMethod** in a test file. Each CI RED costs a Wait turn plus a resubmit.
- **c33:** 3 local BUILD=FAILs plus a risk fix, all in the new component and its bUnit tests.
