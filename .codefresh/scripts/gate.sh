#!/usr/bin/env bash
# Aggregates the gate step results with the semantics of GitHub Actions
# `build-result` (app:docs/ci-single-gate.md, contract §7.7):
#   - CODE_CHANGED=false (a docs-only change set): pass without inspecting the gates.
#   - Otherwise every required gate must report "success". A gate that failed,
#     was skipped, was terminated or never reported fails the build.
#   - Advisory gates (security_scan) are reported but never fail the build.
#
# Usage: gate.sh [--advisory <gate>]... <gate>...
#   The result of each gate is read from the environment variable GATE_<gate>,
#   which the pipeline sets to ${{steps.<gate>.result}}. An unresolved
#   placeholder counts as "not reported" [VERIFY result vocabulary: success,
#   failure/error, skipped, terminated].
#
# Writes a Markdown summary to stdout and to $GATE_SUMMARY_FILE
# (default: ${CF_VOLUME_PATH}/reports/gate-summary.md, or ./gate-summary.md locally).
set -euo pipefail

die() {
  printf 'gate.sh: %s\n' "$1" >&2
  exit 2
}

advisory=" "
gates=()

while [ "$#" -gt 0 ]; do
  case "$1" in
    --advisory)
      [ "$#" -ge 2 ] || die "--advisory needs a gate name"
      advisory="${advisory}$2 "
      shift 2
      ;;
    -*)
      die "unknown option: $1"
      ;;
    *)
      gates+=("$1")
      shift
      ;;
  esac
done

[ "${#gates[@]}" -gt 0 ] || die "no gates given"

if [ -n "${CF_VOLUME_PATH:-}" ]; then
  default_summary="${CF_VOLUME_PATH}/reports/gate-summary.md"
else
  default_summary="./gate-summary.md"
fi
summary_file="${GATE_SUMMARY_FILE:-$default_summary}"
mkdir -p "$(dirname "$summary_file")"

code_changed="${CODE_CHANGED:-}"
failed=0

{
  printf '## Codefresh gate (%s)\n\n' "${CF_PIPELINE_NAME:-local}"
  printf -- '- Version: `%s`\n' "${VERSION:-unknown}"
  printf -- '- Code changed: `%s`\n\n' "${code_changed:-not reported}"

  if [ "$code_changed" = "false" ]; then
    printf 'Docs-only change set: gates skipped, result **pass**.\n'
  else
    printf '| Gate | Result | Required |\n|---|---|---|\n'
    for gate in "${gates[@]}"; do
      case "$gate" in
        *[!A-Za-z0-9_]*) die "invalid gate name: $gate" ;;
      esac
      var="GATE_${gate}"
      result="${!var:-not reported}"
      case "$result" in
        *'${{'*) result="not reported" ;;
      esac
      case "$advisory" in
        *" $gate "*) required="advisory" ;;
        *) required="yes" ;;
      esac
      printf '| %s | %s | %s |\n' "$gate" "$result" "$required"
      if [ "$required" = "yes" ] && [ "$result" != "success" ]; then
        failed=1
      fi
    done
    printf '\n'
    if [ "$failed" -ne 0 ]; then
      printf 'Result: **fail**. A required gate did not succeed.\n'
    else
      printf 'Result: **pass**. All required gates succeeded.\n'
    fi
  fi
} | tee "$summary_file"

# The loop ran inside the pipeline's subshell; re-evaluate the verdict here.
if [ "$code_changed" = "false" ]; then
  exit 0
fi
for gate in "${gates[@]}"; do
  case "$advisory" in
    *" $gate "*) continue ;;
  esac
  var="GATE_${gate}"
  if [ "${!var:-}" != "success" ]; then
    exit 1
  fi
done
exit 0
