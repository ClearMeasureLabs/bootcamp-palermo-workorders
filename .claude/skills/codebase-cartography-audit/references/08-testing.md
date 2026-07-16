# Module 8 — Testing & the Automation Pyramid

Advocated across these authorities: **Mike Cohn** (originator of the *Test
Automation Pyramid*, *Succeeding with Agile*, 2009 — fast, numerous unit tests at
the base; fewer service/integration tests; a thin cap of slow UI tests),
**Robert C. Martin** (TDD, the "three laws," tests as a first-class part of Clean
Architecture), **Jeffrey Palermo** (popularized the pyramid in .NET/Onion
practice), **Martin Fowler** (test-pyramid write-ups, "self-testing code," and the
critique of the *ice-cream-cone* anti-pattern — a term coined by Alister Scott),
and **Jeremy D. Miller** (pragmatic, fast-feedback testing; testability as a
design force).

This module both **assesses the inherited safety net** and **defines the bar new
work must meet** — consistent with the project's mandatory Definition of Done
(unit + integration + full-system tests in the same change).

## 8.1 The pyramid (Cohn)

```
        /\        Full-system / UI  — few, slow, high-value
       /  \       (Playwright for web/Blazor; FlaUI/COM for desktop/Excel).
      /----\      All third-party interfaces STUBBED/SIMULATED.
     /      \     Integration      — module boundaries: DB, messaging,
    /--------\                        HTTP endpoints, adapters.
   /          \   Unit             — many, fast, isolated logic.
  /____________\
```

**Anti-pattern:** the *ice-cream cone* / *testing hourglass* — lots of slow
end-to-end tests, little or no unit coverage. AI-generated projects usually have
**no** tests, or a few brittle happy-path E2E tests.

## 8.1b Discover & classify EVERY test suite (scales to dozens)

A large codebase may have **dozens of test suites** across many projects/packages,
and the pyramid is only meaningful once each is discovered and placed in a layer.
Do this mechanically, not by assumption — automate it in the metrics `.csx` when
there are more than a handful.

**Step 1 — discover every test suite.** A "suite" is the finest independently
runnable/attributable unit — usually a test *project/assembly* (`.csproj`
referencing a test SDK), but split further by top-level namespace/folder/fixture
when one project mixes layers. Find them by their framework markers, not folder
names: `Microsoft.NET.Test.Sdk`/`xunit`/`nunit`/`MSTest` (.NET),
`pytest`/`unittest` (Python), `jest`/`vitest`/`mocha` (JS), `junit`/`testng`
(Java), `_test.go` (Go), `*.feature` (SpecFlow/Cucumber).

**Step 2 — count each suite with the runner** (`--list-tests`/`--collect-only`),
never by grepping attributes (Module 9).

**Step 3 — classify each suite by *evidence*, not by its name**, into
**unit / integration / acceptance(full-system)** using the strongest signal present:

| Layer | Positive signals (package refs, usings, base classes, attributes) |
|-------|-------------------------------------------------------------------|
| **Unit** | only the SUT + a mocking/assertion lib (Moq, NSubstitute, FakeItEasy, FluentAssertions, Shouldly); no DB driver, no HTTP, no container, no host bootstrap; fast |
| **Integration** | EF/ORM, a DB driver or real SQLite/SQL, `WebApplicationFactory`/`TestServer`, Testcontainers, a DI-container/host boot (Lamar/Autofac), message-bus/queue clients, filesystem/network, **bUnit component render** (renders a component in isolation — NOT full-system) |
| **Acceptance / full-system** | drives the real UI or whole running app: Selenium, Playwright, Cypress, FlaUI/WinAppDriver/COM, SpecFlow `.feature` against a running host, or end-to-end against a live endpoint |

> bUnit is a component-level library (unit/integration), not full-system — do not
> file bUnit suites under Acceptance or the pyramid cap will be over-stated.

Rules for honest classification:
- Classify on the **dominant signal**; when a project genuinely mixes layers,
  split its count by namespace/fixture and report the breakdown rather than
  forcing one label — or mark it `mixed` with sub-counts.
- A folder called `IntegrationTests` that only news-up POCOs and mocks is **unit**;
  a `UnitTests` project that opens a real SQLite file is **integration**. Trust the
  signal over the name and note the discrepancy (it is itself a finding).
- Record the deciding signal per suite so the classification is auditable.

**Step 4 — emit a suite matrix and roll up to the pyramid.** One row per suite:
`suite, project/package, framework, test count (runner), layer, deciding signal`.
Sum counts per layer to get the pyramid shape. This table is what §5 of the report
and the testing C4 diagram (Module 10) are built from.

## 8.2 What to assess

- **Presence & shape:** is there a test project at all? What's the ratio of
  unit / integration / full-system? Is it inverted (ice-cream cone)? **Count tests
  with the runner, not by hand** — `dotnet test --list-tests`, `pytest
  --collect-only -q`, `go test -list '.*' ./...`, etc. — per project/layer; attribute-grepping
  miscounts parameterized/skipped cases (see Module 9). Report the tool numbers.
- **Coverage of risk:** are the god classes and money/rule logic tested, or only
  trivial getters?
- **Boundary tests:** integration tests wherever the code crosses to a DB,
  queue, HTTP endpoint, or third party?
- **Full-system:** does any test start the whole app and drive the real UI with
  externals stubbed?
- **Quality:** deterministic (no `DateTime.Now`/random/sleep flakiness)? Assert
  behavior, not implementation? Isolated (no shared mutable state)?
- **Testability as design signal:** code that is hard to test is telling you
  about DIP/SRP/coupling violations — cross-reference Modules 1, 2, 6.

**Inspection prompt**
> Assess the test suite of {scope}. (0) **Discover EVERY test suite** (all test
> projects/assemblies by framework marker, split mixed ones by namespace/fixture),
> **count each with the runner** (`--list-tests`/`--collect-only`), and **classify
> each as unit / integration / acceptance(full-system) by evidence** (package refs,
> usings, base classes) not by folder name — emit a suite matrix (suite, project,
> framework, count, layer, deciding signal) and note any suite whose name
> contradicts its signal. (1) Roll the matrix up by layer and report the pyramid
> shape — flag an inverted ice-cream-cone/hourglass. (2) Determine whether the
> highest-risk logic (the god classes,
> money/date/business rules, boundary code) is actually covered or only trivial
> paths are. (3) Check for boundary/integration tests at every DB/queue/HTTP/
> third-party seam. (4) Check whether any full-system test starts the app and
> drives the real UI with externals stubbed. (5) Flag flakiness sources
> (`DateTime.Now`, random, sleeps, shared state) and tests that assert
> implementation detail. Report gaps by risk.

## 8.3 Remediation strategy (order matters)

1. **Characterize before you refactor.** For untested code you must change,
   first write *characterization tests* that pin current behavior (Feathers,
   *Working Effectively with Legacy Code*). Then refactor safely.
2. **Break dependencies for testability** using DIP/IoC (Modules 1.5, 6.1):
   inject the clock, config, and I/O so units can run isolated.
3. **Build the pyramid bottom-up:** fast unit tests for logic, integration tests
   at each boundary, a thin full-system layer driving the UI with stubs.
4. **Enforce the Definition of Done** on all new/changed behavior: unit +
   integration + full-system in the same change, externals simulated, runnable
   headlessly in CI where possible.

**Cross-links:** hard-to-test code ⇒ inspect DIP (1.5), SRP (1.1), Onion (2),
IoC & persistence ignorance (6). Testability is the fastest litmus test for the
whole audit.
