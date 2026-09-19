---
name: feature-loop-pi
description: >
  Pi-native feature loop for ONE Bootcamp work item on ClearMeasureLabs/bootcamp-palermo-workorders.
  Use when driving issue #N across the .NET Bootcamp board with Pi subagents (scout/planner/worker/reviewer).
  EVERY column launches a FRESH subagent that inspects repo files before journaling.
  Model: ollama/qwen38-27b-gsq-rco (49k). NEVER merge PRs in this experiment mode.
---

# Feature Loop (Pi + local subagents) — per-column fresh subagents

You drive **one** issue `#N` (board item `ITEM_ID`) through the columns below by running the
**exact commands listed**, in order. Replace only `N`, `ITEM_ID`, `PR`, `BRANCH`, and the `<...>` placeholders
with facts from the issue or from subagent output. Do not improvise commands.

Columns: Conceptual Definition → UX Design → Technical Design → Test Design → Development → Functional Testing → STOP

## Step 0 (every session, before anything else)
`read` this file (`~/.pi/agent/skills/feature-loop-pi/SKILL.md`) so the READ/ADVANCE/JOURNAL commands are in context.
Then `gh issue view N --json title,body` — the body is the ONLY product intent. BRANCH = `pi/<3-5-word-kebab-slug>-N`.

## Hard rules
0. **You are not done until you have printed `STATUS: COMPLETE column=Functional Testing PR=#.`.** Finishing one column is
   never completion — immediately start the next column's script in the same session. Never end your turn to "report progress".
1. **NEVER merge a PR / never push to master.** Stop at green PR. Final line `STATUS: COMPLETE` or `STATUS: BLOCKED`.
   Never *claim* a merge, an advance, or a journal you did not run as a command in this session.
   `gh pr merge`, `git merge`, pushes to master, `gh issue close`, `gh project *`, invented board GraphQL,
   Start-Sleep poll loops, and Done/Release Queue moves are hard-blocked by the `no-merge-guard` extension;
   a BLOCKED tool result means stop and print `STATUS: COMPLETE` (for merge attempts) or `STATUS: BLOCKED` (for real blockers).
2. **One column at a time. Per column: FRESH subagent → journal from THAT subagent's output only → ADVANCE → READ.**
   Never reuse a prior column's subagent session for the next column. Always start a new `subagent` call.
3. **Parent never reads or edits product files.** Only `gh`/`git` one-liners and `subagent`. If a worker stalls,
   launch a **new** `subagent worker` with the same task (mention "previous worker stalled; start fresh"). Never implement yourself.
4. **Every column subagent MUST inspect the repo first** (at least: the target Razor page + `.razor.cs`, one mirror helper
   under `src/UI.Shared/`, relevant domain type under `src/Core/Model/`, and one existing unit + acceptance test). Journals that
   invent paths, CSS classes, enum values, or strings not present in those files are invalid — redo the column with a new subagent.
5. **Domain match gate (Conceptual):** if the issue's acceptance strings contradict what the subagent finds in Core/UI
   (wrong status names, columns that don't exist, etc.), print `STATUS: BLOCKED acceptance-mismatch` with the mismatch and stop.
   Do not rubber-stamp a bad spec into later columns.
6. Only the two GraphQL commands below exist. `gh run view` is allowed ONLY in the exact filtered form under "Acceptance Tests red".
7. A failed command is never retried with variants. Report `STATUS: BLOCKED` + the error.
8. Branch is always fresh from `origin/master`, cut inside the checkout named in your prompt. Ignore leftover remote `pi/*` branches.
9. Scope = the issue body **as corrected by repo facts from Conceptual**. Do not touch auth, persistence, migrations, messaging, shared layout, or any text the issue does not name.
10. **Never `Start-Sleep`**: the only waits are `gh pr checks PR --watch --fail-fast` and `subagent` calls (both block).

## Board constants
projectId `PVT_kwDOAGeHnM4BHwEz` · statusFieldId `PVTSSF_lADOAGeHnM4BHwEzzg4ad5k`
Options: Conceptual `c61d01c4` · UX Design `7e0d075c` · Technical Design `379cf003` · Test Design `f75ad846` · Development `b378aaa0` · Functional Testing `b10cd406`

READ (verify after every advance):
```
gh api graphql -f query='query { node(id:"ITEM_ID") { ... on ProjectV2Item { fieldValues(first:20) { nodes { ... on ProjectV2ItemFieldSingleSelectValue { name field { ... on ProjectV2SingleSelectField { name } } } } } } } }'
```
ADVANCE (OPTION from the list above):
CRITICAL: `itemId` MUST be the full Project item id `PVTI_…` from the dispatch prompt. NEVER put the GitHub issue number in `itemId`.
```
gh api graphql -f query='mutation { updateProjectV2ItemFieldValue(input:{projectId:"PVT_kwDOAGeHnM4BHwEz",itemId:"ITEM_ID",fieldId:"PVTSSF_lADOAGeHnM4BHwEzzg4ad5k",value:{singleSelectOptionId:"OPTION"}}){projectV2Item{id}}}'
```
JOURNAL (≤8 lines; PowerShell backtick-n for newlines). Body MUST quote paths/strings the subagent actually read:
```
gh issue comment N --body "## <Column> — Journal`nOutcome: ...`nRepo facts: ...`nFiles/Tests: ...`nNext: <next column>"
```

## Repo facts (bootcamp-palermo-workorders)
- Solution `src/ChurchBulletin.sln`. UI: `src/UI.Shared/Pages/*.razor(.cs)`, helpers like `src/UI.Shared/LoginDisplayNameFormatter.cs`.
- Domain: `src/Core/Model` (e.g. `WorkOrderStatus` is Draft/Assigned/InProgress/Complete/Cancelled — NOT Open/Closed).
- Unit tests: `src/UnitTests/UI.Shared/...` (NUnit + Shouldly). Acceptance: `src/AcceptanceTests/...` (Playwright; Homer Simpson).
- Unit test command: `dotnet test src/UnitTests/UnitTests.csproj --filter "FullyQualifiedName~<ClassName>"`.
- CI job "Qodana (Community .NET)" fails on ANY new style notice.

## Column scripts

**Conceptual Definition** (FRESH `subagent scout` — must inspect repo)
1. `subagent scout`: "Issue #N title+body: <paste>. FIRST read these paths in the worktree (or repo checkout from the prompt): the Razor page named in Hints (else search WorkOrderSearch.razor), its .razor.cs, LoginDisplayNameFormatter.cs, and any Core model type the acceptance mentions (e.g. WorkOrderStatus.cs). Quote the exact current markup/property types. Compare acceptance bullets to repo reality. Reply ≤15 lines: (a) current behavior with file:line quotes, (b) acceptance OK or MISMATCH listing wrong strings, (c) proposed Outcome restated ONLY if OK. Do not edit."
2. If scout says MISMATCH → `STATUS: BLOCKED acceptance-mismatch` and stop.
3. JOURNAL from scout output (include Repo facts with file:line). 4. ADVANCE `7e0d075c`. 5. READ.

**UX Design** (FRESH `subagent scout` — must inspect UI markup)
1. `subagent scout`: "Issue acceptance: <paste>. Conceptual journal: <paste>. Read the Razor page + any CSS classes on the target cell. Return exact user-visible strings AFTER the change, 2–3 concrete examples, and the exact DOM/css selector a Playwright test should use (must exist today or be added in the plan). Quote current markup. Do not edit."
2. JOURNAL from that output only. 3. ADVANCE `379cf003`. 4. READ.

**Technical Design** (FRESH `subagent scout`, then FRESH `subagent planner`)
1. `subagent scout`: "Spec: <Outcome+Acceptance>. Find files to change: Razor/.razor.cs, mirror helper, unit-test folder, acceptance test file. Return ≤12 lines `path — why` with evidence from reads. Do not edit."
2. `subagent planner`: "Spec: <same>. Scout findings: <paste>. Produce ≤12-line plan: each file + exact change (helper signature matching REAL types from Core, Razor expression, test class + 3–5 cases with expected strings from UX journal). No invented enums. Do not edit."
3. JOURNAL: plan file list + helper signature. 4. ADVANCE `f75ad846`. 5. READ.

**Test Design** (FRESH `subagent planner` — must read existing tests)
1. `subagent planner`: "Plan: <paste Technical Design journal>. Read one existing unit test and one acceptance test near the target area. Produce assertion table (input → expected) matching REAL method signatures, plus the Playwright locator that already works in sibling tests (e.g. td:nth-child(N)). Do not invent CSS classes that are not in the Razor or the plan. Do not edit."
2. JOURNAL: assertion table + locator. 3. ADVANCE `b378aaa0`. 4. READ.

**Development** (FRESH `subagent worker` — must read before edit)
1. `git fetch origin && git checkout -b BRANCH origin/master`
2. `subagent worker`: "Implement exactly this plan: <paste Technical + Test Design journals>. FIRST re-read every file you will edit. Rules: edit only listed files (+ new test files); mirror LoginDisplayNameFormatter.cs style; NUnit+Shouldly under src/UnitTests/UI.Shared/; Playwright under src/AcceptanceTests/ with exact expected strings; grep AcceptanceTests for assertions this change breaks and update them. Run `dotnet test src/UnitTests/UnitTests.csproj --filter \"FullyQualifiedName~<TestClass>\"` and report pass/fail. Do NOT commit. If stuck >15 tool calls without edits, stop and summarize blocker."
3. If worker stalls/fails without a diff: launch a **new** `subagent worker` once with the same task + prior blocker notes. Still never edit as parent.
4. `git add src && git commit -m "feat: <issue title> (#N)" && git push -u origin BRANCH` (always `git add src`, never `-A`)
5. `gh pr create --title "feat: <issue title> (#N)" --body "Closes #N. Experiment PR — DO NOT MERGE." --base master`
6. `gh pr checks PR --watch --fail-fast` (timeout 1800). If RED: one FRESH `subagent worker` fix task naming the failing job, then commit/push, watch again. Max 2 fix rounds, then BLOCKED.
   - **Qodana red**: get findings with exactly:
     `$sha = git rev-parse HEAD; $qid = gh api "repos/ClearMeasureLabs/bootcamp-palermo-workorders/commits/$sha/check-runs?per_page=50" --jq '.check_runs[]|select(.name=="Qodana (Community .NET)")|.id'`
     `gh api "repos/ClearMeasureLabs/bootcamp-palermo-workorders/check-runs/$qid/annotations" --jq '.[]|select(.path|startswith("UI")or startswith("Unit")or startswith("Acceptance")or startswith("Core"))|"\(.path):\(.start_line) \(.message)"'`
   - **Acceptance Tests red**:
     `$sha = git rev-parse HEAD; $url = gh api "repos/ClearMeasureLabs/bootcamp-palermo-workorders/commits/$sha/check-runs?per_page=50" --jq '.check_runs[]|select(.name=="Acceptance Tests")|.details_url' | Select-Object -First 1; $run = ($url -split '/runs/')[1] -split '/' | Select-Object -First 1; $job = ($url -split '/job/')[1]`
     `gh run view $run --job $job --log-failed | Select-String -Pattern "✗|Error Message|Expected|Assert|TimeoutException" | Select-Object -First 15`
7. After CI returns, `read` this SKILL.md again before continuing.
8. JOURNAL: branch, PR, CI green. 9. ADVANCE `b10cd406`. 10. READ.

**Functional Testing** (FRESH `subagent reviewer` — must inspect diff)
0. If ADVANCE/READ commands are gone from context, `read` this file again FIRST.
1. `subagent reviewer`: "Run `gh pr diff PR` and `gh pr checks PR`. Re-read acceptance bullets. Reply ≤8 lines: GREEN/RED; each acceptance bullet PASS/FAIL with evidence from the diff; any file outside the plan?"
2. JOURNAL: reviewer result; "PR left open for parent to discard; no merge."
3. Print `STATUS: COMPLETE column=Functional Testing PR=#PR` and stop. Do NOT advance further.
