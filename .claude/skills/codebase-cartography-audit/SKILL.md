---
name: codebase-cartography-audit
description: >-
  Full cartographic audit of an inherited or AI-generated codebase. Produces a
  self-contained report directory with: a complete inventory split into System vs
  DevOps/Test code (with LOC/function-point/cost subtotals + total); five
  C4-PlantUML views (logical, runtime, dependencies, testing, build/devops/deploy)
  plus a metrics-annotated logical view, rendered to PNG and visually inspected
  for legibility; a maintainability-defect
  and code-smell analysis grounded in SOLID, Onion & Clean Architecture, Fowler
  smells, enterprise/persistence patterns and the testing pyramid; tool-measured
  metrics (lines of code, cyclomatic complexity, linting/duplication, and a
  Function Point count via IFPUG cross-checked with Capers Jones backfiring, plus
  an economic valuation that turns function points into effort in staff-months and
  a build-cost / replacement-investment dollar range using Capers Jones
  productivity rates and current median engineer salaries)
  gathered by downloading and running the right CLI tools for the detected stack,
  with all analysis helpers written as single-file C# scripts (no Python/PowerShell);
  a logical diagram annotated with LOC and cyclomatic complexity in each box; and a README
  with the rendered diagram PNGs embedded. Use when onboarding to unfamiliar
  code, reviewing an AI-coded app, or planning a refactor and you want both a
  defensible principle-linked defect backlog AND measured, diagrammed evidence.
---

# Codebase Cartography Audit

A superset of `inherited-code-audit`. It keeps that skill's evidence-backed,
principle-linked defect catalogue and adds three things: **measured metrics**
(LOC, cyclomatic complexity, lint, duplication — from real tools), **five C4
architecture diagrams** rendered to inspected PNGs, and a **single self-contained
report directory** with the diagrams embedded in a README.

Deliverable: a new directory (default `./codebase-audit-report/`) containing
`README.md` (the report, with embedded diagram PNGs), `images/` (rendered PNGs),
`diagrams/` (PlantUML sources), and `metrics/` (raw tool output). Every claim
cites `file:line` or a reproducible command.

## When to use this

- You inherited a codebase (especially AI-generated) and want the full picture:
  map + measured health + prioritized defect backlog in one artifact.
- You are planning a refactor and need diagrams, hotspot metrics, and a
  principle-linked backlog to justify sequencing.
- You want a shareable onboarding document for a system you're taking over.

## The inspection catalogue

Modules 1–8 are the maintainability/architecture lenses (identical in spirit to
`inherited-code-audit`). Modules 9–10 add the measured and diagrammed scope.

| # | Module | File | Focus |
|---|--------|------|-------|
| 1 | SOLID (SRP, OCP, LSP, ISP, DIP) | `references/01-solid.md` | Martin |
| 2 | Onion Architecture | `references/02-onion-architecture.md` | Palermo |
| 3 | Clean Architecture & component rules | `references/03-clean-architecture.md` | Martin |
| 4 | Fowler code smells | `references/04-code-smells.md` | Fowler / Beck |
| 5 | Enterprise & domain patterns | `references/05-enterprise-patterns.md` | Fowler, Evans |
| 6 | Persistence, IoC & low-ceremony | `references/06-jeremy-miller.md` | Miller |
| 7 | AI-code anti-patterns | `references/07-ai-antipatterns.md` | (composite) |
| 8 | Testing & the automation pyramid | `references/08-testing.md` | Palermo, Martin, Cohn |
| 9 | Inventory, metrics & linting (tools) | `references/09-inventory-and-metrics.md` | measured |
| 10 | C4 diagrams (PlantUML) + rendering | `references/10-c4-plantuml-diagrams.md` | diagrammed |
| 11 | Function points (IFPUG + Jones backfiring) | `references/11-function-points.md` | sized |
| 12 | Economic valuation (effort, cost, replacement value) | `references/12-economic-valuation.md` | priced |

## How to run the audit (ordered pipeline)

Do these in order; later stages consume earlier outputs. Fan the read-only
investigation stages out to parallel subagents where the codebase is large.

1. **Scope & inventory.** Detect languages/stack, solution layout, entry points,
   project/module list, and size (file & LOC counts). Note build output and
   vendored dirs to exclude from everything. Create the **self-contained** report
   directory skeleton — `codebase-audit-report/` holding `README.md`, `images/`,
   `diagrams/`, and `metrics/` (all four inside the one directory, so the report
   is portable and its relative links resolve).

2. **Map the architecture (Modules 2 & 3).** Reconstruct layers and the
   dependency direction from project references and imports. This frames the
   diagrams and the defect hunt. Identify the domain core, application layer,
   infrastructure, UI, and any dependency-direction violations.

3. **Measure (Modules 9 & 11).** Acquire and run the right tools for the detected
   stack (scc for LOC always; plus a per-language lint/duplication tool). Save raw
   output to `metrics/`. Write the metric rollup and the function-point analysis
   as **single-file C# scripts** (`.csx`, run with `dotnet-script` — no Python,
   no PowerShell for analysis logic). Produce the per-component LOC + McCabe
   cyclomatic-complexity + density rollup, and the two-method Function Point
   estimate (IFPUG count reconciled against Capers Jones backfiring, Module 11),
   and the economic valuation that turns FP into effort and a build-cost /
   replacement-investment dollar range across **multiple productivity bands**
   (0.32–3.0 FP/man-day per Capers Jones & Steve McConnell), using median engineer
   salaries × burden (look salaries up live and cite), plus a schedule sanity
   check (Module 12). Also count classes and **average CC per class**, and get
   **test counts from the test runner** (`--list-tests`), not by hand.
   Classify all code into **two categories — System vs DevOps/Test** (tests +
   build/deploy scripts + tooling; run scc over the whole repo) and report a
   **subtotal for each plus a total** for LOC, function points, and cost
   (Modules 9.3b, 11.0, 12). For **cost**, the per-band Total column is
   coding-centric bands only — at the 0.32 full-lifecycle row the DevOps/Test and
   Total cells are "n/a (in System)" (never sum them; Module 12.2). Productivity
   bands span **0.32–3.0 FP/man-day** (incl. the Jones 0.32 baseline).

4. **Analyze defects & smells (Modules 1, 4, 5, 6, 7).** Follow the risk — the
   high-LOC/high-complexity components from step 3 first. Sweep the cheap
   high-signal AI defects (magic strings, duplication, hardcoded config, god
   classes), then the principle checks. Every finding: `file:line`, principle,
   why-it-hurts, named fix, effort/risk. **Report each defect once** under its
   most-specific principle and cross-reference the others (god class, switch-on-
   type, dead code, DIP/IoC, anemic domain recur across Modules 1/4/5/6/7 — do not
   emit the same file:line three times under different names).

5. **Assess the safety net (Module 8).** **Discover every test suite** (there may
   be dozens), count each with the runner, and **classify each as unit /
   integration / acceptance(full-system) by evidence, not folder name** — emit a
   suite matrix and roll it up to the pyramid shape. Determine whether the risky
   logic is covered. Thin coverage ⇒ the backlog leads with characterization tests.

6. **Diagram (Module 10).** Author the five views plus the metrics-annotated
   logical view using the **C4-PlantUML template library** (`<C4/…>`, renders
   offline via Graphviz), each showing the **System vs DevOps/Test** split via
   separate `System_Boundary`s with subtotal labels. ASCII labels only (no
   em-dashes/arrows — they mojibake). Render to PNG. **Read each rendered PNG and
   verify legibility**; fix source and re-render any that fail. Record that each
   was visually inspected.

7. **Assemble the report.** Write `codebase-audit-report/README.md` embedding the
   verified PNGs (`![](images/xx.png)`), followed by the executive summary,
   architecture map, metrics tables, prioritized findings, and the sequenced
   refactoring backlog. **Before delivering, run this self-check** — any missing
   item means the audit is not done: (i) the 5-row IFPUG function-type table;
   (ii) the 14-row GSC table + VAF math; (iii) per-category backfiring tables;
   (iv) the printed `Σ(components)==subtotal` reconciliation lines and an Excluded
   (non-authored) bucket; (v) the per-band cost table with the 0.32-row cells
   marked "n/a (in System)"; (vi) System/DevOps-Test/Total for LOC, FP, and cost;
   (vii) a legibility verdict for each of the 6 PNGs you actually opened.

## Finding format (required for every defect)

```
### [SEVERITY] <short title>
- **Principle:** <e.g. DIP — Dependency Inversion (Module 1)>
- **Location:** path/to/File.cs:120-148  (+ other sites if duplicated)
- **Evidence:** <the concrete smell — quote the code or describe precisely>
- **Why it hurts:** <maintainability/bug-surface/duplication cost>
- **Fix:** <the specific refactoring move, named where possible>
- **Effort / Risk:** <S/M/L, and whether tests exist to make it safe>
```

Severity: **Critical** (bug/security/data-loss), **High** (blocks change /
spreads widely), **Medium** (localized), **Low** (cosmetic).

## Report structure (`codebase-audit-report/README.md`)

1. **Title + how-to-reproduce** — tools used (name+version) and the commands, so
   every metric and diagram can be regenerated.
2. **Executive summary** — overall health, 3–5 systemic problems, single
   highest-leverage fix.
3. **Architecture diagrams** — the five embedded PNGs + the annotated logical
   PNG, each with a one-paragraph reading.
4. **Inventory & metrics** — language breakdown, per-component LOC/complexity/
   density rollup (with class count & avg CC/class), top-10 most-complex files,
   lint summary, duplication clones, the Function Point sizing, and the economic
   valuation. **Present LOC, function points, and cost each as System /
   DevOps-Test / Total** (Modules 9.3b, 11.0, 12) — for cost, the Total is
   coding-centric only and the 0.32 full-lifecycle row's DevOps/Test + Total cells
   are "n/a (in System)".
5. **Prioritized findings** — grouped by module, sorted by severity × spread.
6. **Testing assessment** — pyramid shape, coverage gaps, refactor-safety verdict.
7. **Refactoring backlog** — sequenced, "characterize with tests first" flagged
   wherever coverage is thin.

## Output fidelity rules (avoid these common LLM failure modes)

These are the mistakes that most often make an otherwise-good audit wrong. Treat
each as mandatory:

1. **Emit FULL tables, never prose summaries, for quantitative sections.** The
   function-point count in particular MUST be full tables — the IFPUG
   function-type table (count × weight = FP), the 14-row GSC table with a
   justification per rating and the VAF computation, per-category backfiring
   tables, and the subtotals+total table. Collapsing to "EI59 EO25 … VAF 1.21" is
   a defect. Same for the LOC rollup, cost bands, and suite matrix.
2. **Reconcile every subtotal with its parts.** Σ(component line items) must equal
   each category subtotal and the grand total. A subtotal larger than the visible
   rows (phantom LOC from a whole-repo walk) is a bug — find and attribute or
   exclude it. Print the reconciliation check. Unattributed LOC silently inflates
   FP and cost.
3. **Exclude non-authored code** (vendored, minified, generated, seed/config
   dumps) from the LOC tally, or bucket it separately — never fold it into System
   or DevOps/Test. Flag any single file/dir >~2k LOC and confirm it's real source.
4. **Count data functions (ILF/EIF) as logical files / aggregate roots, not
   per-table**; rate GSCs conservatively and show them. These are the two biggest
   IFPUG divergence sources between analysts (see Module 11.0-pre).
5. **Do not present a full-lifecycle Total that sums System + DevOps/Test** — that
   double-counts test-writing. Headline = System full-lifecycle alone.
6. **Actually inspect each rendered PNG** (Read the image) before claiming it
   passed — don't assert a legibility verdict you didn't check.
7. **Every objective number traces to a tool command**; qualitative findings each
   cite `file:line`. When a hand-count (IFPUG) and a mechanical count (backfiring)
   disagree, the mechanical one is the sanity bound.

## Guiding stance

- **Evidence over opinion.** Every defect cites code; every number cites a
  command. No principle name without a concrete instance.
- **Objective counts come from tools, never inference.** LOC, classes, complexity,
  test counts, lint, function points, and cost are each produced by a command or a
  committed `.csx` and captured in `metrics/`. Never eyeball-and-assert a count;
  in particular, count tests with the runner (`--list-tests`), not by grepping
  `[Test]` attributes. Only qualitative findings are analyst-authored.
- **Measure, then follow the risk.** The highest LOC × complexity component,
  touched by every feature, outranks a tidy leaf class.
- **Diagrams must be readable.** A rendered PNG that is clipped, overlapping, or
  mislabelled is a defect to fix, not ship. Inspect every image.
- **Refactor behind tests.** Where the net is thin, the first backlog item is
  characterization tests.
- **Prefer removing over adding.** The best outcome is less code: dedup, delete
  dead paths, collapse needless abstraction.
- **Never mutate the target repo** to gather metrics; install tools globally / as
  standalone binaries and exclude build/vendor output.
- **Analysis helpers are single-file C# (`.csx` via `dotnet-script`).** Not
  Python, not PowerShell — C# handles the multi-variable rollup and function-point
  math cleanly and keeps the toolchain on .NET. PowerShell may be used only for
  build/orchestration glue, never for the metric/FP computation.

See `PROMPT.md` for a portable single-paste version of this whole pipeline.
