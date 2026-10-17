# Run 4 — new type: unit tests for an untested class (generic lean skill)

- Agent bc-23f09c58-b7ce-53e7-a21e-85e5e9bcfdb2 · issue #9943 · PR #9944 (+98/-0, 1 file; closed unmerged by the dispatcher). The branch was **not deleted**: branch deletion is blocked by a repo rule, so experiment branches are left in place.
- Transcript 28,928 B, 36 lines; 25 assistant messages, **10 tool calls**. No token fields, so figures are estimates.
- Est. replayed context **~37k** (-98.7% vs the 2.90M baseline); est. output ~2.7k.

| # | Call | Needed? |
|---|---|---|
| 1 | read lean SKILL.md | partly (the prompt could inline it) |
| 2 | Get-AgentTarget.ps1 | wrong tool for this type (built for the guard shape) |
| 3 | read the class under test | yes, but a script could print it |
| 4 | `ls` UnitTests/Core/Services | pattern discovery |
| 5 | read a sibling test (5.7 KB) | pattern discovery; the largest result |
| 6 | git checkout -b | redundant (the publish script branches) |
| 7 | write the test file | yes |
| 8 | build+test script (24 s) | yes |
| 9 | publish script | yes |
| 10 | stray edit after the PR | no |

Canned recipe (run 5): `Get-TestContext.ps1` prints the class, its referenced sibling types, the test path and the house header. `Complete-AgentItem.ps1` does build, test, title lookup and publish. Target: 3 calls.
