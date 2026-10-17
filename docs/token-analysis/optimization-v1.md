# Optimization v1 (for run 2)
- `scripts/agent/Get-AgentTarget.ps1`: prints the SRC/TEST paths, the target method and the test header. This replaces about 14 lookup calls.
- `scripts/agent/Invoke-AgentBuildTest.ps1`: quiet `-warnaserror` build of UnitTests (`NuGetAudit=false` avoids the NU1903 audit errors that broke run 1's first attempt) plus filtered tests, with summary-only output. Verified on a clean tree: 9 s, 4/4 pass.
- `scripts/agent/Publish-AgentPr.ps1`: commit, push, PR to master and the session comment in one call.
- `.claude/skills/feature-loop-lean/SKILL.md`: 6 fixed steps. No exploration, MCP listing, local Docker/PrivateBuild/acceptance runs, board moves without project scope, or CI polling.
