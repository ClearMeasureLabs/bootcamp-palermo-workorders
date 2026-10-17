# Iteration 2: lean v1 (all techniques T1-T5 together)

- Agent bc-3a658237-8277-5778-8472-f0f3312a3b5b · #9979 · PR #9980 · head 01fe3c74 **green** (14 success, 2 skipped). Transcript 16,048 B, 29 lines, 1 run (no notification re-run).
- Estimates (bytes/4):

| | Iter 2 | Baseline (iter 1) | Δ |
|---|---|---|---|
| Input | **~13.4k** | ~1.68M (lower bound) | **-99.2%** |
| Output | **~1.8k** | ~12.4k | **-85.5%** |
| Tool calls | 7 | 60 + 7 subagents | |
| CI reds | 0 | 0 | |

## Calls
1. `Invoke-TitleLengthItem.ps1` (47 s): applied, tested and opened the PR, then **failed to start Wait-PrChecks**. Publish-AgentPr switches to a branch from master, where `scripts/agent/` doesn't exist.
2. Rerun of Wait-PrChecks: missing.
3-4. Diagnosis (`ls`, `find`): 2.9 KB.
5. `git checkout analysis/token-reduction -- scripts/agent/Wait-PrChecks.ps1` then wait (**640 s, one call**) → CHECKS=GREEN.
6. Handoff artifact. 7. Cleanup of the restored script.

4 of 7 calls were recovery from the script-path bug. Fixed in v2: Invoke copies the scripts to a temp dir before publishing and prints `WAIT_CMD=...` for red-fix loops.

**T3 works:** a single blocking call held through ~10.7 min of CI without being backgrounded.
