# Session-size experiment (v33g): 1 vs 4 vs 8 work items per Cursor session

**Question (Jeffrey):** is it more token-efficient to run one work item per session (clean context window), or several items per session in one growing context?

## Design
- **Skill:** v33g (e5365956) for all arms, launched from `analysis/token-reduction`. Never seen before: the t-set kinds aren't in the u/p/q/r/s sets.
- **Same issues, separate branches:** each issue exists once and every arm runs it on its own branch (`…-v33-a|b|c`). All arms see byte-identical issue text and Get-Context output, so the only variable is session packing. Three copies of each issue would change the issue numbers and so the Get-Context output and transcripts. Each arm opens its own PR per item.
- **Arms:**
  - **A:** 8 sessions × 1 item (the current way).
  - **B:** 2 sessions × 4 items (B1 = t01-t04, B2 = t05-t08).
  - **C:** 1 session × 8 items (t01-t08).
- **Order:** same item order in every arm. Launch all 11 sessions at about the same time so CI load is comparable.
- **Multi-item support (v33g):**
  - `Start-NextItem.ps1` resets src/ to the starting ref. Publish-AgentPr diffs the whole workspace src, so leftovers would leak into the next PR.
  - It also resets the Get-Context 3-call budget, and a new `-Issue` resets the budget too.
  - It marks the session multi-item, so Wait-PrChecks says "print RESULT, then the next item" instead of "stop".
- **Measurement:** same method as every round (`measure_transcript.py`, bytes/4).
  - Primary metric: **total input per arm for the same 8 items**. Also report per-session totals.
  - For B/C, split per item at each `Start-NextItem.ps1` call. An item's cost is the sum of the input at its turns, and it includes replaying the earlier items' context.
  - Also report: wall time per arm, GREEN count, CI REDs, calls after GREEN, items skipped or merged into others, and any cross-item leakage (PR files outside the item's scope).
- **Expectation to test:**
  - In C, each turn replays every earlier item, so later items should cost more. If the platform summarizes or truncates context, the growth may flatten.
  - A pays the skill-file and first-call overhead 8 times; B pays it 2 times, C once.
  - The answer is whichever arm has the lowest total for 8 GREEN items.

## Issues (same for all arms)
| Item | Issue | Title | Arm A branch | Arm B branch (session) | Arm C branch |
|---|---|---|---|---|---|
| t01 | #10573 | token-exp p6 t01: Idempotency also covers PATCH | cursor/token-exp-p6-t01-v33-a | cursor/token-exp-p6-t01-v33-b (B1) | cursor/token-exp-p6-t01-v33-c |
| t02 | #10574 | token-exp p6 t02: Request buffering threshold has an upper bound | cursor/token-exp-p6-t02-v33-a | cursor/token-exp-p6-t02-v33-b (B1) | cursor/token-exp-p6-t02-v33-c |
| t03 | #10575 | token-exp p6 t03: Realtime notifications omit null properties | cursor/token-exp-p6-t03-v33-a | cursor/token-exp-p6-t03-v33-b (B1) | cursor/token-exp-p6-t03-v33-c |
| t04 | #10576 | token-exp p6 t04: Role displays its name | cursor/token-exp-p6-t04-v33-a | cursor/token-exp-p6-t04-v33-b (B1) | cursor/token-exp-p6-t04-v33-c |
| t05 | #10577 | token-exp p6 t05: LLM health cache age is rounded | cursor/token-exp-p6-t05-v33-a | cursor/token-exp-p6-t05-v33-b (B2) | cursor/token-exp-p6-t05-v33-c |
| t06 | #10578 | token-exp p6 t06: Read-only role factory | cursor/token-exp-p6-t06-v33-a | cursor/token-exp-p6-t06-v33-b (B2) | cursor/token-exp-p6-t06-v33-c |
| t07 | #10579 | token-exp p6 t07: Dated batch limit message shows the count | cursor/token-exp-p6-t07-v33-a | cursor/token-exp-p6-t07-v33-b (B2) | cursor/token-exp-p6-t07-v33-c |
| t08 | #10580 | token-exp p6 t08: Realtime envelope factory for remotable events | cursor/token-exp-p6-t08-v33-a | cursor/token-exp-p6-t08-v33-b (B2) | cursor/token-exp-p6-t08-v33-c |

## Launch prompts
### Arm A (8 sessions, one per item; starting ref analysis/token-reduction)
**A-t01:**
```
Follow `.claude/skills/feature-loop-lean/GENERAL.md` for issue #10573 on branch cursor/token-exp-p6-t01-v33-a. Do only what it says. Start with `pwsh -NoProfile scripts/agent/Get-Context.ps1 -Issue 10573`. Submit with `pwsh -NoProfile scripts/agent/Submit-Change.ps1 -Issue 10573 -Branch cursor/token-exp-p6-t01-v33-a`; it waits for CI itself. Only if it printed no CHECKS=GREEN/RED, run the printed WAIT_CMD until it does (if a wait shows no output, run it again; never read terminal files). If a script fails for a reason outside your code, reply SCRIPT_FAIL with its last line and stop. Never call get_mcp_tools. No narration, subagents, MCP, git, board, journal, todos or handoff files. Do NOT merge. Reply with only the RESULT line: `RESULT=GREEN|RED|ALREADY_DONE PR=<url or evidence>`; make no tool calls after it.
```
**A-t02:**
```
Follow `.claude/skills/feature-loop-lean/GENERAL.md` for issue #10574 on branch cursor/token-exp-p6-t02-v33-a. Do only what it says. Start with `pwsh -NoProfile scripts/agent/Get-Context.ps1 -Issue 10574`. Submit with `pwsh -NoProfile scripts/agent/Submit-Change.ps1 -Issue 10574 -Branch cursor/token-exp-p6-t02-v33-a`; it waits for CI itself. Only if it printed no CHECKS=GREEN/RED, run the printed WAIT_CMD until it does (if a wait shows no output, run it again; never read terminal files). If a script fails for a reason outside your code, reply SCRIPT_FAIL with its last line and stop. Never call get_mcp_tools. No narration, subagents, MCP, git, board, journal, todos or handoff files. Do NOT merge. Reply with only the RESULT line: `RESULT=GREEN|RED|ALREADY_DONE PR=<url or evidence>`; make no tool calls after it.
```
**A-t03:**
```
Follow `.claude/skills/feature-loop-lean/GENERAL.md` for issue #10575 on branch cursor/token-exp-p6-t03-v33-a. Do only what it says. Start with `pwsh -NoProfile scripts/agent/Get-Context.ps1 -Issue 10575`. Submit with `pwsh -NoProfile scripts/agent/Submit-Change.ps1 -Issue 10575 -Branch cursor/token-exp-p6-t03-v33-a`; it waits for CI itself. Only if it printed no CHECKS=GREEN/RED, run the printed WAIT_CMD until it does (if a wait shows no output, run it again; never read terminal files). If a script fails for a reason outside your code, reply SCRIPT_FAIL with its last line and stop. Never call get_mcp_tools. No narration, subagents, MCP, git, board, journal, todos or handoff files. Do NOT merge. Reply with only the RESULT line: `RESULT=GREEN|RED|ALREADY_DONE PR=<url or evidence>`; make no tool calls after it.
```
**A-t04:**
```
Follow `.claude/skills/feature-loop-lean/GENERAL.md` for issue #10576 on branch cursor/token-exp-p6-t04-v33-a. Do only what it says. Start with `pwsh -NoProfile scripts/agent/Get-Context.ps1 -Issue 10576`. Submit with `pwsh -NoProfile scripts/agent/Submit-Change.ps1 -Issue 10576 -Branch cursor/token-exp-p6-t04-v33-a`; it waits for CI itself. Only if it printed no CHECKS=GREEN/RED, run the printed WAIT_CMD until it does (if a wait shows no output, run it again; never read terminal files). If a script fails for a reason outside your code, reply SCRIPT_FAIL with its last line and stop. Never call get_mcp_tools. No narration, subagents, MCP, git, board, journal, todos or handoff files. Do NOT merge. Reply with only the RESULT line: `RESULT=GREEN|RED|ALREADY_DONE PR=<url or evidence>`; make no tool calls after it.
```
**A-t05:**
```
Follow `.claude/skills/feature-loop-lean/GENERAL.md` for issue #10577 on branch cursor/token-exp-p6-t05-v33-a. Do only what it says. Start with `pwsh -NoProfile scripts/agent/Get-Context.ps1 -Issue 10577`. Submit with `pwsh -NoProfile scripts/agent/Submit-Change.ps1 -Issue 10577 -Branch cursor/token-exp-p6-t05-v33-a`; it waits for CI itself. Only if it printed no CHECKS=GREEN/RED, run the printed WAIT_CMD until it does (if a wait shows no output, run it again; never read terminal files). If a script fails for a reason outside your code, reply SCRIPT_FAIL with its last line and stop. Never call get_mcp_tools. No narration, subagents, MCP, git, board, journal, todos or handoff files. Do NOT merge. Reply with only the RESULT line: `RESULT=GREEN|RED|ALREADY_DONE PR=<url or evidence>`; make no tool calls after it.
```
**A-t06:**
```
Follow `.claude/skills/feature-loop-lean/GENERAL.md` for issue #10578 on branch cursor/token-exp-p6-t06-v33-a. Do only what it says. Start with `pwsh -NoProfile scripts/agent/Get-Context.ps1 -Issue 10578`. Submit with `pwsh -NoProfile scripts/agent/Submit-Change.ps1 -Issue 10578 -Branch cursor/token-exp-p6-t06-v33-a`; it waits for CI itself. Only if it printed no CHECKS=GREEN/RED, run the printed WAIT_CMD until it does (if a wait shows no output, run it again; never read terminal files). If a script fails for a reason outside your code, reply SCRIPT_FAIL with its last line and stop. Never call get_mcp_tools. No narration, subagents, MCP, git, board, journal, todos or handoff files. Do NOT merge. Reply with only the RESULT line: `RESULT=GREEN|RED|ALREADY_DONE PR=<url or evidence>`; make no tool calls after it.
```
**A-t07:**
```
Follow `.claude/skills/feature-loop-lean/GENERAL.md` for issue #10579 on branch cursor/token-exp-p6-t07-v33-a. Do only what it says. Start with `pwsh -NoProfile scripts/agent/Get-Context.ps1 -Issue 10579`. Submit with `pwsh -NoProfile scripts/agent/Submit-Change.ps1 -Issue 10579 -Branch cursor/token-exp-p6-t07-v33-a`; it waits for CI itself. Only if it printed no CHECKS=GREEN/RED, run the printed WAIT_CMD until it does (if a wait shows no output, run it again; never read terminal files). If a script fails for a reason outside your code, reply SCRIPT_FAIL with its last line and stop. Never call get_mcp_tools. No narration, subagents, MCP, git, board, journal, todos or handoff files. Do NOT merge. Reply with only the RESULT line: `RESULT=GREEN|RED|ALREADY_DONE PR=<url or evidence>`; make no tool calls after it.
```
**A-t08:**
```
Follow `.claude/skills/feature-loop-lean/GENERAL.md` for issue #10580 on branch cursor/token-exp-p6-t08-v33-a. Do only what it says. Start with `pwsh -NoProfile scripts/agent/Get-Context.ps1 -Issue 10580`. Submit with `pwsh -NoProfile scripts/agent/Submit-Change.ps1 -Issue 10580 -Branch cursor/token-exp-p6-t08-v33-a`; it waits for CI itself. Only if it printed no CHECKS=GREEN/RED, run the printed WAIT_CMD until it does (if a wait shows no output, run it again; never read terminal files). If a script fails for a reason outside your code, reply SCRIPT_FAIL with its last line and stop. Never call get_mcp_tools. No narration, subagents, MCP, git, board, journal, todos or handoff files. Do NOT merge. Reply with only the RESULT line: `RESULT=GREEN|RED|ALREADY_DONE PR=<url or evidence>`; make no tool calls after it.
```
### Arm B (2 sessions)
**B1:**
```
Follow `.claude/skills/feature-loop-lean/GENERAL.md` for these 4 issues, in this order, one at a time:
1. issue #10573, branch cursor/token-exp-p6-t01-v33-b
2. issue #10574, branch cursor/token-exp-p6-t02-v33-b
3. issue #10575, branch cursor/token-exp-p6-t03-v33-b
4. issue #10576, branch cursor/token-exp-p6-t04-v33-b
For EACH item: first run `pwsh -NoProfile scripts/agent/Start-NextItem.ps1` (resets src/ and the Get-Context budget; it must print NEXT_ITEM=READY), then `pwsh -NoProfile scripts/agent/Get-Context.ps1 -Issue <N>`, make the change, and submit with `pwsh -NoProfile scripts/agent/Submit-Change.ps1 -Issue <N> -Branch <branch>`; it waits for CI itself. Only if it printed no CHECKS=GREEN/RED, run the printed WAIT_CMD until it does (if a wait shows no output, run it again; never read terminal files). When the item is done, print its line `RESULT=GREEN|RED|ALREADY_DONE ISSUE=<N> PR=<url or evidence>` and go straight to the next item. Never carry changes from one item into the next. If a script fails for a reason outside your code, print `SCRIPT_FAIL ISSUE=<N>: <last line>` and continue with the next item. Never call get_mcp_tools. No narration, subagents, MCP, git, board, journal, todos or handoff files. Do NOT merge. Your final reply is only the RESULT lines, one per item, in order; make no tool calls after the last one.
```
**B2:**
```
Follow `.claude/skills/feature-loop-lean/GENERAL.md` for these 4 issues, in this order, one at a time:
1. issue #10577, branch cursor/token-exp-p6-t05-v33-b
2. issue #10578, branch cursor/token-exp-p6-t06-v33-b
3. issue #10579, branch cursor/token-exp-p6-t07-v33-b
4. issue #10580, branch cursor/token-exp-p6-t08-v33-b
For EACH item: first run `pwsh -NoProfile scripts/agent/Start-NextItem.ps1` (resets src/ and the Get-Context budget; it must print NEXT_ITEM=READY), then `pwsh -NoProfile scripts/agent/Get-Context.ps1 -Issue <N>`, make the change, and submit with `pwsh -NoProfile scripts/agent/Submit-Change.ps1 -Issue <N> -Branch <branch>`; it waits for CI itself. Only if it printed no CHECKS=GREEN/RED, run the printed WAIT_CMD until it does (if a wait shows no output, run it again; never read terminal files). When the item is done, print its line `RESULT=GREEN|RED|ALREADY_DONE ISSUE=<N> PR=<url or evidence>` and go straight to the next item. Never carry changes from one item into the next. If a script fails for a reason outside your code, print `SCRIPT_FAIL ISSUE=<N>: <last line>` and continue with the next item. Never call get_mcp_tools. No narration, subagents, MCP, git, board, journal, todos or handoff files. Do NOT merge. Your final reply is only the RESULT lines, one per item, in order; make no tool calls after the last one.
```
### Arm C (1 session)
```
Follow `.claude/skills/feature-loop-lean/GENERAL.md` for these 8 issues, in this order, one at a time:
1. issue #10573, branch cursor/token-exp-p6-t01-v33-c
2. issue #10574, branch cursor/token-exp-p6-t02-v33-c
3. issue #10575, branch cursor/token-exp-p6-t03-v33-c
4. issue #10576, branch cursor/token-exp-p6-t04-v33-c
5. issue #10577, branch cursor/token-exp-p6-t05-v33-c
6. issue #10578, branch cursor/token-exp-p6-t06-v33-c
7. issue #10579, branch cursor/token-exp-p6-t07-v33-c
8. issue #10580, branch cursor/token-exp-p6-t08-v33-c
For EACH item: first run `pwsh -NoProfile scripts/agent/Start-NextItem.ps1` (resets src/ and the Get-Context budget; it must print NEXT_ITEM=READY), then `pwsh -NoProfile scripts/agent/Get-Context.ps1 -Issue <N>`, make the change, and submit with `pwsh -NoProfile scripts/agent/Submit-Change.ps1 -Issue <N> -Branch <branch>`; it waits for CI itself. Only if it printed no CHECKS=GREEN/RED, run the printed WAIT_CMD until it does (if a wait shows no output, run it again; never read terminal files). When the item is done, print its line `RESULT=GREEN|RED|ALREADY_DONE ISSUE=<N> PR=<url or evidence>` and go straight to the next item. Never carry changes from one item into the next. If a script fails for a reason outside your code, print `SCRIPT_FAIL ISSUE=<N>: <last line>` and continue with the next item. Never call get_mcp_tools. No narration, subagents, MCP, git, board, journal, todos or handoff files. Do NOT merge. Your final reply is only the RESULT lines, one per item, in order; make no tool calls after the last one.
```
