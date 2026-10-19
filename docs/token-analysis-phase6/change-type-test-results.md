# Change-type test results: c01-c40 (rounds 22-26, v35g → v39g)

40 items in 8 categories, weighted toward master's 2026 change mix (`change-type-catalog-2026.md`), run 1 item/session.
- **Measurement:** `measure_transcript.py`, bytes/4 summed over every turn's full input.
- **Pass:**
  - **PR green** means a PR exists and `gh pr checks` is all pass or skipping (copilot ignored).
  - **Clean** means the agent also replied with the RESULT line.
- **Caveat:** the skill changed between rounds (v35→v39), so category numbers mix versions. Each category has 4-6 items.

Totals for the 40 items: 15,362k; average 384.1k; median 223.7k. PR green 38/40; clean 35/40.

## Per category
| category | n | average | median | range | PR green | clean |
|---|---|---|---|---|---|---|
| UI (bUnit/Playwright) | 6 | 930.4k | 1,050.6k | 79.8-1,930.8k | 5/6 | 5/6 |
| Multi-layer (3+ layers) | 6 | 692.0k | 677.5k | 175.0-1,228.0k | 6/6 | 5/6 |
| Vague ask | 4 | 347.5k | 207.8k | 77.9-896.7k | 4/4 | 4/4 |
| Quality batch | 4 | 279.9k | 266.4k | 203.9-382.7k | 4/4 | 4/4 |
| Trap (designed RED) | 4 | 212.9k | 245.3k | 63.9-297.1k | 4/4 | 4/4 |
| Symptom-only bug | 6 | 211.3k | 217.7k | 42.0-355.5k | 6/6 | 5/6 |
| Test-only | 4 | 142.4k | 99.6k | 38.7-331.9k | 4/4 | 4/4 |
| CI / build / docs | 6 | 71.4k | 57.9k | 13.7-183.2k | 5/6 | 4/6 |

Items behind the failures:
- **No PR:** c04 (v35 published src only) and c17 (v37 budget bug → SCRIPT_FAIL).
- **Empty final reply despite GREEN:** c03, c34, c36.

## Cheap vs expensive, and why
- **Cheap:**
  - **CI/build/docs (median 58k):** since v36 there is no .NET build for non-code changes and Get-Context prints the named files. Workflow/docs items finish in 6-10 calls (c20 18.5k, c28 13.7k, the c04 probe 18.3k).
  - **Test-only (median 100k):** one test file plus an existing exemplar.
  - **Symptom bugs (median 218k):** `-Find` locates the cause without file names. The fixes are 1-2 files.
- **Middle:**
  - **Traps (213k):** the RED→fix loop stayed bounded at 2-3 Submits. CRAP/Qodana risk hints often catch the trap before push.
  - **Quality batches (280k):** many small edits; the cost scales with the number of sites.
- **Expensive:**
  - **UI with Playwright or new bUnit components:** c01 811k, c09 1,931k, c25 1,291k, c33 1,290k. The bUnit-only help-text item c16 cost just 180k. These items need the page, code-behind, CSS, the Elements enum, the bUnit harness (StubBus, IUserSession, IListener stubs) and an acceptance-test exemplar.
    - Get-Context's RECIPE doesn't supply the test harness, so agents read 10-25 files over 50+ turns. Every turn replays the growing context.
    - Local BUILD/TEST failures on new bUnit tests add 3-7 Submits.
  - **Multi-layer (692k):** DB, EF, entity, UI and MCP each need a read. Item c40 had 10 Submits, 2 of them CI REDs on Qodana findings in test files that the local risk check does not cover.
  - **Vague asks:** bimodal. c08 78k vs c23 897k: the cost depends on how big a feature the agent decides to build.

## What each version fixed, and whether it held
| version | fix | held? |
|---|---|---|
| v35g | Wait-PrChecks ignores the copilot reviewer check (8/8 false RED in round 21); object-initializer risk rule | **Held.** No false RED in rounds 22-26. |
| v36g | non-code publish/submit path (.github, docs, root ps1); ranged reads after the read budget instead of nothing | **Held.** Workflow and docs PRs published (c04 probe, c20, c28, c36). Failed probe edits went 75 → 2 (round 23) → 9 → 0 → 1. |
| v37g | batched multi-range `-File`; no counter tampering | Partly. Tampering and grep/read fallbacks stopped, **but a ×10 line-budget bug** made c17 SCRIPT_FAIL and c18 loop (round 24). |
| v38g | budget bug fixed; the budget message is a `NEXT:` instruction, not `LIMIT:` | **Held.** 0 SCRIPT_FAIL in rounds 25-26. |
| v39g | `-Find` allowed in the 4 extra calls; NEXT says stop calling | Mostly. NEXT hits went 23 → 17. c33 and c37 still shell-read while hunting test stubs. |

## Main remaining cost drivers (round 26 attribution)
- **Reads before the first edit: 34-58% of input per round.** This is concentrated in UI and multi-layer items, where 50+ turns replay the context.
- **Assistant args/edits: 35-43%.** Large edit payloads are replayed on every later turn.
- **CI RED loops: wait-turn replay is 6.5-12.6% per round, plus the resubmit turns.** They are mostly Qodana findings that local checks do not catch (round 26: RedundantTypeArgumentsOfMethod, null-coalescing on a non-nullable).
- **Small fixed costs:** get_mcp_tools at 2-6% (harness-driven, always the second call) and the skill file at 5-9%.

## Next optimizations, ranked by expected savings
1. **UI/bUnit test-harness RECIPE in Get-Context.** For UI.Shared items, the first call should print the bUnit setup exemplar (TestContext, StubBus, IUserSession/IListener stubs, Elements pattern) and, when the issue says Playwright/acceptance, one acceptance-test exemplar (AcceptanceTestBase usage).
   - UI items were 36% of all 40-item input (5,582k of 15,362k) at about 930k each, against 180k for c16, which needed no new harness.
   - Bringing the 4 Playwright/component items toward the multi-layer median would save about 2-3M, the largest single lever.
2. **Run JetBrains InspectCode (Qodana's engine) locally on changed files in Submit.** Use the repo's .DotSettings and fail with the same QODANA lines.
   - CI REDs from Qodana caused about 2 extra Wait+Submit turns each (c40 x2, c25, c26, c34, round-21 u04).
   - This removes the 10% wait replay and the 7-minute CI cycles behind it. It costs about 1-2 minutes per Submit.
3. **First-call breadth for multi-layer items.** When the issue spans 3+ layers, Get-Context should print every EXISTING edit target's relevant region (mapping, entity, handler, page, MCP tool) in call 1, within a size cap.
   - The goal is to cut the 10-16 follow-up reads seen in c10, c18, c26 and c40, which drive the multi-layer average of 692k.

Harness item (no token effect, but it affects the pass rate): when the final assistant message is empty, read the RESULT line from the last tool output (c03, c34, c36).
