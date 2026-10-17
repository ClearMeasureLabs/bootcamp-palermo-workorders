---
name: feature-loop-lean
description: Token-lean single-item loop for small, fully specified code changes (guard clause + test). Use when the issue names the exact file, change, and test.
---
# Feature loop: lean mode

The issue body is the full spec. **Do not explore.** Specifically: no MCP tool listing; no reading other skills, build scripts, or config; no searching the repo; no Docker, PrivateBuild, acceptance tests, trufflehog or CRAP runs locally (CI runs them on the PR); no board moves when the gh token lacks project scope; no CI polling.

Run exactly these steps, one command each (pwsh):

1. `gh issue view <N> --json body -q .body`
2. `pwsh -NoProfile scripts/agent/Get-AgentTarget.ps1 -Type <Type>`. This prints SRC, TEST, the method and the test header.
3. Make the edits with your editor tool: one edit in SRC and one appended test in TEST. Do not re-read the files after editing.
4. `pwsh -NoProfile scripts/agent/Invoke-AgentBuildTest.ps1 -Filter "FullyQualifiedName~<Type>Tests"`. If you get FAIL, fix only the reported error and rerun (at most 2 retries).
5. `pwsh -NoProfile scripts/agent/Publish-AgentPr.ps1 -Issue <N> -Branch cursor/<short-slug> -Title "<issue title>" -AgentUrl <your agent URL>`
6. Reply with the PR URL plus one line. Stop. Do not merge.
