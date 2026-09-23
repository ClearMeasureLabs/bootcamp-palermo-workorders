# C: Debate Round (Runtime Architect lens)

This is a critique of A-frontier.md, B-tasks.md and D-governance.md from the runtime point of view: can the runtime actually execute these designs, at what cost, and what is the smallest version that runs next week. Prices come from the current Anthropic price list: Opus 5.5 at $4 input / $20 output / $0.20 cache-read per million tokens, Sonnet 5 at $2 / $10, Haiku 4.5 at $1 / $5.

---

## 1. Agreements

All four designs converge on the same core, which is a strong signal:

1. **Tasks, not personas. State lives in GitHub, not in context windows.** A (P1, P10), B (§9.1), D (§2.9) and C (§7) all make the tracker plus git the durable state, with a fresh session per task.
2. **Liveness is mechanical and external.** A (P6), B (`lane-stall-watchdog`), D (§4.6) and C (§8, watchdog service) all generalize this repo's `Check-StalledLanes.ps1` lesson: the stalled agent can never be the one that detects the stall.
3. **Never accept an agent's claim of success.** D's "independent verification principle" and C's dispatcher postconditions are the same mechanism. Both descend from feature-loop's "CI is API-verified only" rule.
4. **Events are primary, with timers as a reconciling safety net.** A (anti-pattern 8), B (§9.6) and C (§4, §5) all note that routine GitHub events are dropped over hourly caps.
5. **Enforcement belongs in the execution path, not the prompt.** D's position 2 and A's "hooks as law" are what C's three-layer kill switch, `PreToolUse` guard and lease checks implement.
6. **Self-generated work goes through a gate.** A's dedupe-and-value gate, B's `agent-proposal-gate`, D's Incubator and C's `aiorg:proposal` label all agree. D's Incubator (evidence scoring, cooling period, caps, source quotas) is the most complete and should be adopted as the spec. C's §7.5 becomes its runtime.
7. **Use subagents, not agent teams, as the runtime fan-out primitive.** Teams are experimental, interactive-only, non-resumable and cannot nest (A §1.1, C §1).

---

## 2. Disagreements (with proposed resolution)

### 2.1 D's demands versus what the runtime can enforce

| D requirement | Enforceable? | How, concretely | What C must change |
|---|---|---|---|
| **Distinct identities per duty** (P2 contributor, P3 reviewer, P4 merger, P6 governor) | **Yes**, but not with a single bot | One GitHub App per tier (`aiorg-contributor`, `aiorg-reviewer`, `aiorg-merger`, `aiorg-governor`). The dispatcher mints a short-lived, single-repo installation token for each run, choosing the app from the agent's tier. `claude-code-action` accepts these through `github_token`, so it stops acting as the shared `claude[bot]`. Separation of duties is enforced by rulesets: last-push approval is required, only the merger app is on the bypass list for merges, and `.github/workflows/**` and the oracle paths are push-restricted for the contributor app | C's single `aiorg-bot` (C §3.2) is **wrong under D**. Replace it with four apps. Verify during Phase 0 that approving reviews from a GitHub App count toward required reviews in the org's ruleset configuration. If they do not, the reviewer verdict must be a required **status check** posted by the reviewer app, not a PR approval |
| **Reviewer from a different model family** | **Partly.** Claude Code runs only Claude models | Add a non-Claude `RunnerAdapter`. The cheapest is GitHub Copilot code review (`request_copilot_review`), which this repo already receives as bot findings. `docs/ai-workers.md` also lists Cursor and IBM Bob. The reviewer verdict comes back as a check run posted by the reviewer app | C's adapter interface already allows this. Make it a first-class requirement for T1+ items |
| **Hidden holdout tests the implementer cannot read** | **Yes**, if the holdouts run *outside* the PR's own CI | Holdouts live in a private `…-holdouts` repo. The implementer's token is scoped to the product repo only. A **verifier run** (box runner, reviewer or governor identity) triggers on `check_suite.completed` = green, checks out the PR head SHA, fetches the holdouts with its own token, runs them, and posts a required commit status `aiorg/holdout`. The holdouts must **not** run inside `build.yml` on the PR branch: a same-repo PR can edit the workflow and exfiltrate the holdout token. Ruleset path restrictions also help, but defense in depth says the secret never enters the PR's execution context at all | New agent `holdout-verifier` and a new required check. **Open issue:** feedback leakage. If the verifier posts failure details, the implementer learns the holdouts over retries. Post only coarse categories ("2 holdout scenarios failed in WorkOrder search"). Cap holdout retries at 2 per item, then escalate. Coarse feedback also raises cost: expect +1 implement iteration per item |
| **Lethal-trifecta separation** | **Yes for external text. Only partly for repo contents** | Quarantined readers (triage, support, alert text) run on `gha` with no write tools and schema-only output (`--json-schema`). Privileged runs receive the reader's structured output, never raw issue bodies: remove `mcp__github__issue_read` from P2 tools and pass the handoff instead. The egress allowlist is enforced by the Bash sandbox plus container network policy. P2 comments are scanned by TruffleHog before posting (the `trufflehog` skill already exists) | Tighten C's implementer tool list. It currently allows `gh api repos/*`, which can read any issue. **Honest limit:** the implementer must read repo code, CI logs and dependency sources, which are untrusted in principle. The mitigation is having no secrets in the run: the push token is single-repo and short-lived, and the model API key goes through OIDC. For a private, single-owner repo that residual risk is acceptable. For public repos with external contributors, stricter rules are needed (§3) |
| **Kill switch in under 60 s** | **Yes**, but only by revoking credentials, not by asking agents to stop | Order of operations: (1) **suspend all aiorg GitHub App installations** with `PUT /app/installations/{id}/suspended`. This stops every GitHub write immediately, including from runners that ignore hooks. (2) Deactivate the org's Anthropic API keys or workspace through the Admin API. (3) Cancel GHA runs, interrupt SDK runs, and interrupt or archive cloud and Managed Agent sessions. C's hook-level `killcheck` becomes the *graceful* path, not the guarantee | **Owner-identity runners (Routines, `claude --cloud`) break this.** They act as the owner's GitHub account, which cannot be suspended without locking out the owner. Resolution: under D, owner-identity runners may **only** read or call the dispatcher. They never push, comment or merge. This is the main reason v0 (§4) must retire them from the write path by v1. Drill it in D's Phase 0: measure time-to-last-write after HALT |

### 2.2 B's catalog: 401 agents is a coverage map, not a deployment unit

- **Evals do not scale.** D requires 20-50 golden tasks per task type. At 401 agents that is 8,000-20,000 eval cases and a registry nobody can maintain. **Resolution:** keep B's 401 as *lenses/configurations* grouped into about **40 agent families**, each with one prompt, one tool policy, one eval suite and one autonomy level, parameterized by lens. Examples: `pr-lens-reviewer{security|arch|a11y|perf|migration|…}` replaces about 25 path-filtered reviewers, and `observer{signal}` replaces about 60 weekly reporters. B already hints at this in its §9.2.
- **Sub-hour timers are infeasible as LLM work, and often infeasible on the proposed substrate.** Claude Code's session cron needs an open session and expires after 7 days. Routines have a 1 h minimum. GitHub Actions `schedule` has a 5-minute minimum and is documented to be delayed under load. B's `1m timer-scheduler` and `health-check-monitor` must be **deterministic code in the control plane**, or be replaced by existing tooling: App Insights availability tests firing an alert webhook. B's own §6 note ("cheap probes that start LLM sessions only on threshold") is right. It should be a hard rule enforced by the dispatcher (§3.3 below), not guidance.
- **Fan-out on `pull_request.synchronize` (32 agents) is a CI and API problem before it is a token problem.** At about 5 pushes per agent PR, that is roughly 160 potential runs per PR. See the GitHub API limits in §3.

### 2.3 A: Routines as the timer and trigger substrate

A (§8.3) recommends Routines for timers and GitHub/API triggers. That works for a single-owner v0, but not as the org's backbone:
- Routines act as the owner (this conflicts with D's identity model).
- They have daily run caps and hourly GitHub-event caps, and drop events over the cap.
- Their minimum interval is 1 h.
- A green run status only means "no infrastructure error".

**Resolution:** Routines are used in v0 for daily and weekly *observer* jobs whose only writes are proposal issues, where owner identity is tolerable. From v1 onward, timers move to the control-plane scheduler (C §5), and Routines keep one role: the owner's personal intake.

### 2.4 The merge step: LLM or deterministic?

C's `merge-closer` is an LLM agent. D wants the merger to be deterministic and P4-only. **D is right.** Resolution: split C's merge-closer into two parts.
- `bot-finding-triager` (an LLM with P2 rights) fixes findings or declines them with a PR reply.
- Merging uses **GitHub native auto-merge plus merge queue**, gated by rulesets: required checks `build-result`, `aiorg/holdout` and `aiorg/review`, plus approval for T3+. Only the merger app is allowed to enable auto-merge, and only after a deterministic policy check (risk tier, labels, provenance branch pattern). This also closes D's "Pi experiment merged by mistake" scar: `probe/*` and `exp/*` branches are excluded from the merge queue by ruleset.

### 2.5 D's column progression versus B's parallel column agents

B wakes up to 13 agents on `projects_v2_item.edited`. The repo contract (feature-loop) and D both require one column at a time with a single owner per column. **Resolution:** the dispatcher enforces *one writer lease per issue*. Other agents woken by the same event run as read-only lenses whose output is attached to the column owner's handoff.

---

## 3. Gaps nobody covered

1. **CI capacity is the real throughput ceiling.** `build.yml` fans out to Linux, SQLite, ARM, Windows, Qodana, security scan, and acceptance tests on x64 and ARM. Agents push far more often than humans. **This is not in any cost model.** It needs: a local `PrivateBuild.ps1` pass before any push (already a repo rule), per-branch `cancel-in-progress` for agent branches, and a CI-minutes budget meter. It also needs a verification-rate throttle (A's Osmani point, made concrete): the dispatcher limits concurrent implementers to what CI plus review can drain, e.g. `writers = floor(ci_slots_free / 2)`.
2. **GitHub API secondary limits.** Beyond the hourly 5k-request (REST) and GraphQL point budgets, GitHub throttles *content-creating* requests (roughly 80/min and 500/h). Comments, labels, reviews and issue creation all count. A 400-agent org that comments on every event will hit this on busy days. The fix is to batch agent output into one comment per run (C's single handoff comment), keep a token bucket per installation in the dispatcher, and split identities across apps, which also spreads the quota.
3. **Shared single environments.** `deploy.yml` serializes TDD through one concurrency group. B's `deployment_status.success` fan-out (12 agents) and D's outcome verification queue up behind a single TDD/UAT slot. An `env:` lease plus ephemeral per-PR environments are needed (Container Apps revisions are cheap) before verification can scale.
4. **Cost attribution on subscription runners.** Routines and cloud sessions draw on plan usage and do not report `total_cost_usd` the way API-key `claude -p` runs do. D's "verified value per $" metric is unmeasurable for those runs. Another reason to move writes to API-key runners by v1.
5. **Version pinning of the harness itself.** Many behaviors the design depends on are gated to specific Claude Code versions, e.g. `--permission-prompts` needs v2.1.259+ and worktree-resume refusal fields v2.1.274+. Runners must pin the CLI version and upgrade through the same eval gate as model changes (D §4.8).
6. **A staging org.** D puts probes in a sandbox repo, but nobody specified where *changes to the org itself* get canaried. Proposal: a fork `bootcamp-palermo-workorders-staging` with synthetic issues where new agent-family versions run in shadow before touching the real board.
7. **Transcript and telemetry data handling.** Transcripts contain code and possibly PII from logs. Managed Agents is not eligible for ZDR, and OTel content logging must stay off. Retention and access control for the ledger's transcript store need an owner decision.
8. **Owner attention as a metered resource.** D sets targets (≤30 min/day), but the runtime needs a queue model: a digest-batching deadline, a maximum of N blocking questions open per owner, and back-pressure that pauses new self-generated work when the question queue is full.

---

## 4. Revised recommendations

### 4.1 Cost model (single mid-size repo)

**Unit costs per run.** These assume about 90% prompt-cache hits, typical for agentic sessions, with blended cost dominated by output and uncached context.

| Run type | Model | Typical shape | Est. $/run |
|---|---|---|---|
| Triage / classify / reader | Haiku 4.5 | 5 turns, ~15k ctx | 0.03-0.08 |
| PR lens review | Sonnet 5 | 20 turns, ~40k ctx | 0.50-1.00 |
| CI fix / bot triage | Sonnet 5 | 30-60 turns | 1-3 |
| Implement (incl. build loops) | Sonnet 5 | 80-200 turns, ~60k ctx | 3-8 (×1.5 for retries) |
| Design / test design / refine | Opus 5.5 | 30 turns, ~50k ctx | 2-4 |
| Observer / retro / incident RCA | Opus 5.5 or Sonnet 5 | 20-60 turns | 1-4 |

**Per work item (owner request to Functional Testing).** Triage 0.05, refine 2, design 3, test design 2, implement 9, CI fixes 1.5, review 3 (2 Claude lenses plus cross-vendor), bot triage 1, holdout verify 1.5. That is roughly **$23/item**, or $30-35 at full scale with more lenses.

| Scale | Assumptions | LLM $/month | Infra/CI $/month | Total |
|---|---|---|---|---|
| **MVP, about 60 agents (about 15 families)** | 40 items/month (the repo does about 12 merged PRs/month today); about 10 daily and 10 weekly LLM observers; 200 triage events; 10 alerts | Items ~$920; observers ~$380; triage, alerts and digests ~$100; +20% retries/overhead → **~$1.7k** (range $1.2-2.5k) | GHA minutes for about 200 agent-triggered builds (heavy multi-OS pipeline; free if the repo is public) ~$100-300; control plane $0 in v0, ~$150 in v1 | **≈ $2-3k** |
| **Full, about 400 agents, disciplined** (about 40% deterministic, LLM only on threshold, lenses path-filtered) | 80 items/month; 32 daily and 65 weekly observers with about half LLM; `synchronize` lenses about 8 LLM per push × 400 pushes × $0.5; `deployment_status` about 6 LLM × 60 deploys × $1 | Items ~$2.6k; observers ~$1.2k; PR lenses ~$1.6k; deploy verification ~$0.4k; incidents, digests and evals ~$0.8k; +20% → **~$8k** (range $6-12k) | CI ~$0.5-1.5k; Container Apps, Postgres and OTel ~$300-600 | **≈ $9-14k** |
| **Full, naive** (every catalog entry an LLM session, every event, Opus default, 1m/5m probes as LLM) | Same volume | $30-50k+. A 1-minute Haiku probe alone costs about $0.02 × 43,200 = $860/month for nothing. This is A's documented 12-15× multi-agent multiplier and the runaway pattern | — | Not viable |

For scale: the disciplined full organization costs about as much as one fully loaded engineer per month. MVP costs a fraction of that. The deciding factor is outcome-verified value per dollar (D §2.10), not the absolute bill.

### 4.2 What must be deterministic code vs. LLM

**Deterministic (no model call):** event routing and dedupe, lease/claim locks, scheduler and timers, all sub-hour probes, kill switch and governor, budget meters and breakers, WIP limits, column moves and the parent clamp, merge and auto-merge policy, risk-tier classification from paths, migration linter and numbering guard, protected-path guard, test-count diff and skip-attribute guard, PR-size guard, secret/SAST/SCA/license/SBOM scanners (existing tools and skills), DORA and flow metrics, cost reporting, stall watchdog, certificate, backup and quota checks, the Incubator's scoring, cooling and caps, and outcome-metric queries.

**LLM:** classifying untrusted text (quarantined readers), refining requests and acceptance criteria, design and test design, implementation and CI fixing, review lenses, bot-finding triage, root-cause analysis, hypothesis writing, digest prose, retro and skill proposals.

**Rule:** a deterministic component may *call* an LLM only to explain or propose, never to decide an enforcement outcome. This is D's closing principle, applied mechanically.

Applying this to B's catalog moves roughly 150-170 of the 401 entries to code or existing tools.

### 4.3 Where C is over-built for an MVP

These parts of C should be deferred:
- the Postgres ledger, gateway, Service Bus, CloudEvents envelope and CEL routing;
- Container Apps Jobs runners and the Managed Agents adapter;
- `path:` leases (one writer per repo is enough at MVP volume);
- memory harvesting pipelines;
- trace propagation;
- the concierge channel.

All of them earn their place only past about 40 items/month, multiple repos, or sub-hour scheduling.

### 4.4 v0: runs next week, using only GitHub Actions + Claude Code + the existing skills

Autonomy is L1 everywhere: a human merges everything. All new workflow files ship in **one PR the owner approves**, per CLAUDE.md's pipeline rule.

| # | Piece | Mechanism | Reuses |
|---|---|---|---|
| 1 | **Triage** | `aiorg-triage.yml` on `issues.opened` → `claude-code-action@v1`, `--model haiku --max-turns 8`, tools limited to labels and one comment. Applies `type/area/risk` and `aiorg:triaged` | — |
| 2 | **Implement lane** | The owner labels an issue `aiorg:go`, then runs `/feature-loop-dispatch <issues>` in a Claude Code cloud session (as today). Cloud VMs with the repo's setup script handle the build, as this session demonstrates | `feature-loop`, `feature-loop-dispatch`, `factory-loop.json` unchanged |
| 3 | **PR review** | `aiorg-review.yml` on `pull_request` opened/ready → `claude-code-action@v1` running a repo review skill (onion rules, testing policy, style), plus Copilot review requested for cross-family coverage. Findings go in as review comments. The existing bot-triage rule in feature-loop handles them | `stylecop`, `run-semgrep`, `trufflehog` skills |
| 4 | **CI-failure fixes** | Auto-fix is enabled on agent PRs from the cloud session (built-in PR-activity subscription). A fallback workflow posts `@claude fix the failing checks` on `check_suite` failure for `aiorg`-labeled PRs | `checkin-dance` |
| 5 | **Watchdog** | `aiorg-watchdog.yml` cron `*/15` runs `pwsh Check-StalledLanes.ps1 -Json`. On a stall it labels the PR `aiorg:stalled` and posts an `@claude` instruction matching the stall kind (GREEN_UNMERGED → "triage bots, then request owner merge"; DIRTY → "merge master") | `Check-StalledLanes.ps1` as-is |
| 6 | **Nightly observer** | One **Routine** (daily): the backlog-synthesizer prompt (C §10.6) reading CI runs, `qodana.sarif.json`, `run-crap-audit.ps1`, and dependency scans. At most 3 proposals per night, labeled `aiorg:proposal`, with a fingerprint dedupe. The owner promotes by adding `aiorg:go` | `owasp-dependency-scan`, `npm-audit`, CRAP scripts |
| 7 | **Weekly security sweep** | Routine (weekly) running the scan skills. Findings become proposals | same |
| 8 | **Daily digest** | Routine (weekday 08:00) updates one pinned "AI org status" issue: merged, blocked, proposals, and spend (from GHA `total_cost_usd` logs) | — |
| 9 | **Kill switch** | Repo variable `AIORG_ENABLED` checked in every workflow's `if:`. Routines are paused with their toggle. Hard stop is uninstalling or suspending the Claude GitHub App on the repo | — |
| 10 | **Budgets** | `--max-turns` and `--max-budget-usd` in every `claude_args`, `timeout-minutes` per job, `concurrency:` groups per issue or PR (these double as leases), and a monthly cap on the API key in the Console | — |

v0 cost is about $300-800/month for LLM (roughly 10-20 items, observers, triage) plus CI minutes.

**Known v0 gaps, accepted:**
- Owner-identity writes (routines and cloud sessions) mean D's identity separation and the 60 s kill switch are not met.
- There are no holdouts.
- The ledger is labels plus GHA logs.

Owner-identity writes are why the human merges everything in v0.

### 4.5 Growth path

| Stage | Trigger to advance | Adds |
|---|---|---|
| **v0** (week 1) | — | §4.4 |
| **v1: governed** (weeks 2-6) | v0 stable for 2 weeks; ≥ 20 agent PRs merged | Four GitHub Apps and rulesets (D §4.1). Implement lane moved from owner cloud sessions to `claude-code-action` or a self-hosted runner under the contributor app. Test-author/implementer split with CODEOWNERS-frozen oracle paths. `holdout-verifier` workflow on a separate identity. Native merge queue plus auto-merge for T0/T1. JSONL ledger in `aiorg-control` written by workflows. OTel env to App Insights. Kill-switch drill under 60 s using app suspension. D's Phase 0 exit criteria |
| **v2: control plane** (months 2-3) | Any of: > 40 items/month, a second repo, sub-hour schedules needed, cost attribution gaps, GitHub content-creation throttling observed | C's gateway, dispatcher and Postgres ledger. Container Apps Jobs `box` runners. Leases and breakers. D's Incubator as runtime. Agent families with eval suites. Shadow mode in the staging fork |
| **v3: breadth** | D's Walk-phase exit criteria met | B's catalog rolled in family by family (about 40 families, 400 lenses), path-filtered, LLM-on-threshold. Managed Agents for laptop-independent long runs. Multi-repo portfolio |

**Net revisions to C:**
1. Four identities instead of one bot.
2. Deterministic merge through the merge queue.
3. A holdout verifier outside PR CI.
4. The kill switch is defined as credential revocation.
5. Agent families instead of 400 definitions.
6. A CI-capacity throttle.
7. The control plane is deferred to v2, behind measurable triggers.
