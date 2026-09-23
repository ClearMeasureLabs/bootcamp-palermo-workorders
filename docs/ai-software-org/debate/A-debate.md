# A — Debate Round: Frontier Researcher critique of B (tasks), C (runtime), D (governance)

Lens: what the published evidence from teams actually running agent factories (Anthropic's C-compiler team, Anthropic's long-running harness, Factory Missions, Gas Town, StrongDM, AIDev/MAST/SpecBench/EvilGenie research) says about each design choice. Citations are in `A-frontier.md` §9 unless given inline.

---

## 1. Agreements

The four designs converge on more than they diverge. These points are settled and should not be relitigated:

1. **Task-oriented agents, with roles kept only as provenance.** B's "roles survive only as tags," C's "one definition, one trigger contract, one output schema," and D's "each arrow is a separate small task agent" all match frontier practice.
2. **Deterministic code decides and enforces; LLMs generate and explain.** D states it outright ("LLMs generate and explain; deterministic code decides and enforces"). C builds it: kill switch at three layers, `PreToolUse` lease checks, and a dispatcher that verifies postconditions via the API. B has cheap probes emitting events. This matches the C-compiler harness (git lock files, GCC oracle), Gas Town (Witness/Deacon/Refinery are mechanical), and the repo's own `Check-StalledLanes.ps1`.
3. **Never trust the agent's "done."** C's postcondition verification and D's "no agent's claim is accepted" generalize the feature-loop's API-verified CI rule. This is the single most validated pattern in the evidence.
4. **GitHub as the human-visible blackboard.** C's handoff comment with a fenced JSON block, labels, sub-issues, and board columns keeps state durable and inspectable. That is the Anthropic harness and Gas Town "beads" pattern, applied through GitHub.
5. **Frozen oracle, hidden holdouts, test-diff guard.** D §4.2 is the best treatment in the set. It directly answers SpecBench (visible tests are always saturated) and EvilGenie (Claude Code and Codex caught editing tests).
6. **Quarantined readers for untrusted text, least-privilege App identities, no broad PATs.** C §11 and D §4.3 answer GitInject and the Toxic Agent Flow correctly.
7. **Self-generated work is proposal-only by default, deduped, capped, and outcome-verified.** D's Incubator (caps, source quotas, cooling) and outcome verification go beyond anything shipping commercially. Jules, Seer, and AI-PM tools all stop at "proposed."
8. **Event-primary, timer-as-safety-net.** B §9.6 and C's 15-minute rescan are right. Routine GitHub events are documented to be *dropped* over hourly caps.
9. **Staged rollout with shadow mode first.** C's shadow week and D's Phase 0–3 exit criteria both follow the graduated-autonomy model that AI SRE vendors use.

---

## 2. Disagreements (with proposed resolution)

### 2.1 B: 401 agents is the wrong granularity. Keep ~15 parameterized agent templates and 401 *task specs*.

**The evidence against 401 distinct agents:**

- **No working factory looks like this.** The C-compiler team ran one feature-worker type plus about five standing specializations: dedup, performance, output quality, design critique, and docs. Gas Town has seven lifecycle functions. Factory Missions has orchestrator, worker, and validator. Anthropic Research has lead, subagent, and citation pass. Throughput in all of them came from *many instances of few types*, not many types.
- **Evaluation economics break.** D's promotion rule (≥30 merged items and ≥95% golden-task pass per task type) multiplied by 401 types means most types never gather enough evidence to be promoted or demoted. At perhaps 5–15 merged items per day, the typical type in B would take years to reach n=30. Autonomy that can never be earned stays at L1 forever, or gets granted without evidence. Neither is acceptable.
- **Handoff chains compound failure.** B's Critical Path A runs about 20 hops from `owner-request-intake` to production. MAST's top failure classes are specification loss and verification failure *at handoffs*, and Cognition's point is that every hop drops implicit decisions. At 97% per-hop fidelity, 20 hops deliver about 54% end-to-end. Factory's evidence is that "the planning phase is where most of the value comes from": one rich spec conversation, not five serial document writers (problem-statement → PRD → acceptance criteria → …).
- **Maintenance surface.** 401 prompts means 401 regression suites and 401 things a model upgrade can silently break. The repo already struggles to keep two skills (`feature-loop` and its Cursor mirror) in sync.
- **Report inflation.** B schedules 65 weekly and 35 monthly jobs, most of which produce reports. Reports are not value, and the AIDev data shows reviewer attention is already the scarcest resource (38% of rejected agent PRs were simply abandoned).

**Resolution.** Separate three concepts that B merges:

| Concept | Count | What it is |
|---|---|---|
| **Agent template** | ~12–18 | A prompt plus harness plus tool policy plus output schema, evaluated as one unit. Examples: `Observer(sensor, rubric)`, `Reader(source schema)`, `Specifier`, `TestDesigner`, `Builder(scope)`, `Reviewer(lens)`, `Verifier(oracle)`, `Integrator`, `Operator(runbook)`, `Writer(artifact type)`, `Reporter(audience)`, `Gatekeeper(policy)`, `Hypothesizer`, `Retro/SkillSmith`. |
| **Task spec** | 401 (B's catalog, kept) | A config row: `{template, parameters, trigger, inputs, done-check, risk tier, budget}`. For example, `sca-scanner` = `Observer(sensor=dotnet-vulnerable+owasp, rubric=cve-reachability)`. |
| **Deterministic job** | many | Everything in G00 (router, scheduler, lease, budget, audit, registry) and most sensors. These are code, not agents. |

Autonomy is then tracked per *template × risk tier*, which pools evidence across specs so promotions become statistically meaningful. Per-spec statistics are still logged for demotion. B's catalog stays valuable as a **coverage checklist** that proves nothing a real org does was left out, and as the configuration backlog. The MVP should be about 8 templates and about 25 specs, not "about 60 agents."

Also collapse B's design chain into one `Specifier` run. It produces an EARS-style spec plus acceptance scenarios, and it may hold a clarification exchange with the owner. It is followed by an independent `TestDesigner` that writes the frozen oracle. Two hops, not six.

### 2.2 C: A custom Postgres dispatcher is justified eventually, but not first. Start GitHub-native.

C's analysis of the platform gaps is correct:
- Routines run as the owner's identity.
- Routines have daily caps, a 1-hour minimum interval, and GitHub triggers only for PR and release events.
- Agent teams are not available headless.
- Plugin subagents drop hooks.
- This repo's full build needs SQL Server, Playwright, and Docker.

The conclusion does not follow, though. Those gaps call for *GitHub Actions + a GitHub App + self-hosted runners*, not a new control plane. Gas Town runs 20–30 agents on a single binary with git-backed state, and the C-compiler team coordinated 16 agents with lock files in git.

GitHub-native mapping of C's components:

| C component | GitHub-native equivalent (Phase 1) | Known weakness |
|---|---|---|
| Gateway + CloudEvents | Workflow `on:` triggers (all event types, including `issue_comment`, `check_suite`, `workflow_run`, `repository_dispatch` for Azure Monitor/Sentry via a relay) | No custom dedupe; handled by `run_key` markers C already specifies |
| Router (`routes.yaml`) | One `aiorg-route.yml` workflow that evaluates `routes.yaml` and fans out with `workflow_dispatch` | Actions fan-out latency of seconds to minutes (acceptable) |
| Leases | `concurrency: group: issue-${n}` with cancel-in-progress false, plus branch protection, plus C's `path:`/`migrations` leases as labels or a lock file in a `aiorg-state` branch (C-compiler style) | Concurrency groups keep only one *pending* run and drop others; the reconciler must re-derive |
| Scheduler | Actions `schedule:` (5-minute minimum) for org timers; Routines only for owner-personal flows | Cron jitter under load |
| `box` runner | Self-hosted Actions runner (Container Apps Jobs or a VM) with SQL Server and Playwright | None beyond ops |
| Ledger | Run record as a structured Actions job summary plus an appended JSONL in an `aiorg-ledger` branch or artifact; `total_cost_usd` from the claude-code-action output; nightly aggregator into a Markdown/HTML dashboard | No transactional budget *reservation* |
| Budgets | `--max-budget-usd` per run (hard), plus a daily aggregate check in the router workflow (soft) | Soft org-level cap can overshoot by in-flight runs |
| Kill switch | Repo/org variable `AIORG_MODE` checked by the router and by a `PreToolUse` hook; App suspension for HALT | Same as C |

**Build C's Postgres dispatcher when measured triggers fire**, not up front:
- More than about 3 repos, or cross-repo leases are needed.
- Budget overshoot from non-atomic reservation exceeds 10% of the daily envelope.
- Concurrency-group drops cause more than 1 lost trigger per week (visible in reconciler logs).
- Actions queue latency blocks the incident path.

C's *contracts* are the durable intellectual property and are store-agnostic: handoff schema, postconditions, `run_key` idempotency, lease key taxonomy, failure-class table, compile-don't-discover. Keep them exactly and swap the storage later. This also answers C's own §14 risk ("research-preview features change"): Actions plus the SDK are GA.

Secondary C issues:
- **"Retry with +50% budget if progress observed"** conflicts with SpecBench (more iterations did not close the holdout gap and sometimes widened it) and with the runaway-cost incidents. Replace it with: at most one continuation, then a *different* template (Reviewer diagnoses) or escalation. Never raise the budget automatically.
- **Three runner classes** add complexity for little gain. Two are enough: GitHub-hosted for light work, self-hosted for heavy work. Treat Managed Agents/cloud as an optional adapter later.
- **The long-lived concierge session** will suffer context rot (Chroma). Make it stateless: every owner query rebuilds context from the ledger and board.
- **Per-agent `MEMORY.md` via PR** creates a review stream that lands on the human. Gate memory PRs on the golden suite (D §4.9) and batch them weekly.

### 2.3 D: correct principles, but too many axes and thresholds tuned for a high-traffic product

- **Two 5-level matrices without a join.** D defines change risk tiers T0–T4 and task-type autonomy levels L0–L4, plus permission tiers P0–P6. Nothing specifies the effective permission when an L3 task type produces a T2 change. **Resolution:** one decision table, `allowed_action = min(L(template), ceiling(T(change)))`, where T3 caps at "human merge" and T4 at "two-key." Encode it as a single policy file that the Gatekeeper evaluates.
- **Seven identities is over-engineered for a single-owner org at Phase 1.** Start with four GitHub App identities: Reader (no write), Builder (`agent/*` branches, draft PRs), Reviewer/Merger (reviews plus merge-queue enqueue; may not push), and Operator (deploy to non-prod, revert). Split further only when an incident shows the need. The separation that matters most, author ≠ approver ≠ merger, survives.
- **"Different model family reviewer" on every PR** is plausible but unmeasured, and it doubles vendor surface. Reviewer noise has a cost: the best commercial reviewer sits at roughly 49% precision, while Tricorder's bar is 90%. **Resolution:** require cross-family review at T2+ only; track each reviewer's precision (the finding was fixed, not declined) and drop reviewers below 70%.
- **Incubator thresholds assume rich telemetry.** This repo is a low-traffic training app with essentially no customer or usage signal. "Independent signals," a 7-day cooling period, and a 40% source quota will either starve the Incubator or block the only productive sensor family (static analysis/CI). **Resolution:** calibrate per repo. Oracle-bearing classes (flaky test, CVE, Qodana/CRAP finding, failing build) bypass the independence requirement and cooling. The quotas apply only to speculative product ideas.
- **Outcome verification needs a "structurally verifiable" class.** Many changes have no runtime metric in a low-traffic app. Accept structural outcomes (CRAP delta, flake rate, finding count, bundle size) and cap "unverifiable" closures at a quota, rather than generating *revert-or-keep* cards for every null result.
- **Holdout authorship.** D has the test-author agent write the holdouts. If it shares a model family and the spec with the builder, blind spots correlate. StrongDM's holdouts are *human-curated*. **Resolution:** for T2+ features, the owner approves the scenario list (one checklist in the spec PR), and agents implement the scenarios in the private holdout repo.
- **Stryker.NET and other new tools** fall under the CLAUDE.md "no new packages without approval" rule. List them as owner-gated Phase 2 items, not defaults.

### 2.4 Cross-cutting: B and C over-weight the control plane; everyone under-weights the merge path

B puts 18 agents in G00, C builds a gateway, bus, dispatcher, ledger, and watchdog, and D has a governor. Nobody specifies a **merge queue**, even though merge contention is the dominant physics once more than 2–3 builders run (Gas Town's Refinery exists for exactly this reason). C's `path:` leases prevent some conflicts but serialize builders early and pessimistically. **Resolution:** GitHub native merge queue with the required `build-result` check. Builders never merge; the Integrator enqueues. On a red queue batch, bisect and bounce the culprit back to its lane, which is the Refinery pattern. Keep only the `repo:migrations` lease, because DbUp's sequential numbering is a true serialization point.

---

## 3. Gaps nobody covered

1. **Autonomy should be capped by oracle strength, not only by agent track record.** The C-compiler lesson ("the verifier must be nearly perfect") implies an area with 20% coverage and no acceptance tests cannot support L3 for *any* agent. Add an **oracle-strength score** per code area: coverage, mutation score where available, presence of acceptance and holdout scenarios, and architecture-rule tests. The effective level is `min(L, oracle_ceiling(area))`. Low-oracle areas generate "strengthen oracle" work first.
2. **A cost model with numbers.** No design estimates spend. Anchors: the C compiler cost ~$10 per session ($20K/2,000); Factory missions run ~12× a normal session; StrongDM spends $1,000/day per engineer. For this repo, a plausible Phase 2 steady state is 5–10 items/day at $5–30 each plus observers at about $20/day, so **$50–350/day**. Set the org envelope explicitly in the charter, alert at 3× median hourly spend (D), and report **cost per outcome-verified item** as the headline metric.
3. **Harness ergonomics for agents (context-thrifty tooling).** `PrivateBuild.ps1` and `AcceptanceTests.ps1` are long and verbose. The C-compiler team found that time blindness and log flooding waste most tokens. Needed:
   - a `--fast` sampled test mode for the inner loop, with the full suite in CI only;
   - one-line grep-able failure summaries;
   - a build-output summarizer hook (`PostToolUse`) that truncates logs before they enter context.
   This is cheap and probably the largest single cost lever.
4. **Duplicate work across heterogeneous workers.** `docs/ai-workers.md` lists Cursor, Copilot, Claude, and Bob, and humans also commit. Duplicates are 23% of rejected agent PRs. Every Builder start needs a **pre-flight collision check**: open PRs and branches touching the same issue or paths from *any* author, including other vendors' agents.
5. **Master-breakage response.** Only D's "harm → revert proposal" addresses it. Add a deterministic rule: if the default branch goes red after an agent merge, the Integrator auto-reverts that merge within N minutes (P6 Operator identity), then files a child issue. This is standard in Refinery- and Bors-style systems.
6. **Model and harness drift as a first-class trigger.** C pins model IDs and D has golden suites, but nobody routes "new model/Claude Code version available" as an *event* that runs the golden suite in shadow before adoption. Claude Code ships multiple versions per week, and a hook or schema behavior change is as dangerous as a model change.
7. **Human-attention accounting.** D targets ≤30 min/day, but B defines 42 human-gated specs, C defines approval-gated actions, and D defines T3/T4 keys, with no shared queue. Required: **one decision inbox** (a single issue label or digest) with a per-day attention budget. When it is exceeded, the org stops *generating* gated work rather than queuing more of it.
8. **Evaluation of decomposition itself.** Nobody measures whether an epic was split well: rework loops, children reopened, parent-clamp pullbacks. The repo's parent-clamp comments are already that signal. Mine them.
9. **Public-repo exposure.** If the target repo is public, anyone can file an issue or comment. Trust should be assigned by **author identity** (owner or org member = trusted request, others = quarantined reader), not per text field. This also removes D's burden of scanning every owner request.

---

## 4. Revised recommendations (merged position)

1. **Catalog ≠ agents.** Adopt B's 401 rows as *task specs* on top of about 15 *agent templates*. Autonomy and evals attach to templates × risk tier. The MVP is 8 templates and about 25 specs: Reader, Specifier, TestDesigner, Builder, Reviewer(lenses), Verifier, Integrator, and Observer(CI/flake, Qodana/CRAP, CVE, stall).
2. **GitHub-native first, dispatcher later.** Use:
   - Actions workflows as router and scheduler;
   - concurrency groups plus lock files as leases;
   - self-hosted runners for the heavy .NET build;
   - claude-code-action or the SDK with `--max-budget-usd` and structured output;
   - a GitHub App per identity class;
   - a JSONL ledger with a nightly dashboard.

   Keep C's contracts verbatim. Move to C's Postgres dispatcher only on the measured triggers in §2.2.
3. **One policy function.** `effective_autonomy = min(L(template, tier), oracle_ceiling(area), change_ceiling(T))`, evaluated deterministically, stored as a protected file, and changed only by the owner.
4. **Oracle first.** Keep D's frozen oracle, test-diff guard, and hidden holdouts. Owner-approved scenario lists for T2+. Autonomy capped by oracle strength.
5. **Merge queue plus auto-revert** as the only path to the default branch. Builders never merge.
6. **No automatic budget escalation on retry.** Allow one continuation, then a different template or escalation. Enforce a hard per-run cap and a charter-level daily envelope. Headline metric: cost per outcome-verified item.
7. **Incubator calibrated per repo.** Oracle-bearing signal classes fast-track; speculative ideas face D's evidence, cooling, and quota rules. Self-generation brake at 4:1 (D).
8. **Four identities at Phase 1**: Reader, Builder, Reviewer/Merger, Operator. Split further on evidence.
9. **Context-thrifty harness work is Phase 0**: fast sampled tests, log summarization hooks, one-line failure output.
10. **Single decision inbox with an attention budget.** When it is exceeded, generation of gated work pauses.
11. **Drift events**: model or Claude Code version bumps run the golden suite in shadow before adoption.
12. **Keep the repo's feature-loop as the Builder lane's reference behavior.** Its rules (API-verified CI, children-first, parent clamp, bot-finding triage) move from prompt text into router code and hooks, as C proposes. Keep the skill as the human-invocable fallback.
