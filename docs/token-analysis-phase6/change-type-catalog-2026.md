# Change-type catalog for master in 2026, and coverage gaps in the token-optimization tests

Source: `gh pr list --state merged --base master --search merged:>=2026-01-01` (549 PRs; none came from `cursor/token-exp-*`). Each PR gets ONE primary type from `/workspace/cls.py`. The classifier applies ordered rules to the title keywords and changed-file paths. "Lines" means additions + deletions. Layers are the `src/<Project>` folders, `.github`, ps1 build scripts, docs, and agent-skill folders. All numbers below were computed from that data; none are estimates.

Caveats: the classification is heuristic. Fix-type titles appear on 61 PRs (11.1%) across all types, which is more than the 14 that land in "Bug fix" as a primary type, because many fixes are UI or test changes. 86 PRs (15.7%) are synthetic pipeline smoke items (AISF burn-in, README notes, [FST], "Tiny:", Auto Merge).

## Catalog

| Change type | PRs | % | Median files | Median lines (+/-) | Typical layers (share of PRs) | Examples |
|---|---|---|---|---|---|---|
| UI/Blazor feature or text | 104 | 18.9% | 3 | 57.5 | UnitTests 84%, UI.Shared 83%, AcceptanceTests 33%, UI 25% | #9322 Login screen: show a Welcome back message when a m, #9316 Login screen: show a Version label in the sign-in  |
| Test-only | 86 | 15.7% | 2 | 43 | AcceptanceTests 47%, UnitTests 41%, IntegrationTests 37%, agent-skills 13% | #9179 New API endpoint GET /api/tools/random for configu, #9147 Add acceptance coverage for GET /api/health/detail |
| Docs-only | 80 | 14.6% | 1 | 4 | docs 97%, agent-skills 1%, .github 1%, labs 1% | #9214 Add docs/branch-naming.md documenting the agent br, #9212 Add docs/prompt-locations.md noting where AI worke |
| CI/build/deploy/infra | 72 | 13.1% | 1 | 27.5 | .github 58%, build-scripts 23%, docs 8%, arch 4% | #547 Add ARM64 integration build job with SQLite, #546 Add Headful switch to acceptance test runner |
| API endpoint/health | 45 | 8.2% | 5 | 90 | UI 95%, UnitTests 91%, IntegrationTests 75%, AcceptanceTests 6% | #9178 New API endpoint POST /api/tools/guid-generator fo, #9173 New API endpoint GET /api/tools/timestamp-converte |
| Static-analysis/quality remediation | 40 | 7.3% | 15.5 | 521 | UnitTests 77%, IntegrationTests 62%, UI 60%, Core 50% | #9045 Qodana C8: naming, dead members, cancellation toke, #8850 Qodana Phase 1: remediate P0 High defects (#8842) |
| Agent skills/instructions | 27 | 4.9% | 2 | 96 | agent-skills 92%, build-scripts 25%, docs 3%, .github 3% | #6964 npm-audit skill, #1279 Document GitHub issue body update guidance for clo |
| LLM/AI features | 23 | 4.2% | 8 | 218 | UnitTests 78%, IntegrationTests 56%, LlmGateway 52%, AcceptanceTests 47% | #9337 Health check: cache the LlmGateway probe result in, #1457 Stabilize ApplicationChatHandler assign integratio |
| MCP server tools | 20 | 3.6% | 6 | 295.5 | McpServer 100%, IntegrationTests 75%, AcceptanceTests 65%, UI 35% | #634 Convert ApplicationChat tool provider to use MCP l, #1407 Add correlation ID middleware (#1398) |
| DB schema + entity/field (multi-layer) | 14 | 2.6% | 12 | 346 | Database 100%, IntegrationTests 71%, UnitTests 57%, DataAccess 57% | #543 jeffreypalermo/update architecture docs, #9050 Quiet benign console output during unit and integr |
| Bug fix | 14 | 2.6% | 4 | 62 | UI.Shared 64%, UnitTests 57%, AcceptanceTests 57%, UI 21% | #7155 Revert auto-merged IbmBob PRs #7154 (ping) and #71, #3424 Fix spelling: CanFulfilWorkOrder → CanFulfillWorkO |
| Aspire/telemetry/messaging | 6 | 1.1% | 12.5 | 350.5 | ChurchBulletin.AppHost 50%, agent-skills 50%, Worker 50%, ChurchBulletin.ServiceDefaults 50% | #6961 Add owasp-dependency-scan skill for .NET and npm S, #453 OTEL tracing, events, logging, and metrics written |
| Dependency bump | 4 | 0.7% | 5 | 74 | .github 75%, build-scripts 25%, docs 25%, IntegrationTests 25% | #8999 Bump release version scheme to 2.4.x, #9920 Build: keep a NuGet cache path the machine already |
| Domain/Core logic | 4 | 0.7% | 6 | 161.5 | Core 75%, UnitTests 75%, IntegrationTests 75%, DataAccess 25% | #9145 Persist tracked work-order relationships across st, #607 Added cancel option |
| Validator/business rule | 3 | 0.5% | 19 | 439 | UnitTests 100%, Core 100%, AcceptanceTests 66%, docs 33% | #8611 Add Instructions field validation message for work, #1416 Add input validation middleware (#1391) |
| Demo video (Remotion) | 3 | 0.5% | 30 | 4584 | video 100%, docs 66% | #8339 Add Remotion demo video pipeline (#8338), #9168 Remotion: Grok Bot proves TDD MCP on the live boar |
| Refactor/rename/removal | 2 | 0.4% | 7 | 84 | UI.Shared 100%, AcceptanceTests 100% | #9052 Remove artificial acceptance test delays |
| gRPC/proto | 2 | 0.4% | 39.5 | 2106 | UnitTests 100%, IntegrationTests 100%, UI 100%, LlmGateway 50% | #9085 Add optional work order due date, Chicago urgency  |

Merged PRs by month: 2026-01 2, 2026-02 56, 2026-03 103, 2026-04 25, 2026-05 4, 2026-07 71, 2026-08 117, 2026-09 138, 2026-10 33

All PRs: files median 2, p75 6, p90 17; lines median 58, p75 248, p90 616
PRs touching 3+ production layers: 46 (8.4%)
Branch prefixes: cursor 224, ibmbob 124, claude 46, jeffreypalermo 33, jeffrey 23, copilot 13, cm-ai-ops-bot 10, feature 8, palermo 8, issues 5

Additional cross-cuts (title regex counts): 61 fix/bug/revert/stabilize titles (11.1%); 38 tooltip/hint/subtitle/footer/heading UI-text items (6.9%); 31 `/api/` endpoint titles (5.6%).

## What rounds 15-20 tested (unseen sets, from round-*.md)
- R15 u-set (#10493-10500): domain method, new API endpoint, new Blazor page, DbUp column, MCP tool change, validator, bug fix (query handler trim), acceptance-test text.
- R16 p-set (#10509-10516): state-command permission, HTTP header, health check data, CSV parser error, nav menu link, logging, utility class, search spec filter.
- R17 q-set (#10525-10532): enum + rule, options flag, date calculation, console connection string, header sanitizing, file guard, middleware filter, component parameter.
- R18 r-set (#10541-10548): telemetry field + mapper, LLM gateway guard, view-model property, shared-constant refactor, enum extension, accessibility markup, message handler, helper extraction.
- R19 s-set (#10557-10564): URL builder, list dedupe, CSS mapping, voice selection, rate-limit key, LLM reply parsing, reflection tagging, gRPC proto field.
- R20 t-set (#10573-10580, x3 session arms): PATCH idempotency, buffer bound, JSON nulls, Role.ToString, health rounding, read-only role, batch message, envelope factory.

Profile of the tested items: 48 unique items, all written by us, single-concern, and small. Measured cost runs at about 3-13 calls each. Almost every one is a 1-2 production-file change plus a unit test. Two were DB/multi-layer (u04, the earlier t06), one was gRPC, and about 3 were MCP, about 3 LLM, and about 5 UI.

## Gap analysis
| Frequent real type | Share of master | Tested? | Gap |
|---|---|---|---|
| UI/Blazor feature or text | 18.9% (+6.9% UI-text cross-cut) | ~5 small items (nav link, CSS, a11y, component param, page) | **Under-tested.** Real UI PRs touch UI.Shared and UnitTests (bUnit) and, a third of the time, AcceptanceTests (Playwright). No test item needed a Playwright acceptance test end to end. |
| Test-only (acceptance/integration stabilization) | 15.7% | No | **Untested.** Flaky-test stabilization and acceptance-coverage items exercise test projects, not Get-Context's production RECIPE. |
| Docs-only | 14.6% | No | **Untested.** These are cheap; the only question is whether the skill short-circuits (no build, no CRAP) or wastes calls. |
| CI/build/deploy/infra (.github, ps1) | 13.1% | No | **Untested.** Submit's local build/test can't validate a workflow change, and the RECIPE has no exemplar for YAML or ps1. |
| API endpoint | 8.2% | Yes (u02, p02, t01) | Adequate. |
| Static-analysis/quality remediation | 7.3%, median 15.5 files / 521 lines | No | **Untested and large.** This is the main large-diff type, and our 4-call read LIMIT may fight it. |
| Agent skills/instructions | 4.9% | No | Low priority. |
| LLM/AI | 4.2%, median 8 files | Small guards only | Under-tested for multi-file features and live-LLM test handling. |
| MCP tools | 3.6%, median 6 files / 296 lines | Small changes only | Under-tested: none added a NEW tool with integration and acceptance tests. |
| DB schema + entity/field end to end | 2.6%, median 12 files / 346 lines | 1-2 small items | **Under-tested on size.** 8.4% of all PRs touch 3+ production layers; none of our items did. |
| Bug fix from a real symptom report | 2.6% primary, 11.1% by title | 2 synthetic | Under-tested: real reports name a symptom, not a file (e.g. #9646 home 400, #9346 raw Razor glyph). |
| gRPC/proto | 0.4%, median 39.5 files | s08 only | Covered once; v33 should be re-checked with one more item. |
| Aspire/telemetry/messaging | 1.1%, median 12.5 files | r01, r07 (small) | Minor. |

Size gap: real PRs have p75 = 6 files / 248 lines and p90 = 17 files / 616 lines. Our test items sit near the master median (2 files) or below it. Nothing tested the top quartile, which is where the large-cost outliers came from (s08 gRPC 1.7M, round-8 z04).

## Recommended additional test sets (unseen, one item per session, same measurement)
1. **v-set, multi-layer features (6 items):** new entity field DB→DataAccess→Core→UI→tests (2), new MCP tool with integration and acceptance tests (2), LLM feature spanning LlmGateway and UI (1), gRPC field round 2 (1). Proves the cost scales sanely to the p75-p90 sizes and that the RECIPE/NEW-names output works across 3+ layers.
2. **b-set, real bug reports (6 items):** re-open reproductions of real 2026 fixes, worded by symptom only (e.g. #9646, #9346, the scrollToBottom JSException, the MCP wildcard discovery). Proves Get-Context's `-Find` budget finds the cause without file hints.
3. **ui-set, UI with Playwright (6 items):** 3 UI-text/tooltip items like the AISF style, and 3 interactive features (sort, filter toggle, dark mode, like #9531/#9534/#9536) that need AcceptanceTests. Proves the cost stays steady when acceptance tests are required and CI's acceptance job is part of GREEN.
4. **c-set, CI, build and docs (6 items):** 2 workflow YAML changes, 2 ps1 build-script changes, 2 docs-only. Proves the skill fast-paths non-code work (no CRAP/Qodana loops) and Submit's waiting handles workflow-only CI.
5. **q-set, quality remediation (4 items):** Qodana/CRAP batches of 10-20 findings across projects. Proves the read LIMIT and fix loop hold up on 15-file diffs; expect to need a batch mode.
6. **t-set, test-only and stabilization (4 items):** add acceptance coverage for an existing endpoint (2), and stabilize a flaky integration test (2). Proves Get-Context's TEST SETUP output works when no production change is needed.
7. **f-set, failure recovery (4 items):** items seeded to hit CI RED (a Qodana trap, a CRAP over 6, a broken acceptance test, an ALREADY_DONE item). Proves the RED→fix→ForceRerun loop is bounded.
8. **a-set, ambiguous or large items (4 items):** vague product asks (e.g. "dashboard cards by status" like #9538) with no files named. Proves the cost on under-specified work, which the round sets never had.

Total: 40 items in 5 rounds of 8. Weighting this mix to master's distribution (UI ~25%, test/docs/CI ~40%, multi-layer ~10%, quality ~7%) would make the reported average representative of real work rather than of small single-file items.
