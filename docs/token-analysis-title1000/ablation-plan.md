# Ablation plan (attribute input/output deltas per technique)

Reference = **iter 3, full v2** (T1-T5 plus the path fix). Each ablation removes **one** technique from the reference; Δ = ablation minus reference.

| Iter | Removed | How |
|---|---|---|
| 3 | — (reference) | `Invoke-TitleLengthItem.ps1` one call |
| 4 | **T3** one-call CI wait | `-NoWait`; agent polls CI with `gh` itself |
| 5 | **T2** canned whole-change script | agent edits by hand from `Get-ItemContext.ps1` + issue file map, then `Invoke-TitleLengthItem.ps1 -SkipApply` |
| 6 | **T1** file map in issue | outcome-only issue; agent finds files itself, then `-SkipApply` |
| 7 | **T4** local SQLite tests | skip local IT; push and rely on CI |
| 8 | **T5** lean skill (no subagents/board/MCP) | stock feature-loop skill, but scripts available |
| 9+ | repeat the reference / v3 refinements | variance check (the ledger needs >=2 samples) |

**Revision (after iters 3-5):** T1 (file map) only matters when the agent edits by hand, so iter 6 = iter-5 setup *without* the file map (Δ vs iter 5). The reference is re-run as iter 9 on v3, because iter 3 was flake-contaminated; iter 2 (minus its 4 recovery calls) is the interim reference.
