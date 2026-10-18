# Phase 6 round 19 (v32g, 9249b040; s02 rerun on eada6687): s01-s08 (-v32), unseen kinds

Measured as in earlier rounds: `measure_transcript.py` totals, `attribute_transcript_tokens.py` attribution, bytes/4. 8/8 checks green. s02's failed first run (SCRIPT_FAIL, the Get-Context counter race fixed in eada6687) is included in s02 and in the total.

| Item | Kind | Issue | PR | Transcript(s) | Input | Output | Calls | Get-Context calls (KB) | Edits | Submits | Waits | ACCESS | Rework / notes |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| s01 | URL builder (internal, NOT visible) | #10557 | 10566 | bc-794e5799 | 46.0k | 0.67k | 9 | 2 (16.3) +1 LIMIT | 3 | 1 | 1 | NOT visible -> csproj line added; first Submit passed | delete_file after GREEN (7.5k) |
| s02 | list dedupe (internal, NOT visible) | #10558 | 10572 | bc-5dd14408 + bc-d35a0db5 | 140.1k (15.3k + 124.8k) | 3.40k | 21 | 4 (14.7) failed run; 3 (13.6) +1 LIMIT rerun | 4 | 3 | 3 | NOT visible -> csproj line added; no visibility build error | Run 1 SCRIPT_FAIL (counter race). Rerun: CI RED (a new test's assertion failed in the 4 Integration Build jobs), fixed; get_mcp_tools + todo after GREEN |
| s03 | CSS class mapping | #10559 | 10565 | bc-6f7e1b4c | 25.7k | 0.39k | 9 | 1 (4.1) | 2 | 1 | 2 (1 empty) | visible | get_mcp_tools + mcp after GREEN (8.4k) |
| s04 | voice selection | #10560 | 10567 | bc-7065d43a | 52.5k | 0.65k | 13 | 1 (4.2) | 5 | 2 | 3 (1 empty) | visible | CI RED: Qodana PossibleMultipleEnumeration x2; mcp after GREEN |
| s05 | rate-limit key normalization | #10561 | 10568 | bc-2e8d8efa | 24.4k | 0.40k | 8 | 1 (6.6) | 2 | 1 | 1 | visible | todo_write x2 |
| s06 | LLM reply parsing | #10562 | 10570 | bc-c8c7530f | 21.4k | 0.73k | 7 | 2 (6.8) | 2 | 1 | 1 | visible | none |
| s07 | reflection tagging | #10563 | 10569 | bc-f661e983 | 62.8k | 1.32k | 13 | 3 (10.4) | 5 | 3 | 1 | visible | CRAP_RISK x2 (CC 5->6) despite the early-return hint |
| s08 | gRPC proto field | #10564 | 10571 | bc-3bb3bc93 | **1,708.3k** | 4.77k | 92 | 4 (11.0) +1 LIMIT | 77 | 7 | 2 | visible | Hand-edited the checked-in generated protobuf C# (see below) |
| **Total** | | | | | **2,081.2k** | 12.33k | 172 | | 100 | 19 | 14 | | |

- **Per run:** 260.1k average, median 49.2k.
- **Without s08:** 53.3k average, median 46.0k.

## Comparison
| | Per run | Median | Round 19 vs it |
|---|---|---|---|
| Round 18 (v31g, r-set) | 39.4k | 32.8k | avg +560%; median +50% (without s08: avg +35%, median +40%) |
| Round 17 (v30g) | 72.3k | 42.6k | |
| Round 16 (v29g) | 111.7k | 84.0k | |

## s08: 133 KB transcript, 1.71M input
`src/UI/Server/Generated/Protos/Workorders.cs` (protoc output, "DO NOT EDIT") is checked in, and the build does not regenerate it (AGENTS.md: regenerate on x64 with Grpc.Tools). So:
- The agent edited the `.proto`, then got BUILD=FAIL (CS1061, no `Instructions` property). With no tool to regenerate, it wrote the protobuf field code by hand.
- 77 edits went into the generated file. Test-ChangeRisk then flagged the generated code: QODANA_RISK and a CRAP_RISK on generated `Equals`, CC 14 -> 15. The agent resubmitted with `-IgnoreRisk`.
- Two more BUILD=FAILs followed (a duplicate field and a misplaced field number), plus one Qodana object-initializer fix after CI.
- Attribution for this run: edits 751.4k (44.0%), assistant msgs/tool args 497.0k (29.1%), reads before first edit 228.3k (13.4%), submit 115.0k (6.7%).

Locally, Grpc.Tools 2.70.0 protoc output for the unchanged proto is byte-identical to the checked-in files, so regeneration by script is safe (v33g).

## ACCESS (v32g)
s01 and s02 both got `NOT visible to UnitTests: add <InternalsVisibleTo …/>`. Both added the csproj line, and neither run had a visibility build error; round 18 r08 lost 47.9k to exactly that. The six visible-internal items printed "visible" and needed no rework for access.

## CI failures
- s02 rerun: a new test's assertion failed in the Integration Build jobs (it passed locally).
- s04: Qodana PossibleMultipleEnumeration (an `IEnumerable` voices parameter enumerated twice). No local check existed; one is added in v33g.
- s08: a Qodana object initializer in its hand-written code.

## Other drivers
- **Calls after GREEN:** 6 of 8 runs still made tool calls after GREEN (get_mcp_tools, mcp, todo_write, delete_file), even though the launch prompt and the NEXT line forbid them. This looks like agent/platform behavior that wording doesn't fix.
- **s07 CRAP_RISK loop:** the early-return hint still took two tries.

## v33 changes (from this round)
- Regenerate gRPC code automatically:
  - New `Update-GrpcGenerated.ps1`.
  - Submit-Change runs it when a `.proto` changed.
  - Get-Context prints `PROTO:` (edit only the .proto; never open Generated/).
  - Test-ChangeRisk ignores `Generated/`.
- Test-ChangeRisk: PossibleMultipleEnumeration check.
- Get-Context counter race fix (eada6687, already used by the s02 rerun).
- Multi-item session support for the session-size experiment (see session-size-experiment.md).
