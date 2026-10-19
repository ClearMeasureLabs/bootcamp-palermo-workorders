# Launching an agent on one issue

Use this for any single GitHub issue in this repo. The loop lives in `.claude/skills/feature-loop-lean/GENERAL.md`, and its scripts are in `scripts/agent/`.

## Prompt (replace N and B)

```
Your first action is reading .claude/skills/feature-loop-lean/GENERAL.md; do not call any MCP tool (never get_mcp_tools). Follow GENERAL.md for issue #N on branch B. Do only what it says. Start with Get-Context.ps1 -Issue N. Submit with Submit-Change.ps1 -Issue N -Branch B; it waits for CI itself. Only if no CHECKS=GREEN/RED line, run the WAIT_CMD it prints. If a script fails, follow the SCRIPT_FAIL rule in GENERAL.md. Do NOT merge. Reply with only the RESULT line.
```

- **N** is the issue number. **B** is a new branch name, e.g. `cursor/<short-name>`. Submit publishes to B and opens the PR to master. The agent never commits, switches branches or merges.
- **Launch the agent without MCP servers attached.** Agents call `get_mcp_tools` even when told not to, and every call replays the whole context. Removing the servers is the only reliable fix.
- **One issue per agent.** The reply is `RESULT=GREEN PR=<url>`, `RESULT=RED ...`, `RESULT=ALREADY_DONE <file:line>` or `SCRIPT_FAIL: <line>`.

## Writing the issue

Describe the outcome and acceptance criteria: what users see, which tests to add, and any limits such as max lengths. Backticked type names help Get-Context find the target. Vague issues work too; the loop turns them into a visible change.

## What the scripts do

- **`Get-Context.ps1`**
  - Prints the issue criteria, existing files, a recipe for this kind of change (`scripts/agent/recipes.txt`), the edit target and a sibling test.
  - Allows a small read budget afterwards. After the budget runs out, the agent may still read up to 2 files it never read and must edit. A second refusal is final (`STOP:`).
- **`Submit-Change.ps1`**
  - Runs risk checks for Qodana, CRAP, the BogusOverrides length trap and unused new types.
  - Runs unit tests, integration tests on SQLite, the affected acceptance tests (headless Playwright) and local InspectCode.
  - Publishes only `src/`, `.github/`, `docs/` and root `*.ps1` changes, then waits for CI inline.
  - Reruns of an unchanged tree are cached.

## Measured effect

Over 8 items of mixed types, 2026-10:

| | tokens per issue (avg) | CI REDs | delivered |
|---|---|---|---|
| this loop | 371.7k | 0 | 8/8 |
| no instructions | 2,388.7k | 4 | 8/8 |

Tokens = transcript bytes/4, replayed at every tool-call turn.
