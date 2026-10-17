# Phase 2 final summary: Title max length 300→1000, 20 iterations

**Method:** estimates from cloud-agent transcripts; there are no token fields.
- **Input** = conversation bytes replayed per tool-call turn, summed, divided by 4. It excludes the fixed system prompt and tool-schema overhead re-sent each turn, so model turns are the better proxy at small sizes.
- **Output** = (reasoning + text + tool args) bytes / 4.
- **Iter 1** is a lower bound: 7 subagents weren't counted.

**Result:** 19 of 20 iterations produced a full-green PR touching only src (every check-run green; each PR was closed unmerged afterwards). Iter 14 stopped on a remote push rejection with a clean `PUBLISH_FAIL` and no PR.

## Per-iteration results
| Iter | Variant | PR | Input | Output | Calls |
|---|---|---|---|---|---|
| 1 | baseline (stock loop) | #9978 | ~1.68M+ | ~12.4k | 60 (+7 subagents) |
| 2 | lean v1 (T1-T5) | #9980 | ~13.4k | ~1.8k | 7 |
| 3 | v2 reference (CI flake) | #9985 | ~97.4k | ~2.65k | 14 |
| 4 | no one-call CI wait (T3) | #9984 | ~20.5k | ~1.4k | 7 |
| 5 | no canned change (T2) | #9986 | ~606k | ~3.1k | 42 |
| 6 | no file map, hand edits (T1) | #9993 | ~1,183k | ~4.6k | 59 |
| 7 | no local SQLite IT (T4) | #9992 | ~0.95k | ~0.49k | 2 |
| 8 | stock skill + scripts (T5) | #9994 | ~693k | ~10.6k | 38 |
| 9 | v3 reference | #9991 | ~0.86k | ~0.45k | 2 |
| 10 | v4 terse (backgrounded) | #10000 | ~4.5k | ~0.44k | 4 |
| 11 | v4 terse repeat | #9999 | **~0.63k** | **~0.31k** | 2 |
| 12 | RoomNumber 1200 (generic script) | #10001 | ~0.64k | ~0.32k | 2 |
| 13 | v3 prompt on v4 (publish bug) | #10003 | ~33.6k | ~2.2k | 11 |
| 14 | v5 (push rejected, PUBLISH_FAIL) | none | ~0.23k | ~0.37k | 1 |
| 15 | command-list prompt (backgrounded) | #10013 | ~37.3k | ~1.1k | 11 |
| 16 | Title 1200 | #10011 | ~0.64k | ~0.32k | 2 |
| 17 | v5 repeat | #10012 | ~0.65k | ~0.32k | 2 |
| 18 | hand edits + file map | #10016 | ~519k | ~3.4k | 41 |
| 19 | v5 repeat (backgrounded) | #10014 | ~9.8k | ~0.65k | 6 |
| 20 | v5 final repeat | #10015 | ~0.67k | ~0.33k | 2 |

## Baseline vs best
- **Best (iter 11):** input ~1.68M → ~0.63k (**−99.96%**); output ~12.4k → ~0.31k (**−97.5%**).
- **Typical clean v5 (16/17/20):** 0.64-0.67k input (−99.96%) and 0.32-0.33k output (−97.4%).
- **Worst green v5-era run (iter 15, backgrounded):** 37.3k input (−97.8%) and 1.1k output (−91.0%).

## Technique ledger (Δ when the technique is removed, or added where noted)
| Technique | Evidence | Δ input | Δ output |
|---|---|---|---|
| T2 canned whole-change script | 5 vs 2; 18 vs 17 | +593k; +518k | +1.3k; +3.1k |
| T5 lean skill (no subagents/board/MCP/journal) | 8 vs 9 | +692k | +10.2k (largest output lever) |
| T1 file map (hand-edit path only) | 6 vs 5; 6 vs 18 | +577k; +664k | +1.5k; +1.2k |
| T3 one-call CI wait | 4 vs 2 | +7.1k | −0.4k |
| T4 local SQLite IT | 7 vs 9 | ~0 (noise) | ~0 (noise); safety only |
| Flake auto-rerun + 9-min cap (v3, added) | 3 vs 9 | −96.5k | −2.2k |
| Terse prompt / no narration / RESULT-only reply (v4, added) | 11 vs 9 | −0.23k (−27%) | −0.14k (−31%) |
| Guarded, resumable REST publish (v5, added) | 14 vs 13 | failure costs ~0.23k instead of ~33.6k | ~0.37k instead of ~2.2k |
| Generic field script (generalization) | 12, 16 vs 11/17 | ≈0 | ≈0 |
| Command-list prompt | 15 | not measurable (backgrounding confounded it) | - |
| Runtime backgrounding (uncontrolled) | 10/15/19 vs clean | +3.9k to +36.7k | +0.1k to +0.8k |

## Conclusions
1. Input is dominated by **how many turns replay context**. One canned script plus a one-call wait collapses about 60 turns into 2.
2. Output is dominated by **process overhead** (stock skill) and then **narration**. A RESULT-only reply takes it to ~0.3k.
3. The remaining variance comes from the agent runtime's foreground timeout (120-600 s). Once a call is backgrounded, agents poll and explore. **Next lever:** a script whose first call returns well under 120 s (start CI and print WAIT_CMD, `-NoWait`), then a blocking wait of ≤120 s per call. That bounds the backgrounding risk.
4. Hand edits stay ~800x more expensive even with a perfect file map. Canned scripts per work type are the main lever.
