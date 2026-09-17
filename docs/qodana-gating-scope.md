# Qodana Gating Scope (#9519)

Records the decision on which Qodana inspection severities gate the build, and where.

## Decision

The whole-solution single Qodana run stays hard-gated at `failThreshold: 0` in
`qodana.yaml` and `.github/workflows/build.yml` — one linter invocation, no second
`--solution`/scope run.

Two inspections that repeatedly fired on brand-new, otherwise-correct test files are
downgraded from gating to `none` severity, **scoped only to the three test-project
folders**:

- `RedundantUsingDirective`
- `RedundantNullableWarningSuppression`

Scoping is done with a directory-scoped `.editorconfig` in each test project:

- `src/UnitTests/.editorconfig`
- `src/IntegrationTests/.editorconfig`
- `src/AcceptanceTests/.editorconfig`

Each sets:

```
[*.cs]
resharper_redundant_using_directive_highlighting = none
resharper_redundant_nullable_warning_suppression_highlighting = none
```

`root = false` is set explicitly so these files layer on top of `src/.editorconfig`
(naming conventions, other `resharper_*`/`dotnet_diagnostic_*` severities) instead of
stopping the upward `.editorconfig` search for the folder.

**`src/.editorconfig` (production scope) is untouched.** Both inspection IDs remain
gating in every production folder under `src/` — this change does not weaken correctness
or security coverage anywhere outside the three test folders, and it does not weaken
either inspection ID inside the test folders for anything except this style pair.

## Why `.editorconfig` and not `qodana.yaml` `exclude:`

`#8986` already proved `jetbrains/qodana-cdnet` does not honor `qodana.yaml` path
excludes, even name-only ones — every existing `exclude:` entry in `qodana.yaml` is
documentation-only, with real enforcement done by file-level `// ReSharper disable`
comments. `.editorconfig` severity keys are read directly by the ReSharper/Qodana
InspectCode engine via standard `.editorconfig` directory cascading, which is not subject
to that bug. `src/.editorconfig` already sets other `resharper_*`/`dotnet_diagnostic_*`
severities today, so this follows an established, working convention rather than
introducing a new one.

## Why not a second Qodana invocation or a custom inspection profile

- A second `--solution` scoped to only production `.csproj`s means maintaining a second
  `.sln` in sync with every new project, and it would stop scanning test code for
  correctness/security entirely — not acceptable.
- Community `qodana-cdnet` supports `failThreshold` only, not `severityThresholds`, so
  there is no built-in "gate only certain categories" invocation. Doing this with a custom
  `.idea/*.xml` inspection profile is a much larger, harder-to-review surface than three
  `.editorconfig` lines, and would still need the `.editorconfig` fallback anyway.

## Scope limits (intentional)

- Only the two named inspection IDs are downgraded. A future AI-authored test file that
  trips a *different* style inspection (e.g. `RedundantAttributeUsage`,
  `ArrangeStaticMemberQualifier`) still hard-fails the build under `failThreshold: 0` —
  this is a narrow, observed-failure-mode fix, not a blanket "style findings in tests are
  non-gating" policy.
- Every other inspection Qodana reports — including the ones documented in `qodana.yaml`
  (`ConditionalAccessQualifierIsNonNullableAccordingToAPIContract`,
  `UnusedAutoPropertyAccessor.*`, `NotAccessedPositionalProperty.*`,
  `MethodHasAsyncOverload`, `PropertyCanBeMadeInitOnly.*`, `UnusedType.Global`,
  `UnusedMethodReturnValue.Global`, `ConvertToPrimaryConstructor`) — remains gating
  everywhere, including inside the three test folders.

## Verification

`src/UnitTests/BuildGates/QodanaTestScopeGateTests.cs` asserts the gate config, the build
workflow's `--fail-threshold,0` / `--solution` args, and the presence of the two downgrade
keys in each test-project `.editorconfig`, and asserts `src/.editorconfig` does **not**
carry either downgrade key.

A full-system probe was run on PR for #9519: a scratch file with a deliberately redundant
`using` was pushed to `src/UnitTests`, `gh api
repos/ClearMeasureLabs/bootcamp-palermo-workorders/commits/{sha}/check-runs` was polled to
confirm `Qodana (Community .NET)` and `Build result` stayed `success`, then the scratch
file was removed in a follow-up commit. See the PR description for the run URL and
recorded conclusions.

## Related

- #9519 (this change)
- #8986 (qodana.yaml path-exclude non-functionality)
- #9039 / #9440 (file-level `// ReSharper disable` pattern for correctness/security-scoped
  exceptions, which this change does not replace)
