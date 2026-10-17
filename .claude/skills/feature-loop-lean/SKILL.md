---
name: feature-loop-lean
description: Token-lean single-item loop for small, fully specified code changes. Use when the issue names the exact file, change and test.
---
# Feature loop: lean mode (v2)

The launch prompt and the issue are the full spec. **Do not read files, search, list MCP tools, look up your agent URL, run Docker/PrivateBuild/acceptance tests, move board cards, or poll CI.**

## Guard-clause items (null guard + unit test)
Run ONE command and then stop:

`pwsh -NoProfile scripts/agent/Invoke-GuardItem.ps1 -Issue <N> -Type <Type> -Branch <branch>`

That script edits SRC and TEST, builds quietly with warnings as errors, runs the filtered tests, opens the PR to master and posts the issue comment. Reply with the `PR=` line. If it prints FAIL, fix only the reported error, rerun once, then stop.

## Other fully specified items
1. `pwsh -NoProfile scripts/agent/Get-AgentTarget.ps1 -Type <Type>`. Edit using its output only; don't re-read.
2. `pwsh -NoProfile scripts/agent/Invoke-AgentBuildTest.ps1 -Filter "FullyQualifiedName~<Type>Tests"`
3. `pwsh -NoProfile scripts/agent/Publish-AgentPr.ps1 -Issue <N> -Branch <branch> -Title "<title>"`

## Unit tests for an untested class
Three calls, then stop:
1. `pwsh -NoProfile scripts/agent/Get-TestContext.ps1 -Type <Type>`. This prints the TEST path, the class source, referenced sibling types and the test header. Don't read any other file.
2. Create the TEST file in one write (header + tests named `Should...`, Shouldly asserts).
3. `pwsh -NoProfile scripts/agent/Complete-AgentItem.ps1 -Issue <N> -Type <Type> -Branch <branch>`. Builds, tests, opens the PR and comments. On FAIL, fix only the reported error, rerun once.

## Visibility: seal a non-inherited class
Run ONE command, then stop: `pwsh -NoProfile scripts/agent/Invoke-SealItem.ps1 -Issue <N> -Type <Type> -Branch <branch>`. It refuses if the class is inherited or mocked.

## All types
The platform's post-run handoff artifact write (`/opt/cursor/artifacts/...`) is expected; it is not waste.
