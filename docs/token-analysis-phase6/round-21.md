# Round 21 — v34g, u-set (#10605-10612), 1 item/session

Method: `scripts/agent/measure_transcript.py` (bytes/4; input = the sum over tool-call turns of the cumulative transcript).

| Item | Transcript | PR | Input | Out | Calls | Real CI | Reported |
|---|---|---|---|---|---|---|---|
| u01 | bc-77e4a2cf | 10614 | 53.3k | 0.69k | 12 | GREEN | RED |
| u02 | bc-142ea47f | 10620 | 110.8k | 1.18k | 16 | GREEN | RED |
| u03 | bc-8633837a | 10616 | 31.4k | 0.58k | 9 | GREEN | RED |
| u04 | bc-1a9c91db | 10613 | 93.8k | 3.16k | 14 | GREEN (after 1 real Qodana fix) | RED |
| u05 | bc-0d944951 | 10619 | 35.1k | 0.62k | 9 | GREEN | RED |
| u06 | bc-a777112d | 10618 | 23.1k | 0.70k | 7 | GREEN | RED |
| u07 | bc-2b84827a | 10615 | 19.7k | 0.53k | 7 | GREEN | RED |
| u08 | bc-cae920d9 | 10617 | 49.1k | 0.73k | 11 | GREEN | RED |

Total 416.2k; avg 52.0k/item; median 42.1k. Round 20 arm A (v33g, 1/session) was 34.5k/item.

Real CI (`gh pr checks`): all 8 PRs show 14 pass and 2 skipping (Publish to Octopus Deploy, Security Scan). Skipping was already treated as a pass.

## False RED
The `copilot-pull-request-reviewer` check run now appears in `commits/<sha>/check-runs` on every PR and concludes `failure` ("You have exceeded your monthly quota"). It is not a CI gate and `gh pr checks` does not list it. `Wait-PrChecks.ps1` excluded only `Build result`, so it printed CHECKS=RED on 8/8 PRs. Every agent then ran WAIT again (u08 also did a todo) and still ended with RESULT=RED.
Cost: the turns after a RED whose only failure was copilot add up to **67.5k** (7.1/10.5/5.5/12.9/6.3/5.4/4.6/15.0k), which is 16% of the round. Without it the average is 43.6k.

## Other waste
- u04: a real Qodana `UseObjectOrCollectionInitializer` at ProblemDetailsExceptionHandler.cs:17 (`problemDetails.Extensions[...] = ...` after `var problemDetails = new ProblemDetails {...};`). The v31 rule missed it because the code already had an initializer and used an indexer. This cost about 59k: the fix loop plus 3 Submits.
- u02: 6 read/grep/glob calls after GC (csproj for InternalsVisibleTo, plus result type) and an MCP-recipe GC of 11.5KB.
- u08: 3 todo calls.

## v35g
- Wait-PrChecks ignores check runs named `*copilot*` (neither pending nor failure, and never rerun). Verified: PR 10614 → CHECKS=GREEN / RESULT=GREEN.
- Test-ChangeRisk: a new QODANA_RISK for `x.Prop = ...` / `x.Dict["k"] = ...` directly after the `var x = new T ...;` statement, with or without an initializer. Tested: it fires on the u04 shape and does not fire when another statement comes between.
- GENERAL.md: version bump only (still 2,973 bytes).
