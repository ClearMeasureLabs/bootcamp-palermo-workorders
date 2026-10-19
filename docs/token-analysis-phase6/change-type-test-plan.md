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
| c19 | Bug | "Cancelled work orders stay in My Work Orders" (verified: the side panel queries by Creator with no status filter, and the change event adds any changed order) | symptom→component/query |
| c20 | CI | Docs: docs/agent-pr-checklist.md (what an agent PR must pass), linked from docs/ai-workers.md | docs-only short circuit |
| c21 | Qual | Seal the 12 request/notification handler classes in DataAccess/Handlers and UI/Server/Handlers. Re-scoped: Qodana already gates unused usings, so the planned sweep had no findings | many-file mechanical batch |
| c22 | Test | Playwright acceptance test for the FetchData forecast page (5 rows). Replaced: an Overdue-only acceptance test already exists | acceptance-only item |
| c23 | Vague | "Dispatchers want to see at a glance who is overloaded." | vague, multi-layer-ish |
| c24 | Bug | "Sorting by Room puts Room 10 before Room 9" (verified: CurrentCultureIgnoreCase string sort). Replaced: the health/LLM-off item did not show a clear bug; c30 gets a new trap at round 25 prep | symptom→sort comparer |
| c25 | UI | Title max length 200 (matching DB nvarchar(200); EF says 300) plus a characters-remaining counter with a warning at ≤20 (bUnit + Playwright) | UI + validation + mapping |
| c26 | ML | LLM: "Summarize work order" button on the manage page calling LlmGateway, with a stub chat client in tests | LLM feature across LlmGateway, UI.Server and UI.Shared |
| c27 | Bug | "Assistant fails on 'in progress' status; 'InProgress' works" (verified: WorkOrderStatus.FromKey matches Key only and throws on the friendly name). Replaced the clipboard item: that feature does not exist | symptom→MCP/status parsing |
| c28 | CI | render-diagrams.yml: workflow_dispatch plus a rendered-SVG artifact on failure. Re-scoped: build.yml already uploads TRX | workflow YAML (agent push confirmed by the c04 probe) |
| c29 | Qual | Pass CancellationToken through DataAccess handlers that drop it (GetWorkOrderAsync, PublishCreatedEventAsync, ...) | signature batch |
| c30 | Trap | MCP list-work-orders cap via a new options class (McpServer:MaxListResults, default 100, truncated flag). The natural options class trips Qodana AutoPropertyCanBeMadeGetOnly; the static MCP tool needs options injection. Replaced the room-sort trap, which moved to c24 | options/Qodana trap in MCP |
| c31 | Vague | "The work order screen feels slow and cluttered on a phone." | vague UI/CSS |
| c32 | CI | build.ps1: per-step "took mm:ss" timings (the total already exists) | ps1 change |
| c33 | UI | Nav badge: count of my Assigned/In Progress work orders, refreshed on the change event (bUnit). Replaced: NavLink already renders the active class and aria-current | component + event |
| c34 | ML | gRPC WorkOrder gets due_date_urgency = 12 from GetDueDateUrgency(). Re-scoped: due_date already exists as field 11 | gRPC round 2 with the regen script |
| c35 | Bug | "My Work Orders briefly shows other people's work orders" (verified: MyWorkOrders.Handle adds every changed order with no creator check). Replaced the number-clash item: a 7-char GUID prefix clash is not reproducible | symptom→event handler |
| c36 | CI | Docs: SQLite local section in docs/environments.md plus a quickstart pointer (README has the source notes; environments.md has none) | docs-only (NON_CODE) |
| c37 | Test | Make the login blink-pulse acceptance test deterministic (it samples opacity with 30 fixed 100 ms sleeps); use the Web Animations API instead. Chosen from code: recent build failures are all agent branches, so CI history gives no flake signal | stabilization |
| c38 | Trap | Worker RemotableBus: X-Correlation-Id header on every POST. The natural `var req = new HttpRequestMessage(...); req.Content = ...;` trips UseObjectOrCollectionInitializer (the v35 risk rule should warn). Re-scoped: the Worker has no message headers; it posts over HTTP | v35 rule in a new area |
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
| 23 | v36g | c09 UI 1,930.8k, c10 ML 1,135.7k, c11 Bug 355.5k, c12 CI 183.2k, c13 Qual 203.9k, c14 Test 113.8k, c15 Trap 63.9k, c16 UI 179.7k; avg 520.8k, median 193.6k | probe edits 75→2; the read budget was gamed (c10 counter rewrites, c09 grep/read); v37g adds batched ranges |
| 24 | v37g | c17 UI 79.8k (no PR), c18 ML 458.2k, c19 Bug 100.2k, c20 Docs 18.5k, c21 Qual 230.8k, c22 Test 38.7k, c23 Vague 896.7k, c24 Bug 305.6k; avg 266.1k, median 165.5k; c04 workflow probe 18.3k GREEN | v37 budget ×10 bug → LIMIT loops; v38g fixes it and makes the message NEXT: |
| 25 | v38g | c25 UI 1,291.2k, c26 ML 1,228.0k, c27 Bug 42.0k, c28 CI 13.7k, c29 Qual 302.0k, c30 Trap 274.2k, c31 Vague 151.5k, c32 CI 97.3k; avg 425.0k, median 212.9k | refused -Find calls after the budget (c25 8, c26 9); 0 failed edits; v39g allows -Find in the extra calls |
| 26 | v39g | c33 UI 1,290.3k, c34 ML 175.0k, c35 Bug 129.8k, c36 Docs 71.1k, c37 Test 331.9k, c38 Trap 216.5k, c39 Vague 264.0k, c40 ML 896.8k; avg 421.9k, median 240.2k | c34/c36 had an empty final reply (harness); see change-type-test-results.md |

Round 23 prep (2026-10-18): issues #10636-10643, v36g, branches cursor/token-exp-p6-cNN-v36.
Round 24 prep (2026-10-18): issues #10653-10660, v37g, branches cursor/token-exp-p6-cNN-v37. c20 is docs-only and exercises the NON_CODE path.
Round 25 prep (2026-10-18): issues #10669-10676, v38g, branches cursor/token-exp-p6-cNN-v38.
Round 26 prep (2026-10-18): issues #10685-10692, v39g, branches cursor/token-exp-p6-cNN-v39. Prep lesson from c25: check every later migration, not just the CREATE script.
