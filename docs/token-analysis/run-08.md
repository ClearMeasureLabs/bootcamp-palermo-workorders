# Run 8 — type D (validation rule + test), generic lean
- Agent bc-c199a536-557c-5640-8e87-66a9eefe4359 · #9950 · PR #9952 (+49/-1). Transcript 29,114 B, 41 lines. **13 tool calls**.
- Est. replayed context **~50k** (-98.3% vs baseline). Estimates (bytes/4).
- Calls: read skill · read validator · glob tests · `gh issue view` (already in the prompt) · read sibling test · Get-AgentTarget (wrong type) · **read Complete-AgentItem.ps1 and Publish-AgentPr.ps1** (looking inside the scripts) · read the query record · 2 edits · Complete-AgentItem (36 s) · handoff artifact.
- Waste: about 8 of 13 calls. Canned recipe for run 9: `Invoke-ValidatorRuleItem.ps1` chains the rule, generates the TestHelper tests, then builds, tests and publishes. Verified on a clean tree (UserLoggedInEventValidator MaximumLength(100): 2/2 pass).
