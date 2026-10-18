# Change-type test plan: c01-c40 (v35g, 1 item/session, rounds 22-26)

Source: the gap analysis in `change-type-catalog-2026.md` (248e3836), approved by Jeffrey on 2026-10-18. Skill: v35g (62473d92). Measurement is unchanged (`measure_transcript.py`, bytes/4, cumulative input).
Issues are titled `token-exp p6 cNN: ...`, branches are `cursor/token-exp-p6-cNN-v35` from the analysis/token-reduction tip, and every PR is closed unmerged.

Rules for the items:
- Every item is unseen. None repeats a q/r/s/t/u kind.
- Every item can go green in CI.
- **Bug** (symptom-only) and **Vague** items name no files, types or paths.
- **Trap** items are realistic changes whose obvious implementation fails a real gate (CRAP gate 6, Qodana, or an acceptance test). The agent can still fix them within the PR.
- Bug items for rounds 23-26 are written from the code as of 62473d92. At each round's prep they are re-checked to confirm the symptom still reproduces, and replaced with the same category if it doesn't.

Categories, matching the catalog's recommended sets:
- **UI**: UI with bUnit and/or Playwright (6)
- **ML**: multi-layer, 3+ layers (6)
- **Bug**: symptom-only report (6)
- **CI**: CI, build or docs (6)
- **Qual**: quality batch remediation (4)
- **Test**: test-only or stabilization (4)
- **Trap**: designed to hit CI RED first (4)
- **Vague**: no files named (4)

## Items
| id | cat | title | what it proves |
|---|---|---|---|
| c01 | UI | Search: clicking a result's status badge filters the search to that status (bUnit + Playwright) | UI interaction with an acceptance test, which makes CI's acceptance job part of GREEN |
| c02 | ML | Employee optional JobTitle (DbUp script, EF map, entity, assignee dropdown text, MCP get-employee) | cost on 5+ layers, DB migration plus UI plus MCP |
| c03 | Bug | "Search results header says '1 work orders'" (symptom only) | -Find locates UI text with no file hint |
| c04 | CI | secret-scan and render-diagrams workflows: timeout-minutes plus a concurrency group that cancels superseded runs | workflow-only change; Submit fast path with no .NET diff |
| c05 | Qual | Culture-safe strings/dates: ToLower/ToUpper → invariant or ordinal compare; DateTime.Now → UtcNow (about 10 sites, 6 files) | multi-file batch under the read LIMIT |
| c06 | Test | Acceptance test for GET /_version (only integration-tested today) | TEST SETUP path with no production change |
| c07 | Trap | Search: colour-coded status badge per status (5 statuses + None + unknown); the natural switch has CC 7, over CRAP gate 6 | RED→fix loop stays bounded; the risk check should warn before push |
| c08 | Vague | "New volunteers say the home page doesn't tell them what to do first. Make it more helpful." | cost on under-specified work |
| c09 | UI | Settings: "Compact tables" switch persisted in localStorage, applied to the search table (bUnit + Playwright). Replaced the theme toggle: dark mode already exists | stateful setting plus acceptance |
| c10 | ML | WorkOrder optional EstimatedHours (decimal(5,2)): DbUp, EF, entity validation 0-999, manage form input, MCP save-work-order arg | multi-layer with a validator |
| c11 | Bug | "Description characters-remaining count only updates after I click elsewhere, not while typing". Replaced the trailing-space item, which did not reproduce; verified: the binding is onchange and acceptance tests blur first | symptom→Blazor binding |
| c12 | CI | build.ps1/BuildFunctions.ps1: add -SkipAcceptance switch documented in quickstart.md | ps1 build-script change, no RECIPE exemplar |
| c13 | Qual | Refactor search sorting into one column→key-selector map (CC ≤4), tests for every column in both directions. Re-scoped: no UI.Shared method exceeds the CRAP gate today | multi-method refactor batch |
| c14 | Test | Attachment integration tests: no cross-work-order leakage, multiple attachments keep their metadata, cascade on delete. Re-scoped: add/query are already tested | integration-only TEST SETUP |
| c15 | Trap | MCP list-employees optional `role` filter; the natural `role?.Trim() ?? ""` on a non-nullable parameter trips Qodana | v34 null rule under MCP |
| c16 | UI | Manage page: tooltip/help text for Instructions and Room Number fields (bUnit only) | UI-text cross-cut (6.9% of master) |
| c17 | UI | Search: remember last filters in the session and restore them when returning to the page (bUnit + Playwright) | stateful UI |
| c18 | ML | New MCP tool `list-overdue-work-orders` (Core query, DataAccess handler, MCP tool, integration and acceptance tests) | new MCP tool end to end |
| c19 | Bug | "After cancelling a work order the assignee still sees it under My Work Orders" (re-check at prep) | symptom→query handler |
| c20 | CI | Docs: docs/agent-pr-checklist.md (what an agent PR must pass), linked from docs/ai-workers.md | docs-only short circuit |
| c21 | Qual | Qodana RedundantUsingDirective / unused private members sweep in UI.Server (10-15 findings) | many-file mechanical batch |
| c22 | Test | Playwright acceptance test: Overdue-only toggle shows only overdue rows | acceptance-only item |
| c23 | Vague | "Dispatchers want to see at a glance who is overloaded." | vague, multi-layer-ish |
| c24 | Bug | "Health page says Healthy even when the LLM is turned off in settings" (re-check at prep) | symptom→health check |
| c25 | UI | Work order manage page: character counter for Title with a warning colour near the limit (bUnit + Playwright) | UI with validation feedback |
| c26 | ML | LLM: "Summarize work order" button on the manage page calling LlmGateway, with a stub chat client in tests | LLM feature across LlmGateway, UI.Server and UI.Shared |
| c27 | Bug | "Exporting search results to the clipboard puts the newest work order last even when I sorted newest first" (re-check at prep) | symptom→UI sort/order |
| c28 | CI | build.yml: upload unit-test TRX results as an artifact with 7-day retention | workflow YAML in the main gate |
| c29 | Qual | Add CancellationToken pass-through to every async DataAccess handler that drops it (batch) | signature batch across layers |
| c30 | Trap | Acceptance test for search sort by Room; the natural string sort fails for room numbers like "9" vs "10" (realistic RED) | broken-acceptance→fix loop |
| c31 | Vague | "The work order screen feels slow and cluttered on a phone." | vague UI/CSS |
| c32 | CI | PrivateBuild.ps1: print the elapsed time of each step and the total at the end | ps1 change, local-only validation |
| c33 | UI | Nav: active-page highlight plus aria-current="page" on the current link (bUnit) | a11y UI |
| c34 | ML | gRPC: GetWorkOrder returns due_date and urgency fields (proto, service, mapper, unit test) | gRPC round 2 with the regen script |
| c35 | Bug | "Work order numbers sometimes clash when two people create work orders at the same moment" (re-check at prep) | symptom→generator, concurrency reasoning |
| c36 | CI | Docs: update docs/environments.md and quickstart.md with the SQLite-only local run path | docs-only, 2 files |
| c37 | Test | Stabilize the slowest/flakiest acceptance test from the last 30 days of CI (chosen at prep from failed runs) | stabilization |
| c38 | Trap | Worker: add a retry-count header to the outgoing message; the natural object setup trips UseObjectOrCollectionInitializer | v35 risk rule on a new area |
| c39 | Vague | "Facilities asked for a weekly summary of what got done." | vague, may span LLM, Worker or UI |
| c40 | ML | Employee PreferredLanguage editable on Settings, persisted (EF, handler, UI, tests), used as the speech-voice default | multi-layer UI and persistence |

## Rounds (each has UI, ML, Bug and CI, plus 4 of Qual/Test/Trap/Vague/extra)
| round | items |
|---|---|
| 22 | c01 UI, c02 ML, c03 Bug, c04 CI, c05 Qual, c06 Test, c07 Trap, c08 Vague |
| 23 | c09 UI, c10 ML, c11 Bug, c12 CI, c13 Qual, c14 Test, c15 Trap, c16 UI |
| 24 | c17 UI, c18 ML, c19 Bug, c20 CI, c21 Qual, c22 Test, c23 Vague, c24 Bug |
| 25 | c25 UI, c26 ML, c27 Bug, c28 CI, c29 Qual, c30 Trap, c31 Vague, c32 CI |
| 26 | c33 UI, c34 ML, c35 Bug, c36 CI, c37 Test, c38 Trap, c39 Vague, c40 ML |

## Results
| round | skill | per item (input) | notes |
|---|---|---|---|
| 22 | v35g | c01 UI 810.9k, c02 ML 258.6k, c03 Bug 334.7k, c04 CI 44.8k (no PR: script), c05 Qual 382.7k, c06 Test 85.4k, c07 Trap 297.1k, c08 Vague 77.9k; avg 286.5k, median 277.9k | see round-22.md; v36g fixes the non-code publish path and LIMIT probing |

Round 23 prep (2026-10-18): issues #10636-10643, v36g, branches cursor/token-exp-p6-cNN-v36.
