# Optimization v2 (for run 3)
- `Add-NullGuard.ps1`: canned, idempotent edit (guard + NUnit test) for this type of work. Verified on 3 targets: 30/30 StateCommands tests pass.
- `Invoke-GuardItem.ps1`: edit, build, test, title lookup and publish in **one** call.
- `Publish-AgentPr.ps1`: AgentUrl is optional, so there's no MCP or URL lookup; the dispatcher posts the URL.
- Lean skill v2: a single command for guard items, with explicit bans on reads and URL lookup. The launch prompt inlines the command so reading the skill is optional.
