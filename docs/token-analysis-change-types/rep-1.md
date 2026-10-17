# Rep 1 (v6 general techniques only)

Estimates in tokens (bytes/4). Input = replayed context summed per tool-call turn; output = reasoning + text + tool args.

| Type | PR | Input | Output | Tool calls | CI-wait calls | Result |
|---|---|---|---|---|---|---|
| t01 | #10037 | 1,814k | 7.2k | 73 | 24 | green after CI red: unit, integration and acceptance tests outside the filter |
| t02 | #10042 | 700k | 3.2k | 43 | 5 | green |
| t03 | #10039 | 1,142k | 6.7k | 66 | 16 | green |
| t04 | #10045 | 3,811k | 9.1k | 100 | 12 | green after CI red: ShouldBeRemotableCompatible |
| t05 | #10041 | 1,432k | 7.1k | 69 | 16 | green |
| t06 | #10044 | 1,158k | 5.4k | 58 | 16 | green after CI red: 3 acceptance due-date tests |
| t07 | #10036 | 127k | 1.5k | 19 | 7 | green |
| t08 | #10040 | 102k | 1.6k | 18 | 7 | green |
| t09 | #10038 | 1,927k | 7.6k | 78 | 20 | green after CI red: 3 acceptance tests |
| t10 | #10043 | 388k | 3.0k | 40 | 6 | green |
| t11 | #10046 | 601k | 3.6k | 44 | 5 | green |
| t12 | #10047 | 528k | 2.6k | 34 | 8 | green |
| t13 | #10049 | 564k | 3.6k | 39 | 8 | green |
| t14 | #10048 | 73k | 1.7k | 17 | 6 | green |
| t15 | #10050 | 775k | 3.5k | 41 | 5 | green |
| t16 | - | 1,236k | 5.0k | 50 | 0 | **FAIL**: PUBLISH_FAIL (wrong branch base), no PR |
| t17 | #10052 | 1,270k | 5.7k | 54 | 15 | green |
| t18 | #10051 | 65k | 1.5k | 16 | 5 | green |
| t19 | #10053 | 88k | 2.1k | 18 | 9 | green |
| **total** | | **17.80M** | **82k** | | | 18/19 green |

## Where the tokens went
1. **2-minute CI-wait calls.** Each CI cycle is about 13 min, so it took 5-24 wait turns, and each one replays the whole context (often 50-150 KB after edits). The runs above 1M input all have 12-24 waits. The "first call <120 s, then ≤120 s waits" idea **backfired** on input.
2. **Getting lost after publish.** The old publish checked out a branch from master, so `scripts/agent` vanished from the workspace. Agents spent calls on `ls`/`find`/reading scripts, plus manual `git commit/push` follow-ups (t01, t03, t05, t17).
3. **CI reds** from unit tests outside the local filter and from acceptance tests (t01, t04, t06, t09). Each one adds log digging plus another full CI cycle of waits.
4. **Discovery:** reads, greps and globs before editing (10-26 reads on the larger types).
5. **t16 publish bug:** the agent's branch was cut from the analysis branch, and the old publish trusted HEAD when it equaled the branch name, so the guard tripped and no PR was opened.

Smallest runs (t07, t08, t14, t18, t19 at 65-127k): small edits, one CI cycle, 5-9 waits.
