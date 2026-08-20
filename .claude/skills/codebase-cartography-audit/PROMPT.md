# Portable Cartography-Audit Prompt (standalone)

Paste into any capable coding-agent session when you can't load the skill. It
compresses the full pipeline: map → measure → analyze → diagram → assemble.

---

You are performing a **full cartographic audit** of a codebase I inherited
(possibly AI-generated). Produce a **self-contained report directory** with
embedded, rendered architecture diagrams and measured metrics. Do not mutate the
target repo; install tools globally or as standalone binaries; exclude
build/vendor output (`bin`,`obj`,`node_modules`,`dist`,`.git`) from every metric.

**Output-fidelity rules (do not skip — these are the usual failure modes):**
(a) Emit **full tables**, not prose summaries — the function-point count MUST show
the IFPUG function-type table (count × weight = FP), all 14 GSCs with a
justification each + the VAF math, per-category backfiring tables, and a
subtotals+total table; never collapse to "EI59 EO25 … VAF 1.21". (b) **Reconcile
every subtotal**: Σ(components) must equal each category subtotal and the grand
total; a subtotal bigger than the listed rows means phantom LOC — find/attribute or
exclude it, print the check. (c) **Exclude non-authored code** (vendored, minified,
generated, seed/config dumps) from LOC; flag any file/dir >~2k LOC. (d) Count
**ILF/EIF as aggregate roots/logical files, not per-table**; rate GSCs
conservatively (default 2–3) and show them. (e) **Never sum System + DevOps/Test at
the full-lifecycle rate** (double-counts tests) — headline is System-only.
(f) **Actually open/Read each rendered PNG** before claiming it passed.

Run this ordered pipeline:

1. **Scope & inventory.** Detect languages/stack, solution layout, entry points,
   module list, file/LOC size. Create one self-contained directory
   `codebase-audit-report/` containing `README.md`, `images/`, `diagrams/`, and
   `metrics/` (all four inside it, so relative links resolve).

2. **Architecture map.** Reconstruct layers and dependency direction from project
   references and imports. Do all dependencies point inward to a framework-free
   domain core (Onion/Clean)? Where are the violations and cycles?

3. **Measure (real tools, recorded commands).** Acquire and run: **scc** for LOC
   per file/language (`scc --by-file --format json`); a per-language lint tool
   (C#: Roslynator; JS/TS: ESLint; Python: ruff; Java: pmd/checkstyle; Go:
   staticcheck); a duplication tool (jscpd/pmd-cpd). Save raw output to
   `metrics/`. Write the analysis helpers as **single-file C# scripts** (`.csx`,
   run with `dotnet-script` — NOT Python, NOT PowerShell): one that rolls up
   per-component LOC + class/object count + genuine McCabe cyclomatic complexity
   (strip comments/strings; `CC = max(methods,1)+decisionPoints`; do not count
   nullable-type `?`) + **average CC per class** + density, and one for function
   points (3b). **Get test counts from the runner** (`dotnet test --list-tests` /
   `pytest --collect-only`), never by grepping `[Test]` attributes. Every
   objective number must come from a tool, not inference. Cite versions + commands.
   **Run scc over the WHOLE repo** (not just `src/`) and classify every file into
   **two categories — System** (product runtime) vs **DevOps/Test** (all test
   suites + build/deploy scripts + migration/tooling projects). Report a
   **subtotal per category plus a grand total** for LOC, classes, and CC, and the
   System/DevOps-Test LOC split %.

3b. **Function points (two methods, reconciled).** In a `.csx` script:
   (A) **IFPUG** — derive EI (command handlers), EQ (queries), EO (reports/
   exports), ILF (app-owned aggregates), EIF (external data sources) counts from
   the code (document each basis); weight at average (EI 4 / EO 5 / EQ 4 / ILF 10
   / EIF 7); UFP → VAF (`0.65 + 0.01·ΣGSC` over the 14 GSCs) → AFP — this is the
   **System** FP (delivered functionality).
   (B) **Capers Jones backfiring** — LOC ÷ Jones LOC/FP ratios (C#≈54, JS≈47,
   markup≈40, SQL≈13–21; note scc counts physical LOC ~1.2–1.5× logical, so state
   the adjustment) per category: System LOC (cross-checks IFPUG) and DevOps/Test
   LOC (its primary **equivalent FP**). Reconcile the System count (% divergence,
   ±30%), then report **System FP + DevOps/Test FP + Total**.

3c. **Economic valuation (effort + cost + schedule) across MULTIPLE bands.** In a
   `.csx` script: (1) **Effort** = FP ÷ FP-per-man-day for a band of
   **0.32 / 0.5 / 1 / 2 / 3 FP/day** (include the 0.32 Jones-baseline row so the
   headline is readable; → man-months at ~22 days); cite Capers Jones
   (full-lifecycle ~7 FP/staff-month ≈ 0.32 FP/day — "Software Economics and
   Function Point Metrics", IFPUG 2017) and Steve McConnell ("Software Estimation")
   for the 10× productivity spread (NOT a 1 FP/day figure). Show the LOC/man-day
   equivalent. (2) **Cost** = effort × fully-loaded day rate (median base × burden
   1.5–1.75× ÷ 260); look up CURRENT medians (web search) from PayScale,
   Salary.com, US BLS (+ note aggregators) and cite each; give a **cost row per
   band split System / DevOps-Test / Total** (Module 11.0 FP subtotals) — except
   the 0.32 full-lifecycle row, where DevOps/Test and Total are "n/a (in System)"
   (the Total column is a coding-centric figure only; never sum System + DevOps at
   the lifecycle rate — it double-counts test-writing). Headline = System
   full-lifecycle figure alone. Replacement cost, not market value. (3) **Schedule check** = calendar months ≈ FP^exponent (0.32 small →
   0.45 large) and implied team size; flag if implausible.

4. **Defects & smells.** Following the highest LOC×complexity components first,
   audit: SOLID (SRP/OCP/LSP/ISP/DIP); Onion/Clean dependency-direction & leaked
   infrastructure; Fowler smells (long method/large class/primitive obsession/
   data clumps/duplication/dead code/feature envy/shotgun surgery); enterprise/
   domain (anemic model, transaction-script creep, repository/UoW, value
   objects); AI anti-patterns (magic strings/numbers, hardcoded secrets [Critical],
   swallowed catches, sync-over-async, dead code). Every finding: `file:line`,
   principle, why-it-hurts, named refactoring, effort/risk (S/M/L). Rank by
   severity × spread. Severity: **Critical** (bug/security/data-loss), **High**
   (blocks change / spreads widely), **Medium** (localized), **Low** (cosmetic).

5. **Testing.** **Discover every test suite** (all test projects/assemblies by
   framework marker — there may be dozens — splitting mixed projects by namespace/
   fixture), **count each with the runner** (`--list-tests`/`--collect-only`, not
   attribute grepping), and **classify each as unit / integration /
   acceptance(full-system) by evidence** (package refs, usings, base classes, not
   folder name; flag names that contradict their signal). Emit a suite matrix
   (suite, project, framework, count, layer, signal), roll up by layer, and report
   the pyramid shape (flag ice-cream-cone/hourglass); is the risky logic covered;
   boundary tests at each DB/queue/HTTP/third-party seam; any full-system test
   that starts the app and drives the UI with externals stubbed; flakiness
   sources. Thin coverage ⇒ backlog leads with characterization tests.

6. **Diagrams (PlantUML → PNG, inspected).** Author under `diagrams/` five views:
   (1) **logical** C4 component, (2) **runtime** container/dynamic, (3)
   **dependencies** build-time graph with arrow direction + cycles, (4)
   **testing** suites-by-layer showing the pyramid, (5) **build/devops/deploy**
   source→CI→artifacts→environments. Plus a **logical-annotated** view whose
   boxes carry LOC and cyclomatic complexity from step 3. Use the **C4-PlantUML**
   template library by default (`!include <C4/…>` — renders offline via Graphviz);
   fall back to plain PlantUML only if the bundled stdlib is missing. **ASCII
   labels only** (no em-dashes/arrows — they mojibake). Show the **System vs
   DevOps/Test** split as separate `System_Boundary`s with subtotal labels in the
   logical, dependency, testing, and deploy views. Render into the report's images
   dir. **Then open/read each rendered PNG and verify legibility** — no clipping,
   no overlap, correct arrow direction, annotations readable, sane aspect ratio.
   Fix the source and re-render any that fail. Confirm each image was inspected.

7. **Assemble `codebase-audit-report/README.md`** (self-contained dir holding
   `README.md`, `images/`, `diagrams/`, `metrics/`): how-to-reproduce
   (tools+versions+commands); executive summary (3–5 systemic problems + single
   highest-leverage fix); the embedded diagram PNGs (`![](images/xx.png)`) each
   with a reading; inventory & metrics tables — **each of LOC, function points, and
   cost shown as System / DevOps-Test / Total** — plus per-component rollup
   (with class count & avg CC/class), top-10 complex files, lint summary,
   duplication clones; prioritized findings grouped and sorted by severity ×
   spread; testing assessment (the suite matrix); and a sequenced refactoring
   backlog flagging where characterization tests must come first.

**Self-check before delivering** (any missing item = not done): the 5-row IFPUG
table; the 14-row GSC table + VAF math; per-category backfiring tables; the printed
`Σ(components)==subtotal` reconciliation + an Excluded (non-authored) bucket; the
per-band cost table with 0.32-row cells marked "n/a (in System)"; System/DevOps-
Test/Total for LOC, FP, and cost; and a legibility verdict for each of the 6 PNGs
you actually opened.

Deliver the path to the finished report directory and a short summary of the
worst findings.
