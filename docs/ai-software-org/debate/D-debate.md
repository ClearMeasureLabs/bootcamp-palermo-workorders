# D — Debate Round: Governor & Red Team review of A, B, C

*Scope: reads `A-frontier.md` (frontier research), `B-tasks.md` (401 task agents: 259 A / 100 R / 42 H), and `C-runtime.md` (aiorg plugin + `aiorg-control` repo, gateway, dispatcher, Postgres ledger, box/gha/cloud runners, `aiorg-bot` GitHub App) against the principles in `D-governance.md`. Row numbers such as B 06.06 refer to B's master catalog; § numbers refer to C's section numbers.*

---

## 1. Agreements

The four designs agree on more than they differ. These points can be treated as settled.

1. **Task types, not personas.** A §0.1, B §9.1, C §0 and D all make the task type the unit of agency, with roles kept only as provenance or coverage tags.
2. **State lives outside the model.** GitHub Issues/Projects is the authoritative work state (C §7.1, A §8.1). The ledger holds runtime state only. Agents are disposable.
3. **Liveness is checked mechanically from outside.** All four keep this repo's `Check-StalledLanes.ps1` pattern (B 00.05, C watchdog, A §0.5). A and C both add an event-plus-reconciler hybrid because webhooks get dropped.
4. **Guardrails are enforced by the harness, not the prompt.** C's three-layer kill switch (§8.2), with gateway, dispatcher and a `PreToolUse` hook, is a good concrete version of D §4.6. Other examples are C's `--max-budget-usd` on every run, its "compile, don't discover" policy loading (§0, "Agent loading"), and A's "hooks as law" (§8.1).
5. **Approvals come from the GitHub actor, never from comment text.** C §7.4 states this explicitly. It matches D §3.2 ("authority comes from channel identity, never from content").
6. **Guardrail files cannot be self-modified.** C puts `policies/**`, `routing/**` and `agents/**` behind CODEOWNERS with a human merge. B's §8 item 4 says "an organization that can relax its own gates has no gates."
7. **Self-generated work passes a gate.** Observers file proposals with fingerprints and evidence (C §7.5, B G25, A §8.2), and none of them self-authorize.
8. **Budgets and circuit breakers are first-class.** C's `churn:{item}` breaker (>6 runs or >$25 without column progress) is a good concrete version of D's "2× cost envelope" rule and should be adopted.
9. **Autonomy is a ladder earned by evidence.** A §0.9, B §9.7 and D §4.8 all agree. They differ on what the ladder is keyed on (see Disagreement 1).

---

## 2. Disagreements (with proposed resolution)

### D1. Autonomy keyed per agent (B) vs per task type × repo × risk tier (D, A)

B §9.7 says "Autonomy is a dial per agent." Since B's agents *are* task types, most of the difference disappears. Two problems remain:

- **B's A/R/H letters are fixed in the catalog.** In D, autonomy is a *runtime value* in `autonomy.yaml`, which is human-owned, starts low, and is promoted by evidence. B's own §9.7 says agents "start at R and move to A", yet 259 agents are listed as A on day one.
- **One agent can span several risk tiers.** `feature-implementer` touching a CSS class and `feature-implementer` touching `AuthorizationHandler` are the same agent but different tiers. The autonomy key has to include the risk tier of the *change*, not only the identity of the agent.

**Resolution.** Keep B's catalog letters as the *ceiling* for each task type. The *effective* level is `min(ceiling, autonomy.yaml[task_type][repo], tier_cap[change_risk])`. Every **actor** agent (anything that mutates code, infra, data or external state) starts at L1/R on a new repo, whatever B's letter says. Observers (B §9.4: about 265 agents that only report) can start at A because they cannot mutate anything.

### D2. B "A" assignments that must be earned or gated

The following B rows conflict with D's principles. They are listed with a proposed change.

| B row | Agent | B | Problem | Proposed |
|---|---|---|---|---|
| 12.12 | `migration-deploy-runner` | A | Applies DbUp scripts. C §7.4 treats schema migrations as human-gated, so B and C contradict each other. D classifies destructive DDL/DML as T4. | A for TDD/UAT only. In prod, additive changes (T2) go through `prod-deploy-gate`; destructive changes (T4) need two keys plus restore-drill evidence. |
| 06.06 | `migration-author` | A | Authoring is fine, but a migration linter must classify the change before review. | A for authoring, with a mandatory deterministic migration linter that sets the tier. |
| 08.14 + C §7.5 | `flaky-test-quarantiner` / C's auto-accept list "flaky test quarantine" | R / policy-auto | **Quarantining a test is removing it from the oracle.** It is the most common reward-hacking path (ImpossibleBench: test modification). Auto-accepting it breaks D §4.2. | Quarantine only by a test-owner identity, never the implementer. Each quarantine expires after 14 days, is capped at N per repo, counts in the digest, and blocks merges that touch the quarantined test's subject code. Remove it from C's auto-accept list. |
| 07.15 | `pr-merger` | A | Acceptable *only* if the merger is deterministic and holds a separate identity (see D4). With C's single `aiorg-bot`, the author and the merger are the same actor. | A once the identity split is in place. Until then, R. |
| 18.07, 22.03, 17.13 | `close-the-loop-notifier`, `changelog-page-publisher`, `docs-site-publisher` | A | External or public communication. B's own §8 makes `release-comms-writer` and `website-content-updater` H, so this is internally inconsistent. | R using templates approved once by the owner. A only for templated notices with no free-text LLM content. |
| 01.15 | `hotfix-intake` | A | Triggered by `issues.labeled[hotfix]` and "skips the queue." Anyone who can apply a label, including a compromised triage agent, gets a fast lane past prioritization. | Trigger only when the labeler is an owner or `incident-commander` with an open incident ID. The change keeps its normal risk tier: skipping the queue must not mean skipping gates. |
| 00.09 | `human-decision-recorder` | A | Triggered on `issue_comment.created[approval keyword]`. If it matches keywords without checking the actor, it is an approval-spoofing channel. | Record decisions only from the allow-listed owner actor. T3/T4 only via native GitHub review or environment approval (see D5). |
| 25.09 | `human-override-learner` | R | "Turns repeated approvals into policy." That is an **autonomy promotion written by an agent**, which relaxes gates. | H. It may *propose* a change to `autonomy.yaml`, and the owner merges it. |
| 00.12 / 03.14 | `agent-prompt-tuner`, `process-improvement-implementer` | R (reviewed by `code-review-orchestrator`) | An agent reviewing an agent's changes to agent prompts is a self-improvement loop. C §11 requires a human merge through CODEOWNERS, so B and C conflict. | Follow C: human merge on `aiorg-control/agents/**`, plus an eval harness the tuner cannot edit (D §4.9). |
| 14.17 | `alert-noise-reducer` | R | An agent that can tune alerts can silence the alerts about its own failures, which is metric gaming at org level. | Any change that lowers alert sensitivity for an SLO-bound alert is T3. |
| 10.04 | `secret-rotation-runner` | R | Rotating credentials can cause outages and is part of the credential surface. | T4 two-key for prod secrets; R for non-prod. |
| 18.03 | `customer-log-investigator` | A | Reads customer logs (untrusted input *and* private data). If it can also comment publicly, all three legs of the lethal trifecta are present. | P1 quarantined reader. Output goes through the schema, and it has no public-comment ability. |

**Net effect:** about 12 rows change. The 42-gate count barely moves, because most of these become R or conditional rather than H. That matters for owner load (§4 below).

### D3. Idea incubator: human gate (B) vs mechanical filter (D)

B makes `idea-incubator` and `agent-proposal-gate` H. D's incubator is mechanical: dedupe, an independent-evidence score, a cooling period, caps and source quotas. The owner sees only *graduates*.

**Resolution.** The incubator's filtering is mechanical and the graduation decision is tiered:
- Enumerated low-risk classes (flaky *fix*, not quarantine; patch bump with no new transitive packages; docs drift; Qodana mechanical cleanups) are policy-auto within the weekly cap.
- Product-scope bets and anything outside the charter's capacity allocation go to H.

Making every proposal H would put about 5 decisions a day in the owner's inbox forever.

### D4. C: one GitHub App identity for everything

C §3.2 creates one `aiorg-bot` App with Contents, Issues, PRs, Checks, Actions:read and Projects. C §11 says "all writes go through the `aiorg-bot` GitHub App." That breaks separation of duties at the GitHub layer:
- The implementer, reviewer and merger are the same GitHub actor. GitHub rulesets cannot tell them apart, so "the author cannot merge its own PR" is left to the dispatcher's good behavior.
- A prompt-injected implementer with that token can do anything any agent can do in that repo (label `hotfix`, comment `/aiorg` commands, close issues, edit the board).

**Resolution: at least four Apps plus a token broker.**

| App | Scopes | Used by |
|---|---|---|
| `aiorg-reader` | metadata, issues:read, contents:read | observers, quarantined readers |
| `aiorg-builder` | contents:write limited by ruleset to `agent/*` branches, pull_requests:write | implementers, test-authors (a separate installation, or a path ruleset for oracle paths) |
| `aiorg-reviewer` | pull_requests:write (reviews only), checks:read | reviewer lenses, Test Integrity Auditor |
| `aiorg-integrator` | merge rights via ruleset bypass *only* for the merge queue; no content push | deterministic merger |
| `aiorg-release` | deployments, environments (non-prod) | deployment orchestrator; prod needs human environment approval |

Private keys sit in a **token broker**, a small service separate from the dispatcher. The broker mints a 1-hour, one-repo token only when the run's agent type matches a signed mapping in `aiorg-control/policies/identity-map.yaml`. A compromised dispatcher can then request only what the policy allows, not everything.

### D5. C: dispatcher and repository_dispatch as a high-value target

Several parts of C's design concentrate control in the dispatcher:
- C §10.8's `aiorg-gha.yml` takes `settings: ${{ github.event.client_payload.settings_json }}` and `claude_args: ${{ github.event.client_payload.claude_args }}` **from the dispatch payload**. Whoever can send `repository_dispatch` can therefore choose the settings, permissions and hooks the agent runs under, and the runner holds `secrets.AIORG_APP_TOKEN`. That is the dispatcher or anyone with a write token (including a leaked builder token).
- `AIORG_APP_TOKEN` as a static repo secret is suspect. App installation tokens expire in 1 hour, so a static secret is either stale or a long-lived credential (a PAT or the App's key).
- Runners install the plugin from `aiorg-control`'s default branch. A merge there changes every agent in every repo within minutes, which is a supply-chain single point of failure.

**Resolution:**
- The payload carries only `run_id`. The runner fetches the run spec, which is signed by the dispatcher key and pinned to an `aiorg-control` commit SHA. The runner verifies the signature and checks that the spec's policy hash matches that SHA before starting.
- Mint the token inside the job with `actions/create-github-app-token` from the least-privileged App. No static token secret.
- Pin plugin installs to a release tag or SHA. Promote control-repo releases through the same shadow → canary → full path as model changes (D §4.8).
- The dispatcher gets no merge or deploy authority of its own. It schedules; GitHub rulesets and environments decide.

### D6. Ledger vs GitHub as the source of truth

C is mostly right: GitHub is authoritative for work, and the ledger handles runs, leases and costs. There are three refinements:

1. **Approvals must be GitHub-native.** Use PR reviews by the owner account, environment protection approvals and CODEOWNERS, not `/aiorg approve` parsed by the router into the ledger. That way GitHub itself enforces the gate even if the dispatcher or ledger is compromised. C §7.4 also accepts `/aiorg approve` "from owners or users with write permission". Write permission is too broad, because every human collaborator and any compromised account would qualify. Restrict approvals to `owners`, and require two keys for T4.
2. **The audit trail cannot live only in a mutable Postgres the dispatcher writes to.** Hash-chain the records and export them continuously to write-once blob storage.
3. **Fail closed.** The in-run kill check reads a ledger flag with a 5-second TTL. If the ledger is unreachable, writer agents must stop rather than continue on a stale cache. Add a second budget backstop outside the org: provider-side workspace spend limits, so a dispatcher bug cannot overspend.

### D7. Owner load: is 30 minutes a day realistic?

Not in the first phases. B's 42 gates overstate the load, because most fire quarterly. C's `requireHumanApprovalFor` understates it, because it includes *all* schema migrations and *every* UAT promotion. Estimated owner time for one repo at this repo's recent pace (about 8 to 10 feature items a week, judging from issues #9521–#9530):

| Load source | Crawl | Walk | Run |
|---|---|---|---|
| Confirm request interpretations (echo-and-confirm) | 10 min/day | 5 | 3 |
| Conceptual Definition / acceptance-criteria sign-off | 15 | 5 (veto only) | 2 |
| UX acceptance (`ux-acceptance-gate`) | 15 | 10 | 5 (policy-auto for heuristic pass) |
| Merge approvals (all PRs at L1) | 30 | 0–5 (T3 only) | 2 |
| Prod deploy gate | 5 | 5 | 2 (T3+ only) |
| Migrations / new deps / protected paths | 5 | 5 | 3 |
| Self-generated proposals | 10 | 5 | 3 |
| Digest reading + escalations | 10 | 10 | 8 |
| **Total** | **~100 min/day** | **~45–50** | **~28** |

The 30-minute target is a *Run-phase* number and should be stated that way. It depends on three things: policy-auto for UX and deploys on low-risk classes, batching, and defaults applied on timeout.

---

## 3. Gaps nobody covered

1. **Owner absence.** None of the designs says what happens when the owner is unreachable for two weeks. Proposal:
   - The owner can set a vacation mode with a named delegate.
   - With no delegate, after 72 hours without an owner touch the org drops to `SECURITY_ONLY`: sensors and CVE/SLO work continue, and new feature work stops.
   - T3/T4 requests expire and are never auto-approved.
2. **Who writes the holdout oracle.** A §0.3 argues for holdout scenarios, but B and C have no holdout mechanism. B's test authors are A and live in the same repo. Proposal: a private `aiorg-holdouts` repo, readable only by `aiorg-reviewer`/CI. The owner seeds it with 10–20 scenarios per journey, and a *different model family* extends it.
3. **Incident response for the AI org itself.** There is no runbook for an agent-caused incident. It should cover:
   - flipping to HALT;
   - taking a forensic snapshot of the ledger and transcripts;
   - rotating every App key;
   - diffing the control-repo SHA;
   - re-running the golden suite before resuming.
4. **Rebuilding the org from scratch.** The org needs a documented disaster-recovery path for the control plane: rebuild the ledger cache from GitHub, re-mint identities, restore schedules. C's ledger is recoverable in principle, but the procedure isn't written.
5. **Canary tokens.** Honeytoken credentials and URLs planted in repos and the knowledge base. Any use of them is proof of exfiltration and trips HALT automatically.
6. **Negative tests for guardrails.** Rulesets, CODEOWNERS and hook policies need their own CI. For example, "a builder token cannot push `.github/workflows`" and "a reviewer token cannot push commits." Otherwise a settings drift silently removes a gate.
7. **Transcript data governance.** Transcripts contain code, secrets that were redacted too late, and customer data taken from logs. They need a retention period, a residency decision, access control and redaction at write time.
8. **Counterfactual value measurement.** Nobody measures whether the org beats a baseline. Proposal: each quarter, sample about 10 items, compare against historical human-handled equivalents (cycle time, defects, rework), and report the result in the monthly review.
9. **Idle and base cost.** B says an idle org "must cost close to zero," but nobody sets a number. The charter needs a monthly ceiling per repo and a stated cost per verified outcome. Without them the budget governor has nothing to enforce against.
10. **Multi-owner conflicts.** `org.yaml` allows multiple `owners`. There is no rule for resolving conflicting instructions. Proposal: one *accountable* owner per repo; the others are advisors whose requests enter at normal priority.
11. **Merge queue.** A §0.6 recommends a Bors-style integration queue. C keeps per-lane backmerge-and-merge. At 5+ concurrent lanes this causes merge thrash, and it forces the merger identity to push merge commits. GitHub's native merge queue should be the integrator.

---

## 4. The owner's interface, concretely

### 4.1 Requesting a change in one sentence

Any of these produce the same `Request` object:
- CLI: `/aiorg:request "Add a 'Show overdue only' toggle to the work order list"`
- Chat: `@aiorg add a 'Show overdue only' toggle to the work order list`
- An issue using the `request` form (title only is enough).
- Voice to the concierge. Voice creates requests but never approves anything.

Within 2 minutes the org replies in the same channel:

```
Got it → #9612 "Overdue-only filter on work order list"
Understood as: toggle on /workorders, filters where DueDate < today and Status ∉ {Complete, Cancelled}; persists per session.
Risk: T1 (UI + query). Est: ~$6, ~3h. Starts in Conceptual Definition.
[Confirm] [Edit] [Cancel]     (no answer in 4h → proceeds as understood)
```

### 4.2 Approval inbox

The inbox is a single Projects v2 view, "Owner Inbox" (filter `aiorg:needs-owner`), mirrored as interactive messages in Slack or Teams. The buttons are bound to SSO and emit a *native GitHub action* (a PR review or an environment approval), not a comment. Each card shows:

```
[T3] Approve prod release 2026.09.24-1  · expires in 20h · default: EXPIRE (not approve)
What: 4 PRs (#9612 overdue filter, #9615 CRAP fix, #9617 EF patch 10.0.3, #9620 login copy)
Why T3: #9617 bumps a data-access package
Evidence: CI green (build-result on a1b2c3), holdouts 42/42, canary plan 5%→50%→100% w/ auto-rollback
What could go wrong: EF query translation change → WO list slow; rollback ≤ 5 min
[Approve] [Deny] [Ask a question]
```

Ordering is by expiry, then tier. There is a hard cap of 10 cards; anything beyond that is batched ("approve all 6 patch bumps"). Defaults on timeout:
- T2 proceeds.
- T3/T4 expire.
- Proposals roll back to the incubator.

### 4.3 Daily digest (08:00, readable in under 5 minutes)

```
AIORG DAILY · bootcamp-palermo-workorders · Tue 24 Sep
STATUS: RUN · spend $41 / $60 day · 0 pages
⚠ PROBLEMS FIRST
  • Reverted #9608 (sort order regression, caught by canary in 6 min). Implementer demoted L3→L2 for "UI list" tasks.
  • Flake: AcceptanceTests.LoginTests 3 reruns yesterday → investigation #9619.
⏳ NEEDS YOU (2)  → Owner Inbox
  • [T3] prod release 2026.09.24-1 (expires 20h)
  • [Q] "Should overdue include items due today?" (default in 4h: no)
✅ SHIPPED (3) with predicted outcome & check date
  • #9612 overdue filter → predict ≥15% of WO-list sessions use it by 08 Oct
🔜 STARTING TODAY (veto window 4h): #9621 Qodana batch 7, #9622 patch bump Shouldly
📈 VERIFIED OUTCOMES: #9580 dashboard cache → p95 home 820→310ms ✔
```

Reply `veto 9621` (or tap) to stop an item.

### 4.4 Veto

Any agent-initiated item in its veto window can be stopped by:
- `veto <id>` in chat;
- the button in the digest;
- `/aiorg stop` on the issue by an owner (C §7.4 already has this).

A veto moves the item back to the incubator with the tag `owner-vetoed`. `human-override-learner` records it. Three vetoes of the same class in 30 days lower that class's graduation weight.

### 4.5 Weekly (Monday, 15 minutes)

The weekly review is a one-page scorecard:
- DORA and flow trends;
- SLO status;
- verified value per dollar;
- top-10 WSJF with score components;
- incubator graduates and rejects;
- agent promotions and demotions awaiting approval (as `autonomy.yaml` PRs);
- a "taste questions" section with at most 3 items.

The monthly review adds the charter, capacity allocation, red-team results and the counterfactual value check.

---

## 5. Revised recommendations (merged position)

1. **Adopt C's runtime topology, with four changes:** split the GitHub App (D4), add a token broker, sign and pin run specs, and remove settings from the dispatch payload (D5).
2. **Adopt B's catalog as the capability ceiling.** Enforce `effective = min(ceiling, autonomy.yaml, tier_cap)`. Every actor starts at L1 on a new repo. Apply the 12 row changes in D2.
3. **Make approvals GitHub-native.** Owner reviews, environment protection and CODEOWNERS are the only approvals for T3/T4. `/aiorg approve` is limited to T2 and to `owners` only.
4. **Put a hard information wall around the oracle.** Use a private holdout repo, a test-owner identity separate from the builder, and a test-diff guard. Quarantine is never auto-accepted.
5. **Keep the ledger as runtime state only.** Hash-chain it and export it to write-once storage. Fail closed on ledger loss. Add provider-side spend caps as a second backstop.
6. **Integrate through GitHub's merge queue**, owned by `aiorg-integrator` (A §0.6), instead of per-lane self-merge.
7. **Run the incubator mechanically**, with only product-scope graduates going to the human, and cap the owner inbox at 10 cards with batching and defaults on timeout.
8. **State the owner-time targets per phase:** Crawl ≤ 100 min/day, Walk ≤ 50, Run ≤ 30. Measure the actual minutes (inbox dwell time, digest opens) as a first-class metric. Treat a sustained breach as an org defect for G25 to work on, not as an owner problem.
9. **Add the missing pieces from §3 to the Phase 0 exit criteria:** owner-absence mode, canary tokens, guardrail negative tests, an AI-org incident runbook, and a transcript retention policy.
10. **Build G00 and governance first**, as B §9.10 says. Add the Test Integrity Auditor and the migration linter to B's roughly 60-agent minimum viable org. Without them, the minimum viable org can merge code but has no way to trust it.
