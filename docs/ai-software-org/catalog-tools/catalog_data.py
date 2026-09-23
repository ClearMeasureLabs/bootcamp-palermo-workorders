# Fields: name | roles | triggers | inputs | outputs | done | autonomy(A/R/H) | handoffs
GUILDS = []
def G(code, title, mission, body):
    rows=[]
    for line in body.strip().splitlines():
        line=line.strip()
        if not line or line.startswith('#'): continue
        p=[x.strip() for x in line.split('|')]
        assert len(p)==8, line
        rows.append(dict(name=p[0],roles=[r.strip() for r in p[1].split(',')],trig=[t.strip() for t in p[2].split(';')],inp=p[3],out=p[4],done=p[5],aut=p[6],hand=[h.strip() for h in p[7].split(',')] if p[7] not in ('-','') else []))
    GUILDS.append(dict(code=code,title=title,mission=mission,rows=rows))

G("G00","Orchestration & Control Plane","Runs the agent ecosystem itself: routing, scheduling, locking, budgets, escalation, evaluation, kill switch. Has no human counterpart; replaces the implicit coordination humans do in hallways.","""
event-router | AIOPS | e:*any-webhook | raw webhook payload; routing table | dispatched agent invocations with correlation id | every event routed or dead-lettered in <60s | A | agent-registry-keeper
timer-scheduler | AIOPS | t:1m | timer calendar; agent registry | fired timer invocations | zero missed fires; drift <1 min | A | agent-registry-keeper
work-claim-lock | AIOPS | o:any-agent | item id; agent id; TTL | lease on work item/branch | no two agents mutate one item; stale leases reaped | A | lane-stall-watchdog
lane-dispatcher | SM,EM | e:projects_v2_item.edited[status change]; o:owner-request-intake; o:backlog-prioritizer | ready items; WIP limits; agent capacity | one sub-session per item (feature-loop lane) | item owned by exactly one lane | A | work-claim-lock, lane-stall-watchdog
lane-stall-watchdog | SM,AIOPS | t:15m | lane heartbeats; session status | nudge / restart / escalation | no lane silent >N min (Check-StalledLanes) | A | lane-dispatcher, human-escalation-router
agent-budget-governor | FIN,AIOPS | t:hourly; e:agent.token_spend | spend per agent; budgets | throttle/pause decisions | spend within budget; overruns paused | A | finops-ai-spend-reporter, human-escalation-router
model-router | AIOPS,FIN | o:any-agent | task class; cost/quality table | model + effort selection | cost-per-successful-outcome tracked | A | agent-budget-governor
human-escalation-router | EM | o:any-agent | escalation packet; owner prefs; quiet hours | notification with explicit decision request | human ack, or re-escalate on SLA breach | A | human-decision-recorder
human-decision-recorder | EM,LEGAL | e:issue_comment.created[approval keyword]; e:pull_request_review.submitted[human] | human reply | decision record linked to item | every decision attributable & linked | A | audit-trail-keeper, human-override-learner
agent-output-evaluator | AIOPS,QA | e:agent.completed | agent output; rubric; done-criteria | quality score; pass/fail | every agent run scored | A | agent-prompt-tuner
agent-eval-regression-runner | AIOPS,QA | e:pull_request.opened[skills/prompts]; t:weekly | golden task replay set | eval report | no regression >5% on replay set | A | agent-prompt-tuner
agent-prompt-tuner | AIOPS,DX | t:weekly; o:agent-output-evaluator | failing runs; skill/prompt files | PR to skills/prompts | replay score improves; PR reviewed | R | code-review-orchestrator
agent-registry-keeper | AIOPS | e:push:main[.claude/skills or manifests] | skill files; agent manifests | registry; trigger matrix; timer calendar | registry == deployed agents | A | event-router, timer-scheduler
agent-permission-auditor | SEC,AIOPS | t:weekly; e:agent.registry.changed | agent scopes; tokens | least-privilege report; revocation PR | no agent holds an unused scope | R | secrets-store-maintainer
kill-switch-guardian | SRE,AIOPS | e:alert.fired[agent-runaway]; o:owner | loop/mass-edit/spend anomalies | pause of agent or guild | runaway halted <2 min; owner told | A | human-escalation-router
audit-trail-keeper | LEGAL,AIOPS | e:agent.completed | all agent actions | append-only action log | every mutation attributable | A | compliance-evidence-collector
conflict-arbiter | ARCH,AIOPS | o:any-agent[conflicting outputs] | contradicting recommendations | decision + rationale or escalation | conflict closed | R | human-escalation-router
repo-onboarder | DEVOPS,AIOPS | o:owner[point at repo] | repo URL | repo profile (stack, build/test cmds, CI, board ids) as factory config | private build reproduced; config PR merged | H | codebase-cartographer, agent-registry-keeper, dev-environment-validator
""")

G("G01","Intake & Triage","Turns every inbound signal (owner requests, issues, tickets, reports) into a classified, deduplicated, reproducible work item.","""
owner-request-intake | PO,PM | e:issues.opened[author=owner]; e:chat.message[owner] | natural-language change request | structured issue: type, goal, constraints | issue on board in Conceptual Definition | A | request-clarifier, work-item-classifier, problem-statement-writer
request-clarifier | PO | o:owner-request-intake | ambiguous request | clarifying questions or explicit assumptions | ambiguities resolved or assumptions stated | R | acceptance-criteria-writer
work-item-classifier | PO,SUP | e:issues.opened | issue text | labels: type, area, severity; issue type | labeled <5 min | A | duplicate-detector
duplicate-detector | PO,SUP | e:issues.opened | new issue; corpus | duplicate link / close | dupes linked; auto-close at >=0.9 confidence | R | work-item-classifier
bug-reproducer | QA,SUP | e:issues.labeled[bug] | bug report; environment | failing test or repro steps, or needs-info | repro test on branch, or needs-info label | A | bug-severity-assessor, fix-implementer
bug-severity-assessor | QA,SRE | o:bug-reproducer | repro; usage data | severity & priority with rationale | severity set | A | backlog-prioritizer
needs-info-follower | SUP | t:daily | issues labeled needs-info | reminders; stale close | closed after N days without reply | A | -
stale-issue-gardener | PO | t:weekly | open issue ages | refresh / close proposals | no issue >90d untouched | R | backlog-groomer
feature-request-analyzer | PM,PO | e:issues.labeled[enhancement] | request; vision | fit score; recommendation | recommendation comment posted | R | opportunity-scorer
intake-channel-bridge | SUP,CS | e:support.ticket.created; e:feedback.received | external ticket/feedback | linked GitHub issue | every actionable ticket mirrored | A | work-item-classifier
epic-decomposer | PO,ARCH,PGM | e:issues.labeled[epic] | epic | child issues with dependencies | children created; clamp rules applied | R | dependency-graph-resolver
dependency-graph-resolver | SM,PGM | o:epic-decomposer; e:sub_issues.changed | issue tree | children-first execution order | acyclic order computed | A | lane-dispatcher
question-answerer | SUP,TW | e:issues.labeled[question]; e:discussion.created | question; docs; code | answer with citations | answered or escalated | A | doc-gap-detector
security-report-intake | SEC | e:repository_advisory.reported; e:email[security@] | vulnerability report | private advisory; triage | acknowledged <24h | H | vuln-triager
hotfix-intake | REL,SRE | e:issues.labeled[hotfix]; o:incident-commander | incident; needed fix | expedited item that skips the queue | item in Development within minutes | A | fix-implementer, hotfix-release-runner
""")

G("G02","Product Discovery & Strategy","Decides what is worth building and why: vision, outcomes, opportunity scoring, experiments, adoption review, sunsetting.","""
product-vision-keeper | PM | t:quarterly; o:owner | vision doc; outcome data | updated vision & strategy | owner-approved | H | roadmap-planner
okr-drafter | PM,EM | t:quarterly | vision; metrics | draft OKRs | owner-approved | H | okr-progress-tracker, kpi-dashboard-builder
okr-progress-tracker | PM,EM | t:weekly | OKRs; metrics | progress report | posted weekly | A | stakeholder-status-reporter
roadmap-planner | PM,PGM | t:monthly; e:okr.changed | backlog; OKRs; capacity | now/next/later roadmap | owner-approved | H | release-planner
opportunity-scorer | PM,PO | o:feature-request-analyzer; t:weekly | candidate items; RICE/WSJF inputs | scores | every candidate scored | A | backlog-prioritizer
competitive-scanner | PM,MKT | t:monthly | competitor sites & changelogs | competitive digest | digest published | A | opportunity-scorer
market-signal-miner | PM,UXR | t:weekly | reviews; forums; social | problem themes with evidence | themes published | A | opportunity-scorer
problem-statement-writer | PM,PO | o:owner-request-intake | request | problem statement; success metric | measurable outcome defined | R | product-requirements-writer, hypothesis-framer
hypothesis-framer | PM,UXR | o:problem-statement-writer | problem | testable hypothesis | metric + threshold defined | R | experiment-designer
experiment-designer | PM,ANA | o:hypothesis-framer | hypothesis | A/B design; flag; sample size | power >=0.8 | R | feature-flag-manager
experiment-readout | ANA,PM | e:experiment.ended; t:daily | experiment data | readout; ship/kill recommendation | significance computed | R | product-vision-keeper
product-requirements-writer | PM,PO | o:problem-statement-writer | problem; research | PRD | PRD reviewed | R | technical-design-author, ux-flow-designer, nfr-specifier, tracking-plan-author
persona-maintainer | UXR,PM | t:quarterly | research; analytics | persona docs | updated and cited | R | -
customer-interview-synthesizer | UXR,PM | e:research.transcript.uploaded | transcripts | insights; jobs-to-be-done | insights tagged | A | persona-maintainer, research-repository-keeper
pricing-packaging-analyst | PM,FIN | t:quarterly | usage; unit costs | pricing recommendation | owner decision | H | -
business-case-writer | PM,PGM | o:roadmap-planner | initiative | cost/benefit; ROI | owner approved/rejected | H | portfolio-balancer
feature-adoption-reviewer | PM,ANA | t:weekly; e:release.published[+30d] | usage telemetry | keep/iterate/remove per feature | report per shipped feature | A | backlog-generator-from-signals, sunset-planner, tutorial-writer
sunset-planner | PM | o:feature-adoption-reviewer | low-adoption feature | deprecation plan | owner-approved | H | deprecation-notice-writer, deprecation-implementer
""")

G("G03","Backlog & Delivery Flow","Product-owner and scrum-master mechanics: readiness, ordering, sizing, WIP, board truth, flow metrics, retros.","""
acceptance-criteria-writer | PO,QA | e:projects_v2_item.edited[status=Conceptual Definition] | issue; PRD | Given/When/Then criteria | criteria testable; reviewed | R | test-design-author
definition-of-ready-checker | PO,SM | e:projects_v2_item.edited[status change] | item | ready / gaps list | DoR passes before Development | A | lane-dispatcher
definition-of-done-checker | PO,QA | e:pull_request.closed[merged] | item; PR; CI | DoD verdict | all DoD checks true before Done | A | board-column-mover
backlog-prioritizer | PO | t:daily; e:issues.labeled[priority inputs] | scores; severity; OKRs | ordered backlog | top-N ordered with rationale | R | lane-dispatcher
backlog-groomer | PO | t:weekly | backlog | merged/split/closed items | backlog size & age within bounds | R | estimator
estimator | PO,SM | o:backlog-groomer; e:projects_v2_item.edited[status=Technical Design] | item; history | size + confidence | estimate recorded | A | story-splitter
story-splitter | PO | o:estimator[size>L]; o:pr-size-guard | large item | vertical slices | each slice <=M | R | dependency-graph-resolver
sprint-planner | SM | t:biweekly | backlog; capacity | iteration goal & scope | owner acknowledged | R | lane-dispatcher
board-column-mover | SM | o:any-agent[column done]; e:pull_request.closed[merged] | item state; completion callback | status field update | board mirrors reality | A | -
wip-limit-enforcer | SM | e:projects_v2_item.edited[status change] | column counts | allow/block | WIP never exceeded | A | lane-dispatcher
flow-metrics-reporter | SM,EM | t:daily | board history | cycle time; throughput; aging WIP | dashboard updated | A | retrospective-facilitator
blocked-item-unblocker | SM | t:hourly; e:issues.labeled[blocked] | blocked items | unblock action or escalation | no silent block >24h | A | human-escalation-router
retrospective-facilitator | SM,EM | t:biweekly | flow metrics; incidents; agent evals | retro doc; action items as issues | actions filed | A | process-improvement-implementer
process-improvement-implementer | SM,AIOPS | o:retrospective-facilitator | action item | skill/config/process PR | merged; metric tracked | R | agent-prompt-tuner
release-scope-tracker | PO,REL | t:daily | release milestone | burn-up; scope risk | risk flagged early | A | stakeholder-status-reporter
item-closure-verifier | PO | e:issues.closed | issue; PR evidence | reopen if unmet | every closed item has merged evidence | A | -
work-item-linker | SM | e:pull_request.opened | PR; issues | PR-issue links; closing keywords | every PR linked | A | -
""")

G("G04","UX Research & Design","Designs and validates the experience: flows, components, copy, heuristics, walkthroughs, research ops.","""
ux-flow-designer | UXD | e:projects_v2_item.edited[status=UX Design] | PRD; acceptance criteria | user flows; wireframes (Mermaid/HTML) | flows cover all criteria | R | ui-component-designer, ux-copy-writer, prototype-builder
ui-component-designer | UXD,FE,A11Y | o:ux-flow-designer | wireframes; design system | component specs | specs use design tokens | R | frontend-implementer
design-system-curator | UXD,FE | t:monthly; e:push:main[UI paths] | components | token/component inventory; drift report | drift issues filed | A | ui-consistency-auditor
ui-consistency-auditor | UXD | e:pull_request.opened[UI paths] | screenshots; tokens | inconsistency comments | none unresolved | A | code-review-orchestrator
ux-copy-writer | UXD,TW,L10N | o:ux-flow-designer | flow | microcopy; error messages | style-guide conformant | R | i18n-string-extractor
prototype-builder | UXD,FE | o:ux-flow-designer | flow | clickable HTML prototype | hosted link | A | usability-heuristic-evaluator
ux-test-plan-writer | UXR,QA | e:projects_v2_item.edited[status=Test Design] | acceptance criteria | UX test script | critical tasks covered | R | ux-walkthrough-recorder
ux-walkthrough-recorder | UXD,QA | e:deployment_status.success[uat] | UAT URL; flows | screenshot/video walkthrough | walkthrough attached per feature | A | usability-heuristic-evaluator
usability-heuristic-evaluator | UXR,UXD,A11Y | e:projects_v2_item.edited[status=UX Testing] | UAT build; walkthrough | heuristic report | findings filed | A | ux-acceptance-gate
visual-regression-reviewer | UXD,QA | e:pull_request.synchronize[UI paths] | screenshot baselines | diff report | diffs approved or fixed | R | -
ux-acceptance-gate | UXD,PO | o:usability-heuristic-evaluator | walkthrough; heuristics | pass/fail for Release Queue | verdict recorded | H | board-column-mover
user-journey-analytics-reviewer | UXR,ANA | t:weekly | funnels; session data | drop-off insights | insights filed | A | backlog-generator-from-signals
survey-designer | UXR | o:hypothesis-framer | research question | survey | owner-approved before sending | H | survey-analyzer
survey-analyzer | UXR,ANA | e:survey.closed | responses | findings report | report published | A | persona-maintainer
research-repository-keeper | UXR | e:research.artifact.added | insights | tagged, deduped research repo | searchable | A | -
""")

G("G05","Architecture & Technical Design","Owns structure: designs, ADRs, contracts, NFRs, layering rules, fitness functions, debt register.","""
technical-design-author | ARCH,BE | e:projects_v2_item.edited[status=Technical Design] | PRD; criteria; codebase map | design doc: components, data, API, risks | reviewed | R | test-design-author, api-contract-designer, data-model-designer, threat-modeler
adr-writer | ARCH | o:technical-design-author; o:human-decision-recorder | decision | ADR | ADR merged | R | -
architecture-rule-enforcer | ARCH,CR | e:pull_request.opened; e:pull_request.synchronize | diff; layering rules (onion) | violations | zero violations | A | code-review-orchestrator
api-contract-designer | ARCH,BE | o:technical-design-author | resource model | OpenAPI change | spec lint clean | R | api-breaking-change-detector, api-endpoint-implementer
api-breaking-change-detector | ARCH,REL | e:pull_request.synchronize[API paths] | old/new OpenAPI | break report; version bump need | no unversioned break | A | release-notes-writer
data-model-designer | ARCH,DBA | o:technical-design-author | domain model | schema change design | migration plan exists | R | migration-author
threat-modeler | SEC,ARCH | o:technical-design-author | design | STRIDE threat model | every threat mitigated or accepted | R | security-requirements-writer
nfr-specifier | ARCH,PM,PERF | o:product-requirements-writer | PRD | measurable NFRs | latency/availability/security targets stated | R | slo-definer
integration-designer | ARCH,BE | o:technical-design-author | external systems | contract; retries; idempotency | contract reviewed | R | contract-test-author
scalability-reviewer | ARCH,PERF | o:technical-design-author | design; load forecast | capacity risks | mitigated or accepted | R | capacity-planner
design-review-board | ARCH,SEC | o:technical-design-author[high risk] | design | multi-agent critique | objections resolved or escalated | R | human-escalation-router
spike-runner | ARCH,BE | o:technical-design-author | open question | time-boxed prototype; findings | answer documented | A | adr-writer
architecture-diagram-updater | ARCH,TW | e:pull_request.closed[merged, structural] | code | C4/PlantUML/Mermaid diagrams | diagrams match code | A | diagram-renderer
codebase-cartographer | ARCH | t:monthly; o:repo-onboarder | repo | inventory; metrics; C4 views | report published | A | tech-debt-registrar
tech-debt-registrar | ARCH,EM | t:weekly | smells; CRAP; hotspots | ranked debt register | register updated | A | backlog-prioritizer, refactoring-planner
fitness-function-runner | ARCH | e:push:main; t:daily | coupling/layering/size tests | trend | thresholds held | A | tech-debt-registrar
refactoring-planner | ARCH | o:tech-debt-registrar; o:crap-score-gate | hotspot | stepwise refactor plan | plan includes safety tests | R | refactoring-implementer
tech-radar-curator | ARCH,DEP | t:quarterly | dependencies; ecosystem | adopt/trial/hold radar | owner-approved | H | dependency-upgrade-planner
""")

G("G06","Construction","Writes and changes code: features, fixes, migrations, endpoints, refactors, conflict and CI repair.","""
feature-implementer | BE,FE | e:projects_v2_item.edited[status=Development] | design; criteria; test design | PR | criteria tests pass; CI green | A | backend-implementer, frontend-implementer, code-review-orchestrator
backend-implementer | BE | o:feature-implementer | design | domain, handlers, API code | unit+integration pass | A | unit-test-author
frontend-implementer | FE | o:feature-implementer; o:ui-component-designer | component spec | UI components | component tests pass | A | a11y-static-checker, ui-component-test-author
fix-implementer | BE,FE | o:bug-reproducer; o:hotfix-intake; o:incident-diagnostician | failing repro test | fix PR | repro passes; CI green | A | code-review-orchestrator
api-endpoint-implementer | BE | o:api-contract-designer | contract | endpoint/controller | contract tests pass | A | contract-test-author
migration-author | DBA,BE | o:data-model-designer; o:index-advisor | schema change | numbered migration script | applies cleanly; rollback noted | A | migration-reviewer
refactoring-implementer | BE | o:refactoring-planner | plan | behavior-preserving PR | tests unchanged and green | A | code-review-orchestrator
mechanical-cleanup-fixer | BE,DX | o:static-analysis-triager; o:build-warning-reducer | lint findings | cleanup PR | 0 warnings with -warnaserror | A | code-review-orchestrator
review-feedback-applier | BE,FE | e:pull_request_review.submitted[changes_requested]; e:pull_request_review_comment.created | review threads | fix commits; thread replies | all threads resolved | A | code-review-orchestrator
branch-updater | DX,BE | e:push:main | open PRs | backmerge of main | no PR behind main | A | merge-conflict-resolver
merge-conflict-resolver | BE | o:branch-updater; e:pull_request.synchronize[mergeable=false] | conflicts | resolved merge | builds green | A | -
ci-failure-fixer | BE,DEVOPS | o:ci-pipeline-watcher[code failure] | logs | fix commit | CI green or escalated after 3 tries | A | flaky-test-detector, human-escalation-router
feature-flag-wirer | BE,FE | o:feature-implementer; o:experiment-designer | flag spec | flag-guarded code | both paths tested | A | feature-flag-manager
config-change-implementer | BE,DEVOPS | o:owner | config request | config PR | environment validated | R | -
dead-code-remover | BE | t:monthly | coverage; references | removal PR | tests green | R | code-review-orchestrator
sdk-client-generator | BE,DX | e:push:main[API contract] | OpenAPI | client libraries | compile + smoke pass | A | -
deprecation-implementer | BE | o:sunset-planner | deprecation plan | shims/removal PR | on schedule | R | -
data-pipeline-implementer | DE | o:technical-design-author[data]; o:data-pipeline-monitor | pipeline design | ETL/ELT code | runs on sample | A | data-quality-checker
llm-feature-implementer | BE | o:feature-implementer[AI feature] | prompt design | LLM-integrated code + evals | LlmTest passes | A | llm-eval-runner
""")

G("G07","Code Review & Code Quality","Multi-lens review, bot-finding triage, risk scoring, protected-path gating and merge.","""
code-review-orchestrator | CR | e:pull_request.opened; e:pull_request.ready_for_review; e:pull_request.synchronize | PR | fan-out to reviewer agents; aggregated review | every lens reported | A | correctness-reviewer, style-conventions-reviewer, test-adequacy-reviewer, security-review-agent, ef-query-reviewer
correctness-reviewer | CR | o:code-review-orchestrator | diff | bug findings | high-confidence findings posted | A | review-feedback-applier
style-conventions-reviewer | CR | o:code-review-orchestrator | diff; CLAUDE.md conventions | convention comments | 0 unresolved | A | review-feedback-applier
test-adequacy-reviewer | CR,QA | o:code-review-orchestrator | diff; coverage | missing-test comments | changed logic covered | A | unit-test-author
simplification-reviewer | CR | o:code-review-orchestrator | diff | reuse/simplify suggestions | posted | A | review-feedback-applier
pr-description-writer | CR,TW | e:pull_request.opened | diff; issue | PR summary; test plan | template filled | A | -
pr-size-guard | CR | e:pull_request.opened | diff stats | split recommendation | oversize flagged | A | story-splitter
change-risk-scorer | CR,REL | e:pull_request.opened | diff; hotspots; history | risk score label | scored | A | human-review-requester
protected-path-guard | CR,DEVOPS | e:pull_request.opened; e:pull_request.synchronize | changed paths (.octopus, workflows, build scripts, packages, SDK) | human-gate label | human approval before merge | H | human-review-requester
human-review-requester | CR | o:protected-path-guard; o:change-risk-scorer; o:new-dependency-gate | PR | review request with summary | human review obtained | H | human-decision-recorder
bot-finding-triager | CR | e:pull_request_review.submitted[bot]; e:check_run.completed[bot] | Copilot/Qodana/bot findings | accept/reject with rationale | every finding dispositioned | A | review-feedback-applier
static-analysis-triager | CR,DX | e:check_run.completed[qodana]; t:weekly | SARIF | ranked findings; fix batches | baseline shrinking | A | mechanical-cleanup-fixer
crap-score-gate | CR,QA | e:workflow_run.completed | coverage; complexity | CRAP report; pass/fail | under threshold | A | refactoring-planner
merge-readiness-checker | CR,REL | e:check_suite.completed; e:pull_request_review.submitted | PR state | merge/no-merge verdict | gates green; no conflicts; API-verified CI | A | pr-merger
pr-merger | REL | o:merge-readiness-checker | green PR | merge | merged by policy | A | board-column-mover, definition-of-done-checker
code-ownership-mapper | CR,EM | t:weekly | git history | CODEOWNERS draft | PR opened | R | -
duplication-detector | CR | t:weekly | code | clone report | clones above threshold filed | A | refactoring-planner
complexity-trend-watcher | CR,ARCH | t:weekly | metric history | trend alerts | regressions flagged | A | tech-debt-registrar
""")

G("G08","Test Engineering & QA","Designs, writes, runs, and curates tests at every level; verifies columns; keeps the suite trustworthy.","""
test-design-author | QA | e:projects_v2_item.edited[status=Test Design] | criteria; design | test matrix (unit/integration/acceptance) | every criterion mapped to a test | R | unit-test-author, integration-test-author, acceptance-test-author
unit-test-author | QA,BE | o:test-design-author; o:test-adequacy-reviewer; o:coverage-gap-analyzer | code | NUnit/Shouldly tests | pass; conventions met | A | -
integration-test-author | QA,BE | o:test-design-author | handlers; DB | integration tests | pass on SQL Server and SQLite | A | -
acceptance-test-author | QA | o:test-design-author | criteria | Playwright tests | pass in CI | A | -
ui-component-test-author | QA,FE | o:frontend-implementer | component | bUnit tests | pass | A | -
contract-test-author | QA,BE | o:integration-designer; o:api-endpoint-implementer | contracts | consumer/provider tests | pass | A | -
test-data-generator | QA,DBA | o:unit-test-author; o:integration-test-author | schema | builders / seed data | reproducible | A | -
functional-test-runner | QA | e:projects_v2_item.edited[status=Functional Testing]; e:deployment_status.success[tdd] | build; criteria | verification report | all criteria verified | A | board-column-mover
exploratory-tester | QA | e:deployment_status.success[uat] | UAT URL; feature | charter & findings | findings filed | A | bug-reproducer
smoke-test-runner | QA,SRE | e:deployment_status.success | environment URL | smoke results | health + key journeys pass | A | rollback-executor
cross-browser-runner | QA,FE | t:nightly | acceptance suite | browser matrix results | all pass | A | bug-reproducer
bug-verification-agent | QA | e:pull_request.closed[merged, fixes #] | bug; build | verified / reopened | repro test in suite | A | item-closure-verifier
flaky-test-detector | QA | e:workflow_run.completed; t:daily | test history | flakiness scores | flakies labeled | A | flaky-test-quarantiner
flaky-test-quarantiner | QA | o:flaky-test-detector | flaky test | quarantine PR + fix issue | main unblocked | R | flaky-test-fixer
flaky-test-fixer | QA | e:issues.labeled[flaky] | flaky test | stabilizing PR | 50 green reruns | A | -
regression-suite-curator | QA | t:weekly | runtimes; failures | pruned/rebalanced suite | runtime within budget | R | -
coverage-gap-analyzer | QA | t:weekly | coverage | gap issues | top gaps filed | A | unit-test-author
mutation-tester | QA | t:weekly | code; tests | mutation score | surviving mutants filed | A | unit-test-author
llm-eval-runner | QA | e:pull_request.synchronize[LLM paths]; t:daily | eval set | pass rates (3-attempt LlmTest semantics) | pass rate >= threshold | A | agent-prompt-tuner
test-env-provisioner | QA,DEVOPS | o:functional-test-runner; o:exploratory-tester | env spec | ephemeral env | ready <10 min | A | ephemeral-env-reaper
test-report-publisher | QA | e:workflow_run.completed | TRX; logs | job summary; trends | posted | A | -
chaos-experiment-runner | SRE,QA | t:weekly | resilience hypotheses (UAT) | findings | filed | R | resilience-improvement-planner
""")

G("G09","Performance & Capacity","Load, stress, soak, benchmarks, profiling, budgets, capacity forecasts.","""
load-test-author | PERF | o:test-design-author; e:push:main[API contract] | endpoints | k6 scripts with thresholds | smoke profile passes | A | load-test-runner
load-test-runner | PERF | t:weekly; e:release.candidate | k6 scripts | p50/p95/p99; RPS; errors | thresholds evaluated | A | perf-regression-analyzer
perf-regression-analyzer | PERF | o:load-test-runner; o:benchmark-runner | baselines | regression report | regressions filed | A | profiler-agent
benchmark-runner | PERF,BE | e:pull_request.synchronize[hot paths] | microbenchmarks | benchmark results | delta within tolerance | A | perf-regression-analyzer
profiler-agent | PERF | o:perf-regression-analyzer; o:soak-tester | traces; profiles | hotspot analysis | root cause found | A | fix-implementer
query-performance-analyzer | PERF,DBA | t:daily | query store; traces | slow-query list | top-N filed | A | index-advisor
frontend-perf-auditor | PERF,FE | e:deployment_status.success[uat]; t:weekly | pages | Core Web Vitals report | budgets met | A | frontend-implementer
bundle-size-watcher | PERF,FE | e:pull_request.synchronize[UI paths] | WASM/JS bundle | size delta | under budget | A | -
cold-start-analyzer | PERF,PLAT | t:weekly | container startup logs | startup trend | within budget | A | container-runtime-tuner
stress-breakpoint-tester | PERF | t:monthly | breakpoint profile | breaking point | documented | A | capacity-planner
soak-tester | PERF | t:monthly | soak profile | leak/degradation report | none or filed | A | profiler-agent
rate-limit-validator | PERF,SEC | e:pull_request.synchronize[rate-limit config] | policy | 429 behavior report | limits behave as designed | A | -
capacity-planner | PERF,SRE,PLAT | t:monthly | growth; saturation | capacity forecast | headroom >= target | R | infra-change-planner
perf-budget-keeper | PERF,PM | t:quarterly | SLOs; NFRs | performance budgets | owner-approved | H | -
""")

G("G10","Security & AppSec","OWASP SAMM across govern/design/implement/verify/operate: scanning, triage, fixing, secrets, access, advisories.","""
sast-scanner | SEC | e:pull_request.synchronize; e:push:main | code | Semgrep/CodeQL findings | every finding dispositioned | A | vuln-triager
secret-scanner | SEC | e:push; e:pull_request.synchronize | diff | TruffleHog findings | no verified secret merged | A | secret-leak-responder
secret-leak-responder | SEC | e:secret_scanning_alert.created; o:secret-scanner | leaked secret | revocation; rotation; purge plan | secret revoked <1h | H | secret-rotation-runner
secret-rotation-runner | SEC,DEVOPS | t:monthly; o:secret-leak-responder | secret inventory | rotated secrets | none beyond max age | R | -
vuln-triager | SEC | e:code_scanning_alert.created; e:dependabot_alert.created; o:sca-scanner | alert; reachability | severity; exploitability; SLA | triaged within SLA | A | vuln-fixer
vuln-fixer | SEC,BE | o:vuln-triager | vulnerability | fix PR | alert closed | A | code-review-orchestrator, security-advisory-publisher
security-review-agent | SEC,CR | o:code-review-orchestrator | diff | security findings | posted | A | review-feedback-applier
dast-scanner | SEC | e:deployment_status.success[uat]; t:weekly | UAT URL | ZAP findings | filed | A | vuln-triager
security-requirements-writer | SEC | o:threat-modeler | threats | security acceptance criteria | criteria added | R | security-test-author
security-test-author | SEC,QA | o:security-requirements-writer | security criteria | abuse-case tests | pass | A | -
authz-matrix-auditor | SEC | t:weekly; e:pull_request.synchronize[auth paths] | endpoints; policies | authz coverage matrix | no unguarded endpoint | A | vuln-fixer
container-image-scanner | SEC,DEVOPS | e:workflow_run.completed[image built]; t:daily | images | CVE list | no critical in prod | A | base-image-updater
iac-security-scanner | SEC,PLAT | e:pull_request.synchronize[infra paths] | IaC | misconfig findings | none high | A | iac-implementer
supply-chain-attestation | SEC,REL | e:release.published | build provenance | SLSA provenance; signatures | artifacts signed & verifiable | A | -
waf-rule-tuner | SEC,SRE | e:alert.fired[waf]; t:weekly | WAF logs | rule changes | false positives down | R | -
access-review-runner | SEC,EM | t:quarterly | repo/cloud access lists | stale-access removals | owner-approved | H | -
security-posture-reporter | SEC,EM | t:monthly | all findings | posture report; SAMM scores | published | A | stakeholder-status-reporter
pen-test-coordinator | SEC | t:annual | scope | engagement; imported findings | findings tracked | H | vuln-triager
security-advisory-publisher | SEC,MKT | o:vuln-fixer[public impact] | fixed vulnerability | GHSA/CVE advisory | owner-approved | H | release-comms-writer
security-guidance-writer | SEC,DX | t:quarterly | recurring finding classes | guidance docs; skill updates | published | R | agent-prompt-tuner
""")

G("G11","Supply Chain, Dependency & License","Keeps third-party code current, safe, licensed and approved.","""
dependency-update-proposer | DEP | t:weekly; e:package.version.published | manifests | grouped upgrade PRs | CI green per group | A | code-review-orchestrator
dependency-upgrade-planner | DEP,ARCH | t:quarterly; o:eol-runtime-watcher | majors; EOL dates | upgrade roadmap (e.g. .NET major) | owner-approved | H | epic-decomposer
sca-scanner | DEP,SEC | e:pull_request.synchronize[manifests]; t:daily | manifests | OWASP DC / dotnet vulnerable / npm audit | no high/critical | A | vuln-triager
new-dependency-gate | DEP,ARCH | e:pull_request.synchronize[new package] | package; rationale | approval request | human-approved (repo rule) | H | human-review-requester
license-compliance-checker | DEP,LEGAL | e:pull_request.synchronize[manifests]; t:weekly | deps; license policy | license report | no disallowed license | A | legal-review-requester
sbom-generator | DEP,SEC | e:release.published; e:push:main | build | CycloneDX/SPDX SBOM | SBOM attached | A | -
typosquat-malware-checker | DEP,SEC | e:pull_request.synchronize[manifests] | new deps | malicious-package verdict | none | A | -
eol-runtime-watcher | DEP | t:monthly | SDK/runtime versions | EOL alerts | issue >=90d before EOL | A | dependency-upgrade-planner
base-image-updater | DEP,DEVOPS | t:weekly; o:container-image-scanner; o:os-patch-manager | Dockerfiles | image bump PRs | CI green | A | -
abandoned-package-detector | DEP | t:monthly | package metadata | unmaintained-package risks | filed | A | tech-radar-curator
lockfile-drift-auditor | DEP | t:monthly | lockfiles; central package versions | drift report | consistent | A | -
third-party-notice-writer | DEP,LEGAL | e:release.published | licenses | NOTICE file | complete | A | -
action-pinning-auditor | DEP,SEC | e:pull_request.synchronize[workflows]; t:weekly | workflow YAML | SHA-pin report | all actions pinned | R | protected-path-guard
""")

G("G12","CI/CD, Build & Release","Pipeline health, artifacts, versions, release trains, environment promotion, rollback, DORA.","""
ci-pipeline-watcher | DEVOPS | e:workflow_run.completed | run results | failure class: code / infra / flaky | every failure classified | A | ci-failure-fixer, flaky-test-detector, ci-infra-fixer
ci-infra-fixer | DEVOPS | o:ci-pipeline-watcher[infra] | runner logs | infra fix or retry | pipeline restored | R | protected-path-guard
ci-duration-optimizer | DEVOPS,DX | t:weekly | job timings | caching/parallelism PR | median duration down | R | protected-path-guard
pipeline-as-code-maintainer | DEVOPS | o:owner; o:ci-duration-optimizer | workflow YAML | workflow PR | human-approved | H | protected-path-guard
build-reproducibility-checker | DEVOPS | t:weekly | two builds | hash comparison | deterministic | A | -
artifact-publisher | REL | e:workflow_run.completed[success, main] | build output | versioned packages/images | published, immutable | A | deployment-orchestrator
version-bumper | REL | e:pull_request.closed[merged] | commits | semver | version correct | A | changelog-generator
changelog-generator | REL,TW | e:release.candidate | merged PRs | CHANGELOG | every user-facing PR present | A | release-notes-writer
release-planner | REL,PM | t:weekly | roadmap; queue | release-train plan | published | R | release-scope-tracker
release-candidate-cutter | REL | e:projects_v2_item.edited[status=Release Queue]; t:weekday | green main | RC tag | tagged | A | deployment-orchestrator
deployment-orchestrator | REL,DEVOPS | e:workflow_run.completed[success, build] | artifacts; env | TDD then UAT deployment | deployed and healthy | A | smoke-test-runner, migration-deploy-runner
migration-deploy-runner | DBA,REL | o:deployment-orchestrator | DbUp scripts | applied migrations | schema version matches | A | -
prod-deploy-gate | REL,PO | e:deployment_status.success[uat] | UAT evidence; risk score | go/no-go packet | human approval (policy-auto for low risk) | H | deployment-orchestrator
progressive-rollout-controller | REL,SRE | o:deployment-orchestrator[prod] | canary metrics | traffic-shift steps | 100% or rolled back | A | rollback-executor
rollback-executor | REL,SRE | e:alert.fired[post-deploy]; o:smoke-test-runner[fail] | previous revision | rollback | health restored | A | incident-commander
release-verifier | REL,QA | e:deployment_status.success[prod] | prod | post-release checks | verified | A | release-comms-writer, synthetic-monitor-author
hotfix-release-runner | REL | o:hotfix-intake | fix PR | expedited pipeline run | deployed | R | prod-deploy-gate
feature-flag-manager | REL,PM | o:experiment-designer; o:feature-flag-wirer; t:weekly | flags | flag state changes; stale report | no flag >90d stale | R | stale-flag-remover
stale-flag-remover | REL,BE | o:feature-flag-manager | stale flag | cleanup PR | merged | A | -
deploy-freeze-enforcer | REL | e:calendar.freeze_window; o:error-budget-tracker | calendar; error budget | deploy block | no deploy in freeze without override | A | -
environment-promotion-tracker | REL | e:deployment_status | deployments | what-is-where matrix | accurate | A | -
docs-only-fast-path | REL | e:pull_request.closed[merged, docs-only] | changed paths | skip release; card to Done | card in Done | A | board-column-mover
dora-metrics-collector | REL,EM | t:daily | deploys; incidents; commits | deploy freq; lead time; CFR; MTTR | dashboard updated | A | engineering-health-reporter
""")

G("G13","Platform & Infrastructure","IaC, environments, drift, certificates, backups, DR, quotas, runners, golden paths.","""
infra-change-planner | PLAT | o:capacity-planner; o:owner | requirement | IaC plan | reviewed | R | iac-implementer, cost-impact-reviewer
iac-implementer | PLAT | o:infra-change-planner; o:drift-detector; o:rightsizing-advisor | plan | IaC PR | plan/what-if clean | R | protected-path-guard
drift-detector | PLAT | t:daily | IaC; live state | drift report | zero unexplained drift | A | iac-implementer
environment-provisioner | PLAT | o:repo-onboarder; o:owner | env spec | new environment | reachable and healthy | H | -
ephemeral-env-reaper | PLAT,FIN | t:hourly | ephemeral envs | deletions | none older than TTL | A | -
certificate-expiry-watcher | PLAT,SRE | t:daily | certificates | renewals / alerts | none expiring <14d | A | -
dns-domain-manager | PLAT | o:owner | domain change | DNS records | propagated | H | -
backup-verifier | PLAT,DBA | t:daily | backups | restore-test result | restore succeeds | A | -
dr-drill-runner | PLAT,SRE | t:quarterly | DR plan | drill report; measured RTO/RPO | within targets | R | resilience-improvement-planner
container-runtime-tuner | PLAT | t:weekly; o:cold-start-analyzer | CPU/mem; scale rules | scaling config PR | utilization in band | R | iac-implementer
os-patch-manager | PLAT,SEC | t:weekly | hosts/images | patches | no overdue critical patch | A | base-image-updater
quota-limit-watcher | PLAT | t:daily | cloud quotas | increase requests | headroom >20% | A | -
network-exposure-auditor | PLAT,SEC | t:weekly | NSGs; ingress | exposure report | no unintended public exposure | A | iac-implementer
runner-fleet-manager | PLAT,DEVOPS | t:hourly | CI queue | runner scaling | queue wait < target | A | -
secrets-store-maintainer | PLAT,SEC | e:secret.requested; o:agent-permission-auditor | vault | provisioned secret refs | no plaintext secrets | R | -
golden-path-maintainer | PLAT,DX | t:quarterly | service templates | updated templates | new repo boots <1h | R | -
""")

G("G14","SRE, Observability & Incident","SLOs, alerting, detection, incident command, diagnosis, postmortems, toil and resilience.","""
slo-definer | SRE,PM | o:nfr-specifier; t:quarterly | NFRs; telemetry | SLIs/SLOs | owner-approved | H | alert-rule-author, error-budget-tracker
alert-rule-author | SRE | o:slo-definer; o:alert-noise-reducer | SLOs | burn-rate alerts | alerts tested | R | runbook-author
health-check-monitor | SRE | t:1m | /_healthcheck per env | health status | unhealthy emits event <1 min | A | incident-declarer
dependency-health-watcher | SRE | t:5m | third-party status (cloud, LLM API) | degradation events | detected | A | incident-declarer
log-anomaly-detector | SRE | t:15m | logs | anomaly events | noise-tuned | A | incident-declarer
error-tracker-triager | SRE,BE | e:exception.new_fingerprint | exceptions | bug issues | every new error filed | A | bug-reproducer
incident-declarer | SRE | e:alert.fired; e:healthcheck.unhealthy; e:slo.burn_rate | alert | Production Incident issue in Release Queue; severity | declared <2 min | A | incident-commander
incident-commander | SRE | o:incident-declarer | incident | roles; timeline; update cadence | mitigated | R | incident-diagnostician, status-page-updater, on-call-pager
incident-diagnostician | SRE | o:incident-commander | logs; traces; recent deploys | ranked hypotheses; likely cause | cause identified | A | rollback-executor, fix-implementer, runbook-executor
runbook-executor | SRE | o:incident-diagnostician | runbook | executed steps | mitigated or escalated | R | -
on-call-pager | SRE | o:incident-commander[SEV1/2] | rotation | page to human | ack within SLA | A | human-escalation-router
status-page-updater | SRE,SUP | o:incident-commander | incident state | status-page updates | updates every 30 min | R | customer-incident-notifier
postmortem-writer | SRE | e:incident.resolved | timeline; data | blameless postmortem | published <5 days | R | action-item-filer
action-item-filer | SRE | o:postmortem-writer | postmortem | action-item issues | all filed and prioritized | A | backlog-prioritizer
error-budget-tracker | SRE | t:daily | SLOs | budget report; freeze recommendation | policy applied | A | deploy-freeze-enforcer
toil-tracker | SRE | t:weekly | manual-intervention log | toil inventory | ranked automation candidates | A | backlog-generator-from-signals
alert-noise-reducer | SRE | t:weekly | alert history | tuning PRs | actionable ratio up | R | alert-rule-author
observability-gap-finder | SRE,BE | e:pull_request.synchronize[handlers/external calls]; t:weekly | code; OTel wiring | missing span/metric findings | new paths instrumented | A | review-feedback-applier
dashboard-maintainer | SRE,ANA | t:monthly | dashboards | fixed dashboards | no broken panels | A | -
runbook-author | SRE,TW | o:postmortem-writer; o:alert-rule-author | alert | runbook | every alert has a runbook | R | runbook-librarian
synthetic-monitor-author | SRE,QA | o:release-verifier | key journeys | synthetic checks | key journeys covered | A | -
resilience-improvement-planner | SRE,ARCH | o:chaos-experiment-runner; o:dr-drill-runner | findings | resilience backlog | filed | A | backlog-prioritizer
""")

G("G15","Data: DBA & Data Engineering","Schema safety, query health, retention, masking, pipelines, data quality.","""
migration-reviewer | DBA | e:pull_request.synchronize[migration scripts] | SQL | review (locking, idempotency, tabs) | approved | A | -
migration-numbering-guard | DBA | e:pull_request.synchronize[scripts/Update] | script names | collision report | unique, sequential | A | -
index-advisor | DBA | o:query-performance-analyzer | query plans | index proposal | measured improvement | R | migration-author
ef-query-reviewer | DBA,BE | o:code-review-orchestrator[DataAccess] | LINQ/EF code | N+1 and tracking findings | posted | A | review-feedback-applier
schema-drift-detector | DBA | t:daily | schema per env | drift report | none | A | -
db-capacity-watcher | DBA | t:daily | size; compute | forecast | headroom kept | A | capacity-planner
db-maintenance-runner | DBA | t:weekly | stats; fragmentation | maintenance run | done | A | -
deadlock-analyzer | DBA | e:alert.fired[deadlock] | deadlock graphs | root cause | filed | A | fix-implementer
data-retention-enforcer | DBA,LEGAL | t:daily | retention policy | purge jobs | compliant | A | -
pii-data-classifier | DBA,LEGAL | e:pull_request.synchronize[schema]; t:monthly | schema | PII classification | all columns classified | A | privacy-impact-assessor
db-restore-to-lower-env | DBA | o:owner; t:weekly | prod backup | masked copy in UAT | masked and loaded | H | pii-data-masker
pii-data-masker | DBA,LEGAL | o:db-restore-to-lower-env | data | masked dataset | no PII in lower envs | A | -
data-migration-runner | DE,DBA | o:owner | backfill plan | executed backfill | counts verified | H | -
data-quality-checker | DE | t:daily; e:pipeline.completed | datasets | DQ report | checks pass | A | -
data-pipeline-monitor | DE | e:pipeline.failed | runs | triage; rerun or issue | resolved | A | data-pipeline-implementer
analytics-schema-maintainer | DE,ANA | e:tracking_plan.changed | tracking plan | warehouse models | builds | A | -
""")

G("G16","Analytics & Insights","Instrumentation, KPIs, anomaly detection, funnels, cohorts, ad-hoc questions.","""
tracking-plan-author | ANA | o:product-requirements-writer | PRD | event tracking spec | reviewed | R | feature-implementer
instrumentation-verifier | ANA,QA | e:deployment_status.success[uat] | tracking plan | event validation | all events fire | A | -
kpi-dashboard-builder | ANA | o:okr-drafter | KPIs | dashboards | live | A | -
metric-definition-keeper | ANA | e:pull_request.synchronize[metrics] | definitions | semantic layer | single source of truth | R | -
weekly-metrics-digest | ANA | t:weekly | KPIs | digest | posted | A | stakeholder-status-reporter
metric-anomaly-detector | ANA | t:daily | KPIs | anomaly alerts | investigated | A | backlog-generator-from-signals
funnel-analyzer | ANA,UXR | t:weekly | events | funnel report | drop-offs flagged | A | user-journey-analytics-reviewer
cohort-retention-analyzer | ANA,CS | t:monthly | usage | retention curves | published | A | churn-risk-detector
ad-hoc-question-answerer | ANA | o:owner; o:any-agent | question | query + answer with SQL shown | answered | A | -
customer-usage-reporter | ANA,CS | t:monthly | tenant usage | customer usage report | sent | R | -
telemetry-cost-optimizer | ANA,FIN,SRE | t:monthly | ingestion volumes | sampling changes | cost down, signal kept | R | -
""")

G("G17","Docs & Knowledge","Keeps human- and agent-facing knowledge accurate: API refs, guides, notes, CLAUDE.md, runbooks, diagrams.","""
release-notes-writer | TW,MKT | e:release.candidate; o:changelog-generator | changelog | user-facing notes | reviewed | R | release-comms-writer
api-reference-generator | TW | e:push:main[API contract] | OpenAPI | API docs | published | A | -
user-guide-writer | TW | e:pull_request.closed[merged, user-facing] | feature | user docs | published | R | -
doc-gap-detector | TW | e:issues.labeled[question]; t:weekly | questions; docs | gap issues | filed | A | user-guide-writer
doc-drift-checker | TW,DX | e:pull_request.synchronize | code vs docs (CLAUDE.md, README, docs/) | stale-doc comments or PR | docs match code | A | -
code-comment-documenter | TW,BE | e:pull_request.synchronize | public APIs | XML doc comments | public API 100% documented | A | -
link-checker | TW | t:weekly | docs | broken-link fixes | 0 broken | A | -
readme-maintainer | TW,DX | t:monthly | repo | README updates | accurate | R | -
runbook-librarian | TW,SRE | t:monthly | runbooks | index; freshness | none >6 months unreviewed | A | -
glossary-maintainer | TW | t:monthly | domain terms | glossary | current | A | -
tutorial-writer | TW,DX | o:feature-adoption-reviewer[low adoption] | feature | tutorial | published | R | -
diagram-renderer | TW,ARCH | e:pull_request.synchronize[*.puml] | PlantUML | PNGs | rendered | A | -
docs-site-publisher | TW | e:push:main[docs] | docs | docs site | deployed | A | -
onboarding-guide-maintainer | TW,DX | t:quarterly | repo changes | onboarding guide for humans and agents | validated by fresh-agent dry run | R | -
kb-article-writer | TW,SUP | o:support-ticket-resolver[recurring] | tickets | KB article | published | R | faq-updater
""")

G("G18","Support & Customer Success","Tickets, investigation, SLAs, customer comms, health, churn and feedback loops.","""
support-ticket-triager | SUP | e:support.ticket.created | ticket | category; priority; route | triaged <15 min | A | support-ticket-resolver
support-ticket-resolver | SUP | o:support-ticket-triager | ticket; KB; logs | reply | resolved or escalated | R | intake-channel-bridge, kb-article-writer
customer-log-investigator | SUP,SRE | o:support-ticket-resolver | tenant/user id | log findings | evidence-backed answer | A | bug-reproducer
support-sla-watcher | SUP | t:15m | tickets | SLA breach alerts | none breached silently | A | human-escalation-router
bug-report-quality-coach | SUP | e:issues.opened[bug, incomplete] | report | request for build, env, logs | report complete | A | -
customer-incident-notifier | SUP,CS | o:status-page-updater | incident | affected-customer notices | sent | R | -
close-the-loop-notifier | SUP,CS | e:issues.closed[linked tickets] | fixed issue | customer notification | notified | A | -
sentiment-analyzer | CS | t:daily | tickets; NPS | sentiment trends | report | A | backlog-generator-from-signals
churn-risk-detector | CS,ANA | t:weekly | usage; tickets | at-risk accounts | flagged | A | customer-health-reporter
customer-health-reporter | CS | t:weekly | health signals | health scores | published | A | -
feature-feedback-router | CS,PM | e:feedback.received | feedback | linked votes on issues | every item linked | A | feature-request-analyzer
support-macro-maintainer | SUP | t:monthly | resolutions | canned responses | updated | A | -
faq-updater | SUP,TW | t:weekly | top questions | FAQ | current | A | -
customer-onboarding-assistant | CS | e:customer.signed_up | account | onboarding checklist | activated | R | -
""")

G("G19","Accessibility & Localization","WCAG conformance and world-readiness as continuous checks, not audits.","""
a11y-static-checker | A11Y | e:pull_request.synchronize[UI paths] | markup | WCAG static findings | none serious | A | review-feedback-applier
a11y-runtime-auditor | A11Y | e:deployment_status.success[uat]; t:weekly | pages | axe-core via Playwright | WCAG 2.2 AA pass | A | frontend-implementer
keyboard-nav-tester | A11Y,QA | e:deployment_status.success[uat] | flows | keyboard-only results | all flows operable | A | -
screen-reader-tester | A11Y | t:weekly | pages | screen-reader transcript review | labels meaningful | R | -
color-contrast-checker | A11Y,UXD | e:pull_request.synchronize[design tokens] | tokens | contrast ratios | >=4.5:1 | A | -
a11y-conformance-reporter | A11Y,LEGAL | t:quarterly | audits | VPAT/ACR | published | H | -
i18n-string-extractor | L10N | e:pull_request.synchronize[UI paths] | code | resource files; hardcoded-string findings | none hardcoded | A | translation-generator
translation-generator | L10N | e:push:main[resources] | resx | machine translations flagged for review | all locales filled | R | translation-reviewer
translation-reviewer | L10N,LEGAL | o:translation-generator | translations | reviewed strings | legal/marketing text human-reviewed | H | -
locale-format-tester | L10N,QA | t:weekly | dates; numbers; RTL | findings | pass | A | -
pseudo-localization-runner | L10N | e:pull_request.synchronize[UI paths] | UI | truncation/overflow findings | none | A | -
""")

G("G20","Legal, Privacy & Compliance","Privacy, licensing, regulatory tracking, evidence, change records, AI-use register.","""
privacy-impact-assessor | LEGAL | o:pii-data-classifier; o:technical-design-author[personal data] | design | DPIA draft | human/DPO approval | H | -
data-subject-request-handler | LEGAL,SUP | e:dsr.received | request | export or deletion | fulfilled within statutory window | R | -
policy-text-watcher | LEGAL | o:privacy-impact-assessor | terms/privacy text | change recommendation | human-approved | H | -
legal-review-requester | LEGAL | o:license-compliance-checker; o:policy-text-watcher | issue | packet for counsel | human decision | H | human-decision-recorder
compliance-evidence-collector | LEGAL,SEC | t:daily | CI logs; approvals; audit trail | SOC 2/ISO evidence | controls evidenced | A | -
control-gap-assessor | LEGAL,SEC | t:quarterly | control framework | gaps | filed | R | -
change-approval-recorder | LEGAL,REL | e:deployment_status.success[prod] | deploy; approvals | change record | every prod change recorded | A | -
cookie-consent-auditor | LEGAL | t:monthly | site | tracker inventory vs consent | compliant | A | -
regulatory-change-watcher | LEGAL | t:monthly | GDPR, EU AI Act, ADA feeds | impact memo | reviewed | R | -
ai-system-register-keeper | LEGAL,AIOPS | t:quarterly | agent inventory | AI system register; disclosures | current | R | -
export-control-checker | LEGAL | e:release.published | crypto use; destinations | classification | cleared | H | -
contributor-license-checker | LEGAL | e:pull_request.opened[external author] | author | CLA/DCO status | signed | A | -
""")

G("G21","FinOps & Cost","Inform, optimize, govern cloud and AI spend (FinOps Framework domains).","""
cloud-cost-reporter | FIN | t:daily | billing export | cost by service/env/tag | published | A | -
cost-anomaly-detector | FIN | t:daily; e:cost.anomaly | spend | anomaly issue | investigated <24h | A | rightsizing-advisor
rightsizing-advisor | FIN,PLAT | t:weekly | utilization | rightsizing proposal | savings realized | R | iac-implementer
idle-resource-reaper | FIN | t:daily | resources | auto-stop non-prod; prod proposals | idle non-prod stopped | A | -
tagging-compliance-enforcer | FIN,PLAT | e:resource.created; t:daily | tags | tag fixes | 100% tagged | A | -
cost-impact-reviewer | FIN,ARCH | e:pull_request.synchronize[infra paths]; o:infra-change-planner | IaC diff | cost delta estimate | posted | A | -
budget-forecaster | FIN,PGM | t:monthly | trend | forecast vs budget | variance explained | A | stakeholder-status-reporter
unit-cost-calculator | FIN,PM | t:monthly | cost; usage | cost per work order / tenant | published | A | pricing-packaging-analyst
finops-ai-spend-reporter | FIN,AIOPS | t:daily | LLM token spend per agent | AI cost report; cost per merged PR | published | A | agent-budget-governor
commitment-planner | FIN | t:quarterly | usage baseline | reservation/savings-plan recommendation | human purchase decision | H | -
saas-license-auditor | FIN | t:quarterly | SaaS seats | unused seats | reclaimed | R | -
""")

G("G22","Communications & Marketing","Release comms, notices, public changelog, stakeholder reporting.","""
release-comms-writer | MKT | e:release.published; o:release-notes-writer | release notes | announcement / blog / email draft | approved | H | website-content-updater
deprecation-notice-writer | MKT,PM | o:sunset-planner | deprecation plan | customer notice | sent on schedule | H | -
changelog-page-publisher | MKT,TW | e:release.published | notes | public changelog | live | A | -
social-post-drafter | MKT | e:release.published | highlights | social drafts | human-approved | H | -
demo-script-writer | MKT,PM | e:release.published[major] | features | demo script | reviewed | R | -
sales-enablement-writer | MKT | t:monthly | features | battlecards | published | R | -
website-content-updater | MKT | o:release-comms-writer | site | content PR | approved | H | -
seo-auditor | MKT | t:monthly | site | SEO findings | filed | A | -
stakeholder-status-reporter | EM,PGM | t:weekly | flow; OKRs; releases; incidents; cost | owner status report | delivered | A | -
internal-digest-writer | EM | t:monthly | achievements | digest | sent | A | -
""")

G("G23","Engineering Management, Portfolio & Program","Capacity allocation across repos, risk, portfolio mix, agent performance management.","""
engineering-health-reporter | EM | t:weekly | DORA; flow; quality; cost | health scorecard | published | A | stakeholder-status-reporter
capacity-allocator | EM,PGM | t:monthly | agent capacity; budget | allocation: features / debt / ops | owner-approved | H | lane-dispatcher
portfolio-balancer | PGM | t:quarterly | initiatives across repos | portfolio view; investment mix | owner decision | H | roadmap-planner
cross-repo-dependency-coordinator | PGM | e:issues.cross_referenced; t:weekly | multi-repo items | dependency plan | no silently blocked cross-repo items | A | dependency-graph-resolver
milestone-risk-assessor | PGM | t:weekly | milestones | risk register | risks owned | A | human-escalation-router
raid-log-keeper | PGM | t:weekly | risks; assumptions; issues; dependencies | RAID log | current | A | -
quarterly-business-review-writer | PGM,EM | t:quarterly | all reports | QBR document | owner reviewed | R | -
agent-performance-reviewer | EM,AIOPS | t:monthly | evals; cost; outcomes | agent scorecards; retire/merge/split proposals | owner-approved | R | agent-registry-keeper
human-gate-queue-balancer | EM | t:weekly | pending human gates | batched decisions; gate SLA report | gate queue < N | A | human-escalation-router
policy-keeper | EM,AIOPS | e:push:main[CLAUDE.md, policies] | policies | policy digest; agent reload | agents on current policy | A | agent-registry-keeper
ai-worker-mix-manager | EM,FIN | t:monthly | per-worker outcomes (Cursor/Copilot/Claude/Bob) | worker mix recommendation | config updated | R | model-router
""")

G("G24","Developer Experience","Keeps the inner loop fast for agents and the human owner: environments, hooks, skills, scaffolds, prompts.","""
dev-environment-validator | DX | t:daily; e:push:main[build files] | setup scripts; devcontainer | fresh-clone build result | builds from zero | A | -
session-start-hook-maintainer | DX | e:push:main[dependencies] | SessionStart hook | updated hook | cloud sessions build and test | R | -
build-warning-reducer | DX | t:weekly | compiler warnings | fix batches | trend down | A | mechanical-cleanup-fixer
skill-library-curator | DX,AIOPS | t:monthly | skills; usage | consolidated skills | no duplicate skills | R | agent-registry-keeper
local-build-time-tracker | DX | t:weekly | build logs | build-time trend | regressions flagged | A | ci-duration-optimizer
scaffolding-generator | DX,BE | o:feature-implementer | pattern (query+handler+test) | scaffolds | compile | A | -
inner-loop-optimizer | DX | t:monthly | build/test loop timings | targeted-test tooling | loop time down | R | -
permission-prompt-reducer | DX | t:weekly | transcripts | allowlist PR | prompts down | R | -
owner-friction-surveyor | DX,EM | t:quarterly | owner feedback | friction list | filed | A | -
api-sandbox-maintainer | DX | e:push:main[API contract] | sandbox | updated samples | working | A | -
""")

G("G25","Kaizen: Self-Observation & Backlog Generation","Converts observed signals into evidence-backed backlog items; learns from reverts, escapes, overrides and rework.","""
signal-aggregator | AIOPS | e:finding.emitted | findings stream from all observers | clustered themes | clusters updated | A | backlog-generator-from-signals
backlog-generator-from-signals | PM,AIOPS | t:daily; o:signal-aggregator | clustered signals | draft items with evidence, labeled agent-proposed | deduped; scored | R | duplicate-detector, opportunity-scorer, agent-proposal-gate
agent-proposal-gate | PO | o:backlog-generator-from-signals | proposals | accept/reject; policy auto-accept for low-risk classes | owner decision or policy | H | backlog-prioritizer
recurring-failure-miner | AIOPS,SRE | t:weekly | CI failures; incidents; reverts | systemic issues | filed | A | backlog-generator-from-signals
revert-analyzer | CR,QA | e:push:main[revert commit] | reverted PR | escape analysis | missing gate filed | A | process-improvement-implementer
escaped-defect-analyzer | QA | e:issues.labeled[bug, production] | bug; history | which gate missed it | gate improvement filed | A | process-improvement-implementer
rework-detector | SM | t:weekly | items bouncing between columns | rework report | root causes filed | A | retrospective-facilitator
agent-friction-miner | AIOPS | t:daily | transcripts: errors, retries, permission denials | friction items | filed | A | agent-prompt-tuner
human-override-learner | AIOPS | o:human-decision-recorder | overrides / rejections | prompt/policy updates | patterns encoded | R | agent-prompt-tuner
goal-drift-detector | PM | t:weekly | shipped work vs OKRs | alignment report | misalignment flagged | A | backlog-prioritizer
quality-trend-sentinel | QA,EM | t:weekly | coverage; CRAP; bugs; flakiness | trend alerts | regressions filed | A | backlog-generator-from-signals
idea-incubator | PM | t:monthly | all insights | bets proposals | owner reviewed | H | product-vision-keeper
""")
