# Lean loop for small src changes (phase 3, v6.1)
1. Read only the files in the issue's **Exact file map**. Make the minimal edits plus tests. Heed **CI traps**. No subagents, board, MCP or journal; don't explore scripts.
2. Run `pwsh -NoProfile scripts/agent/Invoke-ChangeItem.ps1 -Issue N -Branch B`. It runs ALL unit tests plus the IntegrationTests build, then publishes from a temp worktree on master. **Your workspace branch never changes.** On a BUILD/TEST failure: fix and rerun.
3. Run the printed WAIT_CMD (≤8 min per call) until `CHECKS=GREEN` or `FAILED`. If a call returns empty output, run it again. On FAILED: fix the named test in the workspace, rerun step 2 (it pushes a follow-up commit) and wait again. Never use git checkout/commit/push yourself.
4. On `PUBLISH_FAIL`, reply with that line. Never merge. Final reply: the RESULT line only.
