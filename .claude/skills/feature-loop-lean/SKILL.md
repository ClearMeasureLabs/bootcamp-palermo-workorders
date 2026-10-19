---
name: feature-loop-lean
description: Token-lean loop for implementing one GitHub issue end to end (context, edit, local checks, PR, CI). Use for any single-issue code change in this repo.
---
# Feature loop: lean mode

Read and follow [GENERAL.md](GENERAL.md) in this folder. It is the whole loop: `scripts/agent/Get-Context.ps1 -Issue N`, edit, then `scripts/agent/Submit-Change.ps1 -Issue N -Branch B` (local checks, publish, CI wait). Launch prompt: [docs/agent-launch-prompt.md](../../../docs/agent-launch-prompt.md).
