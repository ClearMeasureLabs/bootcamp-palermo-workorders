---
name: feature-loop-pi
description: >
  Pi-native feature loop for ONE Bootcamp work item on ClearMeasureLabs/bootcamp-palermo-workorders.
  Use when driving issue #N across the .NET Bootcamp board with Pi subagents (scout/planner/worker/reviewer).
  Model: ollama/qwen36-35b-a3b-iq3s (49k). NEVER merge PRs in this experiment mode.
---

# Feature Loop (Pi + local subagents) — 49k-optimized, literal-steps edition

You drive **one** issue `#N` (board item `ITEM_ID`) through the columns below by running the
**exact commands listed**, in order. Replace only `N`, `ITEM_ID`, `PR`. Do not improvise.

Columns: Conceptual Definition → UX Design → Technical Design → Test Design → Development → Functional Testing → STOP

## Hard rules
1. **NEVER merge a PR / never push to master.** Stop at green PR. Final line `STATUS: COMPLETE` or `STATUS: BLOCKED`.
2. One column at a time. Per column: **subagent (if listed) → journal comment → Advance mutation → Read Status**.
3. Only the two GraphQL commands below exist. `gh project item-*`, `.graphql` files, `actions/jobs` API, `Start-Sleep` loops are BANNED.
4. A failed command is never retried with variants. Report `STATUS: BLOCKED` + the error.
5. Parent runs only `gh`/`git` one-liners and `subagent`. Any file reading/editing is a subagent task.
6. Branch is always fresh: `pi/login-lastname-initial-N` from `origin/master`. Ignore leftover remote branches
   `feature/login-lastname-first-initial` and `pi/login-lastname-initial-*` (protected, cannot be deleted).

## Board constants
projectId `PVT_kwDOAGeHnM4BHwEz` · statusFieldId `PVTSSF_lADOAGeHnM4BHwEzzg4ad5k`
Options: Conceptual `c61d01c4` · UX Design `7e0d075c` · Technical Design `379cf003` · Test Design `f75ad846` · Development `b378aaa0` · Functional Testing `b10cd406`

READ (verify after every advance):
```
gh api graphql -f query='query { node(id:"ITEM_ID") { ... on ProjectV2Item { fieldValues(first:20) { nodes { ... on ProjectV2ItemFieldSingleSelectValue { name field { ... on ProjectV2SingleSelectField { name } } } } } } } }'
```
ADVANCE (OPTION from the list above):
```
gh api graphql -f query='mutation { updateProjectV2ItemFieldValue(input:{projectId:"PVT_kwDOAGeHnM4BHwEz",itemId:"ITEM_ID",fieldId:"PVTSSF_lADOAGeHnM4BHwEzzg4ad5k",value:{singleSelectOptionId:"OPTION"}}){projectV2Item{id}}}'
```
JOURNAL (≤6 lines; use PowerShell backtick-n for newlines):
```
gh issue comment N --body "## <Column> — Journal`nOutcome: ...`nFiles/Tests: ...`nNext: <next column>"
```

## Feature
Login page dropdown shows uppercase `LAST, I.` (Homer Simpson → `SIMPSON, H.`). Display-only; auth/usernames unchanged.
Files: `src/UI.Shared/LoginDisplayNameFormatter.cs`, `src/UI.Shared/Pages/Login.razor.cs`,
`src/UnitTests/UI.Shared/LoginDisplayNameFormatterTests.cs`, `src/UnitTests/UI.Shared/Pages/LoginPageTests.cs`,
plus acceptance tests under `src/AcceptanceTests` that assert dropdown text.

## Column scripts

**Conceptual Definition** (no subagent)
1. JOURNAL: Outcome = 5-bullet acceptance from the issue body. 2. ADVANCE `7e0d075c`. 3. READ.

**UX Design** (no subagent)
1. JOURNAL: format `LAST, I.` + 3 examples (SIMPSON, H. / WATSON, M. / BURNS, M.). 2. ADVANCE `379cf003`. 3. READ.

**Technical Design**
1. `subagent scout`: "List the current signature and body of LoginDisplayNameFormatter.cs and how Login.razor.cs calls it. ≤15 lines."
2. JOURNAL: files + function names from the scout. 3. ADVANCE `f75ad846`. 4. READ.

**Test Design** (no subagent)
1. JOURNAL: assertion table Homer→SIMPSON, H.; Mary Jane→WATSON, M.; Burns→BURNS, M.; empty first name→LAST only. 2. ADVANCE `b378aaa0`. 3. READ.

**Development**
1. `git fetch origin && git checkout -b pi/login-lastname-initial-N origin/master`
2. `subagent worker`: "Change LoginDisplayNameFormatter to return uppercase 'LAST, I.' from LastName + first letter of FirstName (fallback: LAST only when FirstName empty). Update Login.razor.cs if it formats names itself. Update LoginDisplayNameFormatterTests, LoginPageTests, and any AcceptanceTests that assert the old dropdown text. Run `dotnet test src/UnitTests --filter Login` and report pass/fail counts. Do NOT commit."
3. `git add -A && git commit -m "feat: login dropdown shows LAST, I. (#N)" && git push -u origin pi/login-lastname-initial-N`
4. `gh pr create --title "feat: login dropdown shows LAST, I. (#N)" --body "Closes #N. Experiment PR — DO NOT MERGE." --base master`
5. `gh pr checks PR --watch --fail-fast` (timeout 1800). If RED: one `subagent worker` fix task with the failing job name, commit, push, watch again. Max 2 fix rounds, then BLOCKED.
   - **If "Qodana (Community .NET)" is the failure** (it fails on ANY new code-style notice), get the findings with exactly these two commands, then hand the `path:line message` lines to the worker:
     `$sha = git rev-parse HEAD; $qid = gh api "repos/ClearMeasureLabs/bootcamp-palermo-workorders/commits/$sha/check-runs?per_page=50" --jq '.check_runs[]|select(.name=="Qodana (Community .NET)")|.id'`
     `gh api "repos/ClearMeasureLabs/bootcamp-palermo-workorders/check-runs/$qid/annotations" --jq '.[]|select(.path|startswith("UI")or startswith("Unit")or startswith("Acceptance"))|"\(.path):\(.start_line) \(.message)"'`
   - Typical Qodana fixes: `parts[^1]` instead of `parts[parts.Length - 1]`; `[' ', '\t']` collection expression instead of `new[] { ... }`; no unused assignments; no nested ternaries.
   - Never use `gh run view`, `--log-failed`, or `/logs` endpoints — they do not contain Qodana findings.
6. JOURNAL: branch, PR number, CI green. 7. ADVANCE `b10cd406`. 8. READ.

**Functional Testing**
1. `subagent reviewer`: "Run `gh pr checks PR`. Reply one line: GREEN or RED + failing job names."
2. JOURNAL: reviewer result; "PR left open for parent to discard; no merge."
3. Print `STATUS: COMPLETE column=Functional Testing PR=#PR` and stop. Do NOT advance further.
