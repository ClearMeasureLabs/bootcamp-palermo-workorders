# Iterations 10-13 (estimates, bytes/4; per-turn system/tool overhead not counted)

| Iter | Variant | PR | Input | Output | Tool calls | Notes |
|---|---|---|---|---|---|---|
| 10 | v4 terse prompt | #10000 | ~4.5k | ~0.44k | 4 | Invoke hit the runtime's ~600 s cap. The agent then ran get_mcp_tools and read the terminal file before running WAIT_CMD, which cost +3.6k input. |
| 11 | v4 terse (repeat) | #9999 | **~0.63k** | **~0.31k** | 2 | clean |
| 12 | v4 `-Field RoomNumber -Max 1200` | #10001 | ~0.64k | ~0.32k | 2 | **generalization works**: +22/-11, 7 files, new migration 033, full green |
| 13 | v3 prompt on v4 scripts | #10003 | ~33.6k | ~2.2k | 11 | publish bug (below); the PR had 54 files |

**Prompt terseness (11 vs iter 9's v3 prompt on v3 scripts):** -0.23k input (-27%) and -0.14k output (-31%). Most of the output saving comes from "no narration" plus a RESULT-only reply.
**Variance (v4 terse, n=2 clean + 1 backgrounded):** 0.63k-4.5k input. The spread comes from whether the first call gets backgrounded at the runtime's 600 s cap. Invoke runs about 554 s, which is very close to that cap.

## Iter 13 bug: root cause and fix
- **What happened:** `Publish-AgentPr.ps1` committed on a branch from master, but the push was piped to `Out-Null` and `gh pr create` (GraphQL) failed, so the script died with no clear message. The agent then pushed by hand and reran Invoke. On the master-based branch the scripts were missing, so it merged `analysis/token-reduction` into the PR branch. That gave 54 files, and CI still went green.
- **Second latent cause, reproduced on the box:** if any tracked non-src file is dirty, `git checkout -B <branch> origin/master` fails. A native git failure does not stop `$ErrorActionPreference='Stop'`, so HEAD stays on the analysis branch and the commit lands there.
- **v5 fixes in Publish-AgentPr.ps1:**
  - src edits are carried as a patch, then `checkout -f` from master.
  - A **guard** refuses any diff outside `src/` (verified: it prints `PUBLISH_FAIL: diff has non-src files …`).
  - Push and PR-create results are checked, and failures print a single `PUBLISH_FAIL:` line that says not to repair by hand.
  - It **resumes**: if an open PR already exists for the branch it is reused, and if the local branch already exists Invoke skips apply and tests.
  - A commit identity fallback is set when none is configured.

## REST-only gh (v5)
GraphQL was rate-limited at ~1:41 AM EDT. v5 scripts now use REST only:
- `gh api repos/../pulls` (create, plus find the existing PR by head)
- `pulls/N` → head.sha (in Wait-PrChecks)
- `issues/N` → title
- `issues/N/comments`
- check-runs and run reruns were already REST.

No `gh pr …` or `gh issue …` calls remain in scripts/agent. Verified: `Wait-PrChecks -Pr 10000` → `RESULT … CHECKS=GREEN`.
