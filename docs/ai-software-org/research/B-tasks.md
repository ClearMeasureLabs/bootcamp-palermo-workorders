# B — Task Decomposition Catalog for a 100% AI Software Organization

*Agent B (Work Decomposer). Lens: every role in a complete software organization, decomposed into discrete, trigger-driven TASK agents, then deduplicated into guilds.*

**Totals:** 401 task agents in 26 guilds, covering 32 roles. Autonomy: 259 auto, 100 auto-with-review, 42 human-gate.

## 0. How to read this document

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

## 1. Role inventory (32 roles)

| Code | Role | Grounding | Task agents it contributes to | Guilds touched |
|---|---|---|---|---|
| PM | Product Manager | Pragmatic Framework (37 activities: market, focus, business, planning, programs); SWEBOK Requirements/Economics | 32 | 10 |
| PO | Product Owner | Scrum Guide backlog ownership; board columns in .claude/factory-loop.json | 22 | 6 |
| UXR | UX Researcher | Research ops, surveys, interview synthesis, usability studies | 11 | 3 |
| UXD | UX/UI Designer | Flows, components, design systems; repo 'UX Design' + 'UX Testing' columns | 11 | 2 |
| TW | Technical Writer | Docs-as-code; CLAUDE.md, docs/, arch/ diagrams in repo | 23 | 9 |
| ARCH | Software Architect | SWEBOK v4 Software Architecture & Design KAs; onion rules in CLAUDE.md | 26 | 8 |
| BE | Backend Developer | SWEBOK Construction; MediatR/EF Core handlers in repo | 32 | 10 |
| FE | Frontend Developer | Blazor WASM/Server UI in repo | 12 | 4 |
| DE | Data Engineer | Pipelines, warehouse models, data quality | 5 | 2 |
| DBA | Database Administrator | DbUp migrations (src/Database/scripts/Update), SQL Server | 19 | 7 |
| CR | Code Reviewer | Peer review as DORA's replacement for heavyweight change approval | 20 | 4 |
| QA | QA / Test Automation Engineer | SWEBOK Testing & Quality; NUnit/Shouldly/bUnit/Playwright in repo | 42 | 12 |
| PERF | Performance Engineer | k6-load-testing skill in repo; capacity planning | 16 | 2 |
| SEC | AppSec / Security Engineer | SWEBOK v4 Software Security KA; OWASP SAMM 15 practices; semgrep/trufflehog skills | 34 | 8 |
| DEP | Dependency & License Compliance | owasp-dependency-scan, npm-audit skills; 'no new NuGet without approval' rule | 14 | 2 |
| DEVOPS | DevOps / Build Engineer | build.yml (build-linux, qodana, security-scan, acceptance-tests jobs) | 15 | 8 |
| REL | Release Manager | deploy.yml TDD->UAT->Prod; docs/release-cadence.md | 26 | 7 |
| PLAT | Platform / Cloud Engineer | Azure Container Apps, IaC, environments (docs/environments.md) | 21 | 4 |
| SRE | SRE / On-call / Incident Mgmt | Google SRE book: toil, on-call, incident command, blameless postmortems | 37 | 12 |
| ANA | Product/Data Analyst | KPIs, funnels, experiments, telemetry | 19 | 6 |
| SUP | Support Engineer | docs/support.md: issues picked up by the AI Factory | 18 | 5 |
| CS | Customer Success | Health, churn, onboarding, close-the-loop | 10 | 3 |
| FIN | FinOps Analyst | FinOps Framework domains: Inform, Optimize, Govern, Plan & Forecast | 17 | 6 |
| EM | Engineering Manager | DORA metrics & capabilities; capacity; people-equivalent = agent performance | 23 | 11 |
| SM | Scrum Master / Delivery Lead | Flow, WIP, retros; feature-loop-dispatch stall watchdog | 14 | 4 |
| PGM | Portfolio / Program Manager | Multi-repo dependencies, RAID, QBR | 12 | 5 |
| DX | Developer Experience Engineer | Inner loop, hooks, skills, permission prompts | 22 | 8 |
| A11Y | Accessibility Specialist | WCAG 2.2 AA, VPAT/ACR | 8 | 2 |
| L10N | Localization Engineer | i18n extraction, translation, locale formats | 6 | 2 |
| LEGAL | Legal / Privacy / Compliance | GDPR/DSR, DPIA, licenses, SOC 2 evidence, EU AI Act register | 21 | 5 |
| MKT | Marketing / Release Comms | Announcements, changelog, enablement | 11 | 4 |
| AIOPS | AI-Org Operator (new role) | No human analogue: runs the agent fleet itself | 26 | 7 |

**Reading the counts.** The brief asked for 30-100 tasks per role. Tagged counts range from 5 (DE) to 42 (QA). QA, SRE, SEC, PM and BE land directly in the 30-100 range. The niche roles (DE, L10N, A11Y, CS, UXR, UXD, MKT) tag only 5-12 dedicated agents. That is on purpose, and it is the main point of the consolidation. In a human organization, roughly half of a niche specialist's working week goes to *generic* tasks: attending reviews, writing status, triaging inbound requests, filing tickets, updating docs and answering questions. In this design those tasks are served by shared agents (`code-review-orchestrator`, `stakeholder-status-reporter`, `work-item-classifier`, `question-answerer`, `doc-drift-checker`, `backlog-generator-from-signals`) that apply the specialist's rules as *lenses*. They are not duplicated per role. Adding those roughly 15-20 shared generic tasks to each role's tagged count brings most roles near or into the 30+ range. The niche roles still fall short, because the parts of their jobs that are really unique are small.

## 2. Role → task decomposition

Each role lists the agents that carry out its work. Agents shared with other roles are how the dedupe happens.

### PM: Product Manager (32)
**G01** `owner-request-intake`, `feature-request-analyzer`; **G02** `product-vision-keeper`, `okr-drafter`, `okr-progress-tracker`, `roadmap-planner`, `opportunity-scorer`, `competitive-scanner`, `market-signal-miner`, `problem-statement-writer`, `hypothesis-framer`, `experiment-designer`, `experiment-readout`, `product-requirements-writer`, `persona-maintainer`, `customer-interview-synthesizer`, `pricing-packaging-analyst`, `business-case-writer`, `feature-adoption-reviewer`, `sunset-planner`; **G05** `nfr-specifier`; **G09** `perf-budget-keeper`; **G12** `release-planner`, `feature-flag-manager`; **G14** `slo-definer`; **G18** `feature-feedback-router`; **G21** `unit-cost-calculator`; **G22** `deprecation-notice-writer`, `demo-script-writer`; **G25** `backlog-generator-from-signals`, `goal-drift-detector`, `idea-incubator`

### PO: Product Owner (22)
**G01** `owner-request-intake`, `request-clarifier`, `work-item-classifier`, `duplicate-detector`, `stale-issue-gardener`, `feature-request-analyzer`, `epic-decomposer`; **G02** `opportunity-scorer`, `problem-statement-writer`, `product-requirements-writer`; **G03** `acceptance-criteria-writer`, `definition-of-ready-checker`, `definition-of-done-checker`, `backlog-prioritizer`, `backlog-groomer`, `estimator`, `story-splitter`, `release-scope-tracker`, `item-closure-verifier`; **G04** `ux-acceptance-gate`; **G12** `prod-deploy-gate`; **G25** `agent-proposal-gate`

### UXR: UX Researcher (11)
**G02** `market-signal-miner`, `hypothesis-framer`, `persona-maintainer`, `customer-interview-synthesizer`; **G04** `ux-test-plan-writer`, `usability-heuristic-evaluator`, `user-journey-analytics-reviewer`, `survey-designer`, `survey-analyzer`, `research-repository-keeper`; **G16** `funnel-analyzer`

### UXD: UX/UI Designer (11)
**G04** `ux-flow-designer`, `ui-component-designer`, `design-system-curator`, `ui-consistency-auditor`, `ux-copy-writer`, `prototype-builder`, `ux-walkthrough-recorder`, `usability-heuristic-evaluator`, `visual-regression-reviewer`, `ux-acceptance-gate`; **G19** `color-contrast-checker`

### TW: Technical Writer (23)
**G01** `question-answerer`; **G04** `ux-copy-writer`; **G05** `architecture-diagram-updater`; **G07** `pr-description-writer`; **G12** `changelog-generator`; **G14** `runbook-author`; **G17** `release-notes-writer`, `api-reference-generator`, `user-guide-writer`, `doc-gap-detector`, `doc-drift-checker`, `code-comment-documenter`, `link-checker`, `readme-maintainer`, `runbook-librarian`, `glossary-maintainer`, `tutorial-writer`, `diagram-renderer`, `docs-site-publisher`, `onboarding-guide-maintainer`, `kb-article-writer`; **G18** `faq-updater`; **G22** `changelog-page-publisher`

### ARCH: Software Architect (26)
**G00** `conflict-arbiter`; **G01** `epic-decomposer`; **G05** `technical-design-author`, `adr-writer`, `architecture-rule-enforcer`, `api-contract-designer`, `api-breaking-change-detector`, `data-model-designer`, `threat-modeler`, `nfr-specifier`, `integration-designer`, `scalability-reviewer`, `design-review-board`, `spike-runner`, `architecture-diagram-updater`, `codebase-cartographer`, `tech-debt-registrar`, `fitness-function-runner`, `refactoring-planner`, `tech-radar-curator`; **G07** `complexity-trend-watcher`; **G11** `dependency-upgrade-planner`, `new-dependency-gate`; **G14** `resilience-improvement-planner`; **G17** `diagram-renderer`; **G21** `cost-impact-reviewer`

### BE: Backend Developer (32)
**G05** `technical-design-author`, `api-contract-designer`, `integration-designer`, `spike-runner`; **G06** `feature-implementer`, `backend-implementer`, `fix-implementer`, `api-endpoint-implementer`, `migration-author`, `refactoring-implementer`, `mechanical-cleanup-fixer`, `review-feedback-applier`, `branch-updater`, `merge-conflict-resolver`, `ci-failure-fixer`, `feature-flag-wirer`, `config-change-implementer`, `dead-code-remover`, `sdk-client-generator`, `deprecation-implementer`, `llm-feature-implementer`; **G08** `unit-test-author`, `integration-test-author`, `contract-test-author`; **G09** `benchmark-runner`; **G10** `vuln-fixer`; **G12** `stale-flag-remover`; **G14** `error-tracker-triager`, `observability-gap-finder`; **G15** `ef-query-reviewer`; **G17** `code-comment-documenter`; **G24** `scaffolding-generator`

### FE: Frontend Developer (12)
**G04** `ui-component-designer`, `design-system-curator`, `prototype-builder`; **G06** `feature-implementer`, `frontend-implementer`, `fix-implementer`, `review-feedback-applier`, `feature-flag-wirer`; **G08** `ui-component-test-author`, `cross-browser-runner`; **G09** `frontend-perf-auditor`, `bundle-size-watcher`

### DE: Data Engineer (5)
**G06** `data-pipeline-implementer`; **G15** `data-migration-runner`, `data-quality-checker`, `data-pipeline-monitor`, `analytics-schema-maintainer`

### DBA: Database Administrator (19)
**G05** `data-model-designer`; **G06** `migration-author`; **G08** `test-data-generator`; **G09** `query-performance-analyzer`; **G12** `migration-deploy-runner`; **G13** `backup-verifier`; **G15** `migration-reviewer`, `migration-numbering-guard`, `index-advisor`, `ef-query-reviewer`, `schema-drift-detector`, `db-capacity-watcher`, `db-maintenance-runner`, `deadlock-analyzer`, `data-retention-enforcer`, `pii-data-classifier`, `db-restore-to-lower-env`, `pii-data-masker`, `data-migration-runner`

### CR: Code Reviewer (20)
**G05** `architecture-rule-enforcer`; **G07** `code-review-orchestrator`, `correctness-reviewer`, `style-conventions-reviewer`, `test-adequacy-reviewer`, `simplification-reviewer`, `pr-description-writer`, `pr-size-guard`, `change-risk-scorer`, `protected-path-guard`, `human-review-requester`, `bot-finding-triager`, `static-analysis-triager`, `crap-score-gate`, `merge-readiness-checker`, `code-ownership-mapper`, `duplication-detector`, `complexity-trend-watcher`; **G10** `security-review-agent`; **G25** `revert-analyzer`

### QA: QA / Test Automation Engineer (42)
**G00** `agent-output-evaluator`, `agent-eval-regression-runner`; **G01** `bug-reproducer`, `bug-severity-assessor`; **G03** `acceptance-criteria-writer`, `definition-of-done-checker`; **G04** `ux-test-plan-writer`, `ux-walkthrough-recorder`, `visual-regression-reviewer`; **G07** `test-adequacy-reviewer`, `crap-score-gate`; **G08** `test-design-author`, `unit-test-author`, `integration-test-author`, `acceptance-test-author`, `ui-component-test-author`, `contract-test-author`, `test-data-generator`, `functional-test-runner`, `exploratory-tester`, `smoke-test-runner`, `cross-browser-runner`, `bug-verification-agent`, `flaky-test-detector`, `flaky-test-quarantiner`, `flaky-test-fixer`, `regression-suite-curator`, `coverage-gap-analyzer`, `mutation-tester`, `llm-eval-runner`, `test-env-provisioner`, `test-report-publisher`, `chaos-experiment-runner`; **G10** `security-test-author`; **G12** `release-verifier`; **G14** `synthetic-monitor-author`; **G16** `instrumentation-verifier`; **G19** `keyboard-nav-tester`, `locale-format-tester`; **G25** `revert-analyzer`, `escaped-defect-analyzer`, `quality-trend-sentinel`

### PERF: Performance Engineer (16)
**G05** `nfr-specifier`, `scalability-reviewer`; **G09** `load-test-author`, `load-test-runner`, `perf-regression-analyzer`, `benchmark-runner`, `profiler-agent`, `query-performance-analyzer`, `frontend-perf-auditor`, `bundle-size-watcher`, `cold-start-analyzer`, `stress-breakpoint-tester`, `soak-tester`, `rate-limit-validator`, `capacity-planner`, `perf-budget-keeper`

### SEC: AppSec / Security Engineer (34)
**G00** `agent-permission-auditor`; **G01** `security-report-intake`; **G05** `threat-modeler`, `design-review-board`; **G09** `rate-limit-validator`; **G10** `sast-scanner`, `secret-scanner`, `secret-leak-responder`, `secret-rotation-runner`, `vuln-triager`, `vuln-fixer`, `security-review-agent`, `dast-scanner`, `security-requirements-writer`, `security-test-author`, `authz-matrix-auditor`, `container-image-scanner`, `iac-security-scanner`, `supply-chain-attestation`, `waf-rule-tuner`, `access-review-runner`, `security-posture-reporter`, `pen-test-coordinator`, `security-advisory-publisher`, `security-guidance-writer`; **G11** `sca-scanner`, `sbom-generator`, `typosquat-malware-checker`, `action-pinning-auditor`; **G13** `os-patch-manager`, `network-exposure-auditor`, `secrets-store-maintainer`; **G20** `compliance-evidence-collector`, `control-gap-assessor`

### DEP: Dependency & License Compliance (14)
**G05** `tech-radar-curator`; **G11** `dependency-update-proposer`, `dependency-upgrade-planner`, `sca-scanner`, `new-dependency-gate`, `license-compliance-checker`, `sbom-generator`, `typosquat-malware-checker`, `eol-runtime-watcher`, `base-image-updater`, `abandoned-package-detector`, `lockfile-drift-auditor`, `third-party-notice-writer`, `action-pinning-auditor`

### DEVOPS: DevOps / Build Engineer (15)
**G00** `repo-onboarder`; **G06** `ci-failure-fixer`, `config-change-implementer`; **G07** `protected-path-guard`; **G08** `test-env-provisioner`; **G10** `secret-rotation-runner`, `container-image-scanner`; **G11** `base-image-updater`; **G12** `ci-pipeline-watcher`, `ci-infra-fixer`, `ci-duration-optimizer`, `pipeline-as-code-maintainer`, `build-reproducibility-checker`, `deployment-orchestrator`; **G13** `runner-fleet-manager`

### REL: Release Manager (26)
**G01** `hotfix-intake`; **G03** `release-scope-tracker`; **G05** `api-breaking-change-detector`; **G07** `change-risk-scorer`, `merge-readiness-checker`, `pr-merger`; **G10** `supply-chain-attestation`; **G12** `artifact-publisher`, `version-bumper`, `changelog-generator`, `release-planner`, `release-candidate-cutter`, `deployment-orchestrator`, `migration-deploy-runner`, `prod-deploy-gate`, `progressive-rollout-controller`, `rollback-executor`, `release-verifier`, `hotfix-release-runner`, `feature-flag-manager`, `stale-flag-remover`, `deploy-freeze-enforcer`, `environment-promotion-tracker`, `docs-only-fast-path`, `dora-metrics-collector`; **G20** `change-approval-recorder`

### PLAT: Platform / Cloud Engineer (21)
**G09** `cold-start-analyzer`, `capacity-planner`; **G10** `iac-security-scanner`; **G13** `infra-change-planner`, `iac-implementer`, `drift-detector`, `environment-provisioner`, `ephemeral-env-reaper`, `certificate-expiry-watcher`, `dns-domain-manager`, `backup-verifier`, `dr-drill-runner`, `container-runtime-tuner`, `os-patch-manager`, `quota-limit-watcher`, `network-exposure-auditor`, `runner-fleet-manager`, `secrets-store-maintainer`, `golden-path-maintainer`; **G21** `rightsizing-advisor`, `tagging-compliance-enforcer`

### SRE: SRE / On-call / Incident Mgmt (37)
**G00** `kill-switch-guardian`; **G01** `bug-severity-assessor`, `hotfix-intake`; **G08** `smoke-test-runner`, `chaos-experiment-runner`; **G09** `capacity-planner`; **G10** `waf-rule-tuner`; **G12** `progressive-rollout-controller`, `rollback-executor`; **G13** `certificate-expiry-watcher`, `dr-drill-runner`; **G14** `slo-definer`, `alert-rule-author`, `health-check-monitor`, `dependency-health-watcher`, `log-anomaly-detector`, `error-tracker-triager`, `incident-declarer`, `incident-commander`, `incident-diagnostician`, `runbook-executor`, `on-call-pager`, `status-page-updater`, `postmortem-writer`, `action-item-filer`, `error-budget-tracker`, `toil-tracker`, `alert-noise-reducer`, `observability-gap-finder`, `dashboard-maintainer`, `runbook-author`, `synthetic-monitor-author`, `resilience-improvement-planner`; **G16** `telemetry-cost-optimizer`; **G17** `runbook-librarian`; **G18** `customer-log-investigator`; **G25** `recurring-failure-miner`

### ANA: Product/Data Analyst (19)
**G02** `experiment-designer`, `experiment-readout`, `feature-adoption-reviewer`; **G04** `user-journey-analytics-reviewer`, `survey-analyzer`; **G14** `dashboard-maintainer`; **G15** `analytics-schema-maintainer`; **G16** `tracking-plan-author`, `instrumentation-verifier`, `kpi-dashboard-builder`, `metric-definition-keeper`, `weekly-metrics-digest`, `metric-anomaly-detector`, `funnel-analyzer`, `cohort-retention-analyzer`, `ad-hoc-question-answerer`, `customer-usage-reporter`, `telemetry-cost-optimizer`; **G18** `churn-risk-detector`

### SUP: Support Engineer (18)
**G01** `work-item-classifier`, `duplicate-detector`, `bug-reproducer`, `needs-info-follower`, `intake-channel-bridge`, `question-answerer`; **G14** `status-page-updater`; **G17** `kb-article-writer`; **G18** `support-ticket-triager`, `support-ticket-resolver`, `customer-log-investigator`, `support-sla-watcher`, `bug-report-quality-coach`, `customer-incident-notifier`, `close-the-loop-notifier`, `support-macro-maintainer`, `faq-updater`; **G20** `data-subject-request-handler`

### CS: Customer Success (10)
**G01** `intake-channel-bridge`; **G16** `cohort-retention-analyzer`, `customer-usage-reporter`; **G18** `customer-incident-notifier`, `close-the-loop-notifier`, `sentiment-analyzer`, `churn-risk-detector`, `customer-health-reporter`, `feature-feedback-router`, `customer-onboarding-assistant`

### FIN: FinOps Analyst (17)
**G00** `agent-budget-governor`, `model-router`; **G02** `pricing-packaging-analyst`; **G13** `ephemeral-env-reaper`; **G16** `telemetry-cost-optimizer`; **G21** `cloud-cost-reporter`, `cost-anomaly-detector`, `rightsizing-advisor`, `idle-resource-reaper`, `tagging-compliance-enforcer`, `cost-impact-reviewer`, `budget-forecaster`, `unit-cost-calculator`, `finops-ai-spend-reporter`, `commitment-planner`, `saas-license-auditor`; **G23** `ai-worker-mix-manager`

### EM: Engineering Manager (23)
**G00** `lane-dispatcher`, `human-escalation-router`, `human-decision-recorder`; **G02** `okr-drafter`, `okr-progress-tracker`; **G03** `flow-metrics-reporter`, `retrospective-facilitator`; **G05** `tech-debt-registrar`; **G07** `code-ownership-mapper`; **G10** `access-review-runner`, `security-posture-reporter`; **G12** `dora-metrics-collector`; **G22** `stakeholder-status-reporter`, `internal-digest-writer`; **G23** `engineering-health-reporter`, `capacity-allocator`, `quarterly-business-review-writer`, `agent-performance-reviewer`, `human-gate-queue-balancer`, `policy-keeper`, `ai-worker-mix-manager`; **G24** `owner-friction-surveyor`; **G25** `quality-trend-sentinel`

### SM: Scrum Master / Delivery Lead (14)
**G00** `lane-dispatcher`, `lane-stall-watchdog`; **G01** `dependency-graph-resolver`; **G03** `definition-of-ready-checker`, `estimator`, `sprint-planner`, `board-column-mover`, `wip-limit-enforcer`, `flow-metrics-reporter`, `blocked-item-unblocker`, `retrospective-facilitator`, `process-improvement-implementer`, `work-item-linker`; **G25** `rework-detector`

### PGM: Portfolio / Program Manager (12)
**G01** `epic-decomposer`, `dependency-graph-resolver`; **G02** `roadmap-planner`, `business-case-writer`; **G21** `budget-forecaster`; **G22** `stakeholder-status-reporter`; **G23** `capacity-allocator`, `portfolio-balancer`, `cross-repo-dependency-coordinator`, `milestone-risk-assessor`, `raid-log-keeper`, `quarterly-business-review-writer`

### DX: Developer Experience Engineer (22)
**G00** `agent-prompt-tuner`; **G06** `mechanical-cleanup-fixer`, `branch-updater`, `sdk-client-generator`; **G07** `static-analysis-triager`; **G10** `security-guidance-writer`; **G12** `ci-duration-optimizer`; **G13** `golden-path-maintainer`; **G17** `doc-drift-checker`, `readme-maintainer`, `tutorial-writer`, `onboarding-guide-maintainer`; **G24** `dev-environment-validator`, `session-start-hook-maintainer`, `build-warning-reducer`, `skill-library-curator`, `local-build-time-tracker`, `scaffolding-generator`, `inner-loop-optimizer`, `permission-prompt-reducer`, `owner-friction-surveyor`, `api-sandbox-maintainer`

### A11Y: Accessibility Specialist (8)
**G04** `ui-component-designer`, `usability-heuristic-evaluator`; **G19** `a11y-static-checker`, `a11y-runtime-auditor`, `keyboard-nav-tester`, `screen-reader-tester`, `color-contrast-checker`, `a11y-conformance-reporter`

### L10N: Localization Engineer (6)
**G04** `ux-copy-writer`; **G19** `i18n-string-extractor`, `translation-generator`, `translation-reviewer`, `locale-format-tester`, `pseudo-localization-runner`

### LEGAL: Legal / Privacy / Compliance (21)
**G00** `human-decision-recorder`, `audit-trail-keeper`; **G11** `license-compliance-checker`, `third-party-notice-writer`; **G15** `data-retention-enforcer`, `pii-data-classifier`, `pii-data-masker`; **G19** `a11y-conformance-reporter`, `translation-reviewer`; **G20** `privacy-impact-assessor`, `data-subject-request-handler`, `policy-text-watcher`, `legal-review-requester`, `compliance-evidence-collector`, `control-gap-assessor`, `change-approval-recorder`, `cookie-consent-auditor`, `regulatory-change-watcher`, `ai-system-register-keeper`, `export-control-checker`, `contributor-license-checker`

### MKT: Marketing / Release Comms (11)
**G02** `competitive-scanner`; **G10** `security-advisory-publisher`; **G17** `release-notes-writer`; **G22** `release-comms-writer`, `deprecation-notice-writer`, `changelog-page-publisher`, `social-post-drafter`, `demo-script-writer`, `sales-enablement-writer`, `website-content-updater`, `seo-auditor`

### AIOPS: AI-Org Operator (new role) (26)
**G00** `event-router`, `timer-scheduler`, `work-claim-lock`, `lane-stall-watchdog`, `agent-budget-governor`, `model-router`, `agent-output-evaluator`, `agent-eval-regression-runner`, `agent-prompt-tuner`, `agent-registry-keeper`, `agent-permission-auditor`, `kill-switch-guardian`, `audit-trail-keeper`, `conflict-arbiter`, `repo-onboarder`; **G03** `process-improvement-implementer`; **G20** `ai-system-register-keeper`; **G21** `finops-ai-spend-reporter`; **G23** `agent-performance-reviewer`, `policy-keeper`; **G24** `skill-library-curator`; **G25** `signal-aggregator`, `backlog-generator-from-signals`, `recurring-failure-miner`, `agent-friction-miner`, `human-override-learner`

## 3. Guild summary

| Guild | Name | Agents | auto | review | human | Mission |
|---|---|---|---|---|---|---|
| G00 | Orchestration & Control Plane | 18 | 14 | 3 | 1 | Runs the agent ecosystem itself: routing, scheduling, locking, budgets, escalation, evaluation, kill switch. Has no human counterpart; replaces the implicit coordination humans do in hallways. |
| G01 | Intake & Triage | 15 | 9 | 5 | 1 | Turns every inbound signal (owner requests, issues, tickets, reports) into a classified, deduplicated, reproducible work item. |
| G02 | Product Discovery & Strategy | 18 | 6 | 6 | 6 | Decides what is worth building and why: vision, outcomes, opportunity scoring, experiments, adoption review, sunsetting. |
| G03 | Backlog & Delivery Flow | 17 | 11 | 6 | 0 | Product-owner and scrum-master mechanics: readiness, ordering, sizing, WIP, board truth, flow metrics, retros. |
| G04 | UX Research & Design | 15 | 8 | 5 | 2 | Designs and validates the experience: flows, components, copy, heuristics, walkthroughs, research ops. |
| G05 | Architecture & Technical Design | 18 | 7 | 10 | 1 | Owns structure: designs, ADRs, contracts, NFRs, layering rules, fitness functions, debt register. |
| G06 | Construction | 19 | 16 | 3 | 0 | Writes and changes code: features, fixes, migrations, endpoints, refactors, conflict and CI repair. |
| G07 | Code Review & Code Quality | 18 | 15 | 1 | 2 | Multi-lens review, bot-finding triage, risk scoring, protected-path gating and merge. |
| G08 | Test Engineering & QA | 22 | 18 | 4 | 0 | Designs, writes, runs, and curates tests at every level; verifies columns; keeps the suite trustworthy. |
| G09 | Performance & Capacity | 14 | 12 | 1 | 1 | Load, stress, soak, benchmarks, profiling, budgets, capacity forecasts. |
| G10 | Security & AppSec | 20 | 12 | 4 | 4 | OWASP SAMM across govern/design/implement/verify/operate: scanning, triage, fixing, secrets, access, advisories. |
| G11 | Supply Chain, Dependency & License | 13 | 10 | 1 | 2 | Keeps third-party code current, safe, licensed and approved. |
| G12 | CI/CD, Build & Release | 23 | 16 | 5 | 2 | Pipeline health, artifacts, versions, release trains, environment promotion, rollback, DORA. |
| G13 | Platform & Infrastructure | 16 | 8 | 6 | 2 | IaC, environments, drift, certificates, backups, DR, quotas, runners, golden paths. |
| G14 | SRE, Observability & Incident | 22 | 14 | 7 | 1 | SLOs, alerting, detection, incident command, diagnosis, postmortems, toil and resilience. |
| G15 | Data: DBA & Data Engineering | 16 | 13 | 1 | 2 | Schema safety, query health, retention, masking, pipelines, data quality. |
| G16 | Analytics & Insights | 11 | 7 | 4 | 0 | Instrumentation, KPIs, anomaly detection, funnels, cohorts, ad-hoc questions. |
| G17 | Docs & Knowledge | 15 | 9 | 6 | 0 | Keeps human- and agent-facing knowledge accurate: API refs, guides, notes, CLAUDE.md, runbooks, diagrams. |
| G18 | Support & Customer Success | 14 | 11 | 3 | 0 | Tickets, investigation, SLAs, customer comms, health, churn and feedback loops. |
| G19 | Accessibility & Localization | 11 | 7 | 2 | 2 | WCAG conformance and world-readiness as continuous checks, not audits. |
| G20 | Legal, Privacy & Compliance | 12 | 4 | 4 | 4 | Privacy, licensing, regulatory tracking, evidence, change records, AI-use register. |
| G21 | FinOps & Cost | 11 | 8 | 2 | 1 | Inform, optimize, govern cloud and AI spend (FinOps Framework domains). |
| G22 | Communications & Marketing | 10 | 4 | 2 | 4 | Release comms, notices, public changelog, stakeholder reporting. |
| G23 | Engineering Management, Portfolio & Program | 11 | 6 | 3 | 2 | Capacity allocation across repos, risk, portfolio mix, agent performance management. |
| G24 | Developer Experience | 10 | 6 | 4 | 0 | Keeps the inner loop fast for agents and the human owner: environments, hooks, skills, scaffolds, prompts. |
| G25 | Kaizen: Self-Observation & Backlog Generation | 12 | 8 | 2 | 2 | Converts observed signals into evidence-backed backlog items; learns from reverts, escapes, overrides and rework. |
| | **Total** | **401** | **259** | **100** | **42** | |

## 4. Master catalog

### G00: Orchestration & Control Plane (18)

_Runs the agent ecosystem itself: routing, scheduling, locking, budgets, escalation, evaluation, kill switch. Has no human counterpart; replaces the implicit coordination humans do in hallways._

| # | Agent | Roles | Activation | Inputs | Outputs / artifacts | Done when | Aut. | Hands off to |
|---|---|---|---|---|---|---|---|---|
| 00.01 | `event-router` | AIOPS | ⚡`*any-webhook` | raw webhook payload; routing table | dispatched agent invocations with correlation id | every event routed or dead-lettered in <60s | A | agent-registry-keeper |
| 00.02 | `timer-scheduler` | AIOPS | ⏱`1m` | timer calendar; agent registry | fired timer invocations | zero missed fires; drift <1 min | A | agent-registry-keeper |
| 00.03 | `work-claim-lock` | AIOPS | ↪`any-agent` | item id; agent id; TTL | lease on work item/branch | no two agents mutate one item; stale leases reaped | A | lane-stall-watchdog |
| 00.04 | `lane-dispatcher` | SM, EM | ⚡`projects_v2_item.edited[status change]`<br>↪`owner-request-intake`<br>↪`backlog-prioritizer` | ready items; WIP limits; agent capacity | one sub-session per item (feature-loop lane) | item owned by exactly one lane | A | work-claim-lock, lane-stall-watchdog |
| 00.05 | `lane-stall-watchdog` | SM, AIOPS | ⏱`15m` | lane heartbeats; session status | nudge / restart / escalation | no lane silent >N min (Check-StalledLanes) | A | lane-dispatcher, human-escalation-router |
| 00.06 | `agent-budget-governor` | FIN, AIOPS | ⏱`hourly`<br>⚡`agent.token_spend` | spend per agent; budgets | throttle/pause decisions | spend within budget; overruns paused | A | finops-ai-spend-reporter, human-escalation-router |
| 00.07 | `model-router` | AIOPS, FIN | ↪`any-agent` | task class; cost/quality table | model + effort selection | cost-per-successful-outcome tracked | A | agent-budget-governor |
| 00.08 | `human-escalation-router` | EM | ↪`any-agent` | escalation packet; owner prefs; quiet hours | notification with explicit decision request | human ack, or re-escalate on SLA breach | A | human-decision-recorder |
| 00.09 | `human-decision-recorder` | EM, LEGAL | ⚡`issue_comment.created[approval keyword]`<br>⚡`pull_request_review.submitted[human]` | human reply | decision record linked to item | every decision attributable & linked | A | audit-trail-keeper, human-override-learner |
| 00.10 | `agent-output-evaluator` | AIOPS, QA | ⚡`agent.completed` | agent output; rubric; done-criteria | quality score; pass/fail | every agent run scored | A | agent-prompt-tuner |
| 00.11 | `agent-eval-regression-runner` | AIOPS, QA | ⚡`pull_request.opened[skills/prompts]`<br>⏱`weekly` | golden task replay set | eval report | no regression >5% on replay set | A | agent-prompt-tuner |
| 00.12 | `agent-prompt-tuner` | AIOPS, DX | ⏱`weekly`<br>↪`agent-output-evaluator` | failing runs; skill/prompt files | PR to skills/prompts | replay score improves; PR reviewed | R | code-review-orchestrator |
| 00.13 | `agent-registry-keeper` | AIOPS | ⚡`push:main[.claude/skills or manifests]` | skill files; agent manifests | registry; trigger matrix; timer calendar | registry == deployed agents | A | event-router, timer-scheduler |
| 00.14 | `agent-permission-auditor` | SEC, AIOPS | ⏱`weekly`<br>⚡`agent.registry.changed` | agent scopes; tokens | least-privilege report; revocation PR | no agent holds an unused scope | R | secrets-store-maintainer |
| 00.15 | `kill-switch-guardian` | SRE, AIOPS | ⚡`alert.fired[agent-runaway]`<br>↪`owner` | loop/mass-edit/spend anomalies | pause of agent or guild | runaway halted <2 min; owner told | A | human-escalation-router |
| 00.16 | `audit-trail-keeper` | LEGAL, AIOPS | ⚡`agent.completed` | all agent actions | append-only action log | every mutation attributable | A | compliance-evidence-collector |
| 00.17 | `conflict-arbiter` | ARCH, AIOPS | ↪`any-agent[conflicting outputs]` | contradicting recommendations | decision + rationale or escalation | conflict closed | R | human-escalation-router |
| 00.18 | `repo-onboarder` | DEVOPS, AIOPS | ↪`owner[point at repo]` | repo URL | repo profile (stack, build/test cmds, CI, board ids) as factory config | private build reproduced; config PR merged | H | codebase-cartographer, agent-registry-keeper, dev-environment-validator |

### G01: Intake & Triage (15)

_Turns every inbound signal (owner requests, issues, tickets, reports) into a classified, deduplicated, reproducible work item._

| # | Agent | Roles | Activation | Inputs | Outputs / artifacts | Done when | Aut. | Hands off to |
|---|---|---|---|---|---|---|---|---|
| 01.01 | `owner-request-intake` | PO, PM | ⚡`issues.opened[author=owner]`<br>⚡`chat.message[owner]` | natural-language change request | structured issue: type, goal, constraints | issue on board in Conceptual Definition | A | request-clarifier, work-item-classifier, problem-statement-writer |
| 01.02 | `request-clarifier` | PO | ↪`owner-request-intake` | ambiguous request | clarifying questions or explicit assumptions | ambiguities resolved or assumptions stated | R | acceptance-criteria-writer |
| 01.03 | `work-item-classifier` | PO, SUP | ⚡`issues.opened` | issue text | labels: type, area, severity; issue type | labeled <5 min | A | duplicate-detector |
| 01.04 | `duplicate-detector` | PO, SUP | ⚡`issues.opened` | new issue; corpus | duplicate link / close | dupes linked; auto-close at >=0.9 confidence | R | work-item-classifier |
| 01.05 | `bug-reproducer` | QA, SUP | ⚡`issues.labeled[bug]` | bug report; environment | failing test or repro steps, or needs-info | repro test on branch, or needs-info label | A | bug-severity-assessor, fix-implementer |
| 01.06 | `bug-severity-assessor` | QA, SRE | ↪`bug-reproducer` | repro; usage data | severity & priority with rationale | severity set | A | backlog-prioritizer |
| 01.07 | `needs-info-follower` | SUP | ⏱`daily` | issues labeled needs-info | reminders; stale close | closed after N days without reply | A | — |
| 01.08 | `stale-issue-gardener` | PO | ⏱`weekly` | open issue ages | refresh / close proposals | no issue >90d untouched | R | backlog-groomer |
| 01.09 | `feature-request-analyzer` | PM, PO | ⚡`issues.labeled[enhancement]` | request; vision | fit score; recommendation | recommendation comment posted | R | opportunity-scorer |
| 01.10 | `intake-channel-bridge` | SUP, CS | ⚡`support.ticket.created`<br>⚡`feedback.received` | external ticket/feedback | linked GitHub issue | every actionable ticket mirrored | A | work-item-classifier |
| 01.11 | `epic-decomposer` | PO, ARCH, PGM | ⚡`issues.labeled[epic]` | epic | child issues with dependencies | children created; clamp rules applied | R | dependency-graph-resolver |
| 01.12 | `dependency-graph-resolver` | SM, PGM | ↪`epic-decomposer`<br>⚡`sub_issues.changed` | issue tree | children-first execution order | acyclic order computed | A | lane-dispatcher |
| 01.13 | `question-answerer` | SUP, TW | ⚡`issues.labeled[question]`<br>⚡`discussion.created` | question; docs; code | answer with citations | answered or escalated | A | doc-gap-detector |
| 01.14 | `security-report-intake` | SEC | ⚡`repository_advisory.reported`<br>⚡`email[security@]` | vulnerability report | private advisory; triage | acknowledged <24h | H | vuln-triager |
| 01.15 | `hotfix-intake` | REL, SRE | ⚡`issues.labeled[hotfix]`<br>↪`incident-commander` | incident; needed fix | expedited item that skips the queue | item in Development within minutes | A | fix-implementer, hotfix-release-runner |

### G02: Product Discovery & Strategy (18)

_Decides what is worth building and why: vision, outcomes, opportunity scoring, experiments, adoption review, sunsetting._

| # | Agent | Roles | Activation | Inputs | Outputs / artifacts | Done when | Aut. | Hands off to |
|---|---|---|---|---|---|---|---|---|
| 02.01 | `product-vision-keeper` | PM | ⏱`quarterly`<br>↪`owner` | vision doc; outcome data | updated vision & strategy | owner-approved | H | roadmap-planner |
| 02.02 | `okr-drafter` | PM, EM | ⏱`quarterly` | vision; metrics | draft OKRs | owner-approved | H | okr-progress-tracker, kpi-dashboard-builder |
| 02.03 | `okr-progress-tracker` | PM, EM | ⏱`weekly` | OKRs; metrics | progress report | posted weekly | A | stakeholder-status-reporter |
| 02.04 | `roadmap-planner` | PM, PGM | ⏱`monthly`<br>⚡`okr.changed` | backlog; OKRs; capacity | now/next/later roadmap | owner-approved | H | release-planner |
| 02.05 | `opportunity-scorer` | PM, PO | ↪`feature-request-analyzer`<br>⏱`weekly` | candidate items; RICE/WSJF inputs | scores | every candidate scored | A | backlog-prioritizer |
| 02.06 | `competitive-scanner` | PM, MKT | ⏱`monthly` | competitor sites & changelogs | competitive digest | digest published | A | opportunity-scorer |
| 02.07 | `market-signal-miner` | PM, UXR | ⏱`weekly` | reviews; forums; social | problem themes with evidence | themes published | A | opportunity-scorer |
| 02.08 | `problem-statement-writer` | PM, PO | ↪`owner-request-intake` | request | problem statement; success metric | measurable outcome defined | R | product-requirements-writer, hypothesis-framer |
| 02.09 | `hypothesis-framer` | PM, UXR | ↪`problem-statement-writer` | problem | testable hypothesis | metric + threshold defined | R | experiment-designer |
| 02.10 | `experiment-designer` | PM, ANA | ↪`hypothesis-framer` | hypothesis | A/B design; flag; sample size | power >=0.8 | R | feature-flag-manager |
| 02.11 | `experiment-readout` | ANA, PM | ⚡`experiment.ended`<br>⏱`daily` | experiment data | readout; ship/kill recommendation | significance computed | R | product-vision-keeper |
| 02.12 | `product-requirements-writer` | PM, PO | ↪`problem-statement-writer` | problem; research | PRD | PRD reviewed | R | technical-design-author, ux-flow-designer, nfr-specifier, tracking-plan-author |
| 02.13 | `persona-maintainer` | UXR, PM | ⏱`quarterly` | research; analytics | persona docs | updated and cited | R | — |
| 02.14 | `customer-interview-synthesizer` | UXR, PM | ⚡`research.transcript.uploaded` | transcripts | insights; jobs-to-be-done | insights tagged | A | persona-maintainer, research-repository-keeper |
| 02.15 | `pricing-packaging-analyst` | PM, FIN | ⏱`quarterly` | usage; unit costs | pricing recommendation | owner decision | H | — |
| 02.16 | `business-case-writer` | PM, PGM | ↪`roadmap-planner` | initiative | cost/benefit; ROI | owner approved/rejected | H | portfolio-balancer |
| 02.17 | `feature-adoption-reviewer` | PM, ANA | ⏱`weekly`<br>⚡`release.published[+30d]` | usage telemetry | keep/iterate/remove per feature | report per shipped feature | A | backlog-generator-from-signals, sunset-planner, tutorial-writer |
| 02.18 | `sunset-planner` | PM | ↪`feature-adoption-reviewer` | low-adoption feature | deprecation plan | owner-approved | H | deprecation-notice-writer, deprecation-implementer |

### G03: Backlog & Delivery Flow (17)

_Product-owner and scrum-master mechanics: readiness, ordering, sizing, WIP, board truth, flow metrics, retros._

| # | Agent | Roles | Activation | Inputs | Outputs / artifacts | Done when | Aut. | Hands off to |
|---|---|---|---|---|---|---|---|---|
| 03.01 | `acceptance-criteria-writer` | PO, QA | ⚡`projects_v2_item.edited[status=Conceptual Definition]` | issue; PRD | Given/When/Then criteria | criteria testable; reviewed | R | test-design-author |
| 03.02 | `definition-of-ready-checker` | PO, SM | ⚡`projects_v2_item.edited[status change]` | item | ready / gaps list | DoR passes before Development | A | lane-dispatcher |
| 03.03 | `definition-of-done-checker` | PO, QA | ⚡`pull_request.closed[merged]` | item; PR; CI | DoD verdict | all DoD checks true before Done | A | board-column-mover |
| 03.04 | `backlog-prioritizer` | PO | ⏱`daily`<br>⚡`issues.labeled[priority inputs]` | scores; severity; OKRs | ordered backlog | top-N ordered with rationale | R | lane-dispatcher |
| 03.05 | `backlog-groomer` | PO | ⏱`weekly` | backlog | merged/split/closed items | backlog size & age within bounds | R | estimator |
| 03.06 | `estimator` | PO, SM | ↪`backlog-groomer`<br>⚡`projects_v2_item.edited[status=Technical Design]` | item; history | size + confidence | estimate recorded | A | story-splitter |
| 03.07 | `story-splitter` | PO | ↪`estimator[size>L]`<br>↪`pr-size-guard` | large item | vertical slices | each slice <=M | R | dependency-graph-resolver |
| 03.08 | `sprint-planner` | SM | ⏱`biweekly` | backlog; capacity | iteration goal & scope | owner acknowledged | R | lane-dispatcher |
| 03.09 | `board-column-mover` | SM | ↪`any-agent[column done]`<br>⚡`pull_request.closed[merged]` | item state; completion callback | status field update | board mirrors reality | A | — |
| 03.10 | `wip-limit-enforcer` | SM | ⚡`projects_v2_item.edited[status change]` | column counts | allow/block | WIP never exceeded | A | lane-dispatcher |
| 03.11 | `flow-metrics-reporter` | SM, EM | ⏱`daily` | board history | cycle time; throughput; aging WIP | dashboard updated | A | retrospective-facilitator |
| 03.12 | `blocked-item-unblocker` | SM | ⏱`hourly`<br>⚡`issues.labeled[blocked]` | blocked items | unblock action or escalation | no silent block >24h | A | human-escalation-router |
| 03.13 | `retrospective-facilitator` | SM, EM | ⏱`biweekly` | flow metrics; incidents; agent evals | retro doc; action items as issues | actions filed | A | process-improvement-implementer |
| 03.14 | `process-improvement-implementer` | SM, AIOPS | ↪`retrospective-facilitator` | action item | skill/config/process PR | merged; metric tracked | R | agent-prompt-tuner |
| 03.15 | `release-scope-tracker` | PO, REL | ⏱`daily` | release milestone | burn-up; scope risk | risk flagged early | A | stakeholder-status-reporter |
| 03.16 | `item-closure-verifier` | PO | ⚡`issues.closed` | issue; PR evidence | reopen if unmet | every closed item has merged evidence | A | — |
| 03.17 | `work-item-linker` | SM | ⚡`pull_request.opened` | PR; issues | PR-issue links; closing keywords | every PR linked | A | — |

### G04: UX Research & Design (15)

_Designs and validates the experience: flows, components, copy, heuristics, walkthroughs, research ops._

| # | Agent | Roles | Activation | Inputs | Outputs / artifacts | Done when | Aut. | Hands off to |
|---|---|---|---|---|---|---|---|---|
| 04.01 | `ux-flow-designer` | UXD | ⚡`projects_v2_item.edited[status=UX Design]` | PRD; acceptance criteria | user flows; wireframes (Mermaid/HTML) | flows cover all criteria | R | ui-component-designer, ux-copy-writer, prototype-builder |
| 04.02 | `ui-component-designer` | UXD, FE, A11Y | ↪`ux-flow-designer` | wireframes; design system | component specs | specs use design tokens | R | frontend-implementer |
| 04.03 | `design-system-curator` | UXD, FE | ⏱`monthly`<br>⚡`push:main[UI paths]` | components | token/component inventory; drift report | drift issues filed | A | ui-consistency-auditor |
| 04.04 | `ui-consistency-auditor` | UXD | ⚡`pull_request.opened[UI paths]` | screenshots; tokens | inconsistency comments | none unresolved | A | code-review-orchestrator |
| 04.05 | `ux-copy-writer` | UXD, TW, L10N | ↪`ux-flow-designer` | flow | microcopy; error messages | style-guide conformant | R | i18n-string-extractor |
| 04.06 | `prototype-builder` | UXD, FE | ↪`ux-flow-designer` | flow | clickable HTML prototype | hosted link | A | usability-heuristic-evaluator |
| 04.07 | `ux-test-plan-writer` | UXR, QA | ⚡`projects_v2_item.edited[status=Test Design]` | acceptance criteria | UX test script | critical tasks covered | R | ux-walkthrough-recorder |
| 04.08 | `ux-walkthrough-recorder` | UXD, QA | ⚡`deployment_status.success[uat]` | UAT URL; flows | screenshot/video walkthrough | walkthrough attached per feature | A | usability-heuristic-evaluator |
| 04.09 | `usability-heuristic-evaluator` | UXR, UXD, A11Y | ⚡`projects_v2_item.edited[status=UX Testing]` | UAT build; walkthrough | heuristic report | findings filed | A | ux-acceptance-gate |
| 04.10 | `visual-regression-reviewer` | UXD, QA | ⚡`pull_request.synchronize[UI paths]` | screenshot baselines | diff report | diffs approved or fixed | R | — |
| 04.11 | `ux-acceptance-gate` | UXD, PO | ↪`usability-heuristic-evaluator` | walkthrough; heuristics | pass/fail for Release Queue | verdict recorded | H | board-column-mover |
| 04.12 | `user-journey-analytics-reviewer` | UXR, ANA | ⏱`weekly` | funnels; session data | drop-off insights | insights filed | A | backlog-generator-from-signals |
| 04.13 | `survey-designer` | UXR | ↪`hypothesis-framer` | research question | survey | owner-approved before sending | H | survey-analyzer |
| 04.14 | `survey-analyzer` | UXR, ANA | ⚡`survey.closed` | responses | findings report | report published | A | persona-maintainer |
| 04.15 | `research-repository-keeper` | UXR | ⚡`research.artifact.added` | insights | tagged, deduped research repo | searchable | A | — |

### G05: Architecture & Technical Design (18)

_Owns structure: designs, ADRs, contracts, NFRs, layering rules, fitness functions, debt register._

| # | Agent | Roles | Activation | Inputs | Outputs / artifacts | Done when | Aut. | Hands off to |
|---|---|---|---|---|---|---|---|---|
| 05.01 | `technical-design-author` | ARCH, BE | ⚡`projects_v2_item.edited[status=Technical Design]` | PRD; criteria; codebase map | design doc: components, data, API, risks | reviewed | R | test-design-author, api-contract-designer, data-model-designer, threat-modeler |
| 05.02 | `adr-writer` | ARCH | ↪`technical-design-author`<br>↪`human-decision-recorder` | decision | ADR | ADR merged | R | — |
| 05.03 | `architecture-rule-enforcer` | ARCH, CR | ⚡`pull_request.opened`<br>⚡`pull_request.synchronize` | diff; layering rules (onion) | violations | zero violations | A | code-review-orchestrator |
| 05.04 | `api-contract-designer` | ARCH, BE | ↪`technical-design-author` | resource model | OpenAPI change | spec lint clean | R | api-breaking-change-detector, api-endpoint-implementer |
| 05.05 | `api-breaking-change-detector` | ARCH, REL | ⚡`pull_request.synchronize[API paths]` | old/new OpenAPI | break report; version bump need | no unversioned break | A | release-notes-writer |
| 05.06 | `data-model-designer` | ARCH, DBA | ↪`technical-design-author` | domain model | schema change design | migration plan exists | R | migration-author |
| 05.07 | `threat-modeler` | SEC, ARCH | ↪`technical-design-author` | design | STRIDE threat model | every threat mitigated or accepted | R | security-requirements-writer |
| 05.08 | `nfr-specifier` | ARCH, PM, PERF | ↪`product-requirements-writer` | PRD | measurable NFRs | latency/availability/security targets stated | R | slo-definer |
| 05.09 | `integration-designer` | ARCH, BE | ↪`technical-design-author` | external systems | contract; retries; idempotency | contract reviewed | R | contract-test-author |
| 05.10 | `scalability-reviewer` | ARCH, PERF | ↪`technical-design-author` | design; load forecast | capacity risks | mitigated or accepted | R | capacity-planner |
| 05.11 | `design-review-board` | ARCH, SEC | ↪`technical-design-author[high risk]` | design | multi-agent critique | objections resolved or escalated | R | human-escalation-router |
| 05.12 | `spike-runner` | ARCH, BE | ↪`technical-design-author` | open question | time-boxed prototype; findings | answer documented | A | adr-writer |
| 05.13 | `architecture-diagram-updater` | ARCH, TW | ⚡`pull_request.closed[merged, structural]` | code | C4/PlantUML/Mermaid diagrams | diagrams match code | A | diagram-renderer |
| 05.14 | `codebase-cartographer` | ARCH | ⏱`monthly`<br>↪`repo-onboarder` | repo | inventory; metrics; C4 views | report published | A | tech-debt-registrar |
| 05.15 | `tech-debt-registrar` | ARCH, EM | ⏱`weekly` | smells; CRAP; hotspots | ranked debt register | register updated | A | backlog-prioritizer, refactoring-planner |
| 05.16 | `fitness-function-runner` | ARCH | ⚡`push:main`<br>⏱`daily` | coupling/layering/size tests | trend | thresholds held | A | tech-debt-registrar |
| 05.17 | `refactoring-planner` | ARCH | ↪`tech-debt-registrar`<br>↪`crap-score-gate` | hotspot | stepwise refactor plan | plan includes safety tests | R | refactoring-implementer |
| 05.18 | `tech-radar-curator` | ARCH, DEP | ⏱`quarterly` | dependencies; ecosystem | adopt/trial/hold radar | owner-approved | H | dependency-upgrade-planner |

### G06: Construction (19)

_Writes and changes code: features, fixes, migrations, endpoints, refactors, conflict and CI repair._

| # | Agent | Roles | Activation | Inputs | Outputs / artifacts | Done when | Aut. | Hands off to |
|---|---|---|---|---|---|---|---|---|
| 06.01 | `feature-implementer` | BE, FE | ⚡`projects_v2_item.edited[status=Development]` | design; criteria; test design | PR | criteria tests pass; CI green | A | backend-implementer, frontend-implementer, code-review-orchestrator |
| 06.02 | `backend-implementer` | BE | ↪`feature-implementer` | design | domain, handlers, API code | unit+integration pass | A | unit-test-author |
| 06.03 | `frontend-implementer` | FE | ↪`feature-implementer`<br>↪`ui-component-designer` | component spec | UI components | component tests pass | A | a11y-static-checker, ui-component-test-author |
| 06.04 | `fix-implementer` | BE, FE | ↪`bug-reproducer`<br>↪`hotfix-intake`<br>↪`incident-diagnostician` | failing repro test | fix PR | repro passes; CI green | A | code-review-orchestrator |
| 06.05 | `api-endpoint-implementer` | BE | ↪`api-contract-designer` | contract | endpoint/controller | contract tests pass | A | contract-test-author |
| 06.06 | `migration-author` | DBA, BE | ↪`data-model-designer`<br>↪`index-advisor` | schema change | numbered migration script | applies cleanly; rollback noted | A | migration-reviewer |
| 06.07 | `refactoring-implementer` | BE | ↪`refactoring-planner` | plan | behavior-preserving PR | tests unchanged and green | A | code-review-orchestrator |
| 06.08 | `mechanical-cleanup-fixer` | BE, DX | ↪`static-analysis-triager`<br>↪`build-warning-reducer` | lint findings | cleanup PR | 0 warnings with -warnaserror | A | code-review-orchestrator |
| 06.09 | `review-feedback-applier` | BE, FE | ⚡`pull_request_review.submitted[changes_requested]`<br>⚡`pull_request_review_comment.created` | review threads | fix commits; thread replies | all threads resolved | A | code-review-orchestrator |
| 06.10 | `branch-updater` | DX, BE | ⚡`push:main` | open PRs | backmerge of main | no PR behind main | A | merge-conflict-resolver |
| 06.11 | `merge-conflict-resolver` | BE | ↪`branch-updater`<br>⚡`pull_request.synchronize[mergeable=false]` | conflicts | resolved merge | builds green | A | — |
| 06.12 | `ci-failure-fixer` | BE, DEVOPS | ↪`ci-pipeline-watcher[code failure]` | logs | fix commit | CI green or escalated after 3 tries | A | flaky-test-detector, human-escalation-router |
| 06.13 | `feature-flag-wirer` | BE, FE | ↪`feature-implementer`<br>↪`experiment-designer` | flag spec | flag-guarded code | both paths tested | A | feature-flag-manager |
| 06.14 | `config-change-implementer` | BE, DEVOPS | ↪`owner` | config request | config PR | environment validated | R | — |
| 06.15 | `dead-code-remover` | BE | ⏱`monthly` | coverage; references | removal PR | tests green | R | code-review-orchestrator |
| 06.16 | `sdk-client-generator` | BE, DX | ⚡`push:main[API contract]` | OpenAPI | client libraries | compile + smoke pass | A | — |
| 06.17 | `deprecation-implementer` | BE | ↪`sunset-planner` | deprecation plan | shims/removal PR | on schedule | R | — |
| 06.18 | `data-pipeline-implementer` | DE | ↪`technical-design-author[data]`<br>↪`data-pipeline-monitor` | pipeline design | ETL/ELT code | runs on sample | A | data-quality-checker |
| 06.19 | `llm-feature-implementer` | BE | ↪`feature-implementer[AI feature]` | prompt design | LLM-integrated code + evals | LlmTest passes | A | llm-eval-runner |

### G07: Code Review & Code Quality (18)

_Multi-lens review, bot-finding triage, risk scoring, protected-path gating and merge._

| # | Agent | Roles | Activation | Inputs | Outputs / artifacts | Done when | Aut. | Hands off to |
|---|---|---|---|---|---|---|---|---|
| 07.01 | `code-review-orchestrator` | CR | ⚡`pull_request.opened`<br>⚡`pull_request.ready_for_review`<br>⚡`pull_request.synchronize` | PR | fan-out to reviewer agents; aggregated review | every lens reported | A | correctness-reviewer, style-conventions-reviewer, test-adequacy-reviewer, security-review-agent, ef-query-reviewer |
| 07.02 | `correctness-reviewer` | CR | ↪`code-review-orchestrator` | diff | bug findings | high-confidence findings posted | A | review-feedback-applier |
| 07.03 | `style-conventions-reviewer` | CR | ↪`code-review-orchestrator` | diff; CLAUDE.md conventions | convention comments | 0 unresolved | A | review-feedback-applier |
| 07.04 | `test-adequacy-reviewer` | CR, QA | ↪`code-review-orchestrator` | diff; coverage | missing-test comments | changed logic covered | A | unit-test-author |
| 07.05 | `simplification-reviewer` | CR | ↪`code-review-orchestrator` | diff | reuse/simplify suggestions | posted | A | review-feedback-applier |
| 07.06 | `pr-description-writer` | CR, TW | ⚡`pull_request.opened` | diff; issue | PR summary; test plan | template filled | A | — |
| 07.07 | `pr-size-guard` | CR | ⚡`pull_request.opened` | diff stats | split recommendation | oversize flagged | A | story-splitter |
| 07.08 | `change-risk-scorer` | CR, REL | ⚡`pull_request.opened` | diff; hotspots; history | risk score label | scored | A | human-review-requester |
| 07.09 | `protected-path-guard` | CR, DEVOPS | ⚡`pull_request.opened`<br>⚡`pull_request.synchronize` | changed paths (.octopus, workflows, build scripts, packages, SDK) | human-gate label | human approval before merge | H | human-review-requester |
| 07.10 | `human-review-requester` | CR | ↪`protected-path-guard`<br>↪`change-risk-scorer`<br>↪`new-dependency-gate` | PR | review request with summary | human review obtained | H | human-decision-recorder |
| 07.11 | `bot-finding-triager` | CR | ⚡`pull_request_review.submitted[bot]`<br>⚡`check_run.completed[bot]` | Copilot/Qodana/bot findings | accept/reject with rationale | every finding dispositioned | A | review-feedback-applier |
| 07.12 | `static-analysis-triager` | CR, DX | ⚡`check_run.completed[qodana]`<br>⏱`weekly` | SARIF | ranked findings; fix batches | baseline shrinking | A | mechanical-cleanup-fixer |
| 07.13 | `crap-score-gate` | CR, QA | ⚡`workflow_run.completed` | coverage; complexity | CRAP report; pass/fail | under threshold | A | refactoring-planner |
| 07.14 | `merge-readiness-checker` | CR, REL | ⚡`check_suite.completed`<br>⚡`pull_request_review.submitted` | PR state | merge/no-merge verdict | gates green; no conflicts; API-verified CI | A | pr-merger |
| 07.15 | `pr-merger` | REL | ↪`merge-readiness-checker` | green PR | merge | merged by policy | A | board-column-mover, definition-of-done-checker |
| 07.16 | `code-ownership-mapper` | CR, EM | ⏱`weekly` | git history | CODEOWNERS draft | PR opened | R | — |
| 07.17 | `duplication-detector` | CR | ⏱`weekly` | code | clone report | clones above threshold filed | A | refactoring-planner |
| 07.18 | `complexity-trend-watcher` | CR, ARCH | ⏱`weekly` | metric history | trend alerts | regressions flagged | A | tech-debt-registrar |

### G08: Test Engineering & QA (22)

_Designs, writes, runs, and curates tests at every level; verifies columns; keeps the suite trustworthy._

| # | Agent | Roles | Activation | Inputs | Outputs / artifacts | Done when | Aut. | Hands off to |
|---|---|---|---|---|---|---|---|---|
| 08.01 | `test-design-author` | QA | ⚡`projects_v2_item.edited[status=Test Design]` | criteria; design | test matrix (unit/integration/acceptance) | every criterion mapped to a test | R | unit-test-author, integration-test-author, acceptance-test-author |
| 08.02 | `unit-test-author` | QA, BE | ↪`test-design-author`<br>↪`test-adequacy-reviewer`<br>↪`coverage-gap-analyzer` | code | NUnit/Shouldly tests | pass; conventions met | A | — |
| 08.03 | `integration-test-author` | QA, BE | ↪`test-design-author` | handlers; DB | integration tests | pass on SQL Server and SQLite | A | — |
| 08.04 | `acceptance-test-author` | QA | ↪`test-design-author` | criteria | Playwright tests | pass in CI | A | — |
| 08.05 | `ui-component-test-author` | QA, FE | ↪`frontend-implementer` | component | bUnit tests | pass | A | — |
| 08.06 | `contract-test-author` | QA, BE | ↪`integration-designer`<br>↪`api-endpoint-implementer` | contracts | consumer/provider tests | pass | A | — |
| 08.07 | `test-data-generator` | QA, DBA | ↪`unit-test-author`<br>↪`integration-test-author` | schema | builders / seed data | reproducible | A | — |
| 08.08 | `functional-test-runner` | QA | ⚡`projects_v2_item.edited[status=Functional Testing]`<br>⚡`deployment_status.success[tdd]` | build; criteria | verification report | all criteria verified | A | board-column-mover |
| 08.09 | `exploratory-tester` | QA | ⚡`deployment_status.success[uat]` | UAT URL; feature | charter & findings | findings filed | A | bug-reproducer |
| 08.10 | `smoke-test-runner` | QA, SRE | ⚡`deployment_status.success` | environment URL | smoke results | health + key journeys pass | A | rollback-executor |
| 08.11 | `cross-browser-runner` | QA, FE | ⏱`nightly` | acceptance suite | browser matrix results | all pass | A | bug-reproducer |
| 08.12 | `bug-verification-agent` | QA | ⚡`pull_request.closed[merged, fixes #]` | bug; build | verified / reopened | repro test in suite | A | item-closure-verifier |
| 08.13 | `flaky-test-detector` | QA | ⚡`workflow_run.completed`<br>⏱`daily` | test history | flakiness scores | flakies labeled | A | flaky-test-quarantiner |
| 08.14 | `flaky-test-quarantiner` | QA | ↪`flaky-test-detector` | flaky test | quarantine PR + fix issue | main unblocked | R | flaky-test-fixer |
| 08.15 | `flaky-test-fixer` | QA | ⚡`issues.labeled[flaky]` | flaky test | stabilizing PR | 50 green reruns | A | — |
| 08.16 | `regression-suite-curator` | QA | ⏱`weekly` | runtimes; failures | pruned/rebalanced suite | runtime within budget | R | — |
| 08.17 | `coverage-gap-analyzer` | QA | ⏱`weekly` | coverage | gap issues | top gaps filed | A | unit-test-author |
| 08.18 | `mutation-tester` | QA | ⏱`weekly` | code; tests | mutation score | surviving mutants filed | A | unit-test-author |
| 08.19 | `llm-eval-runner` | QA | ⚡`pull_request.synchronize[LLM paths]`<br>⏱`daily` | eval set | pass rates (3-attempt LlmTest semantics) | pass rate >= threshold | A | agent-prompt-tuner |
| 08.20 | `test-env-provisioner` | QA, DEVOPS | ↪`functional-test-runner`<br>↪`exploratory-tester` | env spec | ephemeral env | ready <10 min | A | ephemeral-env-reaper |
| 08.21 | `test-report-publisher` | QA | ⚡`workflow_run.completed` | TRX; logs | job summary; trends | posted | A | — |
| 08.22 | `chaos-experiment-runner` | SRE, QA | ⏱`weekly` | resilience hypotheses (UAT) | findings | filed | R | resilience-improvement-planner |

### G09: Performance & Capacity (14)

_Load, stress, soak, benchmarks, profiling, budgets, capacity forecasts._

| # | Agent | Roles | Activation | Inputs | Outputs / artifacts | Done when | Aut. | Hands off to |
|---|---|---|---|---|---|---|---|---|
| 09.01 | `load-test-author` | PERF | ↪`test-design-author`<br>⚡`push:main[API contract]` | endpoints | k6 scripts with thresholds | smoke profile passes | A | load-test-runner |
| 09.02 | `load-test-runner` | PERF | ⏱`weekly`<br>⚡`release.candidate` | k6 scripts | p50/p95/p99; RPS; errors | thresholds evaluated | A | perf-regression-analyzer |
| 09.03 | `perf-regression-analyzer` | PERF | ↪`load-test-runner`<br>↪`benchmark-runner` | baselines | regression report | regressions filed | A | profiler-agent |
| 09.04 | `benchmark-runner` | PERF, BE | ⚡`pull_request.synchronize[hot paths]` | microbenchmarks | benchmark results | delta within tolerance | A | perf-regression-analyzer |
| 09.05 | `profiler-agent` | PERF | ↪`perf-regression-analyzer`<br>↪`soak-tester` | traces; profiles | hotspot analysis | root cause found | A | fix-implementer |
| 09.06 | `query-performance-analyzer` | PERF, DBA | ⏱`daily` | query store; traces | slow-query list | top-N filed | A | index-advisor |
| 09.07 | `frontend-perf-auditor` | PERF, FE | ⚡`deployment_status.success[uat]`<br>⏱`weekly` | pages | Core Web Vitals report | budgets met | A | frontend-implementer |
| 09.08 | `bundle-size-watcher` | PERF, FE | ⚡`pull_request.synchronize[UI paths]` | WASM/JS bundle | size delta | under budget | A | — |
| 09.09 | `cold-start-analyzer` | PERF, PLAT | ⏱`weekly` | container startup logs | startup trend | within budget | A | container-runtime-tuner |
| 09.10 | `stress-breakpoint-tester` | PERF | ⏱`monthly` | breakpoint profile | breaking point | documented | A | capacity-planner |
| 09.11 | `soak-tester` | PERF | ⏱`monthly` | soak profile | leak/degradation report | none or filed | A | profiler-agent |
| 09.12 | `rate-limit-validator` | PERF, SEC | ⚡`pull_request.synchronize[rate-limit config]` | policy | 429 behavior report | limits behave as designed | A | — |
| 09.13 | `capacity-planner` | PERF, SRE, PLAT | ⏱`monthly` | growth; saturation | capacity forecast | headroom >= target | R | infra-change-planner |
| 09.14 | `perf-budget-keeper` | PERF, PM | ⏱`quarterly` | SLOs; NFRs | performance budgets | owner-approved | H | — |

### G10: Security & AppSec (20)

_OWASP SAMM across govern/design/implement/verify/operate: scanning, triage, fixing, secrets, access, advisories._

| # | Agent | Roles | Activation | Inputs | Outputs / artifacts | Done when | Aut. | Hands off to |
|---|---|---|---|---|---|---|---|---|
| 10.01 | `sast-scanner` | SEC | ⚡`pull_request.synchronize`<br>⚡`push:main` | code | Semgrep/CodeQL findings | every finding dispositioned | A | vuln-triager |
| 10.02 | `secret-scanner` | SEC | ⚡`push`<br>⚡`pull_request.synchronize` | diff | TruffleHog findings | no verified secret merged | A | secret-leak-responder |
| 10.03 | `secret-leak-responder` | SEC | ⚡`secret_scanning_alert.created`<br>↪`secret-scanner` | leaked secret | revocation; rotation; purge plan | secret revoked <1h | H | secret-rotation-runner |
| 10.04 | `secret-rotation-runner` | SEC, DEVOPS | ⏱`monthly`<br>↪`secret-leak-responder` | secret inventory | rotated secrets | none beyond max age | R | — |
| 10.05 | `vuln-triager` | SEC | ⚡`code_scanning_alert.created`<br>⚡`dependabot_alert.created`<br>↪`sca-scanner` | alert; reachability | severity; exploitability; SLA | triaged within SLA | A | vuln-fixer |
| 10.06 | `vuln-fixer` | SEC, BE | ↪`vuln-triager` | vulnerability | fix PR | alert closed | A | code-review-orchestrator, security-advisory-publisher |
| 10.07 | `security-review-agent` | SEC, CR | ↪`code-review-orchestrator` | diff | security findings | posted | A | review-feedback-applier |
| 10.08 | `dast-scanner` | SEC | ⚡`deployment_status.success[uat]`<br>⏱`weekly` | UAT URL | ZAP findings | filed | A | vuln-triager |
| 10.09 | `security-requirements-writer` | SEC | ↪`threat-modeler` | threats | security acceptance criteria | criteria added | R | security-test-author |
| 10.10 | `security-test-author` | SEC, QA | ↪`security-requirements-writer` | security criteria | abuse-case tests | pass | A | — |
| 10.11 | `authz-matrix-auditor` | SEC | ⏱`weekly`<br>⚡`pull_request.synchronize[auth paths]` | endpoints; policies | authz coverage matrix | no unguarded endpoint | A | vuln-fixer |
| 10.12 | `container-image-scanner` | SEC, DEVOPS | ⚡`workflow_run.completed[image built]`<br>⏱`daily` | images | CVE list | no critical in prod | A | base-image-updater |
| 10.13 | `iac-security-scanner` | SEC, PLAT | ⚡`pull_request.synchronize[infra paths]` | IaC | misconfig findings | none high | A | iac-implementer |
| 10.14 | `supply-chain-attestation` | SEC, REL | ⚡`release.published` | build provenance | SLSA provenance; signatures | artifacts signed & verifiable | A | — |
| 10.15 | `waf-rule-tuner` | SEC, SRE | ⚡`alert.fired[waf]`<br>⏱`weekly` | WAF logs | rule changes | false positives down | R | — |
| 10.16 | `access-review-runner` | SEC, EM | ⏱`quarterly` | repo/cloud access lists | stale-access removals | owner-approved | H | — |
| 10.17 | `security-posture-reporter` | SEC, EM | ⏱`monthly` | all findings | posture report; SAMM scores | published | A | stakeholder-status-reporter |
| 10.18 | `pen-test-coordinator` | SEC | ⏱`annual` | scope | engagement; imported findings | findings tracked | H | vuln-triager |
| 10.19 | `security-advisory-publisher` | SEC, MKT | ↪`vuln-fixer[public impact]` | fixed vulnerability | GHSA/CVE advisory | owner-approved | H | release-comms-writer |
| 10.20 | `security-guidance-writer` | SEC, DX | ⏱`quarterly` | recurring finding classes | guidance docs; skill updates | published | R | agent-prompt-tuner |

### G11: Supply Chain, Dependency & License (13)

_Keeps third-party code current, safe, licensed and approved._

| # | Agent | Roles | Activation | Inputs | Outputs / artifacts | Done when | Aut. | Hands off to |
|---|---|---|---|---|---|---|---|---|
| 11.01 | `dependency-update-proposer` | DEP | ⏱`weekly`<br>⚡`package.version.published` | manifests | grouped upgrade PRs | CI green per group | A | code-review-orchestrator |
| 11.02 | `dependency-upgrade-planner` | DEP, ARCH | ⏱`quarterly`<br>↪`eol-runtime-watcher` | majors; EOL dates | upgrade roadmap (e.g. .NET major) | owner-approved | H | epic-decomposer |
| 11.03 | `sca-scanner` | DEP, SEC | ⚡`pull_request.synchronize[manifests]`<br>⏱`daily` | manifests | OWASP DC / dotnet vulnerable / npm audit | no high/critical | A | vuln-triager |
| 11.04 | `new-dependency-gate` | DEP, ARCH | ⚡`pull_request.synchronize[new package]` | package; rationale | approval request | human-approved (repo rule) | H | human-review-requester |
| 11.05 | `license-compliance-checker` | DEP, LEGAL | ⚡`pull_request.synchronize[manifests]`<br>⏱`weekly` | deps; license policy | license report | no disallowed license | A | legal-review-requester |
| 11.06 | `sbom-generator` | DEP, SEC | ⚡`release.published`<br>⚡`push:main` | build | CycloneDX/SPDX SBOM | SBOM attached | A | — |
| 11.07 | `typosquat-malware-checker` | DEP, SEC | ⚡`pull_request.synchronize[manifests]` | new deps | malicious-package verdict | none | A | — |
| 11.08 | `eol-runtime-watcher` | DEP | ⏱`monthly` | SDK/runtime versions | EOL alerts | issue >=90d before EOL | A | dependency-upgrade-planner |
| 11.09 | `base-image-updater` | DEP, DEVOPS | ⏱`weekly`<br>↪`container-image-scanner`<br>↪`os-patch-manager` | Dockerfiles | image bump PRs | CI green | A | — |
| 11.10 | `abandoned-package-detector` | DEP | ⏱`monthly` | package metadata | unmaintained-package risks | filed | A | tech-radar-curator |
| 11.11 | `lockfile-drift-auditor` | DEP | ⏱`monthly` | lockfiles; central package versions | drift report | consistent | A | — |
| 11.12 | `third-party-notice-writer` | DEP, LEGAL | ⚡`release.published` | licenses | NOTICE file | complete | A | — |
| 11.13 | `action-pinning-auditor` | DEP, SEC | ⚡`pull_request.synchronize[workflows]`<br>⏱`weekly` | workflow YAML | SHA-pin report | all actions pinned | R | protected-path-guard |

### G12: CI/CD, Build & Release (23)

_Pipeline health, artifacts, versions, release trains, environment promotion, rollback, DORA._

| # | Agent | Roles | Activation | Inputs | Outputs / artifacts | Done when | Aut. | Hands off to |
|---|---|---|---|---|---|---|---|---|
| 12.01 | `ci-pipeline-watcher` | DEVOPS | ⚡`workflow_run.completed` | run results | failure class: code / infra / flaky | every failure classified | A | ci-failure-fixer, flaky-test-detector, ci-infra-fixer |
| 12.02 | `ci-infra-fixer` | DEVOPS | ↪`ci-pipeline-watcher[infra]` | runner logs | infra fix or retry | pipeline restored | R | protected-path-guard |
| 12.03 | `ci-duration-optimizer` | DEVOPS, DX | ⏱`weekly` | job timings | caching/parallelism PR | median duration down | R | protected-path-guard |
| 12.04 | `pipeline-as-code-maintainer` | DEVOPS | ↪`owner`<br>↪`ci-duration-optimizer` | workflow YAML | workflow PR | human-approved | H | protected-path-guard |
| 12.05 | `build-reproducibility-checker` | DEVOPS | ⏱`weekly` | two builds | hash comparison | deterministic | A | — |
| 12.06 | `artifact-publisher` | REL | ⚡`workflow_run.completed[success, main]` | build output | versioned packages/images | published, immutable | A | deployment-orchestrator |
| 12.07 | `version-bumper` | REL | ⚡`pull_request.closed[merged]` | commits | semver | version correct | A | changelog-generator |
| 12.08 | `changelog-generator` | REL, TW | ⚡`release.candidate` | merged PRs | CHANGELOG | every user-facing PR present | A | release-notes-writer |
| 12.09 | `release-planner` | REL, PM | ⏱`weekly` | roadmap; queue | release-train plan | published | R | release-scope-tracker |
| 12.10 | `release-candidate-cutter` | REL | ⚡`projects_v2_item.edited[status=Release Queue]`<br>⏱`weekday` | green main | RC tag | tagged | A | deployment-orchestrator |
| 12.11 | `deployment-orchestrator` | REL, DEVOPS | ⚡`workflow_run.completed[success, build]` | artifacts; env | TDD then UAT deployment | deployed and healthy | A | smoke-test-runner, migration-deploy-runner |
| 12.12 | `migration-deploy-runner` | DBA, REL | ↪`deployment-orchestrator` | DbUp scripts | applied migrations | schema version matches | A | — |
| 12.13 | `prod-deploy-gate` | REL, PO | ⚡`deployment_status.success[uat]` | UAT evidence; risk score | go/no-go packet | human approval (policy-auto for low risk) | H | deployment-orchestrator |
| 12.14 | `progressive-rollout-controller` | REL, SRE | ↪`deployment-orchestrator[prod]` | canary metrics | traffic-shift steps | 100% or rolled back | A | rollback-executor |
| 12.15 | `rollback-executor` | REL, SRE | ⚡`alert.fired[post-deploy]`<br>↪`smoke-test-runner[fail]` | previous revision | rollback | health restored | A | incident-commander |
| 12.16 | `release-verifier` | REL, QA | ⚡`deployment_status.success[prod]` | prod | post-release checks | verified | A | release-comms-writer, synthetic-monitor-author |
| 12.17 | `hotfix-release-runner` | REL | ↪`hotfix-intake` | fix PR | expedited pipeline run | deployed | R | prod-deploy-gate |
| 12.18 | `feature-flag-manager` | REL, PM | ↪`experiment-designer`<br>↪`feature-flag-wirer`<br>⏱`weekly` | flags | flag state changes; stale report | no flag >90d stale | R | stale-flag-remover |
| 12.19 | `stale-flag-remover` | REL, BE | ↪`feature-flag-manager` | stale flag | cleanup PR | merged | A | — |
| 12.20 | `deploy-freeze-enforcer` | REL | ⚡`calendar.freeze_window`<br>↪`error-budget-tracker` | calendar; error budget | deploy block | no deploy in freeze without override | A | — |
| 12.21 | `environment-promotion-tracker` | REL | ⚡`deployment_status` | deployments | what-is-where matrix | accurate | A | — |
| 12.22 | `docs-only-fast-path` | REL | ⚡`pull_request.closed[merged, docs-only]` | changed paths | skip release; card to Done | card in Done | A | board-column-mover |
| 12.23 | `dora-metrics-collector` | REL, EM | ⏱`daily` | deploys; incidents; commits | deploy freq; lead time; CFR; MTTR | dashboard updated | A | engineering-health-reporter |

### G13: Platform & Infrastructure (16)

_IaC, environments, drift, certificates, backups, DR, quotas, runners, golden paths._

| # | Agent | Roles | Activation | Inputs | Outputs / artifacts | Done when | Aut. | Hands off to |
|---|---|---|---|---|---|---|---|---|
| 13.01 | `infra-change-planner` | PLAT | ↪`capacity-planner`<br>↪`owner` | requirement | IaC plan | reviewed | R | iac-implementer, cost-impact-reviewer |
| 13.02 | `iac-implementer` | PLAT | ↪`infra-change-planner`<br>↪`drift-detector`<br>↪`rightsizing-advisor` | plan | IaC PR | plan/what-if clean | R | protected-path-guard |
| 13.03 | `drift-detector` | PLAT | ⏱`daily` | IaC; live state | drift report | zero unexplained drift | A | iac-implementer |
| 13.04 | `environment-provisioner` | PLAT | ↪`repo-onboarder`<br>↪`owner` | env spec | new environment | reachable and healthy | H | — |
| 13.05 | `ephemeral-env-reaper` | PLAT, FIN | ⏱`hourly` | ephemeral envs | deletions | none older than TTL | A | — |
| 13.06 | `certificate-expiry-watcher` | PLAT, SRE | ⏱`daily` | certificates | renewals / alerts | none expiring <14d | A | — |
| 13.07 | `dns-domain-manager` | PLAT | ↪`owner` | domain change | DNS records | propagated | H | — |
| 13.08 | `backup-verifier` | PLAT, DBA | ⏱`daily` | backups | restore-test result | restore succeeds | A | — |
| 13.09 | `dr-drill-runner` | PLAT, SRE | ⏱`quarterly` | DR plan | drill report; measured RTO/RPO | within targets | R | resilience-improvement-planner |
| 13.10 | `container-runtime-tuner` | PLAT | ⏱`weekly`<br>↪`cold-start-analyzer` | CPU/mem; scale rules | scaling config PR | utilization in band | R | iac-implementer |
| 13.11 | `os-patch-manager` | PLAT, SEC | ⏱`weekly` | hosts/images | patches | no overdue critical patch | A | base-image-updater |
| 13.12 | `quota-limit-watcher` | PLAT | ⏱`daily` | cloud quotas | increase requests | headroom >20% | A | — |
| 13.13 | `network-exposure-auditor` | PLAT, SEC | ⏱`weekly` | NSGs; ingress | exposure report | no unintended public exposure | A | iac-implementer |
| 13.14 | `runner-fleet-manager` | PLAT, DEVOPS | ⏱`hourly` | CI queue | runner scaling | queue wait < target | A | — |
| 13.15 | `secrets-store-maintainer` | PLAT, SEC | ⚡`secret.requested`<br>↪`agent-permission-auditor` | vault | provisioned secret refs | no plaintext secrets | R | — |
| 13.16 | `golden-path-maintainer` | PLAT, DX | ⏱`quarterly` | service templates | updated templates | new repo boots <1h | R | — |

### G14: SRE, Observability & Incident (22)

_SLOs, alerting, detection, incident command, diagnosis, postmortems, toil and resilience._

| # | Agent | Roles | Activation | Inputs | Outputs / artifacts | Done when | Aut. | Hands off to |
|---|---|---|---|---|---|---|---|---|
| 14.01 | `slo-definer` | SRE, PM | ↪`nfr-specifier`<br>⏱`quarterly` | NFRs; telemetry | SLIs/SLOs | owner-approved | H | alert-rule-author, error-budget-tracker |
| 14.02 | `alert-rule-author` | SRE | ↪`slo-definer`<br>↪`alert-noise-reducer` | SLOs | burn-rate alerts | alerts tested | R | runbook-author |
| 14.03 | `health-check-monitor` | SRE | ⏱`1m` | /_healthcheck per env | health status | unhealthy emits event <1 min | A | incident-declarer |
| 14.04 | `dependency-health-watcher` | SRE | ⏱`5m` | third-party status (cloud, LLM API) | degradation events | detected | A | incident-declarer |
| 14.05 | `log-anomaly-detector` | SRE | ⏱`15m` | logs | anomaly events | noise-tuned | A | incident-declarer |
| 14.06 | `error-tracker-triager` | SRE, BE | ⚡`exception.new_fingerprint` | exceptions | bug issues | every new error filed | A | bug-reproducer |
| 14.07 | `incident-declarer` | SRE | ⚡`alert.fired`<br>⚡`healthcheck.unhealthy`<br>⚡`slo.burn_rate` | alert | Production Incident issue in Release Queue; severity | declared <2 min | A | incident-commander |
| 14.08 | `incident-commander` | SRE | ↪`incident-declarer` | incident | roles; timeline; update cadence | mitigated | R | incident-diagnostician, status-page-updater, on-call-pager |
| 14.09 | `incident-diagnostician` | SRE | ↪`incident-commander` | logs; traces; recent deploys | ranked hypotheses; likely cause | cause identified | A | rollback-executor, fix-implementer, runbook-executor |
| 14.10 | `runbook-executor` | SRE | ↪`incident-diagnostician` | runbook | executed steps | mitigated or escalated | R | — |
| 14.11 | `on-call-pager` | SRE | ↪`incident-commander[SEV1/2]` | rotation | page to human | ack within SLA | A | human-escalation-router |
| 14.12 | `status-page-updater` | SRE, SUP | ↪`incident-commander` | incident state | status-page updates | updates every 30 min | R | customer-incident-notifier |
| 14.13 | `postmortem-writer` | SRE | ⚡`incident.resolved` | timeline; data | blameless postmortem | published <5 days | R | action-item-filer |
| 14.14 | `action-item-filer` | SRE | ↪`postmortem-writer` | postmortem | action-item issues | all filed and prioritized | A | backlog-prioritizer |
| 14.15 | `error-budget-tracker` | SRE | ⏱`daily` | SLOs | budget report; freeze recommendation | policy applied | A | deploy-freeze-enforcer |
| 14.16 | `toil-tracker` | SRE | ⏱`weekly` | manual-intervention log | toil inventory | ranked automation candidates | A | backlog-generator-from-signals |
| 14.17 | `alert-noise-reducer` | SRE | ⏱`weekly` | alert history | tuning PRs | actionable ratio up | R | alert-rule-author |
| 14.18 | `observability-gap-finder` | SRE, BE | ⚡`pull_request.synchronize[handlers/external calls]`<br>⏱`weekly` | code; OTel wiring | missing span/metric findings | new paths instrumented | A | review-feedback-applier |
| 14.19 | `dashboard-maintainer` | SRE, ANA | ⏱`monthly` | dashboards | fixed dashboards | no broken panels | A | — |
| 14.20 | `runbook-author` | SRE, TW | ↪`postmortem-writer`<br>↪`alert-rule-author` | alert | runbook | every alert has a runbook | R | runbook-librarian |
| 14.21 | `synthetic-monitor-author` | SRE, QA | ↪`release-verifier` | key journeys | synthetic checks | key journeys covered | A | — |
| 14.22 | `resilience-improvement-planner` | SRE, ARCH | ↪`chaos-experiment-runner`<br>↪`dr-drill-runner` | findings | resilience backlog | filed | A | backlog-prioritizer |

### G15: Data: DBA & Data Engineering (16)

_Schema safety, query health, retention, masking, pipelines, data quality._

| # | Agent | Roles | Activation | Inputs | Outputs / artifacts | Done when | Aut. | Hands off to |
|---|---|---|---|---|---|---|---|---|
| 15.01 | `migration-reviewer` | DBA | ⚡`pull_request.synchronize[migration scripts]` | SQL | review (locking, idempotency, tabs) | approved | A | — |
| 15.02 | `migration-numbering-guard` | DBA | ⚡`pull_request.synchronize[scripts/Update]` | script names | collision report | unique, sequential | A | — |
| 15.03 | `index-advisor` | DBA | ↪`query-performance-analyzer` | query plans | index proposal | measured improvement | R | migration-author |
| 15.04 | `ef-query-reviewer` | DBA, BE | ↪`code-review-orchestrator[DataAccess]` | LINQ/EF code | N+1 and tracking findings | posted | A | review-feedback-applier |
| 15.05 | `schema-drift-detector` | DBA | ⏱`daily` | schema per env | drift report | none | A | — |
| 15.06 | `db-capacity-watcher` | DBA | ⏱`daily` | size; compute | forecast | headroom kept | A | capacity-planner |
| 15.07 | `db-maintenance-runner` | DBA | ⏱`weekly` | stats; fragmentation | maintenance run | done | A | — |
| 15.08 | `deadlock-analyzer` | DBA | ⚡`alert.fired[deadlock]` | deadlock graphs | root cause | filed | A | fix-implementer |
| 15.09 | `data-retention-enforcer` | DBA, LEGAL | ⏱`daily` | retention policy | purge jobs | compliant | A | — |
| 15.10 | `pii-data-classifier` | DBA, LEGAL | ⚡`pull_request.synchronize[schema]`<br>⏱`monthly` | schema | PII classification | all columns classified | A | privacy-impact-assessor |
| 15.11 | `db-restore-to-lower-env` | DBA | ↪`owner`<br>⏱`weekly` | prod backup | masked copy in UAT | masked and loaded | H | pii-data-masker |
| 15.12 | `pii-data-masker` | DBA, LEGAL | ↪`db-restore-to-lower-env` | data | masked dataset | no PII in lower envs | A | — |
| 15.13 | `data-migration-runner` | DE, DBA | ↪`owner` | backfill plan | executed backfill | counts verified | H | — |
| 15.14 | `data-quality-checker` | DE | ⏱`daily`<br>⚡`pipeline.completed` | datasets | DQ report | checks pass | A | — |
| 15.15 | `data-pipeline-monitor` | DE | ⚡`pipeline.failed` | runs | triage; rerun or issue | resolved | A | data-pipeline-implementer |
| 15.16 | `analytics-schema-maintainer` | DE, ANA | ⚡`tracking_plan.changed` | tracking plan | warehouse models | builds | A | — |

### G16: Analytics & Insights (11)

_Instrumentation, KPIs, anomaly detection, funnels, cohorts, ad-hoc questions._

| # | Agent | Roles | Activation | Inputs | Outputs / artifacts | Done when | Aut. | Hands off to |
|---|---|---|---|---|---|---|---|---|
| 16.01 | `tracking-plan-author` | ANA | ↪`product-requirements-writer` | PRD | event tracking spec | reviewed | R | feature-implementer |
| 16.02 | `instrumentation-verifier` | ANA, QA | ⚡`deployment_status.success[uat]` | tracking plan | event validation | all events fire | A | — |
| 16.03 | `kpi-dashboard-builder` | ANA | ↪`okr-drafter` | KPIs | dashboards | live | A | — |
| 16.04 | `metric-definition-keeper` | ANA | ⚡`pull_request.synchronize[metrics]` | definitions | semantic layer | single source of truth | R | — |
| 16.05 | `weekly-metrics-digest` | ANA | ⏱`weekly` | KPIs | digest | posted | A | stakeholder-status-reporter |
| 16.06 | `metric-anomaly-detector` | ANA | ⏱`daily` | KPIs | anomaly alerts | investigated | A | backlog-generator-from-signals |
| 16.07 | `funnel-analyzer` | ANA, UXR | ⏱`weekly` | events | funnel report | drop-offs flagged | A | user-journey-analytics-reviewer |
| 16.08 | `cohort-retention-analyzer` | ANA, CS | ⏱`monthly` | usage | retention curves | published | A | churn-risk-detector |
| 16.09 | `ad-hoc-question-answerer` | ANA | ↪`owner`<br>↪`any-agent` | question | query + answer with SQL shown | answered | A | — |
| 16.10 | `customer-usage-reporter` | ANA, CS | ⏱`monthly` | tenant usage | customer usage report | sent | R | — |
| 16.11 | `telemetry-cost-optimizer` | ANA, FIN, SRE | ⏱`monthly` | ingestion volumes | sampling changes | cost down, signal kept | R | — |

### G17: Docs & Knowledge (15)

_Keeps human- and agent-facing knowledge accurate: API refs, guides, notes, CLAUDE.md, runbooks, diagrams._

| # | Agent | Roles | Activation | Inputs | Outputs / artifacts | Done when | Aut. | Hands off to |
|---|---|---|---|---|---|---|---|---|
| 17.01 | `release-notes-writer` | TW, MKT | ⚡`release.candidate`<br>↪`changelog-generator` | changelog | user-facing notes | reviewed | R | release-comms-writer |
| 17.02 | `api-reference-generator` | TW | ⚡`push:main[API contract]` | OpenAPI | API docs | published | A | — |
| 17.03 | `user-guide-writer` | TW | ⚡`pull_request.closed[merged, user-facing]` | feature | user docs | published | R | — |
| 17.04 | `doc-gap-detector` | TW | ⚡`issues.labeled[question]`<br>⏱`weekly` | questions; docs | gap issues | filed | A | user-guide-writer |
| 17.05 | `doc-drift-checker` | TW, DX | ⚡`pull_request.synchronize` | code vs docs (CLAUDE.md, README, docs/) | stale-doc comments or PR | docs match code | A | — |
| 17.06 | `code-comment-documenter` | TW, BE | ⚡`pull_request.synchronize` | public APIs | XML doc comments | public API 100% documented | A | — |
| 17.07 | `link-checker` | TW | ⏱`weekly` | docs | broken-link fixes | 0 broken | A | — |
| 17.08 | `readme-maintainer` | TW, DX | ⏱`monthly` | repo | README updates | accurate | R | — |
| 17.09 | `runbook-librarian` | TW, SRE | ⏱`monthly` | runbooks | index; freshness | none >6 months unreviewed | A | — |
| 17.10 | `glossary-maintainer` | TW | ⏱`monthly` | domain terms | glossary | current | A | — |
| 17.11 | `tutorial-writer` | TW, DX | ↪`feature-adoption-reviewer[low adoption]` | feature | tutorial | published | R | — |
| 17.12 | `diagram-renderer` | TW, ARCH | ⚡`pull_request.synchronize[*.puml]` | PlantUML | PNGs | rendered | A | — |
| 17.13 | `docs-site-publisher` | TW | ⚡`push:main[docs]` | docs | docs site | deployed | A | — |
| 17.14 | `onboarding-guide-maintainer` | TW, DX | ⏱`quarterly` | repo changes | onboarding guide for humans and agents | validated by fresh-agent dry run | R | — |
| 17.15 | `kb-article-writer` | TW, SUP | ↪`support-ticket-resolver[recurring]` | tickets | KB article | published | R | faq-updater |

### G18: Support & Customer Success (14)

_Tickets, investigation, SLAs, customer comms, health, churn and feedback loops._

| # | Agent | Roles | Activation | Inputs | Outputs / artifacts | Done when | Aut. | Hands off to |
|---|---|---|---|---|---|---|---|---|
| 18.01 | `support-ticket-triager` | SUP | ⚡`support.ticket.created` | ticket | category; priority; route | triaged <15 min | A | support-ticket-resolver |
| 18.02 | `support-ticket-resolver` | SUP | ↪`support-ticket-triager` | ticket; KB; logs | reply | resolved or escalated | R | intake-channel-bridge, kb-article-writer |
| 18.03 | `customer-log-investigator` | SUP, SRE | ↪`support-ticket-resolver` | tenant/user id | log findings | evidence-backed answer | A | bug-reproducer |
| 18.04 | `support-sla-watcher` | SUP | ⏱`15m` | tickets | SLA breach alerts | none breached silently | A | human-escalation-router |
| 18.05 | `bug-report-quality-coach` | SUP | ⚡`issues.opened[bug, incomplete]` | report | request for build, env, logs | report complete | A | — |
| 18.06 | `customer-incident-notifier` | SUP, CS | ↪`status-page-updater` | incident | affected-customer notices | sent | R | — |
| 18.07 | `close-the-loop-notifier` | SUP, CS | ⚡`issues.closed[linked tickets]` | fixed issue | customer notification | notified | A | — |
| 18.08 | `sentiment-analyzer` | CS | ⏱`daily` | tickets; NPS | sentiment trends | report | A | backlog-generator-from-signals |
| 18.09 | `churn-risk-detector` | CS, ANA | ⏱`weekly` | usage; tickets | at-risk accounts | flagged | A | customer-health-reporter |
| 18.10 | `customer-health-reporter` | CS | ⏱`weekly` | health signals | health scores | published | A | — |
| 18.11 | `feature-feedback-router` | CS, PM | ⚡`feedback.received` | feedback | linked votes on issues | every item linked | A | feature-request-analyzer |
| 18.12 | `support-macro-maintainer` | SUP | ⏱`monthly` | resolutions | canned responses | updated | A | — |
| 18.13 | `faq-updater` | SUP, TW | ⏱`weekly` | top questions | FAQ | current | A | — |
| 18.14 | `customer-onboarding-assistant` | CS | ⚡`customer.signed_up` | account | onboarding checklist | activated | R | — |

### G19: Accessibility & Localization (11)

_WCAG conformance and world-readiness as continuous checks, not audits._

| # | Agent | Roles | Activation | Inputs | Outputs / artifacts | Done when | Aut. | Hands off to |
|---|---|---|---|---|---|---|---|---|
| 19.01 | `a11y-static-checker` | A11Y | ⚡`pull_request.synchronize[UI paths]` | markup | WCAG static findings | none serious | A | review-feedback-applier |
| 19.02 | `a11y-runtime-auditor` | A11Y | ⚡`deployment_status.success[uat]`<br>⏱`weekly` | pages | axe-core via Playwright | WCAG 2.2 AA pass | A | frontend-implementer |
| 19.03 | `keyboard-nav-tester` | A11Y, QA | ⚡`deployment_status.success[uat]` | flows | keyboard-only results | all flows operable | A | — |
| 19.04 | `screen-reader-tester` | A11Y | ⏱`weekly` | pages | screen-reader transcript review | labels meaningful | R | — |
| 19.05 | `color-contrast-checker` | A11Y, UXD | ⚡`pull_request.synchronize[design tokens]` | tokens | contrast ratios | >=4.5:1 | A | — |
| 19.06 | `a11y-conformance-reporter` | A11Y, LEGAL | ⏱`quarterly` | audits | VPAT/ACR | published | H | — |
| 19.07 | `i18n-string-extractor` | L10N | ⚡`pull_request.synchronize[UI paths]` | code | resource files; hardcoded-string findings | none hardcoded | A | translation-generator |
| 19.08 | `translation-generator` | L10N | ⚡`push:main[resources]` | resx | machine translations flagged for review | all locales filled | R | translation-reviewer |
| 19.09 | `translation-reviewer` | L10N, LEGAL | ↪`translation-generator` | translations | reviewed strings | legal/marketing text human-reviewed | H | — |
| 19.10 | `locale-format-tester` | L10N, QA | ⏱`weekly` | dates; numbers; RTL | findings | pass | A | — |
| 19.11 | `pseudo-localization-runner` | L10N | ⚡`pull_request.synchronize[UI paths]` | UI | truncation/overflow findings | none | A | — |

### G20: Legal, Privacy & Compliance (12)

_Privacy, licensing, regulatory tracking, evidence, change records, AI-use register._

| # | Agent | Roles | Activation | Inputs | Outputs / artifacts | Done when | Aut. | Hands off to |
|---|---|---|---|---|---|---|---|---|
| 20.01 | `privacy-impact-assessor` | LEGAL | ↪`pii-data-classifier`<br>↪`technical-design-author[personal data]` | design | DPIA draft | human/DPO approval | H | — |
| 20.02 | `data-subject-request-handler` | LEGAL, SUP | ⚡`dsr.received` | request | export or deletion | fulfilled within statutory window | R | — |
| 20.03 | `policy-text-watcher` | LEGAL | ↪`privacy-impact-assessor` | terms/privacy text | change recommendation | human-approved | H | — |
| 20.04 | `legal-review-requester` | LEGAL | ↪`license-compliance-checker`<br>↪`policy-text-watcher` | issue | packet for counsel | human decision | H | human-decision-recorder |
| 20.05 | `compliance-evidence-collector` | LEGAL, SEC | ⏱`daily` | CI logs; approvals; audit trail | SOC 2/ISO evidence | controls evidenced | A | — |
| 20.06 | `control-gap-assessor` | LEGAL, SEC | ⏱`quarterly` | control framework | gaps | filed | R | — |
| 20.07 | `change-approval-recorder` | LEGAL, REL | ⚡`deployment_status.success[prod]` | deploy; approvals | change record | every prod change recorded | A | — |
| 20.08 | `cookie-consent-auditor` | LEGAL | ⏱`monthly` | site | tracker inventory vs consent | compliant | A | — |
| 20.09 | `regulatory-change-watcher` | LEGAL | ⏱`monthly` | GDPR, EU AI Act, ADA feeds | impact memo | reviewed | R | — |
| 20.10 | `ai-system-register-keeper` | LEGAL, AIOPS | ⏱`quarterly` | agent inventory | AI system register; disclosures | current | R | — |
| 20.11 | `export-control-checker` | LEGAL | ⚡`release.published` | crypto use; destinations | classification | cleared | H | — |
| 20.12 | `contributor-license-checker` | LEGAL | ⚡`pull_request.opened[external author]` | author | CLA/DCO status | signed | A | — |

### G21: FinOps & Cost (11)

_Inform, optimize, govern cloud and AI spend (FinOps Framework domains)._

| # | Agent | Roles | Activation | Inputs | Outputs / artifacts | Done when | Aut. | Hands off to |
|---|---|---|---|---|---|---|---|---|
| 21.01 | `cloud-cost-reporter` | FIN | ⏱`daily` | billing export | cost by service/env/tag | published | A | — |
| 21.02 | `cost-anomaly-detector` | FIN | ⏱`daily`<br>⚡`cost.anomaly` | spend | anomaly issue | investigated <24h | A | rightsizing-advisor |
| 21.03 | `rightsizing-advisor` | FIN, PLAT | ⏱`weekly` | utilization | rightsizing proposal | savings realized | R | iac-implementer |
| 21.04 | `idle-resource-reaper` | FIN | ⏱`daily` | resources | auto-stop non-prod; prod proposals | idle non-prod stopped | A | — |
| 21.05 | `tagging-compliance-enforcer` | FIN, PLAT | ⚡`resource.created`<br>⏱`daily` | tags | tag fixes | 100% tagged | A | — |
| 21.06 | `cost-impact-reviewer` | FIN, ARCH | ⚡`pull_request.synchronize[infra paths]`<br>↪`infra-change-planner` | IaC diff | cost delta estimate | posted | A | — |
| 21.07 | `budget-forecaster` | FIN, PGM | ⏱`monthly` | trend | forecast vs budget | variance explained | A | stakeholder-status-reporter |
| 21.08 | `unit-cost-calculator` | FIN, PM | ⏱`monthly` | cost; usage | cost per work order / tenant | published | A | pricing-packaging-analyst |
| 21.09 | `finops-ai-spend-reporter` | FIN, AIOPS | ⏱`daily` | LLM token spend per agent | AI cost report; cost per merged PR | published | A | agent-budget-governor |
| 21.10 | `commitment-planner` | FIN | ⏱`quarterly` | usage baseline | reservation/savings-plan recommendation | human purchase decision | H | — |
| 21.11 | `saas-license-auditor` | FIN | ⏱`quarterly` | SaaS seats | unused seats | reclaimed | R | — |

### G22: Communications & Marketing (10)

_Release comms, notices, public changelog, stakeholder reporting._

| # | Agent | Roles | Activation | Inputs | Outputs / artifacts | Done when | Aut. | Hands off to |
|---|---|---|---|---|---|---|---|---|
| 22.01 | `release-comms-writer` | MKT | ⚡`release.published`<br>↪`release-notes-writer` | release notes | announcement / blog / email draft | approved | H | website-content-updater |
| 22.02 | `deprecation-notice-writer` | MKT, PM | ↪`sunset-planner` | deprecation plan | customer notice | sent on schedule | H | — |
| 22.03 | `changelog-page-publisher` | MKT, TW | ⚡`release.published` | notes | public changelog | live | A | — |
| 22.04 | `social-post-drafter` | MKT | ⚡`release.published` | highlights | social drafts | human-approved | H | — |
| 22.05 | `demo-script-writer` | MKT, PM | ⚡`release.published[major]` | features | demo script | reviewed | R | — |
| 22.06 | `sales-enablement-writer` | MKT | ⏱`monthly` | features | battlecards | published | R | — |
| 22.07 | `website-content-updater` | MKT | ↪`release-comms-writer` | site | content PR | approved | H | — |
| 22.08 | `seo-auditor` | MKT | ⏱`monthly` | site | SEO findings | filed | A | — |
| 22.09 | `stakeholder-status-reporter` | EM, PGM | ⏱`weekly` | flow; OKRs; releases; incidents; cost | owner status report | delivered | A | — |
| 22.10 | `internal-digest-writer` | EM | ⏱`monthly` | achievements | digest | sent | A | — |

### G23: Engineering Management, Portfolio & Program (11)

_Capacity allocation across repos, risk, portfolio mix, agent performance management._

| # | Agent | Roles | Activation | Inputs | Outputs / artifacts | Done when | Aut. | Hands off to |
|---|---|---|---|---|---|---|---|---|
| 23.01 | `engineering-health-reporter` | EM | ⏱`weekly` | DORA; flow; quality; cost | health scorecard | published | A | stakeholder-status-reporter |
| 23.02 | `capacity-allocator` | EM, PGM | ⏱`monthly` | agent capacity; budget | allocation: features / debt / ops | owner-approved | H | lane-dispatcher |
| 23.03 | `portfolio-balancer` | PGM | ⏱`quarterly` | initiatives across repos | portfolio view; investment mix | owner decision | H | roadmap-planner |
| 23.04 | `cross-repo-dependency-coordinator` | PGM | ⚡`issues.cross_referenced`<br>⏱`weekly` | multi-repo items | dependency plan | no silently blocked cross-repo items | A | dependency-graph-resolver |
| 23.05 | `milestone-risk-assessor` | PGM | ⏱`weekly` | milestones | risk register | risks owned | A | human-escalation-router |
| 23.06 | `raid-log-keeper` | PGM | ⏱`weekly` | risks; assumptions; issues; dependencies | RAID log | current | A | — |
| 23.07 | `quarterly-business-review-writer` | PGM, EM | ⏱`quarterly` | all reports | QBR document | owner reviewed | R | — |
| 23.08 | `agent-performance-reviewer` | EM, AIOPS | ⏱`monthly` | evals; cost; outcomes | agent scorecards; retire/merge/split proposals | owner-approved | R | agent-registry-keeper |
| 23.09 | `human-gate-queue-balancer` | EM | ⏱`weekly` | pending human gates | batched decisions; gate SLA report | gate queue < N | A | human-escalation-router |
| 23.10 | `policy-keeper` | EM, AIOPS | ⚡`push:main[CLAUDE.md, policies]` | policies | policy digest; agent reload | agents on current policy | A | agent-registry-keeper |
| 23.11 | `ai-worker-mix-manager` | EM, FIN | ⏱`monthly` | per-worker outcomes (Cursor/Copilot/Claude/Bob) | worker mix recommendation | config updated | R | model-router |

### G24: Developer Experience (10)

_Keeps the inner loop fast for agents and the human owner: environments, hooks, skills, scaffolds, prompts._

| # | Agent | Roles | Activation | Inputs | Outputs / artifacts | Done when | Aut. | Hands off to |
|---|---|---|---|---|---|---|---|---|
| 24.01 | `dev-environment-validator` | DX | ⏱`daily`<br>⚡`push:main[build files]` | setup scripts; devcontainer | fresh-clone build result | builds from zero | A | — |
| 24.02 | `session-start-hook-maintainer` | DX | ⚡`push:main[dependencies]` | SessionStart hook | updated hook | cloud sessions build and test | R | — |
| 24.03 | `build-warning-reducer` | DX | ⏱`weekly` | compiler warnings | fix batches | trend down | A | mechanical-cleanup-fixer |
| 24.04 | `skill-library-curator` | DX, AIOPS | ⏱`monthly` | skills; usage | consolidated skills | no duplicate skills | R | agent-registry-keeper |
| 24.05 | `local-build-time-tracker` | DX | ⏱`weekly` | build logs | build-time trend | regressions flagged | A | ci-duration-optimizer |
| 24.06 | `scaffolding-generator` | DX, BE | ↪`feature-implementer` | pattern (query+handler+test) | scaffolds | compile | A | — |
| 24.07 | `inner-loop-optimizer` | DX | ⏱`monthly` | build/test loop timings | targeted-test tooling | loop time down | R | — |
| 24.08 | `permission-prompt-reducer` | DX | ⏱`weekly` | transcripts | allowlist PR | prompts down | R | — |
| 24.09 | `owner-friction-surveyor` | DX, EM | ⏱`quarterly` | owner feedback | friction list | filed | A | — |
| 24.10 | `api-sandbox-maintainer` | DX | ⚡`push:main[API contract]` | sandbox | updated samples | working | A | — |

### G25: Kaizen: Self-Observation & Backlog Generation (12)

_Converts observed signals into evidence-backed backlog items; learns from reverts, escapes, overrides and rework._

| # | Agent | Roles | Activation | Inputs | Outputs / artifacts | Done when | Aut. | Hands off to |
|---|---|---|---|---|---|---|---|---|
| 25.01 | `signal-aggregator` | AIOPS | ⚡`finding.emitted` | findings stream from all observers | clustered themes | clusters updated | A | backlog-generator-from-signals |
| 25.02 | `backlog-generator-from-signals` | PM, AIOPS | ⏱`daily`<br>↪`signal-aggregator` | clustered signals | draft items with evidence, labeled agent-proposed | deduped; scored | R | duplicate-detector, opportunity-scorer, agent-proposal-gate |
| 25.03 | `agent-proposal-gate` | PO | ↪`backlog-generator-from-signals` | proposals | accept/reject; policy auto-accept for low-risk classes | owner decision or policy | H | backlog-prioritizer |
| 25.04 | `recurring-failure-miner` | AIOPS, SRE | ⏱`weekly` | CI failures; incidents; reverts | systemic issues | filed | A | backlog-generator-from-signals |
| 25.05 | `revert-analyzer` | CR, QA | ⚡`push:main[revert commit]` | reverted PR | escape analysis | missing gate filed | A | process-improvement-implementer |
| 25.06 | `escaped-defect-analyzer` | QA | ⚡`issues.labeled[bug, production]` | bug; history | which gate missed it | gate improvement filed | A | process-improvement-implementer |
| 25.07 | `rework-detector` | SM | ⏱`weekly` | items bouncing between columns | rework report | root causes filed | A | retrospective-facilitator |
| 25.08 | `agent-friction-miner` | AIOPS | ⏱`daily` | transcripts: errors, retries, permission denials | friction items | filed | A | agent-prompt-tuner |
| 25.09 | `human-override-learner` | AIOPS | ↪`human-decision-recorder` | overrides / rejections | prompt/policy updates | patterns encoded | R | agent-prompt-tuner |
| 25.10 | `goal-drift-detector` | PM | ⏱`weekly` | shipped work vs OKRs | alignment report | misalignment flagged | A | backlog-prioritizer |
| 25.11 | `quality-trend-sentinel` | QA, EM | ⏱`weekly` | coverage; CRAP; bugs; flakiness | trend alerts | regressions filed | A | backlog-generator-from-signals |
| 25.12 | `idea-incubator` | PM | ⏱`monthly` | all insights | bets proposals | owner reviewed | H | product-vision-keeper |

## 5. Trigger matrix (event → agents)

Grouped by base event. Filters are shown in brackets next to each agent. Order is not significant; `event-router` fans out every event in parallel, and `work-claim-lock` serializes any agents that touch the same item.

| Event | # | Agents woken |
|---|---|---|
| `*any-webhook` | 1 | `event-router` |
| `agent.completed` | 2 | `agent-output-evaluator`, `audit-trail-keeper` |
| `agent.registry.changed` | 1 | `agent-permission-auditor` |
| `agent.token_spend` | 1 | `agent-budget-governor` |
| `alert.fired` | 5 | `kill-switch-guardian`[agent-runaway], `waf-rule-tuner`[waf], `rollback-executor`[post-deploy], `incident-declarer`, `deadlock-analyzer`[deadlock] |
| `calendar.freeze_window` | 1 | `deploy-freeze-enforcer` |
| `chat.message` | 1 | `owner-request-intake`[owner] |
| `check_run.completed` | 2 | `bot-finding-triager`[bot], `static-analysis-triager`[qodana] |
| `check_suite.completed` | 1 | `merge-readiness-checker` |
| `code_scanning_alert.created` | 1 | `vuln-triager` |
| `cost.anomaly` | 1 | `cost-anomaly-detector` |
| `customer.signed_up` | 1 | `customer-onboarding-assistant` |
| `dependabot_alert.created` | 1 | `vuln-triager` |
| `deployment_status` | 1 | `environment-promotion-tracker` |
| `deployment_status.success` | 12 | `ux-walkthrough-recorder`[uat], `functional-test-runner`[tdd], `exploratory-tester`[uat], `smoke-test-runner`, `frontend-perf-auditor`[uat], `dast-scanner`[uat], `prod-deploy-gate`[uat], `release-verifier`[prod], `instrumentation-verifier`[uat], `a11y-runtime-auditor`[uat], `keyboard-nav-tester`[uat], `change-approval-recorder`[prod] |
| `discussion.created` | 1 | `question-answerer` |
| `dsr.received` | 1 | `data-subject-request-handler` |
| `email` | 1 | `security-report-intake`[security@] |
| `exception.new_fingerprint` | 1 | `error-tracker-triager` |
| `experiment.ended` | 1 | `experiment-readout` |
| `feedback.received` | 2 | `intake-channel-bridge`, `feature-feedback-router` |
| `finding.emitted` | 1 | `signal-aggregator` |
| `healthcheck.unhealthy` | 1 | `incident-declarer` |
| `incident.resolved` | 1 | `postmortem-writer` |
| `issue_comment.created` | 1 | `human-decision-recorder`[approval keyword] |
| `issues.closed` | 2 | `item-closure-verifier`, `close-the-loop-notifier`[linked tickets] |
| `issues.cross_referenced` | 1 | `cross-repo-dependency-coordinator` |
| `issues.labeled` | 10 | `bug-reproducer`[bug], `feature-request-analyzer`[enhancement], `epic-decomposer`[epic], `question-answerer`[question], `hotfix-intake`[hotfix], `backlog-prioritizer`[priority inputs], `blocked-item-unblocker`[blocked], `flaky-test-fixer`[flaky], `doc-gap-detector`[question], `escaped-defect-analyzer`[bug, production] |
| `issues.opened` | 4 | `owner-request-intake`[author=owner], `work-item-classifier`, `duplicate-detector`, `bug-report-quality-coach`[bug, incomplete] |
| `okr.changed` | 1 | `roadmap-planner` |
| `package.version.published` | 1 | `dependency-update-proposer` |
| `pipeline.completed` | 1 | `data-quality-checker` |
| `pipeline.failed` | 1 | `data-pipeline-monitor` |
| `projects_v2_item.edited` | 13 | `lane-dispatcher`[status change], `acceptance-criteria-writer`[status=Conceptual Definition], `definition-of-ready-checker`[status change], `estimator`[status=Technical Design], `wip-limit-enforcer`[status change], `ux-flow-designer`[status=UX Design], `ux-test-plan-writer`[status=Test Design], `usability-heuristic-evaluator`[status=UX Testing], `technical-design-author`[status=Technical Design], `feature-implementer`[status=Development], `test-design-author`[status=Test Design], `functional-test-runner`[status=Functional Testing], `release-candidate-cutter`[status=Release Queue] |
| `pull_request.closed` | 7 | `definition-of-done-checker`[merged], `board-column-mover`[merged], `architecture-diagram-updater`[merged, structural], `bug-verification-agent`[merged, fixes #], `version-bumper`[merged], `docs-only-fast-path`[merged, docs-only], `user-guide-writer`[merged, user-facing] |
| `pull_request.opened` | 10 | `agent-eval-regression-runner`[skills/prompts], `work-item-linker`, `ui-consistency-auditor`[UI paths], `architecture-rule-enforcer`, `code-review-orchestrator`, `pr-description-writer`, `pr-size-guard`, `change-risk-scorer`, `protected-path-guard`, `contributor-license-checker`[external author] |
| `pull_request.ready_for_review` | 1 | `code-review-orchestrator` |
| `pull_request.synchronize` | 32 | `visual-regression-reviewer`[UI paths], `architecture-rule-enforcer`, `api-breaking-change-detector`[API paths], `merge-conflict-resolver`[mergeable=false], `code-review-orchestrator`, `protected-path-guard`, `llm-eval-runner`[LLM paths], `benchmark-runner`[hot paths], `bundle-size-watcher`[UI paths], `rate-limit-validator`[rate-limit config], `sast-scanner`, `secret-scanner`, `authz-matrix-auditor`[auth paths], `iac-security-scanner`[infra paths], `sca-scanner`[manifests], `new-dependency-gate`[new package], `license-compliance-checker`[manifests], `typosquat-malware-checker`[manifests], `action-pinning-auditor`[workflows], `observability-gap-finder`[handlers/external calls], `migration-reviewer`[migration scripts], `migration-numbering-guard`[scripts/Update], `pii-data-classifier`[schema], `metric-definition-keeper`[metrics], `doc-drift-checker`, `code-comment-documenter`, `diagram-renderer`[*.puml], `a11y-static-checker`[UI paths], `color-contrast-checker`[design tokens], `i18n-string-extractor`[UI paths], `pseudo-localization-runner`[UI paths], `cost-impact-reviewer`[infra paths] |
| `pull_request_review.submitted` | 4 | `human-decision-recorder`[human], `review-feedback-applier`[changes_requested], `bot-finding-triager`[bot], `merge-readiness-checker` |
| `pull_request_review_comment.created` | 1 | `review-feedback-applier` |
| `push` | 1 | `secret-scanner` |
| `push:main` | 16 | `agent-registry-keeper`[.claude/skills or manifests], `design-system-curator`[UI paths], `fitness-function-runner`, `branch-updater`, `sdk-client-generator`[API contract], `load-test-author`[API contract], `sast-scanner`, `sbom-generator`, `api-reference-generator`[API contract], `docs-site-publisher`[docs], `translation-generator`[resources], `policy-keeper`[CLAUDE.md, policies], `dev-environment-validator`[build files], `session-start-hook-maintainer`[dependencies], `api-sandbox-maintainer`[API contract], `revert-analyzer`[revert commit] |
| `release.candidate` | 3 | `load-test-runner`, `changelog-generator`, `release-notes-writer` |
| `release.published` | 9 | `feature-adoption-reviewer`[+30d], `supply-chain-attestation`, `sbom-generator`, `third-party-notice-writer`, `export-control-checker`, `release-comms-writer`, `changelog-page-publisher`, `social-post-drafter`, `demo-script-writer`[major] |
| `repository_advisory.reported` | 1 | `security-report-intake` |
| `research.artifact.added` | 1 | `research-repository-keeper` |
| `research.transcript.uploaded` | 1 | `customer-interview-synthesizer` |
| `resource.created` | 1 | `tagging-compliance-enforcer` |
| `secret.requested` | 1 | `secrets-store-maintainer` |
| `secret_scanning_alert.created` | 1 | `secret-leak-responder` |
| `slo.burn_rate` | 1 | `incident-declarer` |
| `sub_issues.changed` | 1 | `dependency-graph-resolver` |
| `support.ticket.created` | 2 | `intake-channel-bridge`, `support-ticket-triager` |
| `survey.closed` | 1 | `survey-analyzer` |
| `tracking_plan.changed` | 1 | `analytics-schema-maintainer` |
| `workflow_run.completed` | 7 | `crap-score-gate`, `flaky-test-detector`, `test-report-publisher`, `container-image-scanner`[image built], `ci-pipeline-watcher`, `artifact-publisher`[success, main], `deployment-orchestrator`[success, build] |

**Fan-out hot spots.** `pull_request.synchronize` and `pull_request.opened` wake the most agents: the review lenses plus about 25 path-filtered checkers. The design requires path filters, so a docs-only push wakes only the doc agents and `docs-only-fast-path`. `deployment_status.success[uat]` is the second-largest fan-out, because that is where UX, a11y, DAST, exploratory, instrumentation and perf verification all converge. It maps directly onto the repo's UX Testing column.

## 6. Timer calendar (cadence → agents)

| Cadence | Suggested cron (UTC) | # | Agents |
|---|---|---|---|
| 1m | `* * * * *` | 2 | `timer-scheduler`, `health-check-monitor` |
| 5m | `*/5 * * * *` | 1 | `dependency-health-watcher` |
| 15m | `*/15 * * * *` | 3 | `lane-stall-watchdog`, `log-anomaly-detector`, `support-sla-watcher` |
| hourly | `0 * * * *` | 4 | `agent-budget-governor`, `blocked-item-unblocker`, `ephemeral-env-reaper`, `runner-fleet-manager` |
| daily | `0 6 * * *` | 32 | `needs-info-follower`, `experiment-readout`, `backlog-prioritizer`, `flow-metrics-reporter`, `release-scope-tracker`, `fitness-function-runner`, `flaky-test-detector`, `llm-eval-runner`, `query-performance-analyzer`, `container-image-scanner`, `sca-scanner`, `dora-metrics-collector`, `drift-detector`, `certificate-expiry-watcher`, `backup-verifier`, `quota-limit-watcher`, `error-budget-tracker`, `schema-drift-detector`, `db-capacity-watcher`, `data-retention-enforcer`, `data-quality-checker`, `metric-anomaly-detector`, `sentiment-analyzer`, `compliance-evidence-collector`, `cloud-cost-reporter`, `cost-anomaly-detector`, `idle-resource-reaper`, `tagging-compliance-enforcer`, `finops-ai-spend-reporter`, `dev-environment-validator`, `backlog-generator-from-signals`, `agent-friction-miner` |
| weekday | `0 14 * * 1-5` | 1 | `release-candidate-cutter` |
| nightly | `0 2 * * *` | 1 | `cross-browser-runner` |
| weekly | `0 7 * * 1` | 65 | `agent-eval-regression-runner`, `agent-prompt-tuner`, `agent-permission-auditor`, `stale-issue-gardener`, `okr-progress-tracker`, `opportunity-scorer`, `market-signal-miner`, `feature-adoption-reviewer`, `backlog-groomer`, `user-journey-analytics-reviewer`, `tech-debt-registrar`, `static-analysis-triager`, `code-ownership-mapper`, `duplication-detector`, `complexity-trend-watcher`, `regression-suite-curator`, `coverage-gap-analyzer`, `mutation-tester`, `chaos-experiment-runner`, `load-test-runner`, `frontend-perf-auditor`, `cold-start-analyzer`, `dast-scanner`, `authz-matrix-auditor`, `waf-rule-tuner`, `dependency-update-proposer`, `license-compliance-checker`, `base-image-updater`, `action-pinning-auditor`, `ci-duration-optimizer`, `build-reproducibility-checker`, `release-planner`, `feature-flag-manager`, `container-runtime-tuner`, `os-patch-manager`, `network-exposure-auditor`, `toil-tracker`, `alert-noise-reducer`, `observability-gap-finder`, `db-maintenance-runner`, `db-restore-to-lower-env`, `weekly-metrics-digest`, `funnel-analyzer`, `doc-gap-detector`, `link-checker`, `churn-risk-detector`, `customer-health-reporter`, `faq-updater`, `a11y-runtime-auditor`, `screen-reader-tester`, `locale-format-tester`, `rightsizing-advisor`, `stakeholder-status-reporter`, `engineering-health-reporter`, `cross-repo-dependency-coordinator`, `milestone-risk-assessor`, `raid-log-keeper`, `human-gate-queue-balancer`, `build-warning-reducer`, `local-build-time-tracker`, `permission-prompt-reducer`, `recurring-failure-miner`, `rework-detector`, `goal-drift-detector`, `quality-trend-sentinel` |
| biweekly | `0 8 * * 1 (even ISO weeks)` | 2 | `sprint-planner`, `retrospective-facilitator` |
| monthly | `0 8 1 * *` | 35 | `roadmap-planner`, `competitive-scanner`, `design-system-curator`, `codebase-cartographer`, `dead-code-remover`, `stress-breakpoint-tester`, `soak-tester`, `capacity-planner`, `secret-rotation-runner`, `security-posture-reporter`, `eol-runtime-watcher`, `abandoned-package-detector`, `lockfile-drift-auditor`, `dashboard-maintainer`, `pii-data-classifier`, `cohort-retention-analyzer`, `customer-usage-reporter`, `telemetry-cost-optimizer`, `readme-maintainer`, `runbook-librarian`, `glossary-maintainer`, `support-macro-maintainer`, `cookie-consent-auditor`, `regulatory-change-watcher`, `budget-forecaster`, `unit-cost-calculator`, `sales-enablement-writer`, `seo-auditor`, `internal-digest-writer`, `capacity-allocator`, `agent-performance-reviewer`, `ai-worker-mix-manager`, `skill-library-curator`, `inner-loop-optimizer`, `idea-incubator` |
| quarterly | `0 9 1 1,4,7,10 *` | 21 | `product-vision-keeper`, `okr-drafter`, `persona-maintainer`, `pricing-packaging-analyst`, `tech-radar-curator`, `perf-budget-keeper`, `access-review-runner`, `security-guidance-writer`, `dependency-upgrade-planner`, `dr-drill-runner`, `golden-path-maintainer`, `slo-definer`, `onboarding-guide-maintainer`, `a11y-conformance-reporter`, `control-gap-assessor`, `ai-system-register-keeper`, `commitment-planner`, `saas-license-auditor`, `portfolio-balancer`, `quarterly-business-review-writer`, `owner-friction-surveyor` |
| annual | `0 9 15 1 *` | 1 | `pen-test-coordinator` |

**Scheduling notes.**
- Sub-hourly timers (1m/5m/15m) must be *cheap probes* (HTTP health, queue depth, SLA clocks) that emit events. They must not start LLM sessions unless the probe crosses a threshold. This keeps `agent-budget-governor` spend flat.
- The daily jobs are staggered by guild so they finish before the `backlog-prioritizer` run at the end of the batch. The observers (costs, drift, anomalies, flaky tests) go first, then `signal-aggregator` and `backlog-generator-from-signals`, then `backlog-prioritizer`, then `lane-dispatcher`. This ordering is the org's daily "stand-up".
- The weekly jobs cluster on Monday, which gives the org a weekly planning heartbeat: grooming, digests, retro inputs and the health scorecard.
- `release-candidate-cutter` runs on weekdays, matching `docs/release-cadence.md` ("regular releases go out on weekdays once UAT validation passes; hotfixes any time").

## 7. Handoff hubs and critical paths

**Most-handed-to agents (in-degree).** These are the org's load-bearing agents; they need the strongest evals, the highest concurrency, and a human-visible dashboard:

| Agent | Inbound handoffs |
|---|---|
| `human-escalation-router` | 11 |
| `code-review-orchestrator` | 11 |
| `backlog-generator-from-signals` | 8 |
| `review-feedback-applier` | 8 |
| `lane-dispatcher` | 7 |
| `agent-prompt-tuner` | 7 |
| `backlog-prioritizer` | 7 |
| `agent-registry-keeper` | 6 |
| `stakeholder-status-reporter` | 6 |
| `iac-implementer` | 6 |
| `fix-implementer` | 5 |
| `vuln-triager` | 5 |
| `board-column-mover` | 5 |
| `unit-test-author` | 5 |
| `protected-path-guard` | 5 |
| `opportunity-scorer` | 4 |
| `frontend-implementer` | 4 |
| `bug-reproducer` | 4 |
| `human-decision-recorder` | 3 |
| `work-item-classifier` | 3 |

**Critical path A: owner change request to production.**
`owner-request-intake` → `request-clarifier` → `problem-statement-writer` → `product-requirements-writer` → `acceptance-criteria-writer` → (`ux-flow-designer` ∥ `technical-design-author` → `threat-modeler`) → `test-design-author` → `definition-of-ready-checker` → `lane-dispatcher` → `feature-implementer` → `code-review-orchestrator` (+ lenses) → `review-feedback-applier` ↺ → `merge-readiness-checker` → `pr-merger` → `deployment-orchestrator` (TDD) → `functional-test-runner` → (UAT) `ux-walkthrough-recorder` → `usability-heuristic-evaluator` → `ux-acceptance-gate` [H] → `release-candidate-cutter` → `prod-deploy-gate` [H/policy] → `progressive-rollout-controller` → `release-verifier` → `release-notes-writer` → `release-comms-writer` [H] → `feature-adoption-reviewer` (+30d).

**Critical path B: production incident to prevention.**
`health-check-monitor` → `incident-declarer` → `incident-commander` → `incident-diagnostician` → `rollback-executor` | `hotfix-intake` → `fix-implementer` → `hotfix-release-runner` → `postmortem-writer` → `action-item-filer` → `backlog-prioritizer`, while `escaped-defect-analyzer` → `process-improvement-implementer` hardens the gate that missed the defect.

**Critical path C: self-generated backlog.**
Any observer → `finding.emitted` → `signal-aggregator` → `backlog-generator-from-signals` → `duplicate-detector` → `opportunity-scorer` → `agent-proposal-gate` [H or policy] → `backlog-prioritizer` → `lane-dispatcher`.

## 8. Human-gated and irreducibly human tasks

| Agent | Guild | Why it stays human-gated |
|---|---|---|
| `repo-onboarder` | G00 | Pointing the org at a repo grants it write power over someone's asset; consent and scope are the owner's decision. |
| `security-report-intake` | G01 | External reporters and embargoes involve trust relationships and coordinated disclosure. |
| `product-vision-keeper` | G02 | Vision is a statement of intent and values; agents can draft, only the accountable owner can commit. |
| `okr-drafter` | G02 | Outcomes commit the organization; they encode what the owner is willing to be measured on. |
| `roadmap-planner` | G02 | Sequencing encodes trade-offs among stakeholders who are not in the data. |
| `pricing-packaging-analyst` | G02 | Revenue, contracts and market positioning; legally and commercially binding. |
| `business-case-writer` | G02 | Spending decisions with real money and opportunity cost. |
| `sunset-planner` | G02 | Removing value from customers is a trust and contract decision. |
| `ux-acceptance-gate` | G04 | Taste and brand judgment; configurable to policy-auto once heuristic scores are trusted. |
| `survey-designer` | G04 | Contacting real users consumes their goodwill and has consent implications. |
| `tech-radar-curator` | G05 | Long-lived platform bets; sets constraints for years. |
| `protected-path-guard` | G07 | Repo rule: .octopus/, build scripts, pipelines, SDK versions require explicit approval (CLAUDE.md). This is the org's own safety perimeter. |
| `human-review-requester` | G07 | The point where a human is deliberately placed in the loop for high-risk diffs. |
| `perf-budget-keeper` | G09 | Budgets trade cost vs experience; business decision. |
| `secret-leak-responder` | G10 | Revocation can cause outages; purging history rewrites shared state. Automatic revocation is allowed only for keys known to be safe to revoke. |
| `access-review-runner` | G10 | Removing people's access affects humans and contracts. |
| `pen-test-coordinator` | G10 | Engaging external testers is contractual and legal (authorization to attack). |
| `security-advisory-publisher` | G10 | Public disclosure is irreversible and reputational. |
| `dependency-upgrade-planner` | G11 | Major-version migrations are multi-week investments. |
| `new-dependency-gate` | G11 | Repo rule (no new NuGet without approval); new code from strangers is the largest supply-chain risk. |
| `pipeline-as-code-maintainer` | G12 | Pipelines are the org's enforcement mechanism; agents must not be able to weaken their own gates. |
| `prod-deploy-gate` | G12 | Production changes affect real users; can be relaxed to policy-auto for low-risk scores once change-failure-rate is proven low. |
| `environment-provisioner` | G13 | Creates billable, externally reachable infrastructure. |
| `dns-domain-manager` | G13 | Domain/DNS changes are high-blast-radius and hard to undo quickly. |
| `slo-definer` | G14 | SLOs are a promise to users and set the error-budget policy that governs the whole org's speed. |
| `db-restore-to-lower-env` | G15 | Moves production data; privacy exposure. |
| `data-migration-runner` | G15 | Irreversible data mutation at scale. |
| `a11y-conformance-reporter` | G19 | VPAT/ACR is a legal attestation. |
| `translation-reviewer` | G19 | Legal and marketing text in other languages carries liability. |
| `privacy-impact-assessor` | G20 | DPIA sign-off is a legal accountability (DPO). |
| `policy-text-watcher` | G20 | Terms and privacy policies are contracts. |
| `legal-review-requester` | G20 | Legal advice must come from counsel. |
| `export-control-checker` | G20 | Regulatory classification with legal penalties. |
| `commitment-planner` | G21 | Multi-year financial commitments. |
| `release-comms-writer` | G22 | External voice of the company. |
| `deprecation-notice-writer` | G22 | Customer-contract implications. |
| `social-post-drafter` | G22 | Public, irreversible, brand-bearing. |
| `website-content-updater` | G22 | Public brand surface. |
| `capacity-allocator` | G23 | Choosing features vs debt vs ops is the core management trade-off. |
| `portfolio-balancer` | G23 | Cross-repo investment mix is strategy. |
| `agent-proposal-gate` | G25 | Self-generated work must not self-authorize; policy-auto only for enumerated low-risk classes (flaky fix, lint cleanup, doc drift, patch-level dependency bumps). |
| `idea-incubator` | G25 | New bets are strategy. |

### Irreducibly human (not just gated): what the owner actually does
1. **Intent and values.** What the product is for, who it serves, and what it will not do. Agents can mine signals, but they cannot supply purpose.
2. **Accountability that the law assigns to a person.** DPO sign-off, legal attestations (VPAT, SOC 2 management assertion), contracts, export classification, public security disclosures.
3. **Spending and commitments.** Budgets, reservations, vendor and pen-test engagements, pricing.
4. **Changes to the org's own guardrails.** Pipelines, protected paths, agent permissions, the kill switch and the autonomy policy itself. An organization that can relax its own gates has no gates.
5. **Irreversible external acts.** Public comms, deleting customer-facing features, production data migrations, DNS.
6. **Relationships.** Customer escalations beyond a threshold, security reporters, auditors, counsel.
7. **Taste arbitration.** When `conflict-arbiter` cannot resolve a trade-off with data (for example UX tone or naming), the owner decides once and `human-override-learner` encodes the decision so it is not asked again.

**Gate economics.** The target is ≤10% human-gated agents (currently 42/401 = 10%). The gates should also *fire rarely*: most gated agents run quarterly or on exceptional events. The human's steady-state load is dominated by `prod-deploy-gate`, `agent-proposal-gate`, `protected-path-guard` and `ux-acceptance-gate`. `human-gate-queue-balancer` batches those into one daily decision digest, and `human-override-learner` lowers their frequency over time by turning repeated approvals into policy.

## 9. Key positions and design decisions

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

## 10. Mapping onto bootcamp-palermo-workorders

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
