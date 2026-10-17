# Run 6 — type C (seal non-inherited class), generic lean

- Agent bc-54d95a8d-3d6f-5168-a3ea-f0bc50c86b67 · issue #9947 · PR #9948 (+1/-1; closed unmerged by the dispatcher).
- Transcript 11,809 B, 23 lines. **6 tool calls**: read skill · Get-AgentTarget (no Execute method, so it fell back to a file read) · read src · edit · Complete-AgentItem (33 s) · handoff artifact write (`/opt/cursor/artifacts/code-tour-author-handoff.md`, platform behavior and not waste). Earlier "stray post-PR edits" in runs 2 and 4 are probably the same artifact.
- Est. replayed context **~10k** (-99.7% vs baseline). No token fields, so figures are estimates.
- Waste: reading the skill, the target-script call that didn't fit the type, and the src read. That's 3 of 6 calls.
- Canned recipe (run 7): `Invoke-SealItem.ps1` checks for subclasses and mocks, seals, then runs Complete-AgentItem. One call. Tested on a clean tree: it seals StateCommandContext and refuses StateCommandBase (inherited).
