# Run 20 — MyWorkOrders count bug + bUnit (general v4)
- bc-5dc7740e-a1ea-5558-a485-3e26f7cc58ee · #9972 · PR #9975. Transcript 32,061 B, 31 lines. **9 calls**. Est. **~44.6k** tokens (-98.5%).
- Calls: context (4.4 KB) · edit · edit with an anchor miss · **read MyWorkOrdersTests.cs (4.7 KB)**: the outline hid the stub/bus setup the new bUnit test needed · read Employee.cs (to check `Creator?.Id`) · grep · edit · Complete (30 s) · artifact.
- Lesson: for bUnit/fixture-heavy test files, the outline plus last test isn't enough. Include the fixture's helper/stub members (private classes and `[SetUp]`) in the context.
