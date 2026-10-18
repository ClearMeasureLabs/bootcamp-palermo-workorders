# Phase 6 round 17 (v30g, 2bd025bd): q01-q08 (-v30), unseen kinds

Measured as in earlier rounds: `measure_transcript.py` totals, `attribute_transcript_tokens.py` attribution, bytes/4. 8/8 checks green. q02 never printed a RESULT line: its last message was "Fixing Qodana findings." and the final Wait returned CHECKS=GREEN after it.

| Item | Kind | Issue | PR | Transcript | Input | Output | Calls | Get-Context calls (KB) | Shell reads | Edits | Submits | Waits | Rework |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| q01 | enum value + rule | #10525 | 10539 | bc-56a3b084 | 120.3k | 1.09k | 19 | 4 (13.7, 1 LIMIT) | 0 | 10 | 3 | 1 | CRAP_RISK, then TEST=FAIL |
| q02 | options flag | #10526 | 10540 | bc-51550f07 | 253.7k | 3.79k | 25 | 5 (15.4, 2 LIMIT) | 0 | 12 | 5 | 2 | CRAP_RISK x2, BUILD=FAIL (CS9124), CI RED (6 Qodana findings), no RESULT line |
| q03 | date calculation | #10527 | 10536 | bc-d1301b69 | 26.8k | 0.60k | 8 | 1 (5.4) | 0 | 3 | 2 | 1 | BUILD=FAIL (test compile) |
| q04 | console connection string | #10528 | 10537 | bc-5fbd0bb9 | 38.5k | 0.91k | 8 | 2 (16.1) | 0 | 3 | 1 | 1 | none |
| q05 | header sanitizing | #10529 | 10534 | bc-ca8c3a99 | 46.7k | 0.69k | 11 | 2 (8.2) | 0 | 3 | 2 | 3 | CI RED (VariableLengthStringHexEscapeSequence) |
| q06 | file guard | #10530 | 10533 | bc-0d297d91 | 22.0k | 0.48k | 7 | 2 (8.8) | 0 | 2 | 1 | 1 | none |
| q07 | middleware filter | #10531 | 10535 | bc-ad3d9c3d | 57.2k | 1.11k | 13 | 3 (7.0) | 0 | 4 | 3 | 2 | BUILD=FAIL (CS0535), CI RED (UseObjectOrCollectionInitializer x2) |
| q08 | component parameter | #10532 | 10538 | bc-cdb753d0 | 13.2k | 0.32k | 6 | 1 (3.3) | 0 | 2 | 1 | 1 | none |
| **Total** | | | | | **578.4k** | 8.99k | 97 | 20 (77.9 KB) | 0 | 39 | 18 | 12 | |

- **Per run:** 72.3k average, median 42.6k. Max 253.7k (q02), min 13.2k (q08). Without q02: 46.4k average.
- **Cap held:** Get-Context ran at most 3 times per run. The 4th and 5th calls only printed LIMIT. No run read files through the shell (0 shell reads, versus 5 in round 16).

## Comparison
| | Per run | Median | Round 17 vs it |
|---|---|---|---|
| Round 16 (v29g, p-set) | 111.7k | 84.0k | avg -35%, median -49% |
| Round 15 validation (v28g, u-set) | 83.7k | 47.6k | avg -14%, median -10% |
| Round 14 (v28g, familiar items) | 30.0k | | avg +141% |

## Where the tokens went (all 8 runs)
Spend is split by phase. A run is in rework from its first failed Submit (risk, build or test) until a Submit passes, and in CI rework from its first CHECKS=RED onward.
| Phase | Tokens | Share |
|---|---|---|
| Happy path (context, edits, passing Submit, Wait) | 255.4k | 44% |
| Local rework after a failed Submit (CRAP_RISK, BUILD/TEST=FAIL) | 180.8k | 31% |
| CI-RED rework (Qodana) | 142.2k | 25% |

`attribute_transcript_tokens.py` breakdown (Get-Context output counts as "reads before first edit"):
| Source | Tokens | Share |
|---|---|---|
| reads/search before first edit | 204.3k | 35.3% |
| assistant msgs/tool args | 137.4k | 23.8% |
| edits | 97.1k | 16.8% |
| skill file | 76.1k | 13.2% |
| submit | 21.9k | 3.8% |
| prompt | 20.4k | 3.5% |
| wait results | 11.3k | 1.9% |
| reads/search after first edit | 10.0k | 1.7% |

## Drivers
1. **Rework caused by Qodana, not caught locally (q02, q05, q07): 142.2k.** All three CI REDs came from rules Test-ChangeRisk doesn't check:
   - UseObjectOrCollectionInitializer (q02, q07)
   - ConvertToLocalFunction, ConvertToPrimaryConstructor, PropertyCanBeMadeInitOnly.Global and UseUtf8StringLiteral (q02). The new options property had no `// ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global` comment, although the existing options properties carry one.
   - VariableLengthStringHexEscapeSequence (q05)
2. **CRAP_RISK loops when a guard clause is added (q01, q02).** Adding a branch took CC from 5 to 6. q02 extracted a helper twice and still failed, then hit CS9124 (a primary-constructor parameter captured into state). This is most of the 180.8k local rework, and q02's 98.2k is the biggest single share.
3. **Test compile errors on the first Submit (q03, q07 CS0535):** a full Submit cycle each.
4. **Missing RESULT line (q02):** the run finished green but ended mid-thought.

## v31 recommendation
Make `Test-ChangeRisk.ps1` check, in added lines, the Qodana rules this round hit in CI:
- `var x = new T();` followed by `x.Prop =` (use an object initializer)
- a `\x` escape inside a string literal
- `Encoding.UTF8.GetBytes("literal")` (use `"..."u8`)
- a new `{ get; set; }` property in an `*Options` class without the ReSharper disable comment
- a lambda assigned to a `Func`/`Action` local (use a local function)

The CRAP_RISK message should also show the pattern to use: return early from a private `static bool ShouldBypass(...)` helper so the new branch's condition lives outside the method. Have Wait-PrChecks print `NEXT: print RESULT=GREEN PR=<url>` when checks are green. Based on this round, the Qodana checks alone would have avoided up to 142.2k of CI rework (25% of round 17 input), if the agents fixed the issues locally.
