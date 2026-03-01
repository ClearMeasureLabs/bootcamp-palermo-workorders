# TDD Job Completion Time Trend Analysis

**Date:** 2026-03-01
**Target Run:** [#22537888650](https://github.com/ClearMeasureLabs/bootcamp-palermo-workorders/actions/runs/22537888650/job/65288465968) — Version 1.4.214, master branch, TDD job: **5m 6s**

## Raw Data (25 most recent successful Deploy runs)

| Version | Branch | TDD Duration | Seconds |
|---------|--------|-------------|---------|
| 1.4.171 | master | 6m 27s | 387 |
| 1.4.172 | claude/monitor-pr-567-deployment | 5m 18s | 318 |
| 1.4.174 | claude/tracer-bullet-testing | 5m 06s | 306 |
| 1.4.175 | claude/document-architecture | 5m 00s | 300 |
| 1.4.176 | claude/document-architecture | 6m 28s | 388 |
| 1.4.178 | claude/document-architecture | 5m 10s | 310 |
| 1.4.181 | claude/convert-c4-diagrams | 6m 17s | 377 |
| 1.4.184 | claude/convert-c4-diagrams | 5m 20s | 320 |
| 1.4.186 | claude/mermaid-activity-diagram | 5m 01s | 301 |
| 1.4.187 | claude/mermaid-activity-diagram | 4m 59s | 299 |
| 1.4.191 | claude/mermaid-rendering-options | 4m 46s | 286 |
| 1.4.192 | palermo/optimize-build | 6m 01s | 361 |
| 1.4.193 | master | 5m 05s | 305 |
| 1.4.194 | palermo/optimize-build | 5m 07s | 307 |
| 1.4.195 | palermo/optimize-build | 4m 54s | 294 |
| 1.4.196 | palermo/optimize-build | 5m 57s | 357 |
| 1.4.197 | palermo/optimize-build | 4m 35s | 275 |
| 1.4.198 | palermo/optimize-build | 5m 08s | 308 |
| 1.4.201 | palermo/simplify-build | 4m 56s | 296 |
| 1.4.202 | palermo/optimize-build | 5m 10s | 310 |
| 1.4.204 | palermo/optimize-build | 5m 12s | 312 |
| 1.4.206 | palermo/simplify-build | 5m 18s | 318 |
| 1.4.210 | palermo/simplify-build | 4m 56s | 296 |
| 1.4.213 | nsb-testing | 5m 08s | 308 |
| **1.4.214** | **master** | **5m 06s** | **306** |

## Trend Summary

```
TDD Job Duration (seconds)    Versions 1.4.171 → 1.4.214
───────────────────────────────────────────────────────────
400 ┤ *         *
380 ┤                 *
360 ┤                              *            *
340 ┤
320 ┤        *              *  *
300 ┤     *  *  *        *  *  *  *     *  *  *  *  *  *  *
280 ┤                 *                    *        *
260 ┤
───────────────────────────────────────────────────────────
     171 172 174 175 176 178 181 184 186 187 191 192 193 194
     195 196 197 198 201 202 204 206 210 213 214
```

### Segmented Averages

| Segment | Versions | Avg Duration | Avg (seconds) |
|---------|----------|-------------|---------------|
| Oldest 8 | 1.4.171 – 1.4.184 | **5m 38s** | 338 |
| Middle 9 | 1.4.186 – 1.4.197 | **5m 09s** | 309 |
| Newest 8 | 1.4.198 – 1.4.214 | **5m 07s** | 307 |

### Key Statistics

| Metric | Value |
|--------|-------|
| Overall Average | 5m 18s (318s) |
| Minimum | 4m 35s (v1.4.197, palermo/optimize-build) |
| Maximum | 6m 28s (v1.4.176, claude/document-architecture) |
| Std Dev | ~30s |
| Target Run (1.4.214) | 5m 06s — below average, consistent with recent trend |

## Analysis

1. **Downward trend confirmed.** The oldest third of runs averaged 5m 38s; the most recent third averages 5m 07s — a **31-second improvement (~9%)** over the observation window.

2. **The palermo/optimize-build and palermo/simplify-build branches** correlate with the improvement. Starting at version 1.4.192, most runs come from these optimization branches and show consistently lower times.

3. **Occasional spikes** to 6m+ still appear (v1.4.176, v1.4.181, v1.4.192, v1.4.196) but are becoming less frequent in the newest segment.

4. **The target run (1.4.214)** at 5m 06s is right at the recent average (5m 07s) and 12 seconds below the overall average. The TDD job is performing well.

5. **Master branch runs** include the full Deploy pipeline (TDD + UAT + Prod), totaling 10-12 minutes. Feature branch runs only execute the TDD job, finishing in ~5 minutes. The TDD job duration itself is consistent regardless of branch — the difference is whether downstream jobs execute.
