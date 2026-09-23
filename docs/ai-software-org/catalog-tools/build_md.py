import collections
from catalog_data import GUILDS
from render import agents
ROLES = [
("PM","Product Manager","Pragmatic Framework (37 activities: market, focus, business, planning, programs); SWEBOK Requirements/Economics"),
("PO","Product Owner","Scrum Guide backlog ownership; board columns in .claude/factory-loop.json"),
("UXR","UX Researcher","Research ops, surveys, interview synthesis, usability studies"),
("UXD","UX/UI Designer","Flows, components, design systems; repo 'UX Design' + 'UX Testing' columns"),
("TW","Technical Writer","Docs-as-code; CLAUDE.md, docs/, arch/ diagrams in repo"),
("ARCH","Software Architect","SWEBOK v4 Software Architecture & Design KAs; onion rules in CLAUDE.md"),
("BE","Backend Developer","SWEBOK Construction; MediatR/EF Core handlers in repo"),
("FE","Frontend Developer","Blazor WASM/Server UI in repo"),
("DE","Data Engineer","Pipelines, warehouse models, data quality"),
("DBA","Database Administrator","DbUp migrations (src/Database/scripts/Update), SQL Server"),
("CR","Code Reviewer","Peer review as DORA's replacement for heavyweight change approval"),
("QA","QA / Test Automation Engineer","SWEBOK Testing & Quality; NUnit/Shouldly/bUnit/Playwright in repo"),
("PERF","Performance Engineer","k6-load-testing skill in repo; capacity planning"),
("SEC","AppSec / Security Engineer","SWEBOK v4 Software Security KA; OWASP SAMM 15 practices; semgrep/trufflehog skills"),
("DEP","Dependency & License Compliance","owasp-dependency-scan, npm-audit skills; 'no new NuGet without approval' rule"),
("DEVOPS","DevOps / Build Engineer","build.yml (build-linux, qodana, security-scan, acceptance-tests jobs)"),
("REL","Release Manager","deploy.yml TDD->UAT->Prod; docs/release-cadence.md"),
("PLAT","Platform / Cloud Engineer","Azure Container Apps, IaC, environments (docs/environments.md)"),
("SRE","SRE / On-call / Incident Mgmt","Google SRE book: toil, on-call, incident command, blameless postmortems"),
("ANA","Product/Data Analyst","KPIs, funnels, experiments, telemetry"),
("SUP","Support Engineer","docs/support.md: issues picked up by the AI Factory"),
("CS","Customer Success","Health, churn, onboarding, close-the-loop"),
("FIN","FinOps Analyst","FinOps Framework domains: Inform, Optimize, Govern, Plan & Forecast"),
("EM","Engineering Manager","DORA metrics & capabilities; capacity; people-equivalent = agent performance"),
("SM","Scrum Master / Delivery Lead","Flow, WIP, retros; feature-loop-dispatch stall watchdog"),
("PGM","Portfolio / Program Manager","Multi-repo dependencies, RAID, QBR"),
("DX","Developer Experience Engineer","Inner loop, hooks, skills, permission prompts"),
("A11Y","Accessibility Specialist","WCAG 2.2 AA, VPAT/ACR"),
("L10N","Localization Engineer","i18n extraction, translation, locale formats"),
("LEGAL","Legal / Privacy / Compliance","GDPR/DSR, DPIA, licenses, SOC 2 evidence, EU AI Act register"),
("MKT","Marketing / Release Comms","Announcements, changelog, enablement"),
("AIOPS","AI-Org Operator (new role)","No human analogue: runs the agent fleet itself"),
]
RN={c:n for c,n,_ in ROLES}
AUT={'A':'auto','R':'auto+review','H':'human-gate'}
L=[]; w=L.append
all_rows=[r for g in GUILDS for r in g['rows']]
w("# B — Task Decomposition Catalog for a 100% AI Software Organization\n")
w("*Agent B (Work Decomposer). Lens: every role in a complete software organization, decomposed into discrete, trigger-driven TASK agents, then deduplicated into guilds.*\n")
w(f"**Totals:** {len(all_rows)} task agents in {len(GUILDS)} guilds, covering {len(ROLES)} roles. "
  f"Autonomy: {sum(r['aut']=='A' for r in all_rows)} auto, {sum(r['aut']=='R' for r in all_rows)} auto-with-review, {sum(r['aut']=='H' for r in all_rows)} human-gate.\n")
w("""## 0. How to read this document

1. **Section 1** lists the roles and the evidence each one is grounded in.
2. **Section 2** decomposes each role into the task agents it contributes to. One agent often serves several roles, which is the dedupe.
3. **Section 3** summarizes the guilds.
4. **Section 4** is the master catalog, with one table per guild. Each row is one task agent: its activation, inputs, outputs, done-criteria, autonomy and handoffs.
5. **Section 5** is the trigger matrix, mapping each event to the agents it wakes.
6. **Section 6** is the timer calendar, mapping each cadence to its agents.
7. **Section 7** covers handoff hubs and critical paths.
8. **Section 8** lists the human-gated and irreducibly human tasks, with the reason for each.
9. **Section 9** records key positions and design decisions.
10. **Section 10** maps the catalog onto the bootcamp-palermo-workorders repo.

### Conventions
- **Task, not role.** Each agent does exactly one kind of job and has a checkable done-criterion. Roles only matter as *provenance*: they show that nothing a real organization does was left out.
- **Activation grammar.** `e:<event>[filter]` is a webhook or internal event, such as `e:pull_request.synchronize[UI paths]` or `e:alert.fired`. `t:<cadence>` is a timer (1m, 5m, 15m, hourly, daily, weekday, nightly, weekly, biweekly, monthly, quarterly, annual). `o:<agent>` means the agent is invoked on demand by another agent. `o:owner` means it is invoked by the human owner.
- **Internal events** (emitted by agents and not by GitHub): `agent.completed`, `agent.token_spend`, `finding.emitted`, `release.candidate`, `incident.resolved`, `experiment.ended`, `okr.changed`, `pipeline.*`, `alert.fired`, `healthcheck.unhealthy`, `slo.burn_rate`, `cost.anomaly`, `support.ticket.created`, `feedback.received`, `dsr.received`.
- **Autonomy.** **A** = auto: the agent acts and the result is visible after the fact. **R** = auto-with-review: the agent produces the artifact, and a reviewer agent or the owner must accept it before it has external effect. For code, this usually means a PR that passes `code-review-orchestrator`. **H** = human-gate: nothing irreversible or external happens until a human decision is recorded by `human-decision-recorder`.
- **Every agent emits `finding.emitted`** for anything it notices outside its own scope. This is how the org self-generates backlog (see G25).

### Grounding sources
- SWEBOK v4 (18 KAs; the new ones are Architecture, Operations and Security): https://www.computer.org/education/bodies-of-knowledge/software-engineering/v4
- Google SRE book (toil, on-call, incident management, postmortems): https://sre.google/sre-book/eliminating-toil/ , https://sre.google/sre-book/being-on-call/ , https://sre.google/workbook/postmortem-culture/
- DORA capability catalog (technical, process and cultural): https://dora.dev/capabilities/
- FinOps Framework domains: https://www.finops.org/framework/domains/
- OWASP SAMM (5 business functions, 15 practices): https://owaspsamm.org/model/
- Pragmatic Institute Framework (37 product activities): https://www.pragmaticinstitute.com/product/framework/
- Repo grounding: `CLAUDE.md`, `.claude/factory-loop.json` (9 board columns), `.claude/skills/*` (feature-loop, feature-loop-dispatch, k6, semgrep, trufflehog, owasp-dependency-scan, roslynator, stylecop, codebase-cartography-audit, otel-observability-mindset), `.github/workflows/build.yml|deploy.yml|render-diagrams.yml`, `docs/board-columns.md`, `docs/ready-to-move.md`, `docs/release-cadence.md`, `docs/environments.md`, `docs/support.md`, `docs/ai-workers.md`.
""")
# roles
role_map=collections.defaultdict(list)
for g in GUILDS:
    for r in g['rows']:
        for ro in r['roles']: role_map[ro].append((g['code'],r['name']))
w("## 1. Role inventory (32 roles)\n")
w("| Code | Role | Grounding | Task agents it contributes to | Guilds touched |\n|---|---|---|---|---|")
for c,n,src in ROLES:
    ts=role_map[c]; w(f"| {c} | {n} | {src} | {len(ts)} | {len(set(x[0] for x in ts))} |")
w("""
**Reading the counts.** The brief asked for 30-100 tasks per role. Tagged counts range from 5 (DE) to 42 (QA). QA, SRE, SEC, PM and BE land directly in the 30-100 range. The niche roles (DE, L10N, A11Y, CS, UXR, UXD, MKT) tag only 5-12 dedicated agents. That is on purpose, and it is the main point of the consolidation. In a human organization, roughly half of a niche specialist's working week goes to *generic* tasks: attending reviews, writing status, triaging inbound requests, filing tickets, updating docs and answering questions. In this design those tasks are served by shared agents (`code-review-orchestrator`, `stakeholder-status-reporter`, `work-item-classifier`, `question-answerer`, `doc-drift-checker`, `backlog-generator-from-signals`) that apply the specialist's rules as *lenses*. They are not duplicated per role. Adding those roughly 15-20 shared generic tasks to each role's tagged count brings most roles near or into the 30+ range. The niche roles still fall short, because the parts of their jobs that are really unique are small.
""")
w("## 2. Role → task decomposition\n")
w("Each role lists the agents that carry out its work. Agents shared with other roles are how the dedupe happens.\n")
for c,n,_ in ROLES:
    ts=role_map[c]
    w(f"### {c}: {n} ({len(ts)})")
    by=collections.defaultdict(list)
    for gc,a in ts: by[gc].append(a)
    w("; ".join(f"**{gc}** " + ", ".join(f"`{a}`" for a in v) for gc,v in sorted(by.items())) + "\n")
# guild summary
w("## 3. Guild summary\n")
w("| Guild | Name | Agents | auto | review | human | Mission |\n|---|---|---|---|---|---|---|")
for g in GUILDS:
    rs=g['rows']; w(f"| {g['code']} | {g['title']} | {len(rs)} | {sum(r['aut']=='A' for r in rs)} | {sum(r['aut']=='R' for r in rs)} | {sum(r['aut']=='H' for r in rs)} | {g['mission']} |")
w(f"| | **Total** | **{len(all_rows)}** | **{sum(r['aut']=='A' for r in all_rows)}** | **{sum(r['aut']=='R' for r in all_rows)}** | **{sum(r['aut']=='H' for r in all_rows)}** | |\n")
# master catalog
w("## 4. Master catalog\n")
def fmt_t(t):
    return t.replace('e:','⚡`',1)+'`' if t.startswith('e:') else (t.replace('t:','⏱`',1)+'`' if t.startswith('t:') else '↪`'+t[2:]+'`')
for g in GUILDS:
    w(f"### {g['code']}: {g['title']} ({len(g['rows'])})\n")
    w(f"_{g['mission']}_\n")
    w("| # | Agent | Roles | Activation | Inputs | Outputs / artifacts | Done when | Aut. | Hands off to |\n|---|---|---|---|---|---|---|---|---|")
    for i,r in enumerate(g['rows'],1):
        w(f"| {g['code'][1:]}.{i:02d} | `{r['name']}` | {', '.join(r['roles'])} | {'<br>'.join(fmt_t(t) for t in r['trig'])} | {r['inp']} | {r['out']} | {r['done']} | {r['aut']} | {', '.join(r['hand']) if r['hand'] else '—'} |")
    w("")
# trigger matrix
w("## 5. Trigger matrix (event → agents)\n")
w("Grouped by base event. Filters are shown in brackets next to each agent. Order is not significant; `event-router` fans out every event in parallel, and `work-claim-lock` serializes any agents that touch the same item.\n")
ev=collections.defaultdict(list)
for g in GUILDS:
    for r in g['rows']:
        for t in r['trig']:
            if t.startswith('e:'):
                base=t[2:].split('[')[0]; filt=t[2:][len(base):]
                ev[base].append(f"`{r['name']}`{filt}")
def evkey(k):
    fam=k.split('.')[0]; return (fam,k)
w("| Event | # | Agents woken |\n|---|---|---|")
for k in sorted(ev,key=evkey):
    w(f"| `{k}` | {len(ev[k])} | {', '.join(ev[k])} |")
w("")
w("""**Fan-out hot spots.** `pull_request.synchronize` and `pull_request.opened` wake the most agents: the review lenses plus about 25 path-filtered checkers. The design requires path filters, so a docs-only push wakes only the doc agents and `docs-only-fast-path`. `deployment_status.success[uat]` is the second-largest fan-out, because that is where UX, a11y, DAST, exploratory, instrumentation and perf verification all converge. It maps directly onto the repo's UX Testing column.
""")
# timers
w("## 6. Timer calendar (cadence → agents)\n")
order=['1m','5m','15m','hourly','daily','weekday','nightly','weekly','biweekly','monthly','quarterly','annual']
tm=collections.defaultdict(list)
for g in GUILDS:
    for r in g['rows']:
        for t in r['trig']:
            if t.startswith('t:'): tm[t[2:]].append(r['name'])
w("| Cadence | Suggested cron (UTC) | # | Agents |\n|---|---|---|---|")
cron={'1m':'* * * * *','5m':'*/5 * * * *','15m':'*/15 * * * *','hourly':'0 * * * *','daily':'0 6 * * *','weekday':'0 14 * * 1-5','nightly':'0 2 * * *','weekly':'0 7 * * 1','biweekly':'0 8 * * 1 (even ISO weeks)','monthly':'0 8 1 * *','quarterly':'0 9 1 1,4,7,10 *','annual':'0 9 15 1 *'}
for k in order:
    if k in tm: w(f"| {k} | `{cron[k]}` | {len(tm[k])} | {', '.join('`'+a+'`' for a in tm[k])} |")
w("""
**Scheduling notes.**
- Sub-hourly timers (1m/5m/15m) must be *cheap probes* (HTTP health, queue depth, SLA clocks) that emit events. They must not start LLM sessions unless the probe crosses a threshold. This keeps `agent-budget-governor` spend flat.
- The daily jobs are staggered by guild so they finish before the `backlog-prioritizer` run at the end of the batch. The observers (costs, drift, anomalies, flaky tests) go first, then `signal-aggregator` and `backlog-generator-from-signals`, then `backlog-prioritizer`, then `lane-dispatcher`. This ordering is the org's daily "stand-up".
- The weekly jobs cluster on Monday, which gives the org a weekly planning heartbeat: grooming, digests, retro inputs and the health scorecard.
- `release-candidate-cutter` runs on weekdays, matching `docs/release-cadence.md` ("regular releases go out on weekdays once UAT validation passes; hotfixes any time").
""")
# hubs
w("## 7. Handoff hubs and critical paths\n")
indeg=collections.Counter(h for r in all_rows for h in r['hand'])
w("**Most-handed-to agents (in-degree).** These are the org's load-bearing agents; they need the strongest evals, the highest concurrency, and a human-visible dashboard:\n")
w("| Agent | Inbound handoffs |\n|---|---|")
for a,c in indeg.most_common(20): w(f"| `{a}` | {c} |")
w("""
**Critical path A: owner change request to production.**
`owner-request-intake` → `request-clarifier` → `problem-statement-writer` → `product-requirements-writer` → `acceptance-criteria-writer` → (`ux-flow-designer` ∥ `technical-design-author` → `threat-modeler`) → `test-design-author` → `definition-of-ready-checker` → `lane-dispatcher` → `feature-implementer` → `code-review-orchestrator` (+ lenses) → `review-feedback-applier` ↺ → `merge-readiness-checker` → `pr-merger` → `deployment-orchestrator` (TDD) → `functional-test-runner` → (UAT) `ux-walkthrough-recorder` → `usability-heuristic-evaluator` → `ux-acceptance-gate` [H] → `release-candidate-cutter` → `prod-deploy-gate` [H/policy] → `progressive-rollout-controller` → `release-verifier` → `release-notes-writer` → `release-comms-writer` [H] → `feature-adoption-reviewer` (+30d).

**Critical path B: production incident to prevention.**
`health-check-monitor` → `incident-declarer` → `incident-commander` → `incident-diagnostician` → `rollback-executor` | `hotfix-intake` → `fix-implementer` → `hotfix-release-runner` → `postmortem-writer` → `action-item-filer` → `backlog-prioritizer`, while `escaped-defect-analyzer` → `process-improvement-implementer` hardens the gate that missed the defect.

**Critical path C: self-generated backlog.**
Any observer → `finding.emitted` → `signal-aggregator` → `backlog-generator-from-signals` → `duplicate-detector` → `opportunity-scorer` → `agent-proposal-gate` [H or policy] → `backlog-prioritizer` → `lane-dispatcher`.
""")
# human
w("## 8. Human-gated and irreducibly human tasks\n")
w("| Agent | Guild | Why it stays human-gated |\n|---|---|---|")
why={
'repo-onboarder':'Pointing the org at a repo grants it write power over someone\'s asset; consent and scope are the owner\'s decision.',
'security-report-intake':'External reporters and embargoes involve trust relationships and coordinated disclosure.',
'product-vision-keeper':'Vision is a statement of intent and values; agents can draft, only the accountable owner can commit.',
'okr-drafter':'Outcomes commit the organization; they encode what the owner is willing to be measured on.',
'roadmap-planner':'Sequencing encodes trade-offs among stakeholders who are not in the data.',
'pricing-packaging-analyst':'Revenue, contracts and market positioning; legally and commercially binding.',
'business-case-writer':'Spending decisions with real money and opportunity cost.',
'sunset-planner':'Removing value from customers is a trust and contract decision.',
'ux-acceptance-gate':'Taste and brand judgment; configurable to policy-auto once heuristic scores are trusted.',
'survey-designer':'Contacting real users consumes their goodwill and has consent implications.',
'tech-radar-curator':'Long-lived platform bets; sets constraints for years.',
'protected-path-guard':'Repo rule: .octopus/, build scripts, pipelines, SDK versions require explicit approval (CLAUDE.md). This is the org\'s own safety perimeter.',
'human-review-requester':'The point where a human is deliberately placed in the loop for high-risk diffs.',
'perf-budget-keeper':'Budgets trade cost vs experience; business decision.',
'secret-leak-responder':'Revocation can cause outages; purging history rewrites shared state. Automatic revocation is allowed only for keys known to be safe to revoke.',
'access-review-runner':'Removing people\'s access affects humans and contracts.',
'pen-test-coordinator':'Engaging external testers is contractual and legal (authorization to attack).',
'security-advisory-publisher':'Public disclosure is irreversible and reputational.',
'dependency-upgrade-planner':'Major-version migrations are multi-week investments.',
'new-dependency-gate':'Repo rule (no new NuGet without approval); new code from strangers is the largest supply-chain risk.',
'pipeline-as-code-maintainer':'Pipelines are the org\'s enforcement mechanism; agents must not be able to weaken their own gates.',
'prod-deploy-gate':'Production changes affect real users; can be relaxed to policy-auto for low-risk scores once change-failure-rate is proven low.',
'environment-provisioner':'Creates billable, externally reachable infrastructure.',
'dns-domain-manager':'Domain/DNS changes are high-blast-radius and hard to undo quickly.',
'slo-definer':'SLOs are a promise to users and set the error-budget policy that governs the whole org\'s speed.',
'db-restore-to-lower-env':'Moves production data; privacy exposure.',
'data-migration-runner':'Irreversible data mutation at scale.',
'a11y-conformance-reporter':'VPAT/ACR is a legal attestation.',
'translation-reviewer':'Legal and marketing text in other languages carries liability.',
'privacy-impact-assessor':'DPIA sign-off is a legal accountability (DPO).',
'policy-text-watcher':'Terms and privacy policies are contracts.',
'legal-review-requester':'Legal advice must come from counsel.',
'export-control-checker':'Regulatory classification with legal penalties.',
'commitment-planner':'Multi-year financial commitments.',
'release-comms-writer':'External voice of the company.',
'deprecation-notice-writer':'Customer-contract implications.',
'social-post-drafter':'Public, irreversible, brand-bearing.',
'website-content-updater':'Public brand surface.',
'capacity-allocator':'Choosing features vs debt vs ops is the core management trade-off.',
'portfolio-balancer':'Cross-repo investment mix is strategy.',
'agent-proposal-gate':'Self-generated work must not self-authorize; policy-auto only for enumerated low-risk classes (flaky fix, lint cleanup, doc drift, patch-level dependency bumps).',
'idea-incubator':'New bets are strategy.',
}
for r in all_rows:
    if r['aut']=='H':
        g=agents[r['name']][0]['code']; w(f"| `{r['name']}` | {g} | {why.get(r['name'],'External/irreversible effect.')} |")
w("""
### Irreducibly human (not just gated): what the owner actually does
1. **Intent and values.** What the product is for, who it serves, and what it will not do. Agents can mine signals, but they cannot supply purpose.
2. **Accountability that the law assigns to a person.** DPO sign-off, legal attestations (VPAT, SOC 2 management assertion), contracts, export classification, public security disclosures.
3. **Spending and commitments.** Budgets, reservations, vendor and pen-test engagements, pricing.
4. **Changes to the org's own guardrails.** Pipelines, protected paths, agent permissions, the kill switch and the autonomy policy itself. An organization that can relax its own gates has no gates.
5. **Irreversible external acts.** Public comms, deleting customer-facing features, production data migrations, DNS.
6. **Relationships.** Customer escalations beyond a threshold, security reporters, auditors, counsel.
7. **Taste arbitration.** When `conflict-arbiter` cannot resolve a trade-off with data (for example UX tone or naming), the owner decides once and `human-override-learner` encodes the decision so it is not asked again.

**Gate economics.** The target is ≤10% human-gated agents (currently """ + f"{sum(r['aut']=='H' for r in all_rows)}/{len(all_rows)} = {100*sum(r['aut']=='H' for r in all_rows)/len(all_rows):.0f}%" + """). The gates should also *fire rarely*: most gated agents run quarterly or on exceptional events. The human's steady-state load is dominated by `prod-deploy-gate`, `agent-proposal-gate`, `protected-path-guard` and `ux-acceptance-gate`. `human-gate-queue-balancer` batches those into one daily decision digest, and `human-override-learner` lowers their frequency over time by turning repeated approvals into policy.
""")
# positions
w("""## 9. Key positions and design decisions

1. **Tasks, not roles.** A role is a bundle of about 30-100 task types that share a person's calendar. Agents have no calendar constraint, so bundling only adds context bloat and blurs done-criteria. Roles survive only as *tags* for coverage auditing (Section 2) and as *lenses* applied by shared agents.
2. **Deduplication rule.** When two roles do the "same" task with different criteria, use one agent with pluggable lenses. For example, code review is performed by CR, SEC, DBA, ARCH, A11Y, SRE and PERF in a human org; here it is `code-review-orchestrator` fanning out to lens agents. In the catalog, 625 role-to-task mappings collapse into 401 agents, and 217 of those agents serve two or more roles. Most of the remaining savings come from generic tasks (status, triage, review, docs) that would otherwise be repeated in every role, and those are counted once here.
3. **A new role is needed: AIOPS.** G00 and parts of G25 have no human counterpart. Routing, leasing, budgets, evals, prompt tuning, the kill switch and the audit trail are the "management layer" of an AI org. Without them the other 350 agents collide. G00 must be built first, and it is the one guild that should *never* be self-modifying without a human gate: `agent-prompt-tuner` is review-gated and `pipeline-as-code-maintainer` is human-gated.
4. **Observers vs. actors.** A majority of agents are observers. A keyword heuristic over the output column puts about 265 of the 401 as producing reports, findings or proposals rather than mutations. Observers run on timers or events and emit findings or reports, and they never mutate code or infrastructure. Observers are safe to run fully autonomously. Actors (implementers, mergers, deployers, reapers) are fewer, and they are where autonomy levels matter. This split is what makes self-generated backlog safe: observers write to `finding.emitted`, and only `backlog-generator-from-signals` turns findings into proposals, which pass through `agent-proposal-gate`.
5. **Every column has an owner agent and an egress done-criterion.** This mirrors `docs/ready-to-move.md`: AI workers control egress from their columns via a completion callback, not labels.
6. **The event trigger is primary and the timer is a safety net.** Nearly every event-driven check also has a weekly or daily sweep, because webhooks get dropped. Timers catch drift (`drift-detector`, `schema-drift-detector`, `doc-drift-checker` sweeps).
7. **Autonomy is a dial per agent, earned by evidence.** Agents start at R (review) and move to A when `agent-output-evaluator` shows sustained quality. Some H gates (`prod-deploy-gate`, `ux-acceptance-gate`, `agent-proposal-gate`) are designed to become *policy-auto for low-risk classes*, using measured change-failure-rate as the evidence. The ones listed as irreducibly human in Section 8 never move.
8. **Cost is a first-class signal.** `finops-ai-spend-reporter` measures LLM cost per merged PR and per resolved incident, and `model-router` picks the cheapest model that passes each task's evals. At about 400 agents, an idle org must cost close to zero: probes are cheap and LLM sessions start only on a threshold.
9. **Multi-repo from day one.** `repo-onboarder` writes a per-repo profile (like `.claude/factory-loop.json`). Guilds are shared across repos, while leases, boards and budgets are per repo. `cross-repo-dependency-coordinator` and `portfolio-balancer` sit above them.
10. **Build order (minimum viable org, about 60 agents).** First G00 (all), then the G01 intake/bug agents. Then G03 readiness/board/flow, G06 feature/fix/review-applier/CI-fixer, G07 orchestrator + correctness + style + merge, G08 test design/authoring/functional, G12 watcher/deploy/rollback, G14 health, incident and postmortem, and G25 aggregator/generator/gate. After that, every other guild adds coverage without changing the spine.
""")
w("""## 10. Mapping onto bootcamp-palermo-workorders

| Repo artifact | Existing behavior | Catalog agents that own it |
|---|---|---|
| Board column *Conceptual Definition* | item defined | `owner-request-intake`, `problem-statement-writer`, `acceptance-criteria-writer`, `request-clarifier` |
| *UX Design* | flows | `ux-flow-designer`, `ui-component-designer`, `ux-copy-writer`, `prototype-builder` |
| *Technical Design* | design | `technical-design-author`, `api-contract-designer`, `data-model-designer`, `threat-modeler`, `estimator` |
| *Test Design* | test plan | `test-design-author`, `ux-test-plan-writer`, `security-requirements-writer`, `load-test-author` |
| *Development* | code + PR + green CI | `feature-implementer` (+ backend/frontend), `code-review-orchestrator`, `review-feedback-applier`, `ci-failure-fixer`, `merge-readiness-checker`, `pr-merger` |
| *Functional Testing* ("done for now") | verify on TDD | `functional-test-runner`, `bug-verification-agent`, `definition-of-done-checker`, `docs-only-fast-path` |
| *UX Testing* | verify on UAT | `ux-walkthrough-recorder`, `usability-heuristic-evaluator`, `a11y-runtime-auditor`, `ux-acceptance-gate` |
| *Release Queue* | release; production incidents filed here | `release-candidate-cutter`, `prod-deploy-gate`, `incident-declarer` |
| `feature-loop` / `feature-loop-dispatch` skills | one lane per item; children-first; stall watchdog | `lane-dispatcher`, `dependency-graph-resolver`, `epic-decomposer`, `lane-stall-watchdog` (Check-StalledLanes.ps1) |
| `build.yml` jobs (qodana, security-scan, acceptance-tests, build-result) | CI gates | `static-analysis-triager`, `sast-scanner`, `sca-scanner`, `ci-pipeline-watcher`, `test-report-publisher`, `crap-score-gate` |
| `deploy.yml` (TDD → UAT → prod) | promotion | `deployment-orchestrator`, `migration-deploy-runner`, `prod-deploy-gate`, `environment-promotion-tracker` |
| `render-diagrams.yml` | PlantUML render on PR | `diagram-renderer`, `architecture-diagram-updater` |
| Skills: k6, semgrep, trufflehog, owasp-dependency-scan, npm-audit, roslynator, stylecop, cartography, otel | existing task capabilities | `load-test-runner`, `sast-scanner`, `secret-scanner`, `sca-scanner`, `static-analysis-triager`, `style-conventions-reviewer`, `codebase-cartographer`, `observability-gap-finder` |
| CLAUDE.md "do not add NuGet / modify pipelines without approval" | human gates | `new-dependency-gate`, `protected-path-guard`, `pipeline-as-code-maintainer` |
| `[LlmTest]` 3-attempt semantics | LLM test policy | `llm-eval-runner`, `agent-eval-regression-runner` |
| Qodana baseline workflow (fix, then CI, then replace SARIF) | debt burn-down | `static-analysis-triager` → `mechanical-cleanup-fixer` → `bot-finding-triager` |
| `/_healthcheck`; docs/board-columns.md incident rule | incident filing | `health-check-monitor` → `incident-declarer` |
| docs/ai-workers.md (Cursor, Copilot, Claude, Bob) | worker pool | `ai-worker-mix-manager`, `model-router` |

**Gaps the repo shows today** (candidate first items for G25 to propose): there is no SLO definition or error-budget policy; no scheduled (cron) workflows, since all automation is push- or workflow_run-driven, so none of the Section 6 calendar exists yet; no DAST, SBOM or license gate in CI; no a11y or l10n automation; no FinOps visibility; and no postmortem template, although incidents are already auto-filed.
""")
open(__import__('os').path.join(__import__('os').path.dirname(__file__),'..','research','B-tasks.md'),'w').write('\n'.join(L))
print(len(' '.join(L).split()),'words')
