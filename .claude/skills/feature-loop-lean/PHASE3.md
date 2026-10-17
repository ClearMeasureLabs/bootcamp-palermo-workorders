# Lean loop for small src changes (phase 3, v8)
1. If the prompt gives `-Template tNN`, run Invoke with it FIRST, before reading any file. The template is a known-green patch. Read files only if something fails.
   Otherwise read only the files in the issue's **Exact file map**. Don't look up earlier issues, PRs or branches.
2. `pwsh -NoProfile scripts/agent/Invoke-ChangeItem.ps1 -Issue N -Branch B [-Template tNN]`. It runs ALL unit tests plus the IntegrationTests build, then publishes from a temp worktree on master. Your workspace branch never changes. On a BUILD/TEST failure: fix and rerun.
3. Run the printed WAIT_CMD. It already reruns flaky acceptance/integration jobs once. If it's RED only in tests your change doesn't touch, run `WAIT_CMD -ForceRerun` once. Never edit unrelated tests just to force a new commit.
   Keep running WAIT_CMD until `CHECKS=GREEN` or `CHECKS=RED`. If a call returns empty output, run it again. On RED it prints the failing tests, CRAP lines and `QODANA rule file:line` findings: fix exactly those in the workspace, rerun step 2 and wait again. Don't run your own `gh run` or log digging, and never run git checkout/commit/push.
4. CI gates besides tests: every new production method needs unit-test coverage (CRAP gate), and no unused usings or dead code (Qodana).
5. On `PUBLISH_FAIL`, reply with that line. Never merge. Final reply: the RESULT line only.
