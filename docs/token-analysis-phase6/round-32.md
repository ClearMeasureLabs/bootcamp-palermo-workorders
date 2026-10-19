# Round 32 — v45g (a6aca0c9)

Tokens = transcript bytes/4 (`scripts/agent/measure_transcript.py`). Real CI via `gh pr checks`: every PR **14 pass / 2 skipping** (copilot ignored). Delivery checked from each PR's files and diff against its issue.

## Summary

| metric | value |
|---|---|
| items | c81–c88 → PRs 10794, 10795, 10796, 10793, 10791, 10790, 10789, 10792 |
| real CI GREEN | 8/8 |
| correctly delivered | 8/8 |
| total / avg / median | 3217.4k / 402.2k / 416.6k |
| CI RED (CHECKS=RED) | 0 |
| CI flake reruns | 1 (c83 `FLAKE-RERUN: Acceptance Tests (failure, 13 min)`, then GREEN) |
| waits / wait-turn tokens | 14 over 8 CI cycles / 283.8k |
| get_mcp_tools | 1 (c83, after GREEN) |
| CACHED reruns | 1 (c81) |
| LENGTH_RISK / UNUSED_RISK | 0 / 0 |
| wide state-command acceptance | fired on c83 (77 passed, twice) |
| background runs / terminal-file reads | 0 / 0 |
| 10-min cuts | 0; longest call 7.8 min |
| lost tool results | 5, 07:43:56–07:48:12 CDT (c81 Submit, c83 Submit, c87/c88/c84 Wait); all rerun in the foreground |

## Per item

| id | PR | cat | tokens (k) | calls | Submits | waits | wall (min) | mcp | delivered | notes |
|---|---|---|---|---|---|---|---|---|---|---|
| c81 | 10794 | UI | 539.4 | 30 | 5 | 1 | 21.7 | 0 | yes | Today/Next Saturday due-date buttons (Chicago date), bUnit+Playwright; lost Submit already published → rerun CACHED + PUBLISH=NO_CHANGE |
| c82 | 10795 | UI | 726.2 | 41 | 12 | 1 | 22.7 | 0 | yes | **top waste**: 60-char title truncation + tooltip on search; 12 Submits (NUMBER_RISK WO-SHORT; missing BunitContext/IUiBus/StubUserSessionStore). Get-Context ranked an unrelated handler and generic ui-bunit printed LastWorkOrder; agent created a new test file instead of reusing WorkOrderSearchTests.CreateContext |
| c83 | 10796 | multi-layer | 672.3 | 36 | 5 | 3 | 44.4 | 1 | yes | Discard (DraftToCancelledCommand) Draft→Cancelled, creator only, MCP listed; wide ACC `(WorkOrders/*,McpServer/*) Passed: 77` x2; CI acceptance FLAKE-RERUN (no UI files → not suppressed); lost Submit at 4.0 min re-ran fully 7.8 min (tree changed, not CACHED); 2 empty Get-Context calls on a directory path; get_mcp_tools after GREEN |
| c84 | 10793 | multi-layer | 568.4 | 31 | 5 | 2 | 20.6 | 0 | yes | WorkOrdersDueSoonQuery + handler + MCP tool + IT; CRAP_RISK, CS0111, 2 IT fails, INSPECT=FIX; lost Wait |
| c85 | 10791 | vague | 181.2 | 20 | 1 | 2 | 18.8 | 0 | yes | creator email mailto link on WorkOrderManage (visible-change clause worked) |
| c86 | 10790 | bug | 195.6 | 21 | 2 | 1 | 16.4 | 0 | yes | not-allowed message when status matches, status-mismatch otherwise |
| c87 | 10789 | test-only | 40.6 | 9 | 1 | 2 | 14.1 | 0 | yes | tests only (McpCreateDatedWorkOrdersToolTests); lost Wait |
| c88 | 10792 | trap | 293.8 | 27 | 1 | 2 | 16.3 | 0 | yes | Initials HasMaxLength(3), DbUp 033, shown after assignee; ClampLength(1,3) in BogusOverrides case Employee before first Submit (LENGTH_RISK 0); lost Wait |

## vs round 31 (v44g)

| | r31 | r32 |
|---|---|---|
| avg / median | 670.0k / 485.8k | 402.2k / 416.6k |
| total | 5,359.8k | 3,217.4k |
| delivered | 7/8 | 8/8 |
| get_mcp_tools | 7 | 1 |
| lost results | 7 | 5 |

## Findings

- v45 worked: get_mcp_tools 7→1; foreground rerun rule followed (0 background, 0 terminal files); CACHED saved c81's local checks; vague clause produced a visible change (c85); wide acceptance ran for the state command.
- LENGTH_RISK trap again not exercised: the agent clamped in BogusOverrides before the first Submit.
- Top waste c82 (726.2k, 12 Submits): search-page bUnit setup. Next c83 (672.3k): CI acceptance flake (13 min, 3 waits, 82.4k wait tokens) plus a lost Submit whose whole-tree stamp did not match, so all checks re-ran.
- Get-Context `-File <directory>` crashed with empty output (c83, 2 calls).

## v46 (built, 8991de26)

1. `ui-bunit-search` recipe (supersedes `ui-bunit`): WorkOrderSearchTests.cs:1-46 (usings + CreateContext + first test) and a NOTE to append there.
2. Per-stage Submit stamps (unit / it / acc / inspect) keyed by tree hash; a rerun after a lost result skips stages that already passed (local: 122 s → 5 s).
3. Get-Context `-File` on a directory lists its .cs/.razor files.
