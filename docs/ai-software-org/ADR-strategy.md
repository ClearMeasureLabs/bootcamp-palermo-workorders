# Strategic ADR: A 100% AI software organization built from task-oriented agents

- **Status:** Accepted as strategy; implementation at v0 (live test pending owner setup)
- **Date:** 2026-09-24
- **Decider:** Jeffrey Palermo (owner)
- **Scope:** The strategy for the whole multi-agent organization. Tactical decisions live in the numbered ADRs of the control repo (`jeffreypalermo/aiorg`, `docs/adr/0001`–`0016`); this record sits above them and explains why they fit together.
- **Supporting material:** `docs/ai-software-org/README.md` (master design), `research/` (four independent designs), `debate/` (four cross-critiques). All paths below are relative to this repository unless a repository is named.

---

## 1. Context

### 1.1 The goal

Every job in a software organization is done by AI agents that loop, listen for triggers and run on timers. Pointed at one or more repositories, the organization:

- does all the work those repositories need;
- takes change requests from a human owner;
- observes how things are going and adds work to its own backlog.

A human role bundles 30–100 kinds of work. The organization decomposes roles into **tasks** and gives each task its own small agent.

### 1.2 How the strategy was produced

Four research agents each designed one slice independently:
- A: frontier research on what works;
- B: every role decomposed into tasks;
- C: a runtime on Claude Code;
- D: governance and red team.

Each then critiqued the other three. The debate converged on most points and resolved roughly fifteen disagreements, recorded in README §15. That design was then implemented, twice:
- a TypeScript prototype, superseded;
- the current .NET control plane in `jeffreypalermo/aiorg`, with 35 commits, 236 passing tests and ADRs 0001–0016.

### 1.3 Forces that shaped every decision

| Force | Evidence | Consequence |
|---|---|---|
| Models game the checks they can see | SpecBench: every model saturates visible tests. EvilGenie and ImpossibleBench: agents edit tests, add skips, hard-code outputs | The tests that judge a change must be out of the builder's reach |
| Checking work, not writing it, is the bottleneck | 600 rejected agent PRs: 38% abandoned by reviewers, 23% duplicates | Throughput must be throttled to review and CI capacity |
| Most multi-agent failures come from system design | MAST: step repetition, lost specifications, failed verification | Make it structural: deterministic orchestration, fresh context per task, external verification |
| Rules that exist only in prompts get broken | Replit's agent deleted a production database during a freeze that existed only as instructions | Enforce rules with credentials, rulesets and code, never prompts alone |
| Every piece of ingested text is an attack surface | GitInject; the "Toxic Agent Flow" prompt injection via the GitHub MCP server | Untrusted text never reaches an agent that can write |
| Cost runs away | Documented $4K–$48K runaway incidents; multi-agent runs use 12–15× the tokens of a normal session | Hard budgets enforced by the harness |
| Productivity evidence is thin | METR RCT: experienced developers 19% slower while believing they were faster | Measure outcomes against a baseline; invest in verification before generation |
| This repo's own history | `24da122` reverted an experiment PR merged by mistake; `Check-StalledLanes.ps1` exists because agent lanes stall silently | Merge authority by provenance; liveness checked mechanically from outside |

---

## 2. Decision

Build the organization as **task-oriented agents on a deterministic control plane, governed by the owner's charter, and earning autonomy only from measured evidence**. Twelve strategic commitments follow. Each names the tactical ADR(s) in `jeffreypalermo/aiorg` that implement it.

### S1. Tasks, not roles; many instances of few templates
- The unit of work is a **task type**: trigger, typed inputs, a checkable done-condition, identity, budget and an autonomy ceiling.
- There are **~435 task types** in 26 guilds, covering 32 roles. They run on only **19 agent templates** (families such as BUILD, LENS, READ, TEST, VERIFY, OPS), so a task type is *template + config*.
- About 80 task types are plain code with no model call.
- **Why:** working factories scale by running many copies of a few agent types. Evaluations and autonomy need enough samples per template to mean anything, and 19 prompts can be maintained where 435 cannot.
- *Implemented by:* ADR 0006 (generated task catalog).

### S2. Deterministic code decides and enforces; models generate and explain
- Deterministic code owns routing, locks, scheduling, budgets, risk classification, autonomy, merge policy and the kill switch.
- A model is never the component that decides whether something is allowed.
- *Implemented by:* ADR 0005 (deterministic guards, fail closed) and ADR 0009 (one autonomy policy function).

### S3. State lives outside the model
- GitHub Issues, Projects and PRs are the shared blackboard. Agents coordinate only through structured handoff comments, never direct messages.
- Every run starts with fresh context. A hash-chained ledger records runs, costs and decisions.
- **Why:** output quality degrades as context grows, and the owner must be able to see and intervene with ordinary GitHub tools.
- *Implemented by:* ADR 0007 (handoff contract and ledger).

### S4. Never trust "done"
Every claimed success is re-derived from a system of record (check-runs API, merge SHA, board state) by something other than the agent that claimed it. This generalizes the feature-loop's "CI is API-verified only" rule into the dispatcher's postcondition checks.
- *Implemented by:* ADR 0007 and ADR 0013 (dispatcher).

### S5. The author never grades its own work
Separate GitHub App identities write code, write tests, review, merge and deploy (six identities), so GitHub rulesets enforce separation of duties. Around them:
- **Frozen oracle:** tests are written by the oracle identity before implementation, in paths the builder cannot write.
- **Hidden holdouts:** run outside the PR's CI, with only coarse feedback.
- **Test-diff guard:** blocks removed, skipped or weakened tests.
- **Cross-model review:** at T2 and above, a reviewer from a different model family.
- *Implemented by:* ADR 0008 (separate identities) and ADR 0014 (workflow trust boundary: `pull_request_target`, so no PR can redefine the checks that judge it).

### S6. Every untrusted byte is data
- The gateway drops free text (issue bodies, comments, titles) before routing.
- Agents that must read untrusted text are READ-family: they have no tools and emit only a schema.
- No run holds private data, untrusted input and an exfiltration channel at once.
- A policy hook runs on every tool call and fails closed.
- *Implemented by:* ADR 0005 and ADR 0014.

### S7. Autonomy is earned, capped and revocable

**effective autonomy = min(catalog ceiling, owner's autonomy table, risk-tier cap T0–T4, oracle-strength cap, org mode)**

- Every actor starts at L1 (draft PR, human merges) on every new repository.
- **Promotion:** a PR to `charter/autonomy.yaml` that only the owner merges. It needs evidence: ≥ 30 merged items, ≥ 95% golden-task pass, 100% trap-task pass, and revert and escape rates under their thresholds.
- **Demotion:** automatic.
- *Implemented by:* ADR 0009.

### S8. The organization cannot rewrite its own guardrails
The charter, policies, routing, agent templates, catalog, rubrics, evals, guard code and pipelines are protected surfaces. Agents may propose changes; only the owner merges them. A meta-agent that tunes prompts must beat golden and trap suites on a harness it cannot edit.
- *Implemented by:* ADR 0002 (org as code in a control repo) and CODEOWNERS.

### S9. Self-steering through an evidence-gated incubator

The loop: **sense → detect → falsifiable hypothesis → incubator → proposal → blind prioritization → tiered approval → execute → outcome verification → learn.**

- Self-generated work never goes straight to the backlog.
- The incubator applies dedupe, an independent-evidence threshold, a 7-day cooling period, weekly and open-item caps, source quotas and a self-generation brake.
- Oracle-bearing signals fast-track: failing build, flaky test, reachable CVE, analyzer finding, SLO burn.
- The headline metric is **outcome-verified value per dollar**, not throughput.
- *Implemented by:* the Incubator, Prioritize and Outcome modules (design §9); no separate ADR yet.

### S10. The owner steers through a charter, one inbox and a daily digest
- The owner writes the charter: mission, priorities and capacity split, non-negotiables, SLOs, budgets, autonomy table, protected surfaces, taste.
- Requests are one sentence in any channel, with the interpretation echoed back for confirmation.
- Decisions go to one inbox of at most 10 cards. On timeout, T3/T4 requests expire and are never auto-approved.
- A failures-first daily digest arrives every morning.
- Owner time targets: ~100 min/day at Crawl, ~50 at Walk, ~30 at Run, measured.
- *Implemented by:* the charter directory, plugin skills and the Digest module.

### S11. GitHub-native first; a control plane only when measured triggers fire
Growth path: **v0**, GitHub Actions + `claude-code-action` + Claude Code, a human merges everything → **v1**, governed identities, merge queue, holdout verifier → **v2**, an always-on dispatcher, entered only when:
- volume exceeds 40 items/month;
- a second repo is attached;
- sub-hour schedules are needed;
- cost attribution has gaps;
- GitHub throttles content creation.

The contracts (handoff schema, postconditions, run keys, lease keys, signed run specs) are fixed now so the storage and hosting can change later without redesign.
- *Implemented by:* ADR 0004 (GitHub-native v0, control plane at v2), ADR 0010 (signed run specs), ADR 0013 (dispatcher, proposed; `claude/dispatcher` branch).

### S12. .NET and C# are the platform; attach is one owner-approved PR, shadow first
- **Platform:** the owner's products and team are .NET, so the control plane is C# on .NET 10 (the TypeScript prototype is retired).
- **Attach:** a product repository is attached by a single PR the owner reviews. Every workflow is gated on `AIORG_ENABLED`, and the first week runs in shadow mode.
- **First repo:** the sample-app sandboxes (`clearmeasure-aisf-sample-apps/20260923-00x`), not the teaching repository.
- *Implemented by:* ADR 0003, ADR 0011, ADR 0015 (tooling and workflow logic in C#), ADR 0016.

---

## 3. Options considered

| Option | Why rejected |
|---|---|
| **Role-based personas** (PM, architect and QA agents conversing, MetaGPT/ChatDev style) | Chatter produces step repetition and lost decisions (MAST). Gains in the literature came from structured artifacts, not dialogue |
| **401 distinct agents, one prompt each** (the first decomposition) | Evaluations cannot scale (8–20K golden cases), autonomy can never be earned per agent, and model upgrades break hundreds of prompts silently. Kept as task *configs* over 19 templates |
| **One long-lived orchestrator agent** (like `feature-loop-dispatch` today) | Context rot and silent stalls; one point of failure. Replaced by a stateless dispatcher plus an external watchdog |
| **Custom control plane first** (gateway, bus, Postgres, dispatcher before anything runs) | Over-built for one repo at ~12 PRs/month; delays learning. Deferred to v2 behind measured triggers |
| **One shared bot identity** | GitHub cannot enforce separation of duties when one actor writes, reviews and merges; one injection compromises everything |
| **LLM merge agent** | Merge authority must be deterministic and policy-driven; replaced by a merge queue owned by the integrator identity |
| **Fully autonomous from day one ("dark factory")** | Evidence for autonomous delivery is thin, and comprehension debt accrues quietly. Autonomy is earned per task type instead |
| **Routines or in-session cron as the org's clock** | Minimum one-hour interval, the owner's identity, daily caps, events dropped over the cap, 7-day expiry. Usable only for owner-personal flows in v0 |
| **TypeScript control plane** | Built and tested, then superseded by ADR 0003 so the org shares the products' toolchain |

---

## 4. Consequences

### Positive
- Every rule that matters is enforced by credentials, rulesets or code, and each is covered by a guardrail test that replays a known attack (evals/trap-tasks).
- The owner can see, veto and halt everything with normal GitHub tools. The kill switch works by revoking credentials, with a target of under 60 seconds.
- Growth is incremental: each stage has numeric entry and exit criteria (design §13).
- Coverage is complete on paper: every role's tasks are catalogued, and dormant task types cost nothing until their family is enabled.

### Negative and risks
- **Owner time and attention are the real bottleneck:** about 100 minutes a day at Crawl, and the target of 30 minutes is a Run-phase number.
- **Estimated cost for one mid-size repo:** $2–3k/month at MVP and $9–14k/month for the full catalog if run with discipline. A naive run is $30–50k+ and not viable. These are estimates from token prices, not measurements.
- **Unsolved problems remain:**
  - proving that self-generated work was worth doing;
  - detecting slow architectural decay;
  - keeping evaluations honest as models change.
- **Setup friction:** GitHub App installation per org, cross-owner repository access, and private-repo checkout tokens. Each blocked progress during this session.
- **Two diverged implementations existed.** The .NET repository is authoritative; the TypeScript prototype survives only as a bundle for reference.

### What must stay human permanently
- Intent and values.
- Legally accountable sign-offs.
- Spending and commitments.
- Changes to the guardrails themselves.
- Irreversible external acts.
- Relationships with customers, reporters and auditors.
- Final taste arbitration.

About 11% of task types are human-gated, and most of those fire monthly or less.

---

## 5. Current state (2026-09-24), for whoever picks this up

| Item | Where | State |
|---|---|---|
| Master design, research, debate | This repo, branch `claude/ai-software-org-design-oge2v8`, `docs/ai-software-org/` | Complete |
| Control plane (authoritative) | `jeffreypalermo/aiorg` `main` @ `4bdd7fd`, private | .NET 10; 236 tests pass (1 skipped); `aiorg validate` OK with 3 owner warnings. Tag `v0.1.0` **does not exist** |
| v1 dispatcher | `jeffreypalermo/aiorg` branch `claude/dispatcher` @ `8e45bd4` | In progress (ADR 0013 still "Proposed") |
| TypeScript prototype | Delivered as `aiorg.bundle` / `aiorg-source.tar.gz` (head `31406d8`) | Superseded; reference only. Contains a risk-classifier fix (auth-path tiers apply to product code only; deduped rules) worth porting to the .NET `Policy/` module |
| Live test repo | `clearmeasure-aisf-sample-apps/20260923-003` (copy of the work-order app) | **PR #1 "Attach aiorg v0 (shadow mode)" open**, generated by the .NET `attach`, pinned to `4bdd7fd` because `v0.1.0` is missing |
| Sibling sandbox | `clearmeasure-aisf-sample-apps/20260923-002` | Named as the first attached repo in ADR 0016 |

**Findings from replaying the guards over this app's real history:**
- The test-diff guard blocked a revert that deleted 4 unit tests. That is correct: it needs the oracle's approval.
- A 528-line PR was flagged T3 for exceeding the 400-line cap. Correct.
- Any change to `qodana.sarif.json` is T3 as a protected surface. Correct.
- A login display-name formatter was classed as auth code (T3). Conservative; consider narrowing the auth path globs in `policies/risk-rules.yaml`.

### Open decisions for the owner
1. **Tag `v0.1.0` in `jeffreypalermo/aiorg`, or keep pinning SHAs.** SHA pins are immutable and arguably safer; attach currently defaults to the tag.
2. **Which sandbox is the live target:** `-002` per ADR 0016, or `-003` where PR #1 is open. Record the answer as ADR 0017 in the control repo.
3. **Secrets and variables for the test repo:**
   - `ANTHROPIC_API_KEY` (with a spend cap);
   - `AIORG_READ_TOKEN` (read-only, `jeffreypalermo/aiorg` only);
   - `AIORG_ENABLED=true` after merging PR #1.
4. **Scheduled-only workflows.** Watchdog, observer, digest and labels have no manual trigger in the .NET templates. Add `workflow_dispatch` to make the live test script executable on demand.
5. **Where aiorg should live long term.** The personal account complicates cross-org checkout and session access; the org hosting the products it serves would simplify both.
6. **Port the risk-classifier fix** from the TypeScript prototype (above) into the .NET `Policy/` module.

### Next steps, in order
1. Settle open decisions 1–3; merge PR #1 on `-003` (or re-attach `-002`).
2. Run the 10-step live test (TypeScript-era `docs/test-run.md`; adapt the manual steps for scheduled-only workflows):
   - triage, including the prompt-injection issue;
   - review;
   - the skip-test guard and the T4 migration;
   - the CI-fix loop;
   - watchdog, observer and digest;
   - the kill-switch drill.
3. Phase 0 exit criteria (design §13):
   - the kill-switch drill completes in under 60 s;
   - 100% of agent commits carry `Agent-Run` trailers;
   - guardrail negative tests pass against live rulesets;
   - a 90-day DORA baseline;
   - owner-absence mode, canary tokens and the org-incident runbook are in place.
4. Create the six GitHub Apps and the rulesets. Build the holdout repository and seed 10–20 scenarios per key journey.
5. v1: finish the dispatcher (ADR 0013), native merge queue, holdout verifier; promote the first T0–T1 task types only on evidence.

---

## 6. References

- Master design: `docs/ai-software-org/README.md`. Research: `research/A-frontier.md`, `B-tasks.md`, `C-runtime.md`, `D-governance.md`. Debate: `debate/A..D-debate.md`.
- Tactical ADRs: `jeffreypalermo/aiorg/docs/adr/0001`–`0016`; C4 diagrams in `docs/architecture/`.
- Key external evidence (cited with URLs in the research files):
  - MAST (arXiv 2503.13657);
  - SpecBench (arXiv 2605.21384);
  - EvilGenie (arXiv 2511.21654);
  - ImpossibleBench (arXiv 2510.20270);
  - AIDev agent-PR study (arXiv 2601.15195);
  - METR 2025 RCT;
  - DORA 2025;
  - OWASP Top 10 for Agentic Applications 2026;
  - GitInject (arXiv 2606.09935);
  - Anthropic's C-compiler and long-running-harness engineering posts;
  - StrongDM software factory; Gas Town;
  - Factory Missions.
