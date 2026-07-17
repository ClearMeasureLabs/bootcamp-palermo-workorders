#!/usr/bin/env bash
#
# scan_changes.sh — Scan only *changed* code for leaked secrets with TruffleHog.
#
# Modes:
#   working  (default) Uncommitted changes in the working tree: staged + unstaged
#                      modified/added files, plus untracked (non-ignored) files.
#                      Uses `trufflehog filesystem` on the changed files.
#   staged             Only files staged for commit (`git diff --cached`).
#                      Ideal for a pre-commit gate. Uses `trufflehog filesystem`.
#   range              Committed changes between two refs. Requires --base; --head
#                      defaults to the current branch/HEAD. Uses `trufflehog git`
#                      with --since-commit/--branch, which scans the real diff.
#
# Exit codes:
#   0   clean — no secrets found in the changed code
#   183 secrets found (TruffleHog's --fail convention)
#   2   usage / environment error (not installed, not a git repo, bad args)
#
# Findings are emitted to stdout as line-delimited JSON (one object per finding),
# followed by a human-readable summary on stderr.

set -uo pipefail

MODE="working"
BASE=""
HEAD_REF="HEAD"
REPO_DIR="."
VERIFY=1
INCLUDE_UNVERIFIED=0

usage() {
  sed -n '2,25p' "$0" | sed 's/^# \{0,1\}//'
  cat <<'EOF'

Usage:
  scan_changes.sh [--mode working|staged|range] [--base REF] [--head REF]
                  [--repo DIR] [--no-verify] [--include-unverified]

Examples:
  scan_changes.sh                                  # scan uncommitted changes
  scan_changes.sh --mode staged                    # pre-commit gate
  scan_changes.sh --mode range --base main         # this branch vs main
  scan_changes.sh --mode range --base HEAD~1        # just the last commit
  scan_changes.sh --no-verify                       # offline: skip live checks
EOF
}

while [ $# -gt 0 ]; do
  case "$1" in
    --mode) MODE="${2:-}"; shift 2;;
    --base) BASE="${2:-}"; shift 2;;
    --head) HEAD_REF="${2:-}"; shift 2;;
    --repo) REPO_DIR="${2:-}"; shift 2;;
    --no-verify) VERIFY=0; shift;;
    --include-unverified) INCLUDE_UNVERIFIED=1; shift;;
    -h|--help) usage; exit 0;;
    *) echo "Unknown argument: $1" >&2; usage >&2; exit 2;;
  esac
done

# --- Environment checks -------------------------------------------------------
if ! command -v trufflehog >/dev/null 2>&1; then
  echo "ERROR: trufflehog is not installed or not on PATH." >&2
  echo "Install it, e.g.:" >&2
  echo "  brew install trufflehog" >&2
  echo "  # or:" >&2
  echo "  curl -sSfL https://raw.githubusercontent.com/trufflesecurity/trufflehog/main/scripts/install.sh | sh -s -- -b /usr/local/bin" >&2
  exit 2
fi

if ! git -C "$REPO_DIR" rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  echo "ERROR: '$REPO_DIR' is not inside a git working tree." >&2
  exit 2
fi

REPO_ABS="$(cd "$REPO_DIR" && git rev-parse --show-toplevel)"

# --- Build the common TruffleHog flags ---------------------------------------
COMMON=(--json --no-update --fail --fail-on-scan-errors --no-color)
if [ "$VERIFY" -eq 0 ]; then
  COMMON+=(--no-verification)
elif [ "$INCLUDE_UNVERIFIED" -eq 1 ]; then
  COMMON+=(--results=verified,unverified,unknown)
else
  # Default gate: live/valid creds (verified) + cases where verification errored
  # out (unknown). Excludes noisy unverified matches from the pass/fail decision.
  COMMON+=(--results=verified,unknown)
fi

run_trufflehog() {
  # Usage: run_trufflehog <subcommand> [args...]
  # Streams JSON findings to stdout; returns TruffleHog's exit code.
  trufflehog "$@" "${COMMON[@]}"
}

# --- Collect changed files for filesystem-based modes ------------------------
collect_changed_files() {
  local diff_spec=("$@")
  local -a files=()
  # -z gives NUL-delimited names so filenames with spaces survive.
  while IFS= read -r -d '' f; do
    [ -n "$f" ] || continue
    [ -f "$REPO_ABS/$f" ] || continue   # skip deletions / non-regular files
    files+=("$REPO_ABS/$f")
  done < <(git -C "$REPO_ABS" "${diff_spec[@]}")
  # Guard: with an empty array, `printf '%s\0'` would still emit one empty
  # field, and TruffleHog treats an empty path as "scan everything". Emit
  # nothing when there are no files.
  if [ "${#files[@]}" -gt 0 ]; then
    printf '%s\0' "${files[@]}"
  fi
}

EXIT=0

case "$MODE" in
  working|staged)
    declare -a CHANGED=()
    HAS_HEAD=1
    git -C "$REPO_ABS" rev-parse --verify -q HEAD >/dev/null 2>&1 || HAS_HEAD=0

    if [ "$MODE" = "staged" ]; then
      if [ "$HAS_HEAD" -eq 1 ]; then
        mapfile -d '' CHANGED < <(collect_changed_files diff --name-only -z --cached --diff-filter=d HEAD)
      else
        mapfile -d '' CHANGED < <(collect_changed_files diff --name-only -z --cached --diff-filter=d)
      fi
    else
      # working: tracked modifications vs HEAD (staged+unstaged) + untracked files
      declare -a tracked=() untracked=()
      if [ "$HAS_HEAD" -eq 1 ]; then
        mapfile -d '' tracked < <(collect_changed_files diff --name-only -z --diff-filter=d HEAD)
      fi
      mapfile -d '' untracked < <(collect_changed_files ls-files -z --others --exclude-standard)
      CHANGED=("${tracked[@]}" "${untracked[@]}")
    fi

    # De-duplicate and drop any empty entries.
    if [ "${#CHANGED[@]}" -gt 0 ]; then
      mapfile -t CHANGED < <(printf '%s\n' "${CHANGED[@]}" | grep -v '^$' | sort -u)
    fi

    if [ "${#CHANGED[@]}" -eq 0 ]; then
      echo "No changed files to scan (mode: $MODE)." >&2
      exit 0
    fi

    echo "Scanning ${#CHANGED[@]} changed file(s) [mode: $MODE, verify: $VERIFY]..." >&2
    run_trufflehog filesystem "${CHANGED[@]}"
    EXIT=$?
    ;;

  range)
    if [ -z "$BASE" ]; then
      echo "ERROR: --mode range requires --base <ref> (e.g. main or HEAD~1)." >&2
      exit 2
    fi
    echo "Scanning commits ${BASE}..${HEAD_REF} [mode: range, verify: $VERIFY]..." >&2
    run_trufflehog git "file://$REPO_ABS" --since-commit "$BASE" --branch "$HEAD_REF"
    EXIT=$?
    ;;

  *)
    echo "ERROR: unknown --mode '$MODE' (expected working, staged, or range)." >&2
    exit 2
    ;;
esac

# --- Summarize ---------------------------------------------------------------
case "$EXIT" in
  0)   echo "RESULT: clean — no secrets found in changed code." >&2 ;;
  183) echo "RESULT: SECRETS FOUND in changed code (see JSON findings above)." >&2 ;;
  *)   echo "RESULT: scan error (trufflehog exit code $EXIT)." >&2 ;;
esac

exit "$EXIT"
