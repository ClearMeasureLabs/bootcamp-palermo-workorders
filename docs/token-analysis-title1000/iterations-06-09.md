# Iterations 6-9 (estimates, bytes/4)

| Iter | Variant | Agent | PR | Input | Output | Tool calls | Model turns |
|---|---|---|---|---|---|---|---|
| 6 | hand edits, **no file map** | bc-fa343ec2 | #9993 (+315/-8, 13 files) | ~1,183k | ~4.6k | 59 | ~80 |
| 7 | v3, **no local SQLite IT** | bc-1259be4a | #9992 | ~0.95k | ~0.49k | 2 | 4 |
| 8 | **stock skill** + scripts available | bc-41bff4c6 | #9994 | ~693k | ~10.6k | 38 | ~70 |
| 9 | **v3 reference** | bc-fe14b761 | #9991 | **~0.86k** | **~0.45k** | 2 | 4 |

- **Iters 7 and 9:** Invoke ran 554 s and returned `CHECKS=PENDING` under the 9-min cap; one WAIT_CMD rerun (139-229 s) returned GREEN. There were no flakes, no reads and no MCP calls. v3 fixed both iter 3 failure modes.
- **Iter 6:** 16 reads, 10 greps, 3 globs and 17 edits. With no map, discovery plus edit replay doubled iter 5.
- **Iter 8:** the stock loop ignored most of the shortcut. It made MCP listings, board GraphQL calls and journal comments, and **moved #9989's project card to Functional Testing**. That card needs a manual reset; the box token has no project scope.

## Measurement caveats (they matter now that runs are tiny)
1. The input estimate counts **conversation bytes only**. Each model turn also re-sends the system prompt + tool schemas (a fixed overhead S per turn, unknown size, plausibly 10k+ tokens, mostly cache reads). Real input ≈ estimate + turns × S. At ~1k estimated input, **model turns (4)** are the better cost proxy.
2. Some long-running tool results are recorded empty in the transcript (Invoke's first call in iters 7 and 9), so their output isn't counted. Small undercount.
3. Output ≈ 450 tokens is now mostly assistant narration ("Running the script…", "CHECKS=PENDING — rerunning…") plus the final reply.
