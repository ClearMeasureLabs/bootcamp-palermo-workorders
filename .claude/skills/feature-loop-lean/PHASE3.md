# Lean loop for small src changes (phase 3)
1. Read only the files named in the issue's file map. Make the minimal edits plus tests. No subagents, board, MCP or journal.
2. Run `pwsh -NoProfile scripts/agent/Invoke-ChangeItem.ps1 -Issue N -Branch B [-Filter 'FullyQualifiedName~X']`. On BUILD/TEST failure: fix and rerun.
3. Run the printed WAIT_CMD until `CHECKS=GREEN` or `FAILED`. On FAILED: fix the named test, rerun step 2 (it reuses the PR; push a new commit with `git commit -am fix && git push`) and wait again.
4. `PUBLISH_FAIL` → reply with that line. Never merge. Final reply: the RESULT line only.
