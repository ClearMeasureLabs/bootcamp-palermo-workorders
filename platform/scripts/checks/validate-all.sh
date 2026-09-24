#!/usr/bin/env bash
# scripts/checks/validate-all.sh
#
# One entry point for every environment-repo check (design §11.5). Called by
# Codefresh `platform-env/env-checks` (codefresh/pipelines/env-checks.yml) with
# the sub-commands yaml, kustomize, kubeconform, terraform, boundaries,
# consistency and secrets, and by people locally with `all`.
#
# Usage
#   validate-all.sh [--root DIR] [--app-repo DIR] <sub-command>
#
# Sub-commands
#   all          Every sub-command below, in order; exits non-zero if any fails.
#   yaml         yamllint with .yamllint.yaml over every *.yaml and *.yml file.
#   kustomize    `kustomize build` for gitops/workorders/envs/*, gitops/workorders/previews
#                and policies/kyverno/overlays/*; renders into $RENDER_DIR.
#   kubeconform  Schema-validates the rendered overlays and argocd/clusters/**, argocd/optional/**.
#   terraform    `terraform fmt -check -recursive` on terraform/ and octopus/terraform/;
#                with TF_VALIDATE=true also `init -backend=false` and `validate` in a temporary copy.
#   mermaid      Parses every ```mermaid block in Markdown files.
#   boundaries   scripts/checks/tool-boundaries.sh; on the main branch also --audit-bot-commits.
#   consistency  scripts/checks/consistency.sh against contracts/platform-contracts.yaml.
#   secrets      gitleaks over the tree with .gitleaks.toml when present.
#
# Missing tools: skipped with a warning locally; a failure when CI=true.
# The env-checks pipeline sets CI=true explicitly [VERIFY whether Codefresh sets it].
# Absent content (a package not staged yet) is skipped, never failed.
#
# Environment
#   CI                   "true" makes a missing tool fail.
#   RENDER_DIR           Rendered-overlay directory shared by kustomize and kubeconform
#                        (default: $CF_VOLUME_PATH/platform-render in Codefresh [VERIFY],
#                        else ${TMPDIR:-/tmp}/platform-render).
#   YAMLLINT, KUSTOMIZE, KUBECONFORM, TERRAFORM, GITLEAKS, NODE, MMDC
#                        Tool paths overriding PATH lookup.
#   MERMAID_VALIDATOR    Node script that parses the mermaid blocks of one Markdown file
#                        (exit 0 when valid); otherwise `mmdc` (mermaid-cli) is used.
#   KUBECONFORM_SCHEMA_LOCATIONS
#                        Space-separated -schema-location values (default: the built-in
#                        Kubernetes schemas plus the datreeio CRDs catalog).
#   KUBERNETES_VERSION   Passed to kubeconform -kubernetes-version when set.
#   TF_VALIDATE          "true" also runs terraform init/validate (downloads providers).
#   PLATFORM_MAIN_BRANCH Branch that gets the bot-path audit (default: main).
#   PLATFORM_BOT_AUTHORS Identity regex of the platform-bots machine user (see tool-boundaries.sh).
#   APP_REPO_DIR         App repo checkout for --app-repo (auto-detected in the staging layout).
#
# Exit codes: 0 pass (skips allowed), 1 failure, 2 usage error.

set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="${PLATFORM_ROOT:-$(cd "$SCRIPT_DIR/../.." && pwd)}"
APP_REPO="${APP_REPO_DIR:-}"
CRD_CATALOG='https://raw.githubusercontent.com/datreeio/CRDs-catalog/main/{{.Group}}/{{.ResourceKind}}_{{.ResourceAPIVersion}}.json'
SUBCOMMANDS="yaml kustomize kubeconform terraform mermaid boundaries consistency secrets"

usage() {
  cat <<EOF
Usage: validate-all.sh [--root DIR] [--app-repo DIR] <all|${SUBCOMMANDS// /|}>
EOF
}

is_ci() {
  case "${CI:-}" in
    true | TRUE | True | 1 | yes) return 0 ;;
    *) return 1 ;;
  esac
}

log() { printf '%s\n' "$*"; }
warn() { printf 'WARN %s\n' "$*"; }
err() { printf 'FAIL %s\n' "$*"; }

CMD=""
while [ "$#" -gt 0 ]; do
  case "$1" in
    --root | --app-repo)
      if [ "$#" -lt 2 ]; then
        echo "validate-all: $1 needs a value" >&2
        exit 2
      fi
      if [ "$1" = "--root" ]; then ROOT="$2"; else APP_REPO="$2"; fi
      shift 2
      ;;
    -h | --help | help)
      usage
      exit 0
      ;;
    -*)
      echo "validate-all: unknown option '$1'" >&2
      usage >&2
      exit 2
      ;;
    *)
      if [ -n "$CMD" ]; then
        echo "validate-all: one sub-command at a time" >&2
        exit 2
      fi
      CMD="$1"
      shift
      ;;
  esac
done
if [ -z "$CMD" ]; then
  usage >&2
  exit 2
fi
if [ ! -d "$ROOT" ]; then
  echo "validate-all: root '$ROOT' not found" >&2
  exit 2
fi
ROOT="$(cd "$ROOT" && pwd)"

# Staging layout: the environment repo is staged at <app checkout>/platform.
if [ -z "$APP_REPO" ] && [ "$(basename "$ROOT")" = "platform" ] && [ -d "$ROOT/../.codefresh" ]; then
  APP_REPO="$(cd "$ROOT/.." && pwd)"
fi

RENDER_DIR="${RENDER_DIR:-${CF_VOLUME_PATH:-${TMPDIR:-/tmp}}/platform-render}"

# tool NAME -> absolute path via $NAME override (upper case) or PATH; empty when missing.
tool() {
  local name="$1" var override
  var="$(printf '%s' "$name" | tr '[:lower:]-' '[:upper:]_')"
  override="${!var:-}"
  if [ -n "$override" ]; then
    command -v "$override" 2>/dev/null || true
    return 0
  fi
  command -v "$name" 2>/dev/null || true
}

# missing SUB TOOL -> 1 in CI, 3 locally.
missing() {
  if is_ci; then
    err "$1: required tool '$2' not found (CI=true)"
    return 1
  fi
  warn "$1: tool '$2' not found; skipped locally (fails when CI=true)"
  return 3
}

# Finds files under ROOT, skipping VCS, Terraform caches and dependencies.
find_files() {
  find "$ROOT" \( -name .git -o -name .terraform -o -name node_modules \) -prune -o -type f "$@" -print 2>/dev/null | sort
}

# ---------------------------------------------------------------- yaml
cmd_yaml() {
  local y
  y="$(tool yamllint)"
  [ -n "$y" ] || { missing yaml yamllint; return $?; }
  local -a files=()
  mapfile -t files < <(find_files \( -name '*.yaml' -o -name '*.yml' \))
  if [ "${#files[@]}" -eq 0 ]; then
    log "SKIP yaml: no YAML files"
    return 3
  fi
  local -a cfg=()
  if [ -f "$ROOT/.yamllint.yaml" ]; then
    cfg=(-c "$ROOT/.yamllint.yaml")
  fi
  log "yaml: yamllint ${#files[@]} files"
  if "$y" "${cfg[@]}" -f parsable "${files[@]}"; then
    log "PASS yaml"
    return 0
  fi
  err "yaml: yamllint reported errors"
  return 1
}

# ---------------------------------------------------------------- kustomize
kustomize_targets() {
  local d
  for d in "$ROOT"/gitops/workorders/envs/*/ "$ROOT"/gitops/workorders/previews/ "$ROOT"/policies/kyverno/overlays/*/; do
    if [ -f "${d}kustomization.yaml" ]; then
      printf '%s\n' "${d%/}"
    fi
  done
}

# render -> 0 all rendered, 1 a build failed, 3 nothing to render.
render() {
  local k="$1" t name rc=0 count=0
  rm -rf "$RENDER_DIR"
  mkdir -p "$RENDER_DIR"
  while IFS= read -r t; do
    [ -n "$t" ] || continue
    count=$((count + 1))
    name="$(printf '%s' "${t#"$ROOT"/}" | tr '/' '_')"
    if "$k" build "$t" >"$RENDER_DIR/$name.yaml" 2>"$RENDER_DIR/$name.err"; then
      log "PASS kustomize build ${t#"$ROOT"/}"
      rm -f "$RENDER_DIR/$name.err"
    else
      err "kustomize build ${t#"$ROOT"/}: $(tail -n 3 "$RENDER_DIR/$name.err" | tr '\n' ' ')"
      rm -f "$RENDER_DIR/$name.yaml"
      rc=1
    fi
  done < <(kustomize_targets)
  if [ "$count" -eq 0 ]; then
    return 3
  fi
  return "$rc"
}

cmd_kustomize() {
  local k
  k="$(tool kustomize)"
  [ -n "$k" ] || { missing kustomize kustomize; return $?; }
  render "$k"
  local rc=$?
  if [ "$rc" -eq 3 ]; then
    log "SKIP kustomize: no overlays staged"
  fi
  return "$rc"
}

# ---------------------------------------------------------------- kubeconform
# §7.1 placeholders are not valid DNS or Kubernetes names, so schema validation
# runs on copies where each <placeholder> becomes a valid stand-in
# (<tdd-hostname> -> ph-tdd-hostname, <acr-name>.azurecr.io -> ph-acr-name.azurecr.io).
standins() {
  awk '{
    line = $0; out = ""
    while (match(line, /<[A-Za-z0-9{][^<>"]*>/)) {
      tok = tolower(substr(line, RSTART + 1, RLENGTH - 2))
      gsub(/[^a-z0-9.-]/, "-", tok)
      out = out substr(line, 1, RSTART - 1) "ph-" tok
      line = substr(line, RSTART + RLENGTH)
    }
    print out line
  }' "$1"
}

cmd_kubeconform() {
  local kc k rc=0
  kc="$(tool kubeconform)"
  [ -n "$kc" ] || { missing kubeconform kubeconform; return $?; }
  k="$(tool kustomize)"
  if [ -n "$k" ]; then
    if ! render "$k"; then
      # 3 (nothing to render) is fine; 1 means a build failed.
      compgen -G "$RENDER_DIR/*.err" >/dev/null && rc=1
    fi
  elif compgen -G "$RENDER_DIR/*.yaml" >/dev/null; then
    warn "kubeconform: kustomize not found; using overlays rendered earlier in $RENDER_DIR"
  elif [ -n "$(kustomize_targets)" ]; then
    if is_ci; then
      err "kubeconform: kustomize is required to render the overlays (CI=true)"
      return 1
    fi
    warn "kubeconform: kustomize not found; overlays not rendered, raw manifests only"
  fi
  local -a files=() raw=()
  if compgen -G "$RENDER_DIR/*.yaml" >/dev/null; then
    files+=("$RENDER_DIR"/*.yaml)
  fi
  # Raw manifests. Helm values (argocd/bootstrap) are not manifests.
  mapfile -t raw < <(find_files \( -path "$ROOT/argocd/clusters/*" -o -path "$ROOT/argocd/optional/*" \) \( -name '*.yaml' -o -name '*.yml' \))
  files+=("${raw[@]}")
  if [ "${#files[@]}" -eq 0 ]; then
    log "SKIP kubeconform: no manifests staged"
    return 3
  fi
  local -a locations=() args=()
  local loc
  if [ -n "${KUBECONFORM_SCHEMA_LOCATIONS:-}" ]; then
    read -r -a locations <<<"$KUBECONFORM_SCHEMA_LOCATIONS"
  else
    locations=(default "$CRD_CATALOG")
  fi
  for loc in "${locations[@]}"; do
    args+=(-schema-location "$loc")
  done
  if [ -n "${KUBERNETES_VERSION:-}" ]; then
    args+=(-kubernetes-version "$KUBERNETES_VERSION")
  fi
  local work f name
  work="$(mktemp -d)"
  for f in "${files[@]}"; do
    name="${f#"$RENDER_DIR"/}"
    name="${name#"$ROOT"/}"
    standins "$f" >"$work/${name//\//__}"
  done
  log "kubeconform: ${#files[@]} files (placeholders replaced by ph-* stand-ins)"
  if "$kc" -strict -summary -ignore-missing-schemas "${args[@]}" "$work"/*; then
    rm -rf "$work"
    [ "$rc" -eq 0 ] && log "PASS kubeconform"
    return "$rc"
  fi
  rm -rf "$work"
  err "kubeconform: schema validation failed"
  return 1
}

# ---------------------------------------------------------------- terraform
cmd_terraform() {
  local tf rc=0 d
  tf="$(tool terraform)"
  [ -n "$tf" ] || { missing terraform terraform; return $?; }
  local -a dirs=()
  for d in "$ROOT/terraform" "$ROOT/octopus/terraform"; do
    if [ -d "$d" ] && [ -n "$(find "$d" -name '*.tf' -not -path '*/.terraform/*' -print -quit)" ]; then
      dirs+=("$d")
    fi
  done
  if [ "${#dirs[@]}" -eq 0 ]; then
    log "SKIP terraform: no Terraform staged"
    return 3
  fi
  for d in "${dirs[@]}"; do
    if "$tf" fmt -check -recursive -diff "$d"; then
      log "PASS terraform fmt ${d#"$ROOT"/}"
    else
      err "terraform fmt ${d#"$ROOT"/}: files need formatting"
      rc=1
    fi
  done
  if [ "${TF_VALIDATE:-false}" = "true" ]; then
    # Validate in a temporary copy so init never writes .terraform/ into the tree.
    local tmp mod
    tmp="$(mktemp -d)"
    (cd "$ROOT" && tar --exclude=.git --exclude=.terraform -cf - .) | (cd "$tmp" && tar -xf -)
    while IFS= read -r mod; do
      [ -n "$mod" ] || continue
      if "$tf" -chdir="$tmp/$mod" init -backend=false -input=false >/dev/null && "$tf" -chdir="$tmp/$mod" validate -no-color; then
        log "PASS terraform validate $mod"
      else
        err "terraform validate $mod"
        rc=1
      fi
    done < <(cd "$tmp" && find terraform octopus/terraform -name '*.tf' -not -path '*/.terraform/*' -exec dirname {} \; 2>/dev/null | sort -u)
    rm -rf "$tmp"
  fi
  return "$rc"
}

# ---------------------------------------------------------------- mermaid
cmd_mermaid() {
  local -a files=()
  mapfile -t files < <(grep -rlE --include='*.md' --exclude-dir=node_modules --exclude-dir=.git '^```mermaid' "$ROOT" 2>/dev/null | sort)
  if [ "${#files[@]}" -eq 0 ]; then
    log "SKIP mermaid: no mermaid blocks"
    return 3
  fi
  local node mmdc f rc=0
  node="$(tool node)"
  mmdc="$(tool mmdc)"
  if [ -n "${MERMAID_VALIDATOR:-}" ] && [ -n "$node" ] && [ -f "$MERMAID_VALIDATOR" ]; then
    for f in "${files[@]}"; do
      if "$node" "$MERMAID_VALIDATOR" "$f" >/dev/null 2>&1; then
        log "PASS mermaid ${f#"$ROOT"/}"
      else
        err "mermaid ${f#"$ROOT"/}: $("$node" "$MERMAID_VALIDATOR" "$f" 2>&1 | grep -E 'FAIL' | head -n 3 | tr '\n' ' ')"
        rc=1
      fi
    done
    return "$rc"
  fi
  if [ -n "$mmdc" ]; then
    local tmp
    tmp="$(mktemp -d)"
    for f in "${files[@]}"; do
      if "$mmdc" -q -i "$f" -o "$tmp/out.md" >/dev/null 2>&1; then
        log "PASS mermaid ${f#"$ROOT"/}"
      else
        err "mermaid ${f#"$ROOT"/}: mmdc could not render a block"
        rc=1
      fi
    done
    rm -rf "$tmp"
    return "$rc"
  fi
  missing mermaid "MERMAID_VALIDATOR (node script) or mmdc"
}

# ---------------------------------------------------------------- boundaries
cmd_boundaries() {
  local -a args=(--root "$ROOT")
  if [ -n "$APP_REPO" ]; then
    args+=(--app-repo "$APP_REPO")
  fi
  local rc=0 rc_audit
  bash "$SCRIPT_DIR/tool-boundaries.sh" "${args[@]}"
  rc=$?
  local branch main="${PLATFORM_MAIN_BRANCH:-main}"
  branch="${CF_BRANCH:-$(git -C "$ROOT" rev-parse --abbrev-ref HEAD 2>/dev/null || true)}"
  if [ "$branch" = "$main" ]; then
    bash "$SCRIPT_DIR/tool-boundaries.sh" --root "$ROOT" --audit-bot-commits
    rc_audit=$?
    if [ "$rc_audit" -eq 1 ] || [ "$rc_audit" -eq 2 ]; then
      rc=1
    fi
  else
    log "SKIP boundaries: bot-path audit runs on '$main' only (branch '${branch:-unknown}')"
  fi
  return "$rc"
}

# ---------------------------------------------------------------- consistency
cmd_consistency() {
  local -a args=(--root "$ROOT")
  if [ -n "$APP_REPO" ]; then
    args+=(--app-repo "$APP_REPO")
  fi
  local k
  k="$(tool kustomize)"
  if [ -n "$k" ]; then
    KUSTOMIZE="$k" bash "$SCRIPT_DIR/consistency.sh" "${args[@]}"
  else
    bash "$SCRIPT_DIR/consistency.sh" "${args[@]}" --no-render
  fi
}

# ---------------------------------------------------------------- secrets
cmd_secrets() {
  local gl
  gl="$(tool gitleaks)"
  [ -n "$gl" ] || { missing secrets gitleaks; return $?; }
  local -a cfg=()
  if [ -f "$ROOT/.gitleaks.toml" ]; then
    cfg=(--config "$ROOT/.gitleaks.toml")
  else
    warn "secrets: .gitleaks.toml not staged; using the default rules"
  fi
  local rc
  if "$gl" dir --help >/dev/null 2>&1; then
    "$gl" dir "$ROOT" "${cfg[@]}" --redact --no-banner --exit-code 1
    rc=$?
  else
    "$gl" detect --no-git --source "$ROOT" "${cfg[@]}" --redact --no-banner --exit-code 1
    rc=$?
  fi
  if [ "$rc" -eq 0 ]; then
    log "PASS secrets"
    return 0
  fi
  err "secrets: gitleaks reported findings"
  return 1
}

run() {
  log "== $1"
  case "$1" in
    yaml) cmd_yaml ;;
    kustomize) cmd_kustomize ;;
    kubeconform) cmd_kubeconform ;;
    terraform) cmd_terraform ;;
    mermaid) cmd_mermaid ;;
    boundaries) cmd_boundaries ;;
    consistency) cmd_consistency ;;
    secrets) cmd_secrets ;;
    *) return 2 ;;
  esac
}

case "$CMD" in
  all)
    declare -a summary=()
    overall=0
    for sub in $SUBCOMMANDS; do
      run "$sub"
      rc=$?
      case "$rc" in
        0) summary+=("PASS $sub") ;;
        3) summary+=("SKIP $sub") ;;
        *)
          summary+=("FAIL $sub")
          overall=1
          ;;
      esac
    done
    log "== summary (root=$ROOT${APP_REPO:+, app repo=$APP_REPO}, CI=${CI:-false})"
    printf '%s\n' "${summary[@]}"
    exit "$overall"
    ;;
  yaml | kustomize | kubeconform | terraform | mermaid | boundaries | consistency | secrets)
    run "$CMD"
    rc=$?
    [ "$rc" -eq 3 ] && exit 0
    exit "$rc"
    ;;
  *)
    echo "validate-all: unknown sub-command '$CMD'" >&2
    usage >&2
    exit 2
    ;;
esac
