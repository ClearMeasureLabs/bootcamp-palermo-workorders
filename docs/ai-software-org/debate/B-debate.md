# B — Debate Round: Work Decomposer critique of A (frontier), C (runtime), D (governance)

This round checks the other three designs against B's catalog (`B-tasks.md`: 401 task types, 26 guilds, 32 roles). It asks four questions:
1. Can C's runtime fire every trigger and timer the catalog needs?
2. Does D's autonomy model agree with B's autonomy assignments?
3. What do A, C and D imply that B lacks?
4. Is 401 the right granularity?

All counts below come from the catalog data file and a classification script (`families.py`) in the same folder.

---

## Agreements

1. **Task over role** (A position 1; C "one definition, one trigger contract, one output schema"; D autonomy "per task type"). B's catalog is the task-type inventory all three presuppose but do not enumerate.
2. **State lives outside the model.** The GitHub board is the system of record, and every run starts with fresh context. B's column-owner agents map one-to-one onto C's `board-column-entered` route.
3. **Mechanical liveness, and no agent's claim is accepted.** C's postconditions, D's re-derivation rule and B's DoD/merge checkers are the same rule. Pairing webhooks with timer sweeps matches A anti-pattern 8.
4. **Self-generated backlog is evidence-gated and capped.** D's version is the most rigorous and is adopted (X4).
5. **Humans own intent, spend, irreversible acts and the guardrails.** B §8 and D §3.3 name nearly the same categories.

---

## Disagreements (with proposed resolution)

### X1. Granularity: C defines about 22 agents, B defines 401 task types

C's `aiorg-control/agents/` tree has about 22 definitions, from `triage-router` through `implementer`, `pr-reviewer` and `alert-investigator` to `concierge`. It has 11 routes and 10 schedules, which cover roughly the engineering inner loop (about 60–70 of B's task types). D's §7 lists 17 governance components and has no product-facing work.

**Resolution:** both are right, at different layers. The *task type* is the unit of coverage, autonomy (D's `autonomy.yaml` is keyed by task type) and measurement. The *definition file* is the unit of maintenance. Writing 401 prompts is unmaintainable, but 22 is too coarse for D's governance. For example, "implementer" cannot carry one autonomy level when "bump patch dependency" and "add auth endpoint" sit at different risk tiers. The proposal is **task type = template family + config**; see the template-family table below. This yields 18 families (19 once the charter family is added), fewer than 30 prompt files to maintain, and 401+ addressable task types for routing, budgets, evals and autonomy.

### X2. Does C's runtime support every trigger B needs?

The catalog uses 57 distinct event bases. 105 agents are on-demand only, and 133 have at least one on-demand trigger. Coverage against C:

| Trigger class (B usage) | C support | Gap | Fix |
|---|---|---|---|
| GitHub issues, PRs, reviews, checks, push, release, projects_v2_item, sub_issues. The heaviest are `pull_request.synchronize` (32 agents), `push:main` (16), `projects_v2_item.edited` (13) and `issues.labeled` (10) | Yes (gateway §4.1) | `pull_request.synchronize[paths]` filters need the changed-file list, which is not in the webhook payload | Gateway enrichment: fetch the PR file list once per head SHA and cache it. CEL `when` then filters on paths |
| `deployment_status.success[env]` (13 agents: UX walkthrough, DAST, a11y, exploratory, smoke, perf, instrumentation) | Partly: C routes `workflow_run.completed` with workflow=Deploy | `deployment_status` is not subscribed. Deploys here go through Octopus, so the environment may be missing from the payload | Subscribe to `deployment_status` *or* add a `repository_dispatch` at the end of each `deploy.yml` job carrying `{env, version}`. C already proposes this for build.yml |
| GitHub security events: `dependabot_alert`, `code_scanning_alert`, `secret_scanning_alert`, `repository_advisory` | Not listed | Security intake depends on a timer | Add them to the App's subscriptions; they are standard webhook events |
| `issues.cross_referenced` | — | GitHub has no such webhook; it is a timeline event | Change B's trigger to a weekly sweep (already present) plus `issue_comment.created` containing a cross-repo ref |
| Monitoring events: `alert.fired`, `healthcheck.unhealthy`, `slo.burn_rate`, `exception.new_fingerprint` | Yes (Azure Monitor, Sentry) | None | — |
| Business events: `feedback.received`, `survey.closed`, `experiment.ended`, `customer.signed_up`, `dsr.received`, `support.ticket.created`, `research.*`, `cost.anomaly`, `resource.created`, `pipeline.*` (15 agents) | Only the support inbox (polled) | No adapters for product, analytics, cost (Azure Cost Management), Event Grid, data pipelines or privacy requests | Generic HMAC-verified `POST /events/{type}` endpoint plus a per-source schema. Every payload is untrusted and routed through the READ family (see X5) |
| Internal events: `agent.completed`, `agent.token_spend`, `finding.emitted`, `release.candidate`, `incident.resolved`, `okr.changed`, `agent.registry.changed` | Partly: `aiorg.*` bus events exist (`timer.fired`, `watchdog.stall`) | C's principle is "agents never call each other; the loop closes through GitHub." Observer findings have no work item to comment on | Two channels. **Work handoffs** go through the GitHub blackboard (C). **Findings** go to D's Signal Store, not the bus. `agent.*` events come from the dispatcher's run lifecycle, not from agents |
| On-demand handoffs (`o:agent`) | Yes, via the `handoff-next` route | `handoff.next` is a single agent. B has fan-out (code-review-orchestrator to 5 lenses, technical-design-author to 4 designers) and join (aggregate the review) | Make `next` an array, and add a `join: {await: [agents], timeoutMin}` route type that waits until all handoffs exist for a `run_key` lineage |
| **Delayed, one-shot triggers**: `release.published[+30d]`, D's outcome-verification windows, D's 48 h clarification timeout, D's 24 h veto window, SLA re-escalation | Only escalation SLA timers | Nobody specified a per-item "fire at T" primitive. At least 5 B agents and 3 D components depend on one | Add a `deferred_events(fire_at, event)` table to C's scheduler, reusing the same leader loop. It is the runtime twin of `send_later` |

**Cadence check** against C's scheduler (Postgres leader, 1-minute cron resolution):

| Cadence (B agents) | Azure profile | Local profile | Resolution |
|---|---|---|---|
| 1m / 5m / 15m (6 agents: health, dependency health, log anomaly, SLA clocks, watchdog) | OK as `agent: null` mechanical jobs | **Fails.** Routines have a 1 h minimum. GitHub Actions `schedule:` has a 5-minute floor and is often delayed under load | Move sub-15-minute *sensing* to the monitoring platform (App Insights availability tests and alert rules), which pushes events. The org clock then only needs ≥15 minutes |
| hourly, daily, weekday, nightly | OK | Routines OK (daily run caps apply) | — |
| weekly (**65 agents**, all on Monday 07:00 in B's draft) | Works, but floods budget guards and the GitHub API | Exceeds Routine caps | Spread weekly jobs across weekdays by guild and use C's deterministic jitter. B's calendar is revised accordingly |
| biweekly | Cron cannot express "even ISO weeks" | Same | Add `every: 14d, anchor: <date>` to the schedule schema |
| monthly / quarterly / annual | OK | OK | — |
| **Ordered daily chain** (observers, then aggregator, then generator, then prioritizer, then dispatcher) | C has no dependency between schedules. Jitter of up to 1800 s can reorder them | Same | Only the first stage runs on a timer. Each later stage is event-triggered by the previous stage's completion (`after:` or `on: aiorg.run.succeeded[agent=X]`) |
| Quiet hours and freezes | Calendars defer timers | — | B's probes, `incident-declarer` and `rollback-executor` must set `calendarOverride: true`. B's `deploy-freeze-enforcer` becomes C's calendar guard rather than a separate agent |

### X3. Autonomy: B's A/R/H against D's L0–L4 and T0–T4

B's model assigns one static autonomy level to each agent. D keys autonomy to *task type × risk tier of the change*. D's model is better because the same agent (`dependency-update-proposer`) can produce a T1 patch bump or a T3 new package. **Resolution:** B's A/R/H becomes the *ceiling* for each task type. The effective level is `min(ceiling, D-level(task type), tier-rule(change))`, computed by D's deterministic risk classifier (B's `change-risk-scorer`, which becomes deterministic). The 13 changes below were reconciled one by one:

| B task type | B (draft) | D position | Resolution |
|---|---|---|---|
| `ux-acceptance-gate` | H | UI flows are T2: auto plus governor plus a 24 h veto window | **R** with a veto window, judged against `charter/taste.md`. Stays H only for new top-level flows and brand surfaces |
| `prod-deploy-gate` | H (policy-auto later) | T0–T2 auto with canary; T3/T4 need a human key | Adopt D's tiers directly |
| `migration-author` / `migration-reviewer` | A / A | Additive is T2; destructive is T4 with two keys and a restore drill | Adopt D. **C conflicts with D:** C's `requireHumanApprovalFor` gates *all* schema migrations. Proposal: C's rule is the Crawl/Walk default, and D's linter-based tiering applies from Run |
| `secret-rotation-runner`, `secrets-store-maintainer` | R | Secret creation/rotation is T4 (two keys) | **H** |
| `secret-leak-responder` | H | Kill switch auto-trips on a secret hit | Split it. *Containment* (revoke the token, trip the breaker) is **A** under the Governor identity (P6). History purge and replacement secret are **H** |
| `iac-implementer`, `rightsizing-advisor`, `container-runtime-tuner` | R | Infra/IAM is T4 | The PR stays R. *Apply to prod* is H through an environment protection rule |
| `agent-permission-auditor` | R | New permission scope needs a human | Reductions are **A**; grants are **H** (new `permission-manifest-keeper`) |
| `kill-switch-guardian` | A | Trip is auto; *release* is owner-only | Trip stays A; add H for release |
| `agent-prompt-tuner`, `process-improvement-implementer`, `skill-library-curator` | R | Skill/prompt files are protected; the Skill Smith must beat golden plus trap tasks on a harness it cannot edit | **R**, with protected-surface routing (Crawl/Walk: H). The harness and golden suite are human-owned |
| B §9.7 "agents move R→A on evidence" | Implied automatic | Autonomy *promotions* need a human; demotions are automatic | Promotion is **H** (new `autonomy-promotion-proposer`); demotion is **A** (new deterministic `autonomy-demoter`) |
| `flaky-test-quarantiner` | R | The test-diff guard fails on any added skip unless the *test-owner identity* labels it | Runs under the TEST-family identity, never the implementer's. Quarantine counts toward the digest |
| `agent-proposal-gate` | H (policy-auto for classes) | T0–T1 auto; T2 governor plus veto; T3/T4 human; D's incubator comes first | Adopt D. B's generator feeds D's incubator (X4) |
| `pr-merger`, `merge-readiness-checker` | A (LLM agents) | Merge agent is deterministic, P4 identity | **Deterministic services** (X6) |

After reconciliation, B's human-gated count is 42 + 5 − 1 = **46 of about 434** task types, about 11%. That is still consistent with D's ≤30 min/day target, because about 30 of them fire monthly or less.

### X4. The self-generated backlog pipeline has three incompatible drafts

- **B:** `finding.emitted`, then `signal-aggregator`, then `backlog-generator-from-signals`, then `agent-proposal-gate`.
- **C:** each observer files proposals directly with a fingerprint.
- **D:** sensor, then Signal Store, then detector (rules), then hypothesis card, then incubator (cooling, evidence independence, caps, source quotas), then proposal, then a blind prioritizer.

**Resolution:** adopt D's pipeline as the backbone. C's model, where each observer files proposals itself, breaks D's rule that "no single agent both notices a problem and decides it deserves work," and it allows a single sensor family to flood the backlog. B's changes:
- `signal-aggregator` becomes D's deterministic detector.
- `backlog-generator-from-signals` splits into `hypothesis-card-writer` and `proposal-writer`.
- Add `incubator-keeper` (deterministic) and `outcome-verifier`.
- `backlog-prioritizer` loses visibility of item origin.
- B's `idea-incubator` (strategic bets) is renamed `strategic-bet-proposer` to avoid colliding with D's term.

### X5. Untrusted input is processed by writer agents

D's P1 "quarantined reader" rule and A pattern P14 expose a real defect in B. Several B agents read raw untrusted text *and* write:
- `bug-reproducer` pushes a test.
- `support-ticket-resolver` replies to customers.
- `question-answerer` comments.
- `owner-request-intake` creates issues from chat and email.

**Resolution:** READ becomes a mandatory *stage*, not only a family. Any task type whose inputs include issue, comment, ticket, log, web or feedback text gets a compiled two-step run: a P1 reader with no tools except `emit(schema)` produces the structured summary, and then the actor runs on that summary. It is a template rule the dispatcher enforces from `inputs.trust`, not a new task type.

### X6. B modelled deterministic machinery as LLM agents

The CTRL (34) and PROBE (35) families include `event-router`, `work-claim-lock`, `board-column-mover`, `wip-limit-enforcer`, `version-bumper`, `migration-numbering-guard`, `merge-readiness-checker` and `pr-merger`. **B concedes** to D's rule: "LLMs generate and explain; deterministic code decides and enforces." About 55 of these 69 become C services, `agent: null` schedules or dispatcher guards. They are the highest-cadence jobs, so this removes most of the invocation *frequency*.

---

## Gaps nobody covered

Each gap was checked against all four documents.

1. **Delayed/one-shot events as a runtime primitive** (X2). D's outcome verification, veto windows and clarification timeouts, and B's +30-day adoption reviews all assume a primitive that no design specifies.
2. **Fan-out/join semantics** in the handoff protocol (X2). Review lenses and design fan-out need a barrier.
3. **Owner absence and bus factor of one.** Nothing covers an unreachable owner during a T3/T4 wait. Needed: a charter delegate, an `owner-availability-router`, and a safe mode that lets only T0–T1 work and incidents proceed.
4. **Multiple humans.** Conflicting owners, onboarding and offboarding are undefined. `access-review-runner` also needs a membership-change trigger.
5. **LLM provider outage.** The incident agents are LLM-backed themselves. A deterministic degraded mode is needed: freeze writes, page the human, keep probes and rollback running.
6. **Transcript and ledger data governance.** Transcripts contain customer PII from logs and untrusted payloads, and nobody sets their retention. Needed: `transcript-retention-enforcer` (OPS) plus masking before the transcript is stored.
7. **Owner comprehension debt.** The one human may stop understanding the system they approve. Needed: a monthly `owner-comprehension-briefer` built from ADRs and cartography diffs.
8. **AI-authored code provenance and IP.** Needed: licence-contamination and snippet-similarity checks on agent PRs, and an AI-authorship register for legal review. B's `ai-system-register-keeper` covers the agents but not the code they produce.
9. **Coverage beyond engineering.** About 150 B task types (UX, a11y, l10n, analytics, support, legal, FinOps, comms) have no route, adapter or sensor. They can wait until phase 3+, but the generic event endpoint should ship in phase 1.
10. **Evaluation assets as curated work.** Golden suites and holdouts are required, but nobody enumerates the tasks that produce them. These are added in R1.

---

## Revised recommendations

### R1. New task types (33 added, all merged into existing guilds)

| Task type | Family | Autonomy | Source |
|---|---|---|---|
| `test-integrity-auditor` (test-diff and assertion-strength review, different model family) | LENS | A | D §4.2 |
| `cross-vendor-reviewer` | LENS | A | D §4.1 inv. 3 |
| `test-diff-guard`, `literal-leak-scanner` (deterministic) | SCAN | A | D §4.2 |
| `holdout-scenario-curator` (writes to a repo builders cannot read) | TEST | R | A P4, D §4.2 |
| `holdout-suite-runner` | VERIFY | A | A P4 |
| `golden-task-curator` (mines merged history for candidate cases; human approves) | META | H | D §4.8 |
| `trap-task-author` (impossible, injection and onion-violation traps) | META | R | D §4.8 |
| `model-upgrade-evaluator` | META | R | A open problem 8, C §14 |
| `red-team-exerciser` (monthly live-fire injection/tamper drills, in shadow) | META | R | D §5 |
| `lessons-curator`, `agent-memory-reviewer` | META / LENS | R | A P15, C §7.1 |
| `autonomy-promotion-proposer` | META | H | D §3.3 |
| `autonomy-demoter` (deterministic) | CTRL | A | D §4.8 |
| `hypothesis-card-writer`, `proposal-writer` | MINE | A / R | D §2.4, §2.6 |
| `incubator-keeper` (deterministic) | CTRL | A | D §2.5 |
| `outcome-verifier` (delayed trigger) | VERIFY | A | D §2.10 |
| `identity-token-broker` (deterministic) | CTRL | A | D §4.1 |
| `permission-manifest-keeper` | GATE | H | D §3.3 |
| `injection-classifier`, `mcp-server-pin-auditor` | SCAN | A | D §4.3 |
| `ping-pong-detector` (deterministic) | CTRL | A | D §4.6, C churn breaker |
| `merge-queue-integrator` | CTRL | A | A P7 |
| `api-rate-governor` (deterministic) | CTRL | A | C §6.3 |
| `charter-amendment-proposer` | GATE | H | D §3.1 |
| `daily-owner-digest` (failures first) | REPORT | A | D §3.4 |
| `claim-reverifier` (honesty score) | VERIFY | A | D §3.4, §4.8 |
| `spec-document-reader` (echo-and-confirm) | READ | A | D §3.2 |
| `owner-availability-router`, `provider-outage-degrader` | CTRL | A | Gaps 3 and 5 |
| `transcript-retention-enforcer` | OPS | A | Gap 6 |
| `owner-comprehension-briefer` | WRITE | A | Gap 7 |
| `ai-code-provenance-checker` | SCAN | A | Gap 8 |

**Removed or merged:**
- `signal-aggregator` becomes the detector (deterministic).
- `deploy-freeze-enforcer` becomes a calendar guard.
- `backlog-generator-from-signals` is split as in X4.
- `idea-incubator` is renamed `strategic-bet-proposer`.

**Net result:** about **434 task types**, of which about **75 are deterministic** (no LLM).

### R2. Template families: task type = template + config

Each family is one prompt/skill template plus a compiled policy bundle. A task type is a YAML config entry containing:
- `family`
- `trigger` and `filters`
- `lens` (rubric/checklist file)
- `tools` and `skills`
- `inputs.trust`
- `outputs` (schema)
- `postconditions`
- `identity` tier
- `runnerClass`
- `model` tier
- `budgetUsd`
- `autonomy` ceiling

Counts are from `families.py` over the original 401 task types.

| Family | # (of 401) | Example task types | LLM? | Identity (D) | Runner (C) | Model | Varies by config |
|---|---|---|---|---|---|---|---|
| **CTRL**: control service | 34 | router, lease, board mover, merge checker, WIP, version bump | No | P4/P6 | service | — | rule set, thresholds |
| **PROBE**: sensor/collector | 35 | health, drift, cert, quota, DORA, flow, cost | No (optional summary) | P0 | service / gha | haiku | query, threshold, cadence |
| **SCAN**: tool wrapper + disposition | 34 | Semgrep, TruffleHog, SCA, DAST, license, a11y static, CRAP | Tool + LLM triage | P0→P2 for fixes | gha / box | haiku / sonnet | tool command, severity map, SARIF parser |
| **READ**: quarantined reader | 19 | classifier, dedupe, ticket triage, market/competitor miner | Yes, tool-less | **P1** | gha / cloud | haiku | input source, output schema |
| **LENS**: review lens | 20 | correctness, style, tests, security, EF, UI consistency, cost | Yes | **P3** | gha | sonnet | rubric file, path filter |
| **SPEC**: design/spec author | 30 | PRD, criteria, tech design, ADR, threat model, SLO, UX flow | Yes | P2 (docs paths) | cloud / box | **opus** | template doc, required sections |
| **BUILD**: code change | 37 | feature, fix, refactor, migration, endpoint, IaC, dependency bump | Yes | **P2** | **box** (worktree) | sonnet (opus on retry) | path scope, `touches` lease, diff cap |
| **REPAIR**: PR repair loop | 6 | CI fixer, conflicts, review feedback, flaky fix | Yes | P2 | box | sonnet | max attempts, failure class |
| **TEST**: oracle author | 10 (+1) | unit/integration/acceptance/contract/holdout, repro test | Yes | **P2-test** (separate from BUILD) | box | sonnet | test level, frozen-path set |
| **VERIFY**: run and judge | 28 (+3) | functional, smoke, perf, a11y runtime, UX walkthrough, outcome | Tool + LLM verdict | P0/P5 (non-prod) | box / cloud | sonnet | env, suite, SLO thresholds |
| **OPS**: environment actuator | 21 | deploy, rollback, rotation, masking, retention, patching | Mostly scripts | **P5/P6** | service / box | sonnet (explain only) | env, blast-radius cap, two-key flag |
| **INCIDENT**: incident roles | 8 | declarer, commander, diagnostician, status page, postmortem | Mixed | P0/P6 | cloud (urgent) | opus for diagnosis | severity policy |
| **PLAN**: order and size | 19 | prioritizer (blind), estimator, splitter, roadmap, capacity | Formula + LLM | P0 (issues write) | cloud | opus | scoring model (WSJF/RICE), caps |
| **REPORT**: digest/analysis | 25 | status, health, posture, QBR, cohorts, funnels | Yes | P0 | gha | sonnet | audience, cadence, sections, "failures first" |
| **MINE**: signal to hypothesis | 16 | debt registrar, toil, revert/escape analyzers, retro | Yes | P0 | cloud | opus | signal query, hypothesis schema |
| **WRITE**: docs/comms | 29 | release notes, guides, KB, runbooks, comms drafts | Yes | P2 (docs) / H for external | gha | sonnet | audience, style guide, publish target |
| **GATE**: decision packet | 22 | vision, pricing, prod gate, DPIA, legal review, proposals | Drafts only | none (human decides) | cloud | opus | decision schema, SLA, delegate |
| **META**: agent improvement | 8 | evaluator, prompt tuner, eval regression, performance review | Yes | P2 on skills repo only | box | opus | golden set, trap set (read-only) |

That gives 18 families, or 19 once a small **CHARTER** family holds the charter-amendment and permission-manifest GATE variants, which are human-owned per D. The result is **about 19 templates, about 60 rubric/lens files and about 434 config entries**, instead of 434 prompts. Evals run per family on shared harnesses and per task type on golden cases. Autonomy promotion and demotion stay per task type, which D requires.

### R3. Asks of the other designs

- **C:**
  - Add security and `deployment_status` subscriptions and the generic event endpoint.
  - Enrich PR events with changed files.
  - Make `handoff.next` an array and add `join` routes.
  - Add `deferred_events`, `every: 14d`, `after:` chaining and weekday spreading of weekly jobs.
  - Delegate sub-15-minute sensing to monitoring.
  - Enforce the READ stage from `inputs.trust`.
  - Add `family` and `taskType` keys to `x-aiorg`.
- **D:**
  - Key `autonomy.yaml` by B's task-type IDs.
  - Adopt the ceiling × tier rule.
  - Record the migration phase transition and the owner-delegate/safe-mode clauses in `charter/`.
- **A:** Supply published evidence on which task classes reach L3 first. Check whether per-family harnesses catch lens-level regressions when a shared template changes.
