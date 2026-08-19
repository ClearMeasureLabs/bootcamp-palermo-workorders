# Module 9 — Inventory, Metrics & Linting (tool-driven)

This module produces the **objective, measured** half of the audit: lines of
code, cyclomatic complexity, and lint/analyzer findings — gathered by running
real command-line tools, not by eyeballing. Every number in the report must be
reproducible by re-running a command that is recorded in the report.

## 9.1 Principle

- **Every objective number comes from an executable tool, never from inference.**
  LOC, class/method counts, cyclomatic complexity, test counts, lint counts,
  duplication, function points, and cost are all produced by a command or a
  committed `.csx` script whose output is captured in `metrics/`. Do NOT eyeball a
  directory and assert "~600 tests" or "about 300 classes" — run the tool and
  quote its number with the command. Only the *qualitative* defect findings are
  analyst-authored, and each still cites `file:line`. If a number cannot be
  produced by a tool, label it explicitly as an estimate and show the method.
- **Test counts come from the test runner, not from counting `[Test]` attributes
  by hand.** Attribute-grepping miscounts parameterized cases (`[TestCase]`,
  `[Theory]`) and disabled tests. Use the framework's own enumeration —
  `dotnet test <proj> --list-tests` (.NET/VSTest), `pytest --collect-only -q`,
  `go test -list '.*' ./...`, and for Java the Surefire run summary
  (`Tests run: N` — NOT `-DskipTests`, which runs nothing). For Jest, `--listTests`
  lists test *files* only; get case counts from a run with `--json`/a reporter.
  Count what the runner reports, per project/layer.
- **Scripting language for helpers: single-file C#.** Any custom analysis logic
  (rollups, McCabe counting, function-point math, parsing tool JSON) is written
  as a **single-file C# script** (`.csx`) run with `dotnet-script` — NOT Python
  and NOT PowerShell. C# suits this logic (many typed variables, weight tables,
  records) and keeps the toolchain aligned with a .NET target. PowerShell is fine
  for build/orchestration glue, but the metric/FP computation lives in `.csx`.
  Acquire the runner with `dotnet tool install -g dotnet-script` (pin `--version
  1.5.0` on SDK 6). Run with `dotnet script metrics/metrics.csx`.
- **Right tool for the stack.** Detect the languages first (Module: architecture
  map), then pick tools that actually parse those languages. A polyglot repo
  needs a polyglot LOC tool plus per-language complexity/lint tools.
- **Component-level rollups.** Aggregate file metrics up to the architectural
  components (projects/modules/packages) so they can annotate the logical C4
  diagram (Module 10) and rank refactor risk.

## 9.2 Tool selection matrix

| Need | Language-agnostic | .NET / C# | JS/TS | Python | Java | Go |
|------|-------------------|-----------|-------|--------|------|-----|
| LOC + file complexity | **scc** (fast, single Go binary; gives Lines/Code/Comments/Complexity per file & per language) or `cloc` | scc | scc | scc | scc | scc |
| Cyclomatic complexity | **own `.csx` McCabe counter** (§9.4) is the portable default | `Microsoft.CodeAnalysis.Metrics` (`msbuild /t:Metrics`) for per-method CC; else the `.csx` counter | ESLint `complexity` rule, `plato`, `ts-complex` | `radon cc` | `pmd`, `checkstyle` | `gocyclo` |
| Lint / analyzers | — | **Roslynator analyze** (diagnostics only — it has no CC command), `dotnet format --verify-no-changes`, built-in Roslyn analyzers | ESLint | `ruff`, `flake8`, `pylint` | `checkstyle`, `pmd`, SpotBugs | `go vet`, `staticcheck` |
| Duplication | `jscpd` (polyglot clone-block detector) | jscpd | jscpd | jscpd | pmd-cpd | dupl |

> `dotnet-counters` is a runtime perf-counter monitor and cannot compute CC;
> `roslynator analyze` emits analyzer diagnostics, not per-method complexity;
> scc `--dup` only collapses whole-file duplicates (not clone blocks). Compute CC
> with the `.csx` counter (§9.4) or `Microsoft.CodeAnalysis.Metrics`, and detect
> clones with jscpd/PMD-CPD.

**scc** is the workhorse for LOC + a fast complexity proxy across every language
in one pass and needs no runtime. Use it first, always. Then add a per-language
complexity/lint tool for the dominant language(s).

### Acquiring tools when absent (record every step)
- `scc`: download the release binary for the OS from `boyter/scc` GitHub releases
  (single executable, no install). On Windows: fetch the `_Windows_x86_64.zip`,
  unzip, run `scc.exe`.
- `Roslynator.DotNet.Cli`: `dotnet tool install -g Roslynator.DotNet.Cli`. If the
  SDK is older than the latest CLI's target (e.g. SDK 6 vs a net8 CLI), pin a
  compatible older version (`--version 0.8.6` works on SDK 6).
- `PlantUML`: download `plantuml.jar` from the plantuml GitHub release; needs a
  JRE and (for component/deployment diagrams) Graphviz `dot` on PATH.
- Python tools: `pip install radon ruff`. JS: `npm i -g jscpd eslint`.
- Prefer no-install binaries and `-g`/`pipx` tools; never modify the target repo
  to install a tool. Exclude `bin/`, `obj/`, `node_modules/`, `dist/`, generated
  and vendored code from every metric run and say so.

## 9.3 Commands (record these verbatim in the report)

```bash
# LOC + complexity, whole repo, per language, excluding build output
# scc over the WHOLE REPO (not just src) so root build/deploy scripts are counted (9.3b)
scc --exclude-dir bin,obj,node_modules,dist,.git,<report-dir> . > metrics/scc-summary.txt
# machine-readable for rollups + diagram annotation
scc --exclude-dir bin,obj,node_modules,dist,.git,<report-dir> --by-file --format json . > metrics/scc-by-file.json

# C# analyzer / lint findings (diagnostics only — NOT cyclomatic complexity)
roslynator analyze path/to.sln --output metrics/roslynator.xml

# duplication (polyglot clone-block detector)
jscpd --min-lines 8 --reporters json,console -o metrics/jscpd .
```

Always capture: tool name, version (`--version`), the exact command, and the
raw output file path. The report cites these so the metrics are reproducible.

> `--exclude-dir` only removes whole directories; the **file-pattern** exclusions
> of §9.3b (`*.min.*`, `*.g.cs`, `*.designer.cs`, generated/seed dumps) are applied
> **in `metrics.csx` when it classifies files** — the scc command alone does NOT
> satisfy the exclusion rule. State this so no one claims the raw scc total is the
> code total.

## 9.3b Two LOC categories: System vs DevOps/Test

Classify **all** code — including tests and build/deploy assets — into exactly two
categories, and report a subtotal for each plus a grand total (for LOC, classes,
CC, and downstream FP and cost):

- **System code** — the product runtime delivered to end users (the application
  projects/modules).
- **DevOps/Test code** — everything that supports building, verifying, migrating,
  and deploying the system: **all test suites**, **build/deploy scripts**
  (`*.ps1`, `*.yml`, `Dockerfile`, `docker-compose`, `*.cake`, CI YAML),
  database-migration/deploy projects, and one-off tooling utilities. These are
  DevOps assets, not shipped functionality.

Run scc over the **whole repository** (not just `src/`) so root-level build/deploy
scripts are counted; then map each file to a category. The System/DevOps-Test LOC
split is itself a finding — a test+DevOps estate larger than the product is common
and worth calling out. Keep the classification list explicit and auditable in the
script, and note any judgment calls (e.g. whether a data-load utility is product
or tooling).

**Reconciliation rule (mandatory — this is where LLMs go wrong).** Every LOC in a
category subtotal MUST be attributable to a **named component line item**, and each
subtotal MUST equal the sum of its listed components. Compute subtotals *as* the
sum of the rows — never emit a subtotal larger than the visible rows. If a
whole-repo walk adds LOC that no component claims (a "root/misc" bucket), that
bucket must appear as its own explicit line item; a large unexplained gap (e.g.
tens of thousands of LOC not shown against any component) is a **bug to fix, not a
number to report** — it silently inflates FP and cost downstream. Print a
reconciliation check: `Σ(components) == subtotal` per category and overall.

**Exclude non-authored code from the LOC tally** (or list it in a clearly-separate
"excluded" bucket, never inside System or DevOps/Test): vendored/third-party
(`node_modules`, `packages`, `wwwroot/lib`, `Scripts` vendored libs), **minified**
(`*.min.js`, `*.min.css`), **generated** (`*.g.cs`, `*.designer.cs`,
`*.Generated.cs`, `obj/`, scaffolded migrations dumps, `*.feature.cs`), and pure
data/config blobs (large `.json`/`.sql` seed dumps). Flag any single file or
directory contributing >~2,000 LOC and confirm it is authored source before
counting it — a lone generated/minified/seed file can add tens of thousands of
phantom LOC and wreck the totals.

## 9.4 Rolling up for the annotated diagram (single-file C# script)

Write a `metrics/metrics.csx` that:
1. Parses `scc-by-file.json` (`System.Text.Json`).
2. Maps each file to its architectural component (usually its project/module —
   the directory that owns the `.csproj`/`package.json`/`go.mod`).
3. Sums `Code` (LOC) per component and computes **genuine McCabe cyclomatic
   complexity** itself rather than trusting scc's keyword proxy: for each `.cs`/
   `.razor` file, strip block/line comments and string literals, then
   `CC = max(methodCount,1) + decisionPoints` where a decision point is
   `if | for | foreach | while | case | catch | && | || | ?? | ?:`. Count only the
   `if` (an `else`/`else if` adds its decision via that `if`; don't double-count).
   **Do NOT count nullable-type `?` (e.g. `int?`) as a decision point** — that
   over-states complexity badly for EF-generated entities; match only true
   `??`/`?:` operators. Including `&&`/`||`/`??` makes this **extended (CC2-style)
   McCabe**, so numbers read a little higher than strict-McCabe tools — state this
   so the figures are comparable. The file-level `max(methods,1)+decisions` sum is
   equivalent to summing per-method CC across the file.
4. Counts **classes/objects** per component (`class|record|struct|interface`
   declarations) and reports **average CC per class = total CC ÷ class count**
   alongside total CC. (Note the Razor caveat: `.razor` files often declare no
   `class` keyword, so CC/class is inflated for UI projects — flag it.)
5. Computes **complexity density** = total CC ÷ code LOC (size-independent
   hotspot signal).
6. Emits `component-rollup.txt` + `.json` with
   `component, category(System|DevOps/Test|Excluded), files, LOC, classes,
   methods, CC, CC-per-class, density, hottestFile(maxCC)`, **per-category subtotal
   rows** (System, DevOps/Test, and an **Excluded (non-authored)** row listing what
   was removed and why), then the GRAND TOTAL, the System/DevOps-Test LOC split %,
   and a top-15 most-complex-files list. Print the reconciliation identity
   explicitly: **Σ(System) + Σ(DevOps/Test) + Σ(Excluded) == whole-repo scc code
   total** — this is the clean check that the whole-repo scan and the categories
   agree, with no phantom LOC.
7. Feeds LOC + CC into the logical diagram box labels (Module 10.4).

Keep it one file, run it with `dotnet script metrics/metrics.csx`, and commit the
`.csx` alongside its output so the numbers are reproducible.

## 9.5 Interpreting the numbers (don't just print them)

- **Complexity per method > 10** = review; **> 20** = high risk / likely a god
  method. scc reports per-file; a single file with complexity in the hundreds is
  a god class or a mega-switch — cross-link to Modules 1 (SRP/OCP) and 4.
- **High LOC + high density component** = the refactor-risk epicenter; it should
  top the backlog and needs characterization tests first (Module 8).
- **Lint volume** is a proxy for consistency debt; group analyzer hits by rule id
  and report the top offenders, not the raw wall of warnings.
- **Duplication blocks** map straight to the Duplicated Code smell (Module 4);
  quote the two largest clone sites.

**Inspection prompt**
> Detect the languages in {scope}. Acquire scc (LOC) plus a per-language lint/
> duplication tool for the dominant language(s), and `dotnet-script` for the C#
> rollup; record how each was obtained and its version. Run them excluding build/
> vendor output, saving raw output to `metrics/`. Produce (every number from a
> tool, not inference): (a) a language breakdown table; (b) a per-component
> rollup with files, LOC, class count, methods, total McCabe CC, **average CC per
> class**, and density; (c) the top 10 most-complex files; (d) a lint summary
> grouped by rule; (e) the largest duplication clones; (f) **test counts per
> layer from the test runner** (`--list-tests`/`--collect-only`), not from
> attribute grepping. Cite every command so the numbers are reproducible.
