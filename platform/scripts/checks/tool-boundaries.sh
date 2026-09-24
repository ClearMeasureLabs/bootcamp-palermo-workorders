#!/usr/bin/env bash
# scripts/checks/tool-boundaries.sh
#
# One verb per tool (design ADR-D2): Codefresh builds; Octopus releases,
# promotes, approves, migrates and runs runbooks; Argo CD reconciles; GitHub
# Actions gates merges during the parallel run. This lint fails when a file lets
# a tool leave its lane (the R1-P §8 deny rules, extended by design §11.5).
#
# Usage
#   tool-boundaries.sh [--root DIR] [--app-repo DIR]
#       Static lint of the environment repo. With --app-repo, also scans
#       app:.codefresh/ and app:containers/ of the app repo checkout.
#   tool-boundaries.sh --audit-bot-commits [--root DIR] [--range REV_RANGE] [--bot-author REGEX]
#       Bot-path audit (design §6.2): fails when a commit on main made by the
#       platform-bots machine user changes anything other than the newTag lines
#       of gitops/workorders/envs/*/kustomization.yaml.
#
# Environment
#   PLATFORM_BOT_AUTHORS  Extended regex matched against "Name <email>" of each
#                         commit's author and committer: the identity of the
#                         Octopus Git credential's machine user [VERIFY the
#                         identity Octopus writes]. --bot-author overrides it.
#   AUDIT_DEPTH           First-parent commits audited when --range is absent
#                         (default 20). A violation keeps failing main builds
#                         until it leaves this window; the failing status is the alert.
#   CI                    "true" turns a missing bot identity into a failure.
#
# Exit codes: 0 pass, 1 violation, 2 usage error, 3 nothing to check.
# Paths absent from a partial tree are reported as SKIP, never as failures.
# Markdown files are never scanned: docs describe the forbidden features.

set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
APP_REPO=""
MODE="lint"
RANGE=""
BOT_AUTHOR="${PLATFORM_BOT_AUTHORS:-}"
DEPTH="${AUDIT_DEPTH:-20}"

FAILS=0
WARNS=0
CHECKED=0
SKIPS=0

# Matches a single or double quote in extended regexes.
Q="[\"']"

usage() {
  cat <<'EOF'
Usage:
  tool-boundaries.sh [--root DIR] [--app-repo DIR]
  tool-boundaries.sh --audit-bot-commits [--root DIR] [--range REV_RANGE] [--bot-author REGEX]
EOF
}

is_ci() {
  case "${CI:-}" in
    true | TRUE | True | 1 | yes) return 0 ;;
    *) return 1 ;;
  esac
}

while [ "$#" -gt 0 ]; do
  case "$1" in
    --root | --app-repo | --range | --bot-author)
      if [ "$#" -lt 2 ]; then
        echo "tool-boundaries: $1 needs a value" >&2
        exit 2
      fi
      case "$1" in
        --root) ROOT="$2" ;;
        --app-repo) APP_REPO="$2" ;;
        --range) RANGE="$2" ;;
        --bot-author) BOT_AUTHOR="$2" ;;
      esac
      shift 2
      ;;
    --audit-bot-commits)
      MODE="audit"
      shift
      ;;
    -h | --help)
      usage
      exit 0
      ;;
    *)
      echo "tool-boundaries: unknown argument '$1'" >&2
      usage >&2
      exit 2
      ;;
  esac
done

if [ ! -d "$ROOT" ]; then
  echo "tool-boundaries: root '$ROOT' not found" >&2
  exit 2
fi
ROOT="$(cd "$ROOT" && pwd)"
if [ -n "$APP_REPO" ]; then
  if [ ! -d "$APP_REPO" ]; then
    echo "tool-boundaries: app repo '$APP_REPO' not found" >&2
    exit 2
  fi
  APP_REPO="$(cd "$APP_REPO" && pwd)"
fi

say() {
  printf '%-4s %-5s %s\n' "$1" "$2" "$3"
}

# Prints the absolute paths that exist for each spec. A spec is an
# environment-repo path, or `app:<path>` inside --app-repo.
targets() {
  local spec base rel
  for spec in "$@"; do
    case "$spec" in
      app:*)
        [ -n "$APP_REPO" ] || continue
        base="$APP_REPO"
        rel="${spec#app:}"
        ;;
      *)
        base="$ROOT"
        rel="$spec"
        ;;
    esac
    if [ -e "$base/$rel" ]; then
      printf '%s\n' "$base/$rel"
    fi
  done
}

# Rewrites absolute paths in grep output to repo-relative ones.
relativize() {
  local -a args=(-e "s|^$ROOT/||")
  if [ -n "$APP_REPO" ]; then
    args+=(-e "s|^$APP_REPO/|app:|")
  fi
  sed "${args[@]}"
}

# Drops matches whose content is a comment (YAML, HCL, OCL, shell).
strip_comments() {
  awk '{ line = $0; sub(/^[^:]*:[0-9]+:/, "", line); if (line ~ /^[[:space:]]*(#|\/\/)/) next; print }'
}

# grep over the given absolute paths; prints file:line:content.
search() {
  local pattern="$1"
  shift
  grep -rnIE \
    --exclude-dir=.git --exclude-dir=.terraform --exclude-dir=node_modules \
    --exclude='*.md' \
    -e "$pattern" "$@" 2>/dev/null
}

report() {
  local id="$1" desc="$2" hits="$3"
  if [ -n "$hits" ]; then
    say FAIL "$id" "$desc"
    printf '%s\n' "$hits" | sed 's/^/        /'
    FAILS=$((FAILS + 1))
  else
    say PASS "$id" "$desc"
  fi
}

# rule ID DESCRIPTION PATTERN [--with-comments] -- SPEC...
rule() {
  local id="$1" desc="$2" pattern="$3"
  shift 3
  local comments="no"
  if [ "${1:-}" = "--with-comments" ]; then
    comments="yes"
    shift
  fi
  if [ "${1:-}" = "--" ]; then
    shift
  fi
  local -a paths=()
  local p
  while IFS= read -r p; do
    paths+=("$p")
  done < <(targets "$@")
  if [ "${#paths[@]}" -eq 0 ]; then
    say SKIP "$id" "$desc (absent: $*)"
    SKIPS=$((SKIPS + 1))
    return 0
  fi
  CHECKED=$((CHECKED + 1))
  local hits
  if [ "$comments" = "yes" ]; then
    hits="$(search "$pattern" "${paths[@]}" | relativize)"
  else
    hits="$(search "$pattern" "${paths[@]}" | strip_comments | relativize)"
  fi
  report "$id" "$desc" "$hits"
}

# TB09: Octopus scoping annotations belong only on the three named
# Applications (design §7.3, E5); no tenant annotation anywhere (ADR-C8).
check_octopus_annotations() {
  local id="TB09" desc="Octopus annotations only on Applications workorders-tdd, workorders-uat, workorders-prod; no tenant annotation"
  local -a paths=()
  local p
  while IFS= read -r p; do
    paths+=("$p")
  done < <(targets argocd gitops policies terraform octopus .octopus codefresh)
  if [ "${#paths[@]}" -eq 0 ]; then
    say SKIP "$id" "$desc (absent: argocd gitops policies terraform octopus .octopus codefresh)"
    SKIPS=$((SKIPS + 1))
    return 0
  fi
  CHECKED=$((CHECKED + 1))
  local allowed="argocd/clusters/nonprod/apps/workorders-tdd.yaml argocd/clusters/nonprod/apps/workorders-uat.yaml argocd/clusters/prod/apps/workorders-prod.yaml"
  local hits="" f
  while IFS= read -r f; do
    [ -n "$f" ] || continue
    case " $allowed " in
      *" $f "*) ;;
      *) hits+="$f: carries argo.octopus.com/* but is not a named-environment Application"$'\n' ;;
    esac
  done < <(search 'argo\.octopus\.com/' "${paths[@]}" | strip_comments | relativize | cut -d: -f1 | sort -u)
  local tenant
  tenant="$(search 'argo\.octopus\.com/tenant' "${paths[@]}" | strip_comments | relativize)"
  if [ -n "$tenant" ]; then
    hits+="$tenant"$'\n'
  fi
  report "$id" "$desc" "$(printf '%s' "$hits" | sed '/^$/d')"
}

# TB13c: the stored variable sets are included in no project (design §5.3, R5).
check_library_sets() {
  local id="TB13c" desc="Stored variable sets 'Azure Runtime Provisioning' and 'GitHub AISF Sample Apps' are included in no project"
  local -a files=()
  local f
  while IFS= read -r f; do
    files+=("$f")
  done < <(targets octopus .octopus | while IFS= read -r d; do
    find "$d" -type f \( -name '*.tf' -o -name '*.ocl' \) -not -path '*/.terraform/*' 2>/dev/null
  done)
  if [ "${#files[@]}" -eq 0 ]; then
    say SKIP "$id" "$desc (absent: octopus .octopus)"
    SKIPS=$((SKIPS + 1))
    return 0
  fi
  CHECKED=$((CHECKED + 1))
  local hits
  hits="$(awk '
    /included_library_variable_sets|included_variable_sets|IncludedLibraryVariableSet/ { collecting = 1; buf = ""; start = FNR }
    collecting {
      buf = buf " " $0
      if ($0 ~ /\]/ || $0 ~ /\)/) {
        lower = tolower(buf)
        if (lower ~ /runtime[_ -]?provisioning|aisf/) print FILENAME ":" start ":" buf
        collecting = 0
      }
    }' "${files[@]}" | relativize)"
  report "$id" "$desc" "$hits"
}

# TB15: admission never rewrites desired state (ADR-D11: no mutateDigest).
check_mutate_digest() {
  local id="TB15" desc="Kyverno image verification does not mutate image references (mutateDigest)"
  local -a paths=()
  local p
  while IFS= read -r p; do
    paths+=("$p")
  done < <(targets policies)
  if [ "${#paths[@]}" -eq 0 ]; then
    say SKIP "$id" "$desc (absent: policies)"
    SKIPS=$((SKIPS + 1))
    return 0
  fi
  CHECKED=$((CHECKED + 1))
  local hits
  hits="$(search 'mutateDigest:[[:space:]]*true' "${paths[@]}" | strip_comments | relativize)"
  report "$id" "$desc" "$hits"
  # The field defaults to true for image verification [VERIFY for
  # ImageValidatingPolicy], so a verifying policy should set false explicitly.
  local f
  while IFS= read -r f; do
    [ -n "$f" ] || continue
    if ! grep -qE 'mutateDigest:[[:space:]]*false' "$f"; then
      say WARN "$id" "$(printf '%s' "$f" | relativize): verifies images without an explicit 'mutateDigest: false'"
      WARNS=$((WARNS + 1))
    fi
  done < <(grep -rlIE --exclude='*.md' 'kind:[[:space:]]*ImageValidatingPolicy|verifyImages:' "${paths[@]}" 2>/dev/null)
}

run_lint() {
  echo "tool-boundaries: root=$ROOT${APP_REPO:+ app-repo=$APP_REPO}"

  # Codefresh builds.
  rule TB01 "Codefresh builds: no deploy, approval, helm or launch-composition steps" \
    "type:[[:space:]]*${Q}?(deploy|approval|helm|launch-composition)${Q}?([[:space:]]|#|\$)" \
    -- codefresh app:.codefresh
  rule TB02 "Codefresh never reaches app clusters: no argocd, kubectl, helm install/upgrade or az aks credentials" \
    "(^|[^[:alnum:]_./-])(argocd[[:space:]]+(app|appset|proj|cluster|repo|login|account|admin)[[:space:]]|kubectl[[:space:]]+(apply|create|replace|patch|set|delete|edit|scale|rollout|annotate|label|exec|cp|port-forward|get|logs|describe|config)([[:space:]]|\$)|helm[[:space:]]+(install|upgrade|rollback|uninstall)([[:space:]]|\$)|az[[:space:]]+aks[[:space:]]+(get-credentials|command))|type:[[:space:]]*${Q}?[[:alnum:]_/-]*argo-?cd[[:alnum:]_/-]*" \
    -- codefresh app:.codefresh
  rule TB03 "Codefresh GitOps Runtime and Promotions stay off" \
    "apiVersion:[[:space:]]*${Q}?codefresh\\.io/|gitops-runtime|kind:[[:space:]]*${Q}?(PromotionFlow|PromotionPolicy|PromotionTemplate|Product)${Q}?([[:space:]]|\$)" \
    -- argocd gitops policies terraform octopus .octopus codefresh app:.codefresh

  # Argo CD reconciles; Octopus is the only image-tag writer and holds the calendar.
  rule TB04 "Octopus is the only image-tag writer: no Argo CD Image Updater" \
    "argocd-image-updater|image-updater\\.argoproj\\.io|kind:[[:space:]]*${Q}?ImageUpdater" \
    -- argocd gitops policies terraform
  rule TB05 "Freezes live in Octopus: no Argo CD syncWindows" \
    "syncWindows" \
    -- argocd gitops terraform
  rule TB06 "No floating 'latest' tag in desired state, deployment config or pipelines" \
    "(:latest([[:space:]\"'@]|\$)|(newTag|tag|imageTag):[[:space:]]*${Q}?latest${Q}?([[:space:]]|\$))" \
    -- gitops argocd .octopus octopus terraform codefresh app:.codefresh app:containers

  # Octopus releases and migrates; Argo CD applies Kubernetes state.
  rule TB07 "Octopus never applies Kubernetes state: no kubectl apply/set image/patch, Helm or Kubernetes deploy steps, no argocd app sync" \
    "kubectl[[:space:]]+(apply|set[[:space:]]+image|patch|create|replace|edit|scale)([[:space:]]|\$)|helm[[:space:]]+(install|upgrade)([[:space:]]|\$)|Octopus\\.(KubernetesDeploy[[:alnum:]]*|HelmChartUpgrade|Kustomize|KubernetesRunScript)|argocd[[:space:]]+app[[:space:]]+(sync|rollback|set|patch|delete)" \
    -- .octopus octopus

  # Contributor cannot assign roles, lock or assign policy (ADR-D10, E36).
  rule TB08 "Environment layer has no role assignments, role definitions, locks or policy assignments" \
    "azurerm_role_assignment|azurerm_role_definition|azurerm_management_lock|azurerm_[a-z_]*policy_assignment|azuread_app_role_assignment|azuread_directory_role_assignment" \
    --with-comments -- terraform/environment

  check_octopus_annotations

  rule TB10 "Trigger sync stays off in the Argo CD step (ADR-D4) [VERIFY property name]" \
    "[Tt]rigger[._ -]?[Ss]ync[[:alnum:]._]*${Q}?[[:space:]]*[=:][[:space:]]*${Q}?[Tt]rue" \
    -- .octopus
  rule TB11 "Codefresh is the only release creator: no feed or built-in release triggers" \
    "octopusdeploy_(external_feed_create_release_trigger|built_in_trigger)|auto_create_release[[:space:]]*=[[:space:]]*true" \
    -- octopus .octopus
  rule TB12 "No Octopus deployment targets on app clusters (Kubernetes workers only)" \
    "octopusdeploy_kubernetes_(agent_deployment_target|cluster_deployment_target)" \
    -- octopus terraform

  # Stored credentials stay in their lane (ADR-C10, §5.3, R5).
  rule TB13a "The deployment project never uses the stored Azure Runtime Provisioner" \
    "azure-runtime-provisioner|Azure Runtime Provisioner" \
    -- .octopus/workorders
  rule TB13b "Stored Codefresh contexts azure-runtime-provisioner and github-aisf-sample-apps-token stay unattached" \
    "azure-runtime-provisioner|github-aisf-sample-apps-token" \
    -- codefresh app:.codefresh
  check_library_sets

  rule TB14 "No Octopus API keys in pipelines (OIDC only, ADR-D8)" \
    "OCTOPUS_API_KEY|OCTO_API_KEY|X-Octopus-ApiKey|--api-?[Kk]ey([[:space:]=]|\$)|API-[A-Z0-9]{16,}" \
    -- codefresh app:.codefresh

  check_mutate_digest

  rule TB16 "Codefresh reads repositories and posts statuses; it never commits or pushes" \
    "git[[:space:]]+(push|commit)([[:space:]]|\$)|type:[[:space:]]*${Q}?git-commit" \
    -- codefresh app:.codefresh

  echo "tool-boundaries: $CHECKED rules checked, $FAILS failed, $WARNS warnings, $SKIPS skipped"
  if [ "$FAILS" -gt 0 ]; then
    return 1
  fi
  if [ "$CHECKED" -eq 0 ]; then
    return 3
  fi
  return 0
}

# Bot-path audit (design §6.2).
run_audit() {
  local id="AUDIT"
  if ! command -v git >/dev/null 2>&1; then
    say FAIL "$id" "git not found"
    return 1
  fi
  local top
  if ! top="$(git -C "$ROOT" rev-parse --show-toplevel 2>/dev/null)"; then
    say SKIP "$id" "not a git work tree: $ROOT"
    return 3
  fi
  if [ -z "$BOT_AUTHOR" ]; then
    if is_ci; then
      say FAIL "$id" "PLATFORM_BOT_AUTHORS is not set, so the bot-path audit cannot run (CI=true)"
      return 1
    fi
    say SKIP "$id" "PLATFORM_BOT_AUTHORS is not set (fails when CI=true)"
    return 3
  fi
  case "$DEPTH" in
    '' | *[!0-9]*)
      echo "tool-boundaries: AUDIT_DEPTH must be a number" >&2
      return 2
      ;;
  esac

  # "" in the environment repo; "platform/" in the staging checkout.
  local prefix pin_re
  prefix="$(git -C "$ROOT" rev-parse --show-prefix)"
  pin_re="^${prefix//./\\.}gitops/workorders/envs/[^/]+/kustomization\\.yaml\$"
  local newtag_re='^[+-][[:space:]]*newTag:[[:space:]]*"?[0-9A-Za-z][0-9A-Za-z.+_-]*"?[[:space:]]*(#.*)?$'

  local -a revs=()
  local rev_out
  if [ -n "$RANGE" ]; then
    if ! rev_out="$(git -C "$top" rev-list --first-parent --no-merges "$RANGE" 2>&1)"; then
      say FAIL "$id" "cannot list commits for range '$RANGE': $rev_out"
      return 1
    fi
  else
    if ! rev_out="$(git -C "$top" rev-list --first-parent --no-merges --max-count="$DEPTH" HEAD 2>&1)"; then
      say FAIL "$id" "cannot list commits: $rev_out"
      return 1
    fi
  fi
  if [ -n "$rev_out" ]; then
    mapfile -t revs <<<"$rev_out"
  fi
  if [ "$(git -C "$top" rev-parse --is-shallow-repository 2>/dev/null)" = "true" ]; then
    say WARN "$id" "shallow clone: only the fetched history is audited"
  fi

  local total=0 bots=0 bad_commits=0 c ident f l
  local findings=""
  for c in "${revs[@]}"; do
    [ -n "$c" ] || continue
    total=$((total + 1))
    ident="$(git -C "$top" show -s --format='%an <%ae>%n%cn <%ce>' "$c")"
    if ! grep -Eq -- "$BOT_AUTHOR" <<<"$ident"; then
      continue
    fi
    bots=$((bots + 1))
    local bad=""
    while IFS= read -r f; do
      [ -n "$f" ] || continue
      if ! grep -Eq -- "$pin_re" <<<"$f"; then
        bad+="  changes $f (only environment pin files are allowed)"$'\n'
        continue
      fi
      while IFS= read -r l; do
        case "$l" in
          '+++'* | '---'*) continue ;;
        esac
        if ! grep -Eq -- "$newtag_re" <<<"$l"; then
          bad+="  $f: changes a line other than newTag: $l"$'\n'
        elif grep -Eq -- '^\+.*newTag:[[:space:]]*"?latest' <<<"$l"; then
          bad+="  $f: writes the floating tag 'latest'"$'\n'
        fi
      done < <(git -C "$top" show --format= --unified=0 --no-color --no-ext-diff "$c" -- "$f" | grep -E '^[+-]')
    done < <(git -C "$top" diff-tree --root --no-commit-id --name-only -r --no-renames "$c")
    if [ -n "$bad" ]; then
      bad_commits=$((bad_commits + 1))
      findings+="$(git -C "$top" show -s --format='%h %an: %s' "$c")"$'\n'"$bad"
    fi
  done

  if [ "$bad_commits" -gt 0 ]; then
    say FAIL "$id" "$bad_commits of $bots platform-bots commits (of $total audited) change more than an environment pin"
    printf '%s' "$findings" | sed 's/^/        /'
    return 1
  fi
  say PASS "$id" "$total commits audited, $bots by platform-bots, all limited to newTag lines of the pin files"
  return 0
}

case "$MODE" in
  lint) run_lint ;;
  audit) run_audit ;;
esac
exit $?
