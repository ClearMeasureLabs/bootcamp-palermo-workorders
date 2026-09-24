# policies/octopus/prod-deployment-guardrails.rego
#
# STATUS: INACTIVE. Octopus Platform Hub is Deferred (ADR-C9): it is an Enterprise feature (E25)
# and its permissions can only be assigned to system teams (E24). Nothing loads this file today.
# It is staged so that activation is a copy, not a design task. Until then, the same guardrails
# come from pull-request review of .octopus/**, the manual interventions, the sod-guard step, the
# prod-weekend-freeze and Kyverno.
#
# What it enforces, for deployments of project `workorders` to environment `prod` only:
#   1. The step `prod-go-no-go` (manual intervention, team Prod Approvers) is present, enabled
#      and not skipped.
#   2. The release was created from refs/heads/main (channel Default and Hotfix rule, §7.2).
# Runbook runs are out of scope: `Release` is absent for them.
#
# Input fields used (Octopus policy input schema, checked 2026-09-24,
# https://octopus.com/docs/platform-hub/policies/schema): Environment.Slug, Project.Slug,
# Steps[].{Id, Slug, ActionType, Enabled}, SkippedSteps[], Release.GitRef, Runbook.
#
# Activation (https://octopus.com/docs/platform-hub/policies/examples, "Writing policies as OCL
# files"): Platform Hub keeps one OCL file per policy whose file name and Rego package equal the
# policy slug, which cannot contain dashes. Create policies/prod_deployment_guardrails.ocl in the
# Platform Hub repository:
#   name             = "Prod deployment guardrails"
#   description      = "Prod deployments need an unskipped prod-go-no-go and a release from main."
#   violation_reason = "Prod deployments need the prod-go-no-go approval and a release from main."
#   violation_action = "warn"            # "block" after the preview below
#   scope      { rego = <<-EOT ... EOT } # the package line plus the SCOPE section below
#   conditions { rego = <<-EOT ... EOT } # the package line plus the CONDITIONS section below
# Preview it against past deployments with the policy editor's Evaluate button, run it with
# "warn" for two prod releases, then set violation_action and violation_action_setting below to
# "block".
#
# Local check (OPA 1.x, Rego v1): opa check prod-deployment-guardrails.rego
package prod_deployment_guardrails

# "warn" while previewing; "block" once activated for real.
violation_action_setting := "warn"

# ---------------------------------------------------------------- SCOPE ---------
default evaluate := false

evaluate if {
	not input.Runbook
	input.Project.Slug == "workorders"
	input.Environment.Slug == "prod"
}

# ----------------------------------------------------------- CONDITIONS ---------
default result := {
	"allowed": false,
	"action": "warn",
	"reason": "prod-deployment-guardrails could not evaluate this deployment.",
}

result := {"allowed": true} if {
	count(violations) == 0
}

result := {
	"allowed": false,
	"action": violation_action_setting,
	"reason": concat(" ", sort(violations)),
} if {
	count(violations) > 0
}

violations contains "The prod-go-no-go manual intervention is missing, disabled or skipped." if {
	not go_no_go_active
}

violations contains "The release was not created from refs/heads/main." if {
	not release_from_main
}

go_no_go_active if {
	some step in input.Steps
	step.Slug == "prod-go-no-go"
	step.ActionType == "Octopus.Manual"
	step.Enabled == true
	not step.Id in input.SkippedSteps
}

release_from_main if {
	input.Release.GitRef == "refs/heads/main"
}
