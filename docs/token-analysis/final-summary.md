# Token-reduction experiment: final summary

Repo ClearMeasureLabs/bootcamp-palermo-workorders, branch `analysis/token-reduction` (not merged). 2026-10-16, 21 runs (run 0 invalid plus runs 1-20).

## Method
Cursor cloud-agent transcripts carry **no token usage fields** (no input/cache/output counts). All figures are **estimates**: replayed context = sum over each tool-call turn of the transcript bytes so far, divided by 4. This approximates input + cache-read tokens. Output estimate = (thinking + text + tool args)/4. Treat them as relative measures, not billing numbers.

## Headline
| | Est. tokens | vs baseline |
|---|---|---|
| Baseline, run 1 (stock feature-loop skill, guard + test) | **~2.90M** (90 tool calls, ~35 min) | — |
| Best (run 7, canned seal script, 1 call) | ~0.27k | -99.99% |
| Median, runs 2-20 (all types, all paths) | ~10k | **-99.7%** |
| Median, general path v3/v4 with no type script (runs 11, 13-20) | ~9.9k | -99.7% |
| Worst later run (run 18, context gap) | ~251k | -91.3% |
| Mean, general path v3/v4 | ~39k | -98.7% |

Every run from 2 to 20 beat the 80% target. It held on the first two optimized runs (2: -98.4%, 3: -99.99%) and across 5 work types plus 9 general-path items. Wall clock fell from ~35 min to ~1-4 min.

## What changed
| Version | Change |
|---|---|
| v1 (run 2) | `scripts/agent/Get-AgentTarget.ps1`, `Invoke-AgentBuildTest.ps1` (quiet unit build, `-warnaserror`, `NuGetAudit=false`, filtered tests, summary-only output), `Publish-AgentPr.ps1` (branch from master, PR, issue comment); lean skill `.claude/skills/feature-loop-lean/SKILL.md` bans exploration, MCP listing, local Docker/PrivateBuild/acceptance runs, board moves without scope, and CI polling |
| v2 (run 3) | One-shot type scripts: `Invoke-GuardItem.ps1` (+ `Add-NullGuard.ps1`); AgentUrl optional (removed a 30 s MCP lookup) |
| Type recipes (runs 5, 7, 9) | `Get-TestContext.ps1`, `Complete-AgentItem.ps1`, `Invoke-SealItem.ps1`, `Invoke-ValidatorRuleItem.ps1` |
| General v3 (runs 11, 13-16) | `Get-ItemContext.ps1`: issue body + named files (compact) + matching tests + last test as anchor, in one call; recipe = context, edits, Complete |
| General v4 (runs 17-20) | Explicit paths win over prose type names; `.razor` + bUnit tests; outline trimming; comma-split `-Focus` |
| Box skill | `bootcamp-feature-loop/SKILL.md` gained a "Lean mode" section |

Where the baseline tokens went: local Docker + PrivateBuild + acceptance tests (~20 calls, the largest outputs), reading unrelated skills and scripts, target discovery by grep/glob, and CI polling after the PR. Cost scales with **turns × context**, so removing turns mattered more than shrinking any single output.

## Lessons by work type
- **Template-able edits (guard, seal, validator rule):** a canned script gets to 1 call and ~0.3k. Only worth building when the shape recurs.
- **Unit tests for an untested class:** a context script plus one write gets to 3 calls and ~3.3k.
- **Bug fix / small feature / mixed (no template):** the general context script gets 4-6 calls and ~4-12k, provided the issue names the files and the context covers what the edit needs.
- **Larger items:** cost depends on context completeness. Run 18 (the caller of the focus methods was missing) and run 20 (the bUnit fixture stubs were hidden by the outline) each fell back to deliberation and reads.
- **Issue quality is the main lever.** Issues that name exact files, the change and the tests let the agent skip discovery entirely.

## Remaining risks / next steps
- Local verification is unit tests only. DataAccess/integration, acceptance and CRAP gates run only in CI, and these PRs were closed before CI mattered. Real merges still need the CI gates (and they were never checked here).
- "No reads" is brittle when the context has a gap. v5 should: include in-file callers of focus members; include fixture helpers/stubs for test files; print focus code with original whitespace (4 of 8 anchors missed in run 18); make Complete-AgentItem print build errors instead of empty output; allow one targeted read when an anchor misses.
- The lean path skips the board walk and design journals. That's fine for experiments, but it's not a substitute for the full feature loop on real work.
- The scripts live only on `analysis/token-reduction`. Adopting them means a reviewed PR to master.
- Token numbers are estimates. Validate against billing/usage data if Cursor exposes it.

## Housekeeping notes
- **#9936** (run 0) is closed as not planned, but its **project card is still in Functional Testing**. It needs a manual reset: the box gh token lacks the `project` scope, so no board writes were possible from the analysis session.
- **Qodana checklists #9436-#9439 are already fixed on master** (see the README note). The committed `qodana.sarif.json` holds one non-mechanical result. The issue checklists should be reconciled.
- Experiment branches remain on the remote: a repo rule blocks branch deletion.

## All experiment items (every PR closed unmerged; every issue closed not planned)
| Run | Issue | PR | Agent |
|---|---|---|---|
| 0 | #9936 | — | bc-34ba5471 |
| 1 | #9937 | #9938 | bc-3101faa3 |
| 2 | #9939 | #9940 | bc-aea9f32c |
| 3 | #9941 | #9942 | bc-06208a2f |
| 4 | #9943 | #9944 | bc-23f09c58 |
| 5 | #9945 | #9946 | bc-caaf07fd |
| 6 | #9947 | #9948 | bc-54d95a8d |
| 7 | #9949 | #9951 | bc-43a2211a |
| 8 | #9950 | #9952 | bc-c199a536 |
| 9 | #9953 | #9956 | bc-30f95531 |
| 10 | #9954 | #9957 | bc-57c1aa8b |
| 11 | #9959 | #9964 | bc-5fa15c8b |
| 12 | #9955 | #9958 | bc-5761780f |
| 13 | #9960 | #9967 | bc-b478bff3 |
| 14 | #9961 | #9965 | bc-786f0852 |
| 15 | #9962 | #9966 | bc-48cab441 |
| 16 | #9963 | #9968 | bc-be8dc9ef |
| 17 | #9969 | #9974 | bc-603a4bd5 |
| 18 | #9970 | #9976 | bc-5a088dc6 |
| 19 | #9971 | #9973 | bc-98a70dad |
| 20 | #9972 | #9975 | bc-5dc7740e |
