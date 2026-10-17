# Run 5 — unit-test type, canned recipe

- Agent bc-caaf07fd-a9c9-5aaf-979e-406f7d445e4a · issue #9945 · PR #9946 (+33/-0; closed unmerged by the dispatcher).
- Transcript 7,922 B, 14 lines. **3 tool calls**: Get-TestContext (1.6 KB result), one file write, Complete-AgentItem (30 s). No token fields, so figures are estimates.
- Est. replayed context **~3.3k** (vs run 4's ~37k generic lean: -91%; vs the 2.90M baseline: **-99.9%**).
- No remaining waste: the 3 calls are the floor.

## Type 3 selection
All Qodana findings listed in #9436-#9439 are already fixed on master, and qodana.sarif.json has 1 non-mechanical result. Type 3 = **visibility: seal a public class with no subclasses or mocks** (`ClassCanBeSealed`), verified by grep for `: Type`, `For<Type>` and `Mock<Type>`.
