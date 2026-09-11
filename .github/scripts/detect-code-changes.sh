#!/usr/bin/env bash
# Classify whether a change list includes build-relevant paths.
#
# Docs-only (code=false) is an allowlist. Anything else is code=true so Private
# Build / Integration Build still run. Do not pipe grep -q under pipefail: a
# SIGPIPE from grep -q exiting early can skip CI incorrectly.
#
# Usage:
#   detect-code-changes.sh --from-list FILE   # one path per line
#   detect-code-changes.sh --from-list -      # stdin
#
# Writes code=true|false to stdout and to GITHUB_OUTPUT when that file is set.

set -euo pipefail

is_docs_only_path() {
  local path="${1#./}"

  case "$path" in
    *.md) return 0 ;;
    docs/*) return 0 ;;
    LICENSE|LICENSE.*) return 0 ;;
    .github/ISSUE_TEMPLATE/*) return 0 ;;
    *) return 1 ;;
  esac
}

classify_paths() {
  local code=false
  local path
  while IFS= read -r path || [[ -n "$path" ]]; do
    [[ -z "$path" ]] && continue
    if ! is_docs_only_path "$path"; then
      code=true
      break
    fi
  done
  printf '%s' "$code"
}

write_code_output() {
  local code="$1"
  if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
    printf 'code=%s\n' "$code" >> "$GITHUB_OUTPUT"
  fi
  printf 'code=%s\n' "$code"
}

if [[ "${1:-}" != "--from-list" ]]; then
  echo "usage: $0 --from-list FILE" >&2
  exit 2
fi

list_file="${2:-}"
if [[ -z "$list_file" ]]; then
  echo "usage: $0 --from-list FILE" >&2
  exit 2
fi

if [[ "$list_file" == "-" ]]; then
  result="$(classify_paths)"
else
  result="$(classify_paths < "$list_file")"
fi

write_code_output "$result"
