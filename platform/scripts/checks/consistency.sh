#!/usr/bin/env bash
# scripts/checks/consistency.sh
#
# Cross-slice consistency checks: every staged file against
# contracts/platform-contracts.yaml (design §7, §11.5). Each finding names the
# package that owns the file (design §11.6).
#
# Usage
#   consistency.sh [--root DIR] [--app-repo DIR] [--contracts FILE] [--no-render]
#
#   --root       Environment-repo root (default: two levels above this script).
#   --app-repo   App repo checkout; enables the app:.codefresh/ checks.
#   --contracts  Contracts file (default: <root>/contracts/platform-contracts.yaml).
#   --no-render  Skip `kustomize build`; rendered-overlay checks fall back to the base files.
#
# Needs python3 with PyYAML. Uses `kustomize` (or $KUSTOMIZE) when present.
# Output lines: STATUS ID [owner] path: message. STATUS is PASS, FAIL, WARN or SKIP.
# Exit codes: 0 no failures, 1 failures, 2 usage error, 3 skipped (tool missing
# locally; with CI=true a missing tool fails instead).
#
# Robust to partial trees: a file whose package has not been staged yet is SKIP;
# a file missing from a package that has been staged is FAIL.

set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
APP_REPO=""
CONTRACTS=""
RENDER="yes"

usage() {
  cat <<'EOF'
Usage: consistency.sh [--root DIR] [--app-repo DIR] [--contracts FILE] [--no-render]
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
    --root | --app-repo | --contracts)
      if [ "$#" -lt 2 ]; then
        echo "consistency: $1 needs a value" >&2
        exit 2
      fi
      case "$1" in
        --root) ROOT="$2" ;;
        --app-repo) APP_REPO="$2" ;;
        --contracts) CONTRACTS="$2" ;;
      esac
      shift 2
      ;;
    --no-render)
      RENDER="no"
      shift
      ;;
    -h | --help)
      usage
      exit 0
      ;;
    *)
      echo "consistency: unknown argument '$1'" >&2
      usage >&2
      exit 2
      ;;
  esac
done

if [ ! -d "$ROOT" ]; then
  echo "consistency: root '$ROOT' not found" >&2
  exit 2
fi
ROOT="$(cd "$ROOT" && pwd)"
if [ -n "$APP_REPO" ]; then
  if [ ! -d "$APP_REPO" ]; then
    echo "consistency: app repo '$APP_REPO' not found" >&2
    exit 2
  fi
  APP_REPO="$(cd "$APP_REPO" && pwd)"
fi
CONTRACTS="${CONTRACTS:-$ROOT/contracts/platform-contracts.yaml}"

PYTHON_BIN="${PYTHON:-python3}"
if ! command -v "$PYTHON_BIN" >/dev/null 2>&1 || ! "$PYTHON_BIN" -c 'import yaml' >/dev/null 2>&1; then
  if is_ci; then
    echo "FAIL C00   [pragmatist] -: python3 with PyYAML is required (CI=true)"
    exit 1
  fi
  echo "SKIP C00   [pragmatist] -: python3 with PyYAML not found; consistency checks skipped locally"
  exit 3
fi

KUSTOMIZE_BIN=""
if [ "$RENDER" = "yes" ]; then
  KUSTOMIZE_BIN="$(command -v "${KUSTOMIZE:-kustomize}" 2>/dev/null || true)"
fi

exec "$PYTHON_BIN" - "$ROOT" "$APP_REPO" "$CONTRACTS" "$KUSTOMIZE_BIN" <<'PYEOF'
import os
import re
import subprocess
import sys

import yaml

ROOT, APP, CONTRACTS, KUSTOMIZE = sys.argv[1], sys.argv[2] or None, sys.argv[3], sys.argv[4] or None
COUNTS = {"PASS": 0, "FAIL": 0, "WARN": 0, "SKIP": 0}
OWNERSHIP = []

# --------------------------------------------------------------------------- output

def owner(rel):
    if not rel:
        return "-"
    best = ("", "-")
    for item in OWNERSHIP:
        root = item.get("root", "")
        if rel == root.rstrip("/") or rel.startswith(root):
            if len(root) > len(best[0]):
                best = (root, item.get("package", "-"))
    return best[1]


def out(status, cid, rel, msg):
    COUNTS[status] += 1
    print(f"{status:<4} {cid:<4} [{owner(rel)}] {rel or '-'}: {msg}")


def verdict(cid, rel, errors, ok_msg, warn=False):
    if errors:
        out("WARN" if warn else "FAIL", cid, rel, "; ".join(errors))
    else:
        out("PASS", cid, rel, ok_msg)

# --------------------------------------------------------------------------- paths

def P(rel):
    if rel.startswith("app:"):
        return os.path.join(APP, rel[4:]) if APP else None
    return os.path.join(ROOT, rel)


def exists(rel):
    p = P(rel)
    return p is not None and os.path.exists(p)


def top(rel):
    if rel.startswith("app:"):
        return "app:" + rel[4:].split("/")[0]
    return rel.split("/")[0]


def absent(cid, rel, what):
    """Report a missing file: SKIP when its package tree is not staged, FAIL otherwise."""
    if rel.startswith("app:") and not APP:
        out("SKIP", cid, rel, f"{what}: app repo not provided (--app-repo)")
    elif exists(top(rel)):
        out("FAIL", cid, rel, f"{what}: file missing")
    else:
        out("SKIP", cid, rel, f"{what}: package tree '{top(rel)}' not staged yet")


def walk(rel_dir, exts=(".yaml", ".yml")):
    base = P(rel_dir)
    if base is None or not os.path.isdir(base):
        return []
    found = []
    for dirpath, dirnames, filenames in os.walk(base):
        dirnames[:] = [d for d in dirnames if d not in (".git", ".terraform", "node_modules")]
        for name in filenames:
            if exts is None or name.endswith(exts):
                full = os.path.join(dirpath, name)
                if rel_dir.startswith("app:"):
                    found.append("app:" + os.path.relpath(full, APP))
                else:
                    found.append(os.path.relpath(full, ROOT))
    return sorted(found)


def text(rel):
    try:
        with open(P(rel), encoding="utf-8") as f:
            return f.read()
    except (OSError, TypeError):
        return None


def code_text(rel):
    """File text with full-line comments removed (YAML, HCL, OCL, shell)."""
    t = text(rel)
    if t is None:
        return None
    return "\n".join(l for l in t.splitlines() if not re.match(r"^\s*(#|//)", l))

# --------------------------------------------------------------------------- yaml

_DOCS = {}


def docs(rel, cid="C00"):
    if rel in _DOCS:
        return _DOCS[rel]
    result = None
    try:
        with open(P(rel), encoding="utf-8") as f:
            result = [d for d in yaml.safe_load_all(f) if d is not None]
    except (OSError, TypeError):
        result = None
    except yaml.YAMLError as e:
        out("FAIL", cid, rel, "YAML does not parse: " + str(e).splitlines()[0])
        result = None
    _DOCS[rel] = result
    return result


def g(d, path, default=None):
    cur = d
    for key in path:
        if isinstance(cur, dict) and key in cur:
            cur = cur[key]
        elif isinstance(cur, list) and isinstance(key, int) and key < len(cur):
            cur = cur[key]
        else:
            return default
    return cur


def kind_name(d):
    if not isinstance(d, dict):
        return (None, None)
    return (d.get("kind"), g(d, ["metadata", "name"]))


def find_doc(rel, kind, name):
    for d in docs(rel) or []:
        if kind_name(d) == (kind, name):
            return d
    return None


def all_dicts(obj):
    if isinstance(obj, dict):
        yield obj
        for v in obj.values():
            yield from all_dicts(v)
    elif isinstance(obj, list):
        for v in obj:
            yield from all_dicts(v)


def all_strings(obj):
    if isinstance(obj, str):
        yield obj
    elif isinstance(obj, dict):
        for k, v in obj.items():
            if isinstance(k, str):
                yield k
            yield from all_strings(v)
    elif isinstance(obj, list):
        for v in obj:
            yield from all_strings(v)

# --------------------------------------------------------------------------- hcl/ocl blocks

def blocks(src, header_re):
    """Yield (name, body) for blocks whose opening line matches header_re (group 1 = name).
    Braces inside strings and heredocs are ignored."""
    lines = src.splitlines()
    i = 0
    rx = re.compile(header_re)
    while i < len(lines):
        m = rx.match(lines[i])
        if not m:
            i += 1
            continue
        depth, body, heredoc = 0, [], None
        j = i
        while j < len(lines):
            line = lines[j]
            body.append(line)
            if heredoc:
                if line.strip() == heredoc:
                    heredoc = None
                j += 1
                continue
            in_str, esc = False, False
            k = 0
            while k < len(line):
                ch = line[k]
                if in_str:
                    if esc:
                        esc = False
                    elif ch == "\\":
                        esc = True
                    elif ch == '"':
                        in_str = False
                elif ch == '"':
                    in_str = True
                elif ch == "#" or line.startswith("//", k):
                    break
                elif ch == "{":
                    depth += 1
                elif ch == "}":
                    depth -= 1
                elif line.startswith("<<", k):
                    hm = re.match(r"<<-?\s*([A-Za-z_][A-Za-z0-9_]*)", line[k:])
                    if hm:
                        heredoc = hm.group(1)
                        break
                k += 1
            j += 1
            if depth <= 0 and not heredoc and "{" in "".join(body):
                break
        yield m.group(1), "\n".join(body)
        i = j


def quoted_list(body, attr):
    """Quoted strings of every `attr = [ ... ]` list in body (may span lines)."""
    vals = []
    for m in re.finditer(attr + r"\s*=\s*\[(.*?)\]", body, re.S):
        vals += re.findall(r'"([^"]*)"', m.group(1))
    return vals

# --------------------------------------------------------------------------- contracts

def load_contracts():
    cid = "C01"
    rel = os.path.relpath(CONTRACTS, ROOT) if CONTRACTS.startswith(ROOT) else CONTRACTS
    try:
        with open(CONTRACTS, encoding="utf-8") as f:
            c = yaml.safe_load(f)
    except OSError:
        out("FAIL", cid, rel, "contracts file not found")
        return None
    except yaml.YAMLError as e:
        out("FAIL", cid, rel, "contracts file does not parse: " + str(e).splitlines()[0])
        return None
    need = ["expansions", "placeholders", "envRepo", "environments", "azure", "octopus", "argocd",
            "kubernetes", "images", "packages", "pins", "codefresh", "keyVault", "health", "config",
            "statuses", "ownership"]
    missing = [k for k in need if k not in (c or {})]
    OWNERSHIP.extend((c or {}).get("ownership", []))
    verdict(cid, rel, [f"missing section '{k}'" for k in missing], "contracts parse; all sections present")
    return c if not missing else None


C = load_contracts()
if C is None:
    print("consistency: contracts unusable; stopping")
    sys.exit(1)

ENVS = C["expansions"]["env"]
CLUSTERS = C["expansions"]["cluster"]
CLUSTER_OF = C["expansions"]["clusterOfEnv"]
ENV_REPO_URLS = {C["envRepo"]["url"], C["envRepo"]["urlPlaceholder"],
                 C["envRepo"]["url"][:-4] if C["envRepo"]["url"].endswith(".git") else C["envRepo"]["url"]}
NAMED_APPS = {a["name"]: a for a in C["argocd"]["applications"]}
REGISTRY = C["images"]["registry"]
PINNED = [i for i in C["images"]["release"] if i.get("pinned")]
KUSTOMIZE_NAMES = [i["kustomizeName"] for i in PINNED]


def name_present(name, t):
    """Literal name, or its prefix followed by an HCL interpolation (rg-workorders-${each.key})."""
    if f'"{name}"' in t or name in t:
        return True
    values = set(ENVS) | set(CLUSTERS) | {"infra-nonprod", "infra-prod"}
    for v in sorted(values, key=len, reverse=True):
        i = name.find(v)
        if i > 0 and (name[:i] + "${") in t:
            return True
    return False


def x(s, **kw):
    for k, v in kw.items():
        s = s.replace("{" + k + "}", v)
    return s


def env_x(s, env):
    return x(s, env=env, cluster=CLUSTER_OF.get(env, ""))

# --------------------------------------------------------------------------- C02 annotations

def c02_annotations():
    cid = "C02"
    octo_envs = set(C["octopus"]["environments"])
    want_project = C["argocd"]["applicationDefaults"]["annotations"]["argo.octopus.com/project"]
    forbidden = set(C["argocd"]["annotationRules"]["forbiddenKeys"])
    scanned = False
    for root in ("argocd", "gitops"):
        for rel in walk(root):
            for d in docs(rel, cid) or []:
                for dd in all_dicts(d):
                    keys = [k for k in dd if isinstance(k, str) and k.startswith("argo.octopus.com/")]
                    if not keys:
                        continue
                    scanned = True
                    kind, name = kind_name(d)
                    errors = []
                    if kind != "Application" or name not in NAMED_APPS:
                        errors.append(f"{kind}/{name} carries {', '.join(sorted(keys))}; only the named-environment Applications may")
                    else:
                        env = NAMED_APPS[name]["env"]
                        if dd.get("argo.octopus.com/project") != want_project:
                            errors.append(f"project annotation '{dd.get('argo.octopus.com/project')}' != '{want_project}'")
                        got_env = dd.get("argo.octopus.com/environment")
                        if got_env != env:
                            errors.append(f"environment annotation '{got_env}' != '{env}'")
                        if got_env not in octo_envs:
                            errors.append(f"environment annotation '{got_env}' is not an Octopus environment")
                    for k in keys:
                        if k in forbidden:
                            errors.append(f"forbidden annotation {k} (no tenants, ADR-C8)")
                    verdict(cid, rel, errors, f"{kind}/{name}: Octopus annotations match Octopus environment '{dd.get('argo.octopus.com/environment')}'")
    if not scanned:
        if exists("argocd"):
            out("FAIL", cid, "argocd", "no argo.octopus.com/* annotations found on any Application")
        else:
            out("SKIP", cid, "argocd", "package tree 'argocd' not staged yet")

# --------------------------------------------------------------------------- C03 named Applications

def c03_applications():
    cid = "C03"
    dflt = C["argocd"]["applicationDefaults"]
    for name, a in NAMED_APPS.items():
        rel = a["file"]
        if not exists(rel):
            absent(cid, rel, f"Application {name}")
            continue
        app = find_doc(rel, "Application", name)
        if app is None:
            out("FAIL", cid, rel, f"no Application named {name}")
            continue
        e = []
        spec = app.get("spec") or {}
        if spec.get("project") != a["project"]:
            e.append(f"project '{spec.get('project')}' != '{a['project']}'")
        if "sources" in spec:
            e.append("uses spec.sources; the contract is one unnamed Git source (unscoped annotations, E5)")
        src = spec.get("source") or {}
        if src.get("repoURL") not in ENV_REPO_URLS:
            e.append(f"repoURL '{src.get('repoURL')}' is not the environment repo")
        if str(src.get("targetRevision")) != dflt["targetRevision"]:
            e.append(f"targetRevision '{src.get('targetRevision')}' != '{dflt['targetRevision']}'")
        if src.get("path") != a["path"]:
            e.append(f"path '{src.get('path')}' != '{a['path']}'")
        elif exists("gitops") and not exists(a["path"]):
            e.append(f"path '{a['path']}' does not exist in the tree")
        if g(src, ["kustomize", "images"]):
            e.append("overrides kustomize.images; the pin file is the only tag source (ADR-D4)")
        dest = spec.get("destination") or {}
        if dest.get("server") != dflt["destinationServer"]:
            e.append(f"destination.server '{dest.get('server')}' != '{dflt['destinationServer']}'")
        if dest.get("namespace") != a["namespace"]:
            e.append(f"destination.namespace '{dest.get('namespace')}' != '{a['namespace']}'")
        sp = spec.get("syncPolicy") or {}
        auto = sp.get("automated") or {}
        if auto.get("prune") is not True or auto.get("selfHeal") is not True:
            e.append("syncPolicy.automated must set prune: true and selfHeal: true (ADR-D5)")
        if "PruneLast=true" not in (sp.get("syncOptions") or []):
            e.append("syncOptions lacks PruneLast=true")
        retry = sp.get("retry") or {}
        want_retry = dflt["syncPolicy"]["retry"]
        if retry.get("limit") != want_retry["limit"]:
            e.append(f"retry.limit '{retry.get('limit')}' != {want_retry['limit']}")
        bo = retry.get("backoff") or {}
        for k, v in want_retry["backoff"].items():
            if str(bo.get(k)) != str(v):
                e.append(f"retry.backoff.{k} '{bo.get(k)}' != '{v}'")
        if g(app, ["metadata", "finalizers"]):
            e.append("named-environment Applications carry no finalizer (ADR-D5)")
        verdict(cid, rel, e, f"Application {name} matches §7.3 (project, source, destination, sync policy)")

# --------------------------------------------------------------------------- C04 AppProjects

def c04_projects():
    cid = "C04"
    detail = C["argocd"]["appProjectDetail"]
    allow = set(C["argocd"]["namespacedKindAllowList"])
    for cluster, names in C["argocd"]["appProjects"].items():
        rel = f"argocd/clusters/{cluster}/projects.yaml"
        if not exists(rel):
            absent(cid, rel, f"AppProjects for {cluster}")
            continue
        projs = {}
        for d in docs(rel, cid) or []:
            k, n = kind_name(d)
            if k == "AppProject":
                projs[n] = d
        e = []
        for n in names:
            if n not in projs:
                e.append(f"missing AppProject {n}")
        for n in projs:
            if n not in names:
                e.append(f"AppProject {n} is not in §7.3 for cluster {cluster}")
        dflt = projs.get("default")
        if dflt is not None:
            s = dflt.get("spec") or {}
            if s.get("sourceRepos") or s.get("destinations") or s.get("clusterResourceWhitelist"):
                e.append("AppProject default is not locked (sources, destinations and cluster kinds must be empty)")
        for n, d in projs.items():
            s = d.get("spec") or {}
            if "syncWindows" in s:
                e.append(f"{n}: syncWindows are not used (ADR-D5)")
            if not n.startswith("workorders-"):
                continue
            det = detail.get(n, {})
            dests = sorted(str(x.get("namespace")) for x in (s.get("destinations") or []))
            if det.get("destinations") and dests != sorted(det["destinations"]):
                e.append(f"{n}: destinations {dests} != {sorted(det['destinations'])}")
            if n in ("workorders-nonprod", "workorders-prod"):
                repos = s.get("sourceRepos") or []
                if not repos or any(r not in ENV_REPO_URLS for r in repos):
                    e.append(f"{n}: sourceRepos {repos} must be the environment repo only")
            if s.get("clusterResourceWhitelist"):
                e.append(f"{n}: allows cluster-scoped kinds; only platform-addons may")
            kinds = {str(k.get("kind")) for k in (s.get("namespaceResourceWhitelist") or []) if isinstance(k, dict)}
            if n in ("workorders-nonprod", "workorders-prod") and kinds:
                extra = sorted(kinds - allow - {"*"})
                missing = sorted(allow - kinds)
                if "*" in kinds:
                    e.append(f"{n}: namespaceResourceWhitelist allows '*'")
                if extra:
                    e.append(f"{n}: kinds beyond the §7.3 allow-list: {extra}")
                if missing:
                    e.append(f"{n}: allow-list lacks {missing}")
            elif n in ("workorders-nonprod", "workorders-prod"):
                e.append(f"{n}: no namespaceResourceWhitelist (§7.3 allow-list)")
            roles = {str(r.get("name")) for r in (s.get("roles") or []) if isinstance(r, dict)}
            for r in det.get("roles", []):
                if r not in roles:
                    e.append(f"{n}: missing role {r}")
        verdict(cid, rel, e, f"AppProjects {sorted(projs)} match §7.3")

# --------------------------------------------------------------------------- C05 namespaces

def c05_namespaces():
    cid = "C05"
    for cluster in CLUSTERS:
        spec = C["kubernetes"]["namespaces"][cluster]
        rel = f"argocd/clusters/{cluster}/namespaces.yaml"
        if not exists(rel):
            absent(cid, rel, f"namespaces for {cluster}")
            continue
        found = {}
        for d in docs(rel, cid) or []:
            k, n = kind_name(d)
            if k == "Namespace":
                found[n] = g(d, ["metadata", "labels"]) or {}
        e, w = [], []
        for n in spec["addons"] + spec["apps"]:
            if n not in found:
                e.append(f"missing Namespace {n}")
        for n in spec["bootstrap"] + spec["terraformWorkers"]:
            if n in found:
                e.append(f"Namespace {n} is created by Terraform, not by namespaces.yaml (§7.4)")
        for n in found:
            if n not in spec["addons"] + spec["apps"] + spec["bootstrap"] + spec["terraformWorkers"]:
                w.append(f"Namespace {n} is not listed for {cluster} in §7.4")
        for n in spec["apps"]:
            if n in found:
                env = n.replace("workorders-", "")
                want = {k: env_x(v, env) for k, v in C["kubernetes"]["appNamespaceLabels"].items()}
                for k, v in want.items():
                    if str(found[n].get(k)) != v:
                        e.append(f"{n}: label {k}='{found[n].get(k)}' != '{v}'")
        for n in spec["addons"]:
            if n in found and str(found[n].get("tier")) != "platform":
                w.append(f"{n}: label tier != platform")
        verdict(cid, rel, e, f"namespaces {sorted(found)} match §7.4")
        if w and not e:
            out("WARN", cid, rel, "; ".join(w))

# --------------------------------------------------------------------------- C06 root app and bootstrap values

def c06_bootstrap():
    cid = "C06"
    root = C["argocd"]["rootApplication"]
    for cluster in CLUSTERS:
        rel = x(root["valuesFile"], cluster=cluster)
        if not exists(rel):
            absent(cid, rel, "root Application values")
        else:
            e = []
            apps = {}
            for d in docs(rel, cid) or []:
                a = d.get("applications") if isinstance(d, dict) else None
                if isinstance(a, dict):
                    apps.update(a)
                elif isinstance(a, list):
                    apps.update({i.get("name"): i for i in a if isinstance(i, dict)})
            r = apps.get(root["name"])
            if r is None:
                e.append(f"no application '{root['name']}' in argocd-apps values")
            else:
                if r.get("project") != root["project"]:
                    e.append(f"project '{r.get('project')}' != '{root['project']}'")
                src = r.get("source") or {}
                if src.get("repoURL") not in ENV_REPO_URLS:
                    e.append(f"repoURL '{src.get('repoURL')}' is not the environment repo")
                if str(src.get("targetRevision")) != root["targetRevision"]:
                    e.append(f"targetRevision '{src.get('targetRevision')}' != '{root['targetRevision']}'")
                want_path = x(root["path"], cluster=cluster)
                if src.get("path") != want_path:
                    e.append(f"path '{src.get('path')}' != '{want_path}'")
                if g(src, ["directory", "recurse"]) is not True:
                    e.append("source.directory.recurse must be true")
                auto = g(r, ["syncPolicy", "automated"]) or {}
                if auto.get("prune") is not True or auto.get("selfHeal") is not True:
                    e.append("syncPolicy.automated must set prune and selfHeal")
                if r.get("finalizers"):
                    e.append("root Application carries a finalizer")
            verdict(cid, rel, e, f"{root['name']} for {cluster} matches §7.3")
        vrel = x(C["argocd"]["bootstrapValues"], cluster=cluster)
        t = code_text(vrel)
        if t is None:
            absent(cid, vrel, "Argo CD bootstrap values")
            continue
        flat = re.sub(r"\s+", " ", t)
        e = []
        if not re.search(r"timeout\.reconciliation\"?'?\s*:\s*\"?'?120s", t):
            e.append("timeout.reconciliation: 120s not set")
        if not re.search(r"accounts\.octopus\"?'?\s*:\s*\"?'?apiKey", t):
            e.append("accounts.octopus: apiKey not set")
        if not re.search(r"admin\.enabled\"?'?\s*:\s*\"?'?false", t):
            e.append("admin.enabled: false not set")
        for pol in C["argocd"]["config"]["gatewayPolicies"]:
            if re.sub(r"\s+", " ", pol) not in flat:
                e.append(f"RBAC policy missing: '{pol}'")
        verdict(cid, vrel, e, "bootstrap values carry the §7.3 configuration")

# --------------------------------------------------------------------------- C07 images and package IDs

IMAGE_REF = re.compile(re.escape(REGISTRY) + r"/([a-z0-9][a-z0-9-]*/[a-z0-9][a-z0-9-]*)")
BARE_ID = re.compile(r"(?<![A-Za-z0-9_./-])((?:workorders|workorders-previews|platform)/[a-z0-9][a-z0-9-]*)")


def c07_images():
    cid = "C07"
    repos = {i["repository"] for i in C["images"]["release"]} | {i["repository"] for i in C["images"]["previews"]}
    repos.add(C["images"]["ciToolchain"]["repository"])
    pipelines = {p["name"] for p in C["codefresh"]["pipelines"]}
    statuses = set()
    for v in C["statuses"].values():
        statuses |= set(v) if isinstance(v, list) else {v}
    known_ids = repos | pipelines | statuses | set(C["packages"]["buildInformationPackageIds"])
    # Contract self-consistency: Kustomize name = registry + "/" + Octopus package ID.
    e = [f"{i['kustomizeName']} != {REGISTRY}/{i['octopusPackageId']}"
         for i in C["images"]["release"] if i["kustomizeName"] != f"{REGISTRY}/{i['octopusPackageId']}"]
    verdict(cid, "contracts/platform-contracts.yaml", e, "Kustomize image names equal the registry plus the Octopus package IDs")

    # Every image reference and package-like token in staged config is a contract name.
    roots = ["argocd", "gitops", ".octopus", "octopus", "codefresh", "policies", "terraform", "app:.codefresh", "app:containers"]
    for root in roots:
        if root.startswith("app:") and not APP:
            continue
        for rel in walk(root, exts=None):
            if rel.endswith(".md"):
                continue
            t = code_text(rel)
            if t is None:
                continue
            bad = sorted({m.group(1) for m in IMAGE_REF.finditer(t) if m.group(1) not in repos})
            bad += sorted({m.group(1) for m in BARE_ID.finditer(t) if m.group(1) not in known_ids and m.group(1) not in bad})
            if bad:
                out("FAIL", cid, rel, f"image or package names not in §7.5: {bad}")

    # Base manifests: only contract images, without tag or digest (§7.6).
    base = C["pins"]["baseDir"]
    if exists(base):
        for rel in walk(base):
            e = []
            for d in docs(rel, cid) or []:
                for dd in all_dicts(d):
                    for key in ("containers", "initContainers"):
                        for ctr in dd.get(key) or [] if isinstance(dd.get(key), list) else []:
                            img = str((ctr or {}).get("image", ""))
                            if not img:
                                continue
                            if img not in [i["kustomizeName"] for i in C["images"]["release"]]:
                                e.append(f"container image '{img}' is not an untagged §7.5 release image")
            if e:
                out("FAIL", cid, rel, "; ".join(sorted(set(e))))
            else:
                out("PASS", cid, rel, "container images are untagged §7.5 release images")
    else:
        absent(cid, base, "Kustomize base")

    # Octopus Argo CD step names the pinned package IDs on feed acr-workorders.
    rel = ".octopus/workorders/deployment_process.ocl"
    t = code_text(rel)
    if t is None:
        absent(cid, rel, "deployment process")
    else:
        step = dict(blocks(t, r'^\s*step\s+"([^"]+)"\s*\{')).get("update-argo-cd-image-tags")
        if step is None:
            out("FAIL", cid, rel, "step update-argo-cd-image-tags not found")
        else:
            e = [f"package {i['octopusPackageId']} not referenced" for i in PINNED if i["octopusPackageId"] not in step]
            if C["octopus"]["feeds"]["acr"]["name"] not in step:
                e.append(f"feed {C['octopus']['feeds']['acr']['name']} not referenced")
            verdict(cid, rel, e, "Argo CD step packages equal the Kustomize images[].name repositories")

# --------------------------------------------------------------------------- C08 pin shape

VERSION_RE = re.compile(r"^\d+\.\d+\.\d+([-.][0-9A-Za-z.-]+)?$")


def c08_pins():
    cid = "C08"
    shape = C["pins"]["shape"]
    for env in ENVS:
        rel = x(C["pins"]["file"], env=env)
        t = text(rel)
        if t is None:
            absent(cid, rel, f"pin file for {env}")
            continue
        ds = docs(rel, cid)
        if ds is None:
            continue
        e = []
        if len(ds) != 1 or not isinstance(ds[0], dict):
            e.append("must be exactly one YAML mapping")
        else:
            d = ds[0]
            if set(d) != set(shape):
                e.append(f"top-level keys {sorted(d)} != {sorted(shape)}")
            for k in ("apiVersion", "kind", "resources"):
                if d.get(k) != shape[k]:
                    e.append(f"{k} {d.get(k)!r} != {shape[k]!r}")
            imgs = d.get("images") or []
            names = [i.get("name") for i in imgs if isinstance(i, dict)]
            if names != [i["name"] for i in shape["images"]]:
                e.append(f"images[].name {names} != {[i['name'] for i in shape['images']]}")
            for i in imgs:
                if not isinstance(i, dict):
                    continue
                if set(i) != {"name", "newTag"}:
                    e.append(f"{i.get('name')}: keys {sorted(i)} != ['name', 'newTag']")
                tag = i.get("newTag")
                if not isinstance(tag, str):
                    e.append(f"{i.get('name')}: newTag must be a quoted string")
                elif tag == "latest" or not VERSION_RE.match(tag):
                    e.append(f"{i.get('name')}: newTag '{tag}' is not a release version")
            if len(re.findall(r'newTag:\s*"', t)) != len(imgs):
                e.append("newTag values must be double-quoted")
        verdict(cid, rel, e, "exact §7.6 shape (config resource, two images, quoted newTag)")

# --------------------------------------------------------------------------- C09 rendered overlays

def render(env):
    d = P(f"gitops/workorders/envs/{env}")
    try:
        r = subprocess.run([KUSTOMIZE, "build", d], capture_output=True, text=True, timeout=120)
    except (OSError, subprocess.TimeoutExpired) as ex:
        return None, str(ex)
    if r.returncode != 0:
        return None, (r.stderr.strip().splitlines() or ["kustomize failed"])[-1]
    try:
        return [x for x in yaml.safe_load_all(r.stdout) if x], None
    except yaml.YAMLError as ex:
        return None, "rendered output does not parse: " + str(ex).splitlines()[0]


def probe_ok(probe, ctr, name=""):
    """httpGet on the §7.9 probe path and port; readiness may use /ready once WI-01 exists."""
    path = g(probe, ["httpGet", "path"])
    port = g(probe, ["httpGet", "port"])
    if isinstance(port, str):
        for p in ctr.get("ports") or []:
            if p.get("name") == port:
                port = p.get("containerPort")
    paths = {C["health"]["probe"]["path"]}
    if name == "readinessProbe":
        paths.add(C["health"]["futureReadiness"])
    return path in paths and str(port) == str(C["health"]["probe"]["port"])


def c09_rendered():
    cid = "C09"
    if not exists("gitops"):
        out("SKIP", cid, "gitops", "package tree 'gitops' not staged yet")
        return
    if not KUSTOMIZE:
        out("SKIP", cid, "gitops", "kustomize not found or --no-render: rendered-overlay checks skipped; base checked statically")
        c09_static()
        return
    labels = C["kubernetes"]["labels"]
    wl = C["kubernetes"]["workloadObjects"]
    for env in ENVS:
        rel = f"gitops/workorders/envs/{env}"
        if not exists(rel + "/kustomization.yaml"):
            absent(cid, rel + "/kustomization.yaml", f"overlay {env}")
            continue
        objs, err = render(env)
        if objs is None:
            out("FAIL", cid, rel, f"kustomize build fails: {err}")
            continue
        ns = f"workorders-{env}"
        e, w = [], []
        by = {}
        for o in objs:
            by.setdefault(o.get("kind"), {})[g(o, ["metadata", "name"])] = o
            if o.get("kind") not in ("Namespace", "ClusterRole", "ClusterRoleBinding", "CustomResourceDefinition"):
                if g(o, ["metadata", "namespace"]) != ns:
                    e.append(f"{o.get('kind')}/{g(o, ['metadata', 'name'])} namespace '{g(o, ['metadata', 'namespace'])}' != '{ns}'")
        for kind, names in wl.items():
            for n in names:
                present = by.get(kind, {})
                if kind == "ConfigMap":
                    if not any(str(k).startswith(n) for k in present):
                        e.append(f"missing ConfigMap {n}")
                elif n not in present:
                    e.append(f"missing {kind}/{n}")
        pin = (docs(x(C["pins"]["file"], env=env)) or [{}])[0] or {}
        tags = {i.get("name"): i.get("newTag") for i in pin.get("images") or [] if isinstance(i, dict)}
        for dep in ("ui-server", "worker"):
            d = by.get("Deployment", {}).get(dep)
            if d is None:
                continue
            pod = g(d, ["spec", "template"]) or {}
            ctrs = g(pod, ["spec", "containers"]) or []
            want_img = f"{REGISTRY}/workorders/{dep}:{tags.get(f'{REGISTRY}/workorders/{dep}')}"
            if not any(c.get("image") == want_img for c in ctrs):
                e.append(f"{dep}: image is not {want_img}")
            if g(pod, ["spec", "serviceAccountName"]) != dep:
                e.append(f"{dep}: serviceAccountName '{g(pod, ['spec', 'serviceAccountName'])}' != '{dep}'")
            if str(g(pod, ["metadata", "labels", "azure.workload.identity/use"])) != "true":
                e.append(f"{dep}: pod label azure.workload.identity/use: \"true\" missing")
            for k, v in labels[dep].items():
                if g(d, ["metadata", "labels", k]) != v:
                    e.append(f"{dep}: label {k}='{g(d, ['metadata', 'labels', k])}' != '{v}'")
            env_from = [list(ef.keys())[0] + ":" + str(list(ef.values())[0].get("name")) for c in ctrs for ef in (c.get("envFrom") or []) if isinstance(ef, dict) and ef]
            if not any(s.startswith("configMapRef:" + C["config"]["configMap"]) for s in env_from):
                e.append(f"{dep}: envFrom lacks ConfigMap {C['config']['configMap']}")
            if dep == "ui-server":
                if "secretRef:workorders-app" not in env_from:
                    e.append("ui-server: envFrom lacks Secret workorders-app")
                c0 = ctrs[0] if ctrs else {}
                for pname in C["health"]["probe"]["probes"]:
                    if not probe_ok(c0.get(pname) or {}, c0, pname):
                        e.append(f"ui-server: {pname} is not httpGet {C['health']['probe']['path']} on {C['health']['probe']['port']}")
                ru = g(d, ["spec", "strategy", "rollingUpdate", "maxUnavailable"])
                if g(d, ["spec", "strategy", "type"]) not in (None, "RollingUpdate") or str(ru) != "0":
                    w.append("ui-server: RollingUpdate with maxUnavailable 0 expected (ADR-C3)")
            else:
                for c in ctrs:
                    for pname in ("startupProbe", "livenessProbe", "readinessProbe"):
                        if c.get(pname):
                            e.append(f"worker: {pname} set, but the Worker has no endpoint until WI-04")
                replicas = g(d, ["spec", "replicas"])
                if env == "prod" and replicas != 0:
                    e.append(f"worker: replicas {replicas} in prod; 0 until product sign-off (ADR-D16)")
                elif replicas != 0:
                    w.append(f"worker: replicas {replicas} (0 until enabled in this phase, ADR-D16)")
            for o in objs:
                for c in (g(o, ["spec", "template", "spec", "containers"]) or []):
                    for pname in ("startupProbe", "livenessProbe", "readinessProbe"):
                        if g(c, [pname, "httpGet", "path"]) == C["health"]["smoke"]:
                            e.append(f"{g(o, ['metadata', 'name'])}: {pname} uses {C['health']['smoke']} (smoke only, ADR-D12)")
        svc = by.get("Service", {}).get("ui-server")
        if svc is not None:
            ports = g(svc, ["spec", "ports"]) or []
            if not any(str(p.get("port")) == str(C["kubernetes"]["servicePort"]) for p in ports):
                e.append(f"Service ui-server does not expose port {C['kubernetes']['servicePort']}")
        for sa in C["kubernetes"]["workloadObjects"]["ServiceAccount"]:
            o = by.get("ServiceAccount", {}).get(sa)
            if o is not None and not g(o, ["metadata", "annotations", C["kubernetes"]["workloadIdentity"]["serviceAccountAnnotation"]]):
                e.append(f"ServiceAccount {sa} lacks {C['kubernetes']['workloadIdentity']['serviceAccountAnnotation']}")
        eso = C["keyVault"]["eso"]
        ss = by.get("SecretStore", {}).get(eso["secretStore"]["name"])
        if ss is not None:
            if str(g(ss, ["metadata", "annotations", "argocd.argoproj.io/sync-wave"])) != eso["secretStore"]["syncWave"]:
                e.append(f"SecretStore key-vault sync-wave != {eso['secretStore']['syncWave']}")
            az = g(ss, ["spec", "provider", "azurekv"]) or {}
            if az.get("authType") != eso["secretStore"]["authType"]:
                e.append(f"SecretStore authType '{az.get('authType')}' != '{eso['secretStore']['authType']}'")
            if g(az, ["serviceAccountRef", "name"]) != eso["secretStore"]["serviceAccountRef"]:
                e.append("SecretStore serviceAccountRef != workorders-eso")
            want_url = env_x(eso["secretStore"]["vaultUrl"], env)
            if str(az.get("vaultUrl")).rstrip("/") != want_url:
                e.append(f"SecretStore vaultUrl '{az.get('vaultUrl')}' != '{want_url}'")
        es = by.get("ExternalSecret", {}).get(eso["externalSecret"]["name"])
        if es is not None:
            if str(g(es, ["metadata", "annotations", "argocd.argoproj.io/sync-wave"])) != eso["externalSecret"]["syncWave"]:
                e.append(f"ExternalSecret workorders-app sync-wave != {eso['externalSecret']['syncWave']}")
            if g(es, ["spec", "target", "name"]) != eso["externalSecret"]["target"]:
                e.append("ExternalSecret target != workorders-app")
            if g(es, ["spec", "target", "creationPolicy"]) != eso["externalSecret"]["creationPolicy"]:
                e.append("ExternalSecret creationPolicy != Owner")
            if str(g(es, ["spec", "refreshInterval"])) != eso["externalSecret"]["refreshInterval"]:
                w.append(f"ExternalSecret refreshInterval '{g(es, ['spec', 'refreshInterval'])}' != '{eso['externalSecret']['refreshInterval']}'")
            ref = g(es, ["spec", "secretStoreRef"]) or {}
            if ref.get("name") != eso["secretStore"]["name"] or ref.get("kind", "SecretStore") != "SecretStore":
                e.append("ExternalSecret secretStoreRef must be SecretStore key-vault")
        route = by.get("HTTPRoute", {}).get("ui-server")
        if route is not None and env in ("uat", "prod"):
            rules = g(route, ["spec", "rules"]) or []
            for path in C["health"]["redirectedInUatProd"]:
                hit = False
                for r in rules:
                    paths = [str(g(m, ["path", "value"])) for m in (r.get("matches") or [])]
                    redirect = any(f.get("type") == "RequestRedirect" for f in (r.get("filters") or []))
                    if redirect and any(p == path or p.startswith(path) for p in paths):
                        hit = True
                if not hit:
                    e.append(f"HTTPRoute ui-server has no RequestRedirect rule for {path} (ADR-D12)")
        if not by.get("NetworkPolicy"):
            w.append("no NetworkPolicy rendered (ADR-D12 default-deny ingress)")
        verdict(cid, rel, e, f"rendered overlay: {len(objs)} objects in {ns}; images, probes, identities and ESO match §7")
        if w:
            out("WARN", cid, rel, "; ".join(w))


def c09_static():
    cid = "C09"
    rel = "gitops/workorders/base/ui-server.yaml"
    d = find_doc(rel, "Deployment", "ui-server") if exists(rel) else None
    if d is None:
        absent(cid, rel, "Deployment ui-server")
    else:
        ctrs = g(d, ["spec", "template", "spec", "containers"]) or []
        c0 = ctrs[0] if ctrs else {}
        e = [f"{p} is not httpGet /alive on 8080" for p in C["health"]["probe"]["probes"] if not probe_ok(c0.get(p) or {}, c0, p)]
        verdict(cid, rel, e, "ui-server probes use /alive on 8080 (static)")
    rel = "gitops/workorders/base/worker.yaml"
    d = find_doc(rel, "Deployment", "worker") if exists(rel) else None
    if d is None:
        absent(cid, rel, "Deployment worker")
    else:
        ctrs = g(d, ["spec", "template", "spec", "containers"]) or []
        e = [f"{p} set on the Worker (no endpoint until WI-04)" for c in ctrs for p in ("startupProbe", "livenessProbe", "readinessProbe") if c.get(p)]
        verdict(cid, rel, e, "worker has no probes (static)")

# --------------------------------------------------------------------------- C10 Server= prefix

def c10_connection_strings():
    cid = "C10"
    key = "ConnectionStrings__SqlConnectionString"
    prefix = C["config"]["connectionStringPrefix"]
    bad_prefix = C["config"]["forbiddenConnectionStringPrefix"]
    seen = False
    for root in ("gitops", "argocd"):
        for rel in walk(root):
            e = []
            for d in docs(rel, cid) or []:
                for s in all_strings(d):
                    if bad_prefix in s:
                        e.append(f"'{bad_prefix}' selects SQLite and LearningTransport (F4)")
                    if s.startswith(key + "="):
                        seen = True
                        if not s[len(key) + 1:].startswith(prefix):
                            e.append(f"{key} does not start with '{prefix}'")
                for dd in all_dicts(d):
                    if dd.get("name") == key and "value" in dd:
                        seen = True
                        if not str(dd["value"]).startswith(prefix):
                            e.append(f"{key} env value does not start with '{prefix}'")
                    if key in dd and isinstance(dd[key], str):
                        seen = True
                        if not dd[key].startswith(prefix) and "{{" not in dd[key]:
                            e.append(f"{key} value does not start with '{prefix}'")
            if e:
                out("FAIL", cid, rel, "; ".join(sorted(set(e))))
    for root in (".octopus",):
        for rel in walk(root, exts=(".ocl",)):
            t = code_text(rel) or ""
            if bad_prefix in t:
                out("FAIL", cid, rel, f"'{bad_prefix}' in an Octopus connection string (F4)")
    if seen:
        out("PASS", cid, "gitops", f"every {key} found starts with '{prefix}'")
    elif exists("gitops"):
        out("FAIL", cid, "gitops", f"no {key} found in the staged overlays")
    else:
        out("SKIP", cid, "gitops", "package tree 'gitops' not staged yet")

# --------------------------------------------------------------------------- C11 Key Vault and ExternalSecrets

SECRET_TOKEN = re.compile(r"(?<![A-Za-z0-9_<-])((?:workorders-(?:ai|api|appinsights|sql)-[a-z0-9-]+)|(?:argocd-(?:repo|octopus|sso)-[a-z0-9-]+)|octopus-gateway-registration(?:-[a-z0-9-]+)?)")
SECRET_SUFFIX = re.compile(r"-(password|apikey|key|connection-string|token|credential|secret|creds)$")


def c11_key_vault():
    cid = "C11"
    kv = C["keyVault"]
    env_names = {s["name"] for s in kv["environmentSecrets"]} | {s["name"] for s in kv["tddOnlySecrets"]}
    plat = {s["name"]: s for s in kv["platformSecrets"]}
    targets = {s.get("targetSecret") for s in kv["platformSecrets"] if s.get("targetSecret")}
    eso_names = {s["name"] for s in kv["environmentSecrets"] if s.get("consumer") == "eso"}
    octopus_only = {s["name"] for s in kv["environmentSecrets"] if s.get("consumer") != "eso"} | {s["name"] for s in kv["tddOnlySecrets"]}
    want_keys = set(C["config"]["secretKeys"])
    found_app_es = False
    found_platform = {}
    for root in ("gitops", "argocd"):
        for rel in walk(root):
            for d in docs(rel, cid) or []:
                if not isinstance(d, dict) or d.get("kind") != "ExternalSecret":
                    continue
                name = g(d, ["metadata", "name"])
                spec = d.get("spec") or {}
                keys = [g(i, ["remoteRef", "key"]) for i in spec.get("data") or []]
                keys += [g(i, ["extract", "key"]) for i in spec.get("dataFrom") or []]
                keys = [k for k in keys if k]
                e = [f"remote key '{k}' is not a §7.8 secret name" for k in keys if k not in env_names and k not in plat]
                store = spec.get("secretStoreRef") or {}
                if name == kv["eso"]["externalSecret"]["name"]:
                    found_app_es = True
                    if store.get("name") != kv["eso"]["secretStore"]["name"]:
                        e.append("workorders-app must read SecretStore key-vault")
                    for k in sorted(eso_names - set(keys)):
                        e.append(f"missing remote key {k}")
                    for k in sorted(set(keys) & octopus_only):
                        e.append(f"{k} is for Octopus only; pods never receive it")
                    got = {i.get("secretKey") for i in spec.get("data") or []}
                    if spec.get("data") and not want_keys <= got:
                        e.append(f"secret keys missing: {sorted(want_keys - got)}")
                else:
                    tgt = g(spec, ["target", "name"]) or name
                    for k in keys:
                        if k in plat:
                            found_platform[k] = rel
                            ps = plat[k]
                            if ps.get("targetSecret") and tgt != ps["targetSecret"]:
                                e.append(f"{k} must sync to Secret {ps['targetSecret']}, not {tgt}")
                            if store.get("name") != kv["eso"]["clusterSecretStore"]["name"] or store.get("kind") != "ClusterSecretStore":
                                e.append(f"{k} must read ClusterSecretStore {kv['eso']['clusterSecretStore']['name']}")
                verdict(cid, rel, e, f"ExternalSecret {name} uses §7.8 names")
    if exists("gitops") and not found_app_es:
        out("FAIL", cid, "gitops", "ExternalSecret workorders-app not found")
    if exists("argocd"):
        for k, ps in plat.items():
            if k not in found_platform:
                status = "WARN" if ps.get("onlyIfFederationUnavailable") else "FAIL"
                out(status, cid, "argocd", f"no ExternalSecret reads platform secret {k}")
    # Secret-name tokens elsewhere (Octopus scripts, Terraform) must be contract names.
    known = env_names | set(plat) | targets | {"argocd-repo-creds"}
    for root, exts in ((".octopus", (".ocl",)), ("terraform", (".tf", ".example")), ("octopus", (".tf", ".example"))):
        for rel in walk(root, exts=exts):
            t = code_text(rel) or ""
            bad = sorted({m.group(1) for m in SECRET_TOKEN.finditer(t)
                          if m.group(1) not in known and SECRET_SUFFIX.search(m.group(1))})
            if bad:
                out("FAIL", cid, rel, f"Key Vault or Secret names not in §7.8: {bad}")

# --------------------------------------------------------------------------- C12 runbooks

def c12_runbooks():
    cid = "C12"
    contract = {(r["project"], r["name"]): r for r in C["octopus"]["runbooks"]}
    all_envs = set(C["octopus"]["environments"])
    for (project, name), r in contract.items():
        rel = f".octopus/{project}/runbooks/{name}.ocl"
        t = code_text(rel)
        if t is None:
            absent(cid, rel, f"runbook {name}")
            continue
        e, w = [], []
        found = {v for v in quoted_list(t, r"environments") if v in all_envs or v.startswith("infra") or v in ENVS}
        if not found:
            w.append("environment scope not found in OCL [VERIFY config-as-code runbook layout]")
        elif not found <= set(r["environments"]):
            e.append(f"scoped to {sorted(found)}; §7.2 allows {r['environments']}")
        if name == "env-destroy":
            if re.search(r'"(infra-prod|prod)"', t):
                e.append("env-destroy references a prod environment; it exists for infra-nonprod only (ADR-D10)")
        elif re.search(r"TerraformDestroy|terraform\s+destroy", t):
            e.append("only env-destroy may destroy Terraform resources")
        if project == "workorders-infrastructure":
            if re.search(r'=\s*"(azure-runtime-provisioner|Azure Runtime Provisioner)"', t):
                e.append("references the stored provisioner directly; use #{Azure.LifecycleAccount}")
            if name.startswith("env-") and "terraform/environment" not in t:
                e.append("Terraform source directory terraform/environment not referenced")
        verdict(cid, rel, e, f"runbook {name} scoped to {sorted(found) or r['environments']}")
        if w:
            out("WARN", cid, rel, "; ".join(w))
    for project in ("workorders", "workorders-infrastructure"):
        for rel in walk(f".octopus/{project}/runbooks", exts=(".ocl",)):
            name = os.path.basename(rel)[:-4]
            if (project, name) not in contract:
                out("FAIL", cid, rel, f"runbook {name} is not in §7.2")

# --------------------------------------------------------------------------- C13 deployment process

def c13_process():
    cid = "C13"
    rel = ".octopus/workorders/deployment_process.ocl"
    t = code_text(rel)
    if t is None:
        absent(cid, rel, "deployment process")
    else:
        steps = C["octopus"]["deploymentProcess"]["steps"]
        want = [s["slug"] for s in steps]
        found = list(blocks(t, r'^\s*step\s+"([^"]+)"\s*\{'))
        got = [n for n, _ in found]
        if got != want:
            out("FAIL", cid, rel, f"step order {got} != §7.2 {want}")
        else:
            out("PASS", cid, rel, "twelve steps in §7.2 order")
        bodies = dict(found)
        for s in steps:
            body = bodies.get(s["slug"])
            if body is None:
                continue
            e, w = [], []
            envs = {v for v in quoted_list(body, r"environments") if v in ENVS}
            if s.get("environments"):
                if not envs:
                    w.append("environment scope not found")
                elif envs != set(s["environments"]):
                    e.append(f"environments {sorted(envs)} != {s['environments']}")
            if s.get("channels"):
                chans = [v.lower() for v in quoted_list(body, r"channels")]
                if not any(c.lower() in chans for c in s["channels"]):
                    w.append(f"channel scope {s['channels']} not found")
            for key in ("packages",):
                for p in s.get(key, []):
                    if p not in body:
                        e.append(f"package {p} not referenced")
            for key in ("feed", "container", "account", "endpoint", "timeoutVariable"):
                v = s.get(key)
                if v and v.strip("#{}") not in body:
                    e.append(f"{key} '{v}' not referenced")
            if s.get("workerPool") and s["workerPool"].strip("#{}") not in body:
                e.append(f"worker pool '{s['workerPool']}' not referenced")
            if s.get("team"):
                slug = s["team"].lower().replace(" ", "-")
                if s["team"] not in body and slug not in body.lower():
                    w.append(f"team '{s['team']}' not referenced (name or slug)")
            if s["slug"] == "update-argo-cd-image-tags" and s["type"] not in body:
                e.append(f"action type {s['type']} not used")
            if s["slug"] == "acceptance-tests" and "Acceptance.AllowDestructiveReset" not in body:
                e.append("interlock Acceptance.AllowDestructiveReset not referenced (ADR-C11)")
            if s["slug"] == "report-commit-status" and "GitHub.StatusEnabled" not in body:
                e.append("GitHub.StatusEnabled guard not referenced")
            if s["slug"] == "smoke-test" and "Smoke.FailOnDegraded" not in body:
                e.append("Smoke.FailOnDegraded not referenced")
            verdict(cid, rel, e, f"step {s['slug']} matches §7.2")
            if w:
                out("WARN", cid, rel, f"step {s['slug']}: " + "; ".join(w))
    rel = ".octopus/workorders/deployment_settings.ocl"
    t = code_text(rel)
    if t is None:
        absent(cid, rel, "deployment settings")
    else:
        e = [] if re.search(r"allow_deployments_to_no_targets\s*=\s*true", t) else ["allow_deployments_to_no_targets = true not set"]
        verdict(cid, rel, e, "connectivity allows deployments to no targets")

# --------------------------------------------------------------------------- C14 variables

def c14_variables():
    cid = "C14"
    by_file = {}
    for v in C["octopus"]["variables"]:
        by_file.setdefault(v["file"], []).append(v)
    sensitive = {v["name"] for v in C["octopus"]["sensitiveVariables"]}
    for v in C["octopus"].get("promptedVariables", []):
        by_file.setdefault(v["file"], []).append(dict(v, prompted=True))
    all_names = {v["name"] for v in C["octopus"]["variables"]}
    for rel, expected in by_file.items():
        t = code_text(rel)
        if t is None:
            absent(cid, rel, "project variables")
            continue
        found = dict()
        for n, body in blocks(t, r'^\s*variable\s+"([^"]+)"\s*\{'):
            found[n] = found.get(n, "") + "\n" + body
        e, w = [], []
        want = {v["name"] for v in expected}
        for n in sorted(want - set(found)):
            e.append(f"missing variable {n}")
        for n in sorted(set(found) - want):
            if n in sensitive:
                e.append(f"sensitive variable {n} must live in the Octopus database, not Git")
            elif n in all_names:
                w.append(f"§7.2 variable {n} is also defined here; §7.2 places it in another project")
            else:
                e.append(f"variable {n} is not in §7.2")
        for v in expected:
            body = found.get(v["name"])
            if body is None:
                continue
            vals = list((v.get("values") or {}).values()) + ([v["value"]] if "value" in v else [])
            for val in vals:
                if str(val) not in body:
                    w.append(f"{v['name']}: value '{val}' not found")
        lc = found.get("Azure.LifecycleAccount")
        if lc:
            for name, vb in blocks(lc, r'^\s*value\s+"([^"]*)"\s*\{'):
                if "provisioner" in name.lower() and "infra-prod" in vb:
                    e.append("Azure.LifecycleAccount scopes the stored provisioner to infra-prod (ADR-C10)")
        verdict(cid, rel, e, f"variables {sorted(found)} match §7.2")
        if w:
            out("WARN", cid, rel, "; ".join(w))

# --------------------------------------------------------------------------- C15 Octopus Terraform

def c15_octopus_terraform():
    cid = "C15"
    files = walk("octopus/terraform", exts=(".tf", ".example"))
    if not files:
        absent(cid, "octopus/terraform/main.tf", "Octopus Terraform")
        return
    t = "\n".join(code_text(f) or "" for f in files)
    o = C["octopus"]
    names = []
    names += o["environments"]
    names += [lc["name"] for lc in o["lifecycles"]]
    names += [ch["name"] for ch in o["channels"]]
    names += [p["name"] for p in o["projects"]] + [o["projectGroup"], o["gitCredential"]]
    names += o["workerPools"]["kubernetes"] + [o["workerPools"]["dynamic"]]
    names += [a["name"] for a in o["accounts"]["new"]] + [a["name"] for a in o["accounts"]["stored"]]
    names += [o["feeds"]["acr"]["name"]]
    names += [s["name"] for s in o["libraryVariableSets"]["new"]] + list(o["libraryVariableSets"]["stored"])
    names += o["teams"] + [r["name"] for r in o["userRoles"]]
    names += [s["name"] for s in o["serviceAccounts"]] + [o["serviceAccounts"][0]["oidcIdentity"]["name"]]
    names += [f["name"] for f in o["freezes"]]
    names += [p["basePath"] for p in o["projects"]]
    missing = [n for n in names if not name_present(n, t)]
    for s in o["libraryVariableSets"]["new"]:
        missing += [k for k in s["variables"] if k not in t]
    if o["terraformVariables"]["tddAutoDeploy"] not in t:
        missing.append(o["terraformVariables"]["tddAutoDeploy"])
    if o["serviceAccounts"][0]["oidcIdentity"]["issuer"] not in t:
        missing.append(o["serviceAccounts"][0]["oidcIdentity"]["issuer"])
    verdict(cid, "octopus/terraform", [f"§7.2 name not found: {sorted(set(missing))}"] if missing else [],
            f"all {len(names)} §7.2 object names present")
    e = []
    for n in o["libraryVariableSets"]["stored"] + [a["name"] for a in o["accounts"]["stored"]] + [o["gitCredential"]]:
        for m in re.finditer(r'resource\s+"(octopusdeploy_[a-z_]+)"\s+"([^"]+)"\s*\{', t):
            body = next((b for _, b in blocks(t[m.start():], r'^\s*resource\s+"([^"]+)"')), "")
            if re.search(r'name\s*=\s*"' + re.escape(n) + '"', body):
                e.append(f"stored object '{n}' is created by resource {m.group(1)}.{m.group(2)}; look it up instead")
    verdict(cid, "octopus/terraform", e, "stored account, credential and variable sets are looked up, never created")
    if "1.20.0" not in t:
        out("WARN", cid, "octopus/terraform", "provider OctopusDeploy/octopusdeploy 1.20.0 pin not found")

# --------------------------------------------------------------------------- C16 Codefresh

def step_map(d):
    """name -> step dict for every `steps:` mapping at any depth."""
    found = {}
    for dd in all_dicts(d):
        st = dd.get("steps")
        if isinstance(st, dict):
            for k, v in st.items():
                found[k] = v if isinstance(v, dict) else {}
    return found


def c16_codefresh():
    cid = "C16"
    cf = C["codefresh"]
    forbidden_ctx = set(cf["unattachedStores"])
    for p in cf["pipelines"]:
        rel = p["spec"]
        if not exists(rel):
            absent(cid, rel, f"spec for {p['name']}")
            continue
        spec_doc = None
        for d in docs(rel, cid) or []:
            if g(d, ["metadata", "name"]) == p["name"]:
                spec_doc = d
        if spec_doc is None:
            out("FAIL", cid, rel, f"no pipeline spec with metadata.name {p['name']}")
            continue
        s = spec_doc.get("spec") or {}
        e, w = [], []
        if str(spec_doc.get("kind", "")).lower() != "pipeline":
            e.append(f"kind '{spec_doc.get('kind')}' != pipeline")
        rt = g(s, ["runtimeEnvironment", "name"])
        if rt != p["runtime"]:
            e.append(f"runtimeEnvironment '{rt}' != '{p['runtime']}'")
        ctx = s.get("contexts") or []
        if sorted(ctx) != sorted(p["contexts"]):
            e.append(f"contexts {ctx} != {p['contexts']}")
        if set(ctx) & forbidden_ctx:
            e.append(f"attaches stored contexts {sorted(set(ctx) & forbidden_ctx)} (R5)")
        tpl = s.get("specTemplate") or {}
        want_path = p["yaml"][4:] if p["yaml"].startswith("app:") else p["yaml"]
        if str(tpl.get("path", "")).lstrip("./") != want_path.lstrip("./"):
            e.append(f"specTemplate.path '{tpl.get('path')}' != '{want_path}'")
        if p.get("revision") and tpl.get("revision") != p["revision"]:
            e.append(f"specTemplate.revision '{tpl.get('revision')}' != '{p['revision']}'")
        if p.get("concurrency") is not None and s.get("concurrency") != p["concurrency"]:
            e.append(f"concurrency '{s.get('concurrency')}' != {p['concurrency']}")
        if not s.get("triggers") and not s.get("cronTriggers"):
            w.append("no triggers defined")
        if p.get("gitIntegration") and p["gitIntegration"] not in (text(rel) or ""):
            e.append(f"Git integration {p['gitIntegration']} not referenced")
        yrel = p["yaml"]
        ytext = text(yrel)
        if p.get("status") and p["status"] not in (text(rel) or "") + (ytext or ""):
            w.append(f"status name {p['status']} not found in spec or pipeline YAML [VERIFY how Codefresh names statuses]")
        verdict(cid, rel, e, f"{p['name']}: runtime, contexts, template and concurrency match §7.7")
        if w:
            out("WARN", cid, rel, "; ".join(w))
        if ytext is None:
            absent(cid, yrel, f"pipeline YAML for {p['name']}")
            continue
        ydocs = docs(yrel, cid) or []
        steps = {}
        for d in ydocs:
            steps.update(step_map(d))
        e = []
        name = p["name"]
        if name in ("workorders/ci", "workorders/release"):
            e += [f"missing gate step {n}" for n in cf["gateSteps"] if n not in steps]
        if name == "workorders/ci":
            e += [f"release-only step {n} present in ci" for n in cf["releaseOnlySteps"] if n in steps]
        if name == "workorders/release":
            e += [f"missing release step {n}" for n in cf["releaseOnlySteps"] if n not in steps]
            e += c16_handoff(steps)
        if name == "platform-env/env-checks":
            for sub in p["validateAllSubcommands"]:
                if not re.search(r"validate-all\.sh\s+" + sub + r"\b", ytext):
                    e.append(f"does not run validate-all.sh {sub}")
            both = ytext + (text(rel) or "")
            if "PLATFORM_BOT_AUTHORS" not in both:
                out("WARN", cid, yrel, "PLATFORM_BOT_AUTHORS is not set: on main, `validate-all.sh boundaries` "
                    "with CI=true fails because the bot-path audit (§6.2) has no machine-user identity")
            if not re.search(r"\bCI\b\s*[:=]\s*['\"]?true", both):
                out("WARN", cid, yrel, "CI=true is not set explicitly: missing tools would be skipped instead of failing")
        for sn, st in steps.items():
            if str(st.get("type", "")).split(":")[0] in cf["forbiddenStepTypes"]:
                e.append(f"step {sn} uses forbidden type {st.get('type')}")
        verdict(cid, yrel, e, f"{p['name']}: steps match §7.7")


def c16_handoff(steps):
    e = []
    cf = C["codefresh"]
    by_type = {}
    for sn, st in steps.items():
        t = str(st.get("type", ""))
        by_type.setdefault(t.split(":")[0], []).append(st)
    for h in cf["handoff"]:
        found = by_type.get(h["step"])
        if not found:
            e.append(f"handoff step type {h['step']} not used")
            continue
        args = found[0].get("arguments") or {}
        want = h.get("arguments") or {}
        if isinstance(want, dict):
            for k, v in want.items():
                if str(args.get(k)).lower() != str(v).lower():
                    e.append(f"{h['step']}: {k}={args.get(k)!r} != {v!r}")
        for k in h.get("forbiddenArguments", []):
            if k in args:
                e.append(f"{h['step']}: {k} must not be passed (ADR-D7)")
        first = h.get("releaseNotesFirstLine")
        if first and not str(args.get("RELEASE_NOTES", "")).lstrip().startswith(first.split("$")[0]):
            e.append(f"{h['step']}: RELEASE_NOTES must start with '{first}'")
        if h["step"] == "octopusdeploy-push-build-information":
            ids = args.get("PACKAGE_IDS")
            flat = " ".join(ids) if isinstance(ids, list) else str(ids)
            missing = [p for p in C["packages"]["buildInformationPackageIds"] if p not in flat]
            if missing:
                e.append(f"build information PACKAGE_IDS lacks {missing}")
    return e

# --------------------------------------------------------------------------- C17 environment config

def c17_env_config():
    cid = "C17"
    lits = C["config"]["literals"]
    for env in ENVS:
        rel = x(C["pins"]["configDir"], env=env) + "/kustomization.yaml"
        ds = docs(rel, cid) if exists(rel) else None
        if ds is None:
            if not exists(rel):
                absent(cid, rel, f"config overlay for {env}")
            continue
        d = ds[0] if ds and isinstance(ds[0], dict) else {}
        e, w = [], []
        if C["pins"]["configBase"] not in (d.get("resources") or []):
            e.append(f"resources lacks {C['pins']['configBase']}")
        if d.get("namespace") != f"workorders-{env}":
            e.append(f"namespace '{d.get('namespace')}' != 'workorders-{env}'")
        if "images" in d:
            e.append("config overlay sets images; only the pin file may (§7.6)")
        comps = " ".join(str(c) for c in d.get("components") or [])
        for c in C["pins"]["unreferencedComponents"]:
            if c in comps:
                e.append(f"component {c} must stay unreferenced (ADR-C3)")
        gens = [gen for gen in d.get("configMapGenerator") or [] if gen.get("name") == C["config"]["configMap"]]
        if not gens:
            e.append(f"no configMapGenerator {C['config']['configMap']}")
        else:
            literals = {}
            for gen in gens:
                for lit in gen.get("literals") or []:
                    k, _, v = str(lit).partition("=")
                    literals[k] = v
            for k, v in lits.items():
                if k not in literals:
                    e.append(f"literal {k} missing")
                    continue
                want = env_x(str(v), env)
                if k in ("ASPNETCORE_ENVIRONMENT", "ApiKeyAuthentication__Enabled", "ASPNETCORE_FORWARDEDHEADERS_ENABLED"):
                    if literals[k] != want:
                        e.append(f"{k}={literals[k]} != {want}")
                elif literals[k] != want:
                    w.append(f"{k} differs from §7.9: '{literals[k]}'")
        env_dir_text = "\n".join(text(f) or "" for f in walk(x(C["pins"]["configDir"], env=env), exts=None))
        base_text = "\n".join(text(f) or "" for f in walk(C["pins"]["baseDir"], exts=None))
        want_bus = env_x(C["config"]["workerOnly"]["RemotableBus__ApiUrl"], env)
        if want_bus not in env_dir_text and want_bus not in base_text:
            e.append(f"RemotableBus__ApiUrl {want_bus} not set for the worker")
        if f"<{env}-hostname>" not in env_dir_text:
            w.append(f"hostname placeholder <{env}-hostname> not patched into the HTTPRoute")
        if C["kubernetes"]["workloadIdentity"]["serviceAccountAnnotation"] not in env_dir_text:
            w.append("service-account client IDs are not patched per environment")
        verdict(cid, rel, e, f"config overlay for {env} matches §7.9 and ADR-D6")
        if w:
            out("WARN", cid, rel, "; ".join(w))

# --------------------------------------------------------------------------- C18 Kyverno

def c18_kyverno():
    cid = "C18"
    k = C["kyverno"]
    rel = k["imageValidatingPolicy"]["file"]
    t = code_text(rel)
    if t is None:
        absent(cid, rel, "release signature policy")
    else:
        e = []
        if "ImageValidatingPolicy" not in t:
            e.append("kind ImageValidatingPolicy not used")
        for key in ("issuer", "subject", "images"):
            if k["imageValidatingPolicy"][key] not in t:
                e.append(f"{key} '{k['imageValidatingPolicy'][key]}' not found")
        if re.search(r"mutateDigest:\s*true", t):
            e.append("mutateDigest: true (ADR-D11)")
        verdict(cid, rel, e, "keyless identity and image scope match ADR-D11 and E33")
    for cluster, mode in k["modes"].items():
        orel = x(k["overlays"], cluster=cluster) + "/kustomization.yaml"
        t = code_text(orel)
        if t is None:
            absent(cid, orel, f"Kyverno overlay {cluster}")
        else:
            verdict(cid, orel, [] if mode in t else [f"mode {mode} not set"], f"{cluster} runs {mode}")
        arel = f"argocd/clusters/{cluster}/addons/kyverno.yaml"
        at = code_text(arel)
        if at is None:
            absent(cid, arel, f"kyverno add-on {cluster}")
        else:
            want = x(k["overlays"], cluster=cluster)
            verdict(cid, arel, [] if want in at else [f"kyverno-policies does not point at {want}"],
                    f"kyverno-policies Application points at {want}")

# --------------------------------------------------------------------------- C19 Terraform layers

def c19_terraform():
    cid = "C19"
    tf = C["azure"]["terraform"]
    ffiles = walk(tf["foundation"]["path"], exts=(".tf", ".example"))
    if not ffiles:
        absent(cid, tf["foundation"]["path"] + "/versions.tf", "foundation layer")
    else:
        t = "\n".join(code_text(f) or "" for f in ffiles)
        e, w = [], []
        if tf["foundation"]["stateKey"] not in t:
            e.append(f"state key {tf['foundation']['stateKey']} not found")
        w += [f"resource group {rg} not found" for rg in C["azure"]["resourceGroups"] if not name_present(rg, t)]
        if "CanNotDelete" not in t:
            w.append("CanNotDelete locks not found")
        if "azurerm_role_assignment" not in t:
            e.append("no role assignments: the foundation must hold every grant (ADR-D10)")
        verdict(cid, tf["foundation"]["path"], e, "foundation layer holds state key, groups and grants")
        if w:
            out("WARN", cid, tf["foundation"]["path"], "; ".join(w))
    efiles = walk(tf["environment"]["path"], exts=(".tf", ".example"))
    if not efiles:
        absent(cid, tf["environment"]["path"] + "/versions.tf", "environment layer")
    else:
        t = "\n".join(code_text(f) or "" for f in efiles)
        e, w = [], []
        if "argocd/bootstrap" not in t:
            e.append("bootstrap does not read argocd/bootstrap/* (cross-package interface, §11.6)")
        if "octopus-worker-" not in t:
            w.append("Octopus worker releases octopus-worker-<env> not found")
        w += [f"cluster {c} not found" for c in C["azure"]["clusters"].values() if not name_present(c, t)]
        if "environment-" not in t and "StateKey" not in t:
            w.append("state key environment-{class}.tfstate not referenced (may come from the runbook backend settings)")
        # Every argocd/bootstrap file the layer reads must exist (gitops-architect owns them).
        for m in sorted(set(re.findall(r"argocd/bootstrap/([A-Za-z0-9_.${}-]+\.ya?ml)", t))):
            names = [m]
            if "${" in m:
                names = [re.sub(r"\$\{[^}]*\}", cl, m) for cl in CLUSTERS]
            for n in names:
                if not exists("argocd/bootstrap/" + n):
                    e.append(f"reads argocd/bootstrap/{n}, which does not exist")
        verdict(cid, tf["environment"]["path"], e, "environment layer reads existing Argo CD bootstrap values")
        if w:
            out("WARN", cid, tf["environment"]["path"], "; ".join(w))
    for name in ("env-plan", "env-apply", "env-destroy"):
        rel = f".octopus/workorders-infrastructure/runbooks/{name}.ocl"
        t = code_text(rel)
        if t is not None and "Terraform.StateKey" not in t:
            out("WARN", cid, rel, "backend key Terraform.StateKey not referenced")

# --------------------------------------------------------------------------- C22 runbook inputs

def c22_runbook_inputs():
    """Octopus runbooks (octopus-architect) run terraform/environment (sre-security):
    every TF_VAR_<name> they set must be a variable of that layer (§11.6)."""
    cid = "C22"
    files = walk(".octopus/workorders-infrastructure/runbooks", exts=(".ocl",))
    if not files:
        absent(cid, ".octopus/workorders-infrastructure/runbooks/env-apply.ocl", "infrastructure runbooks")
        return
    tf_files = walk(C["azure"]["terraform"]["environment"]["path"], exts=(".tf",))
    declared = set()
    for f in tf_files:
        declared |= set(re.findall(r'^\s*variable\s+"([^"]+)"', code_text(f) or "", re.M))
    for rel in files:
        t = code_text(rel) or ""
        names = sorted(set(re.findall(r"TF_VAR_([A-Za-z0-9_<>-]+)", t)))
        if not names:
            continue
        e = []
        for n in names:
            if n.startswith("<"):
                e.append(f"TF_VAR_{n} is a placeholder; use the terraform/environment variable name")
            elif tf_files and n not in declared:
                e.append(f"TF_VAR_{n} is not declared in terraform/environment")
        if not tf_files:
            out("SKIP", cid, rel, "terraform/environment not staged yet; cannot resolve " + ", ".join(names))
            continue
        verdict(cid, rel, e, f"runbook Terraform inputs {names} are declared in terraform/environment")

# --------------------------------------------------------------------------- C20 previews

def c20_previews():
    cid = "C20"
    pv = C["argocd"]["previews"]
    rel = pv["file"]
    if not exists(rel):
        absent(cid, rel, "previews ApplicationSet (phase 6)")
        return
    d = find_doc(rel, "ApplicationSet", pv["applicationSet"])
    if d is None:
        out("FAIL", cid, rel, f"no ApplicationSet {pv['applicationSet']}")
        return
    t = text(rel) or ""
    e = []
    tpl = g(d, ["spec", "template"]) or {}
    if g(tpl, ["spec", "project"]) != pv["project"]:
        e.append(f"template project '{g(tpl, ['spec', 'project'])}' != '{pv['project']}' (never templated)")
    if g(tpl, ["metadata", "name"]) != pv["appName"]:
        e.append(f"application name '{g(tpl, ['metadata', 'name'])}' != '{pv['appName']}'")
    if g(tpl, ["spec", "source", "path"]) != pv["path"]:
        e.append(f"path '{g(tpl, ['spec', 'source', 'path'])}' != '{pv['path']}'")
    if pv["imageTag"] not in t:
        e.append(f"image tag template '{pv['imageTag']}' not found")
    if str(pv["requeueAfterSeconds"]) not in t:
        e.append("requeueAfterSeconds 300 not found")
    if "preview" not in t:
        e.append("label filter [preview] not found")
    if "resources-finalizer.argocd.argoproj.io" not in t:
        e.append("preview Applications need resources-finalizer.argocd.argoproj.io")
    if "CreateNamespace=true" not in t:
        e.append("CreateNamespace=true not set")
    verdict(cid, rel, e, "previews ApplicationSet matches §7.3 and ADR-C4")

# --------------------------------------------------------------------------- C21 placeholders

PLACEHOLDER = re.compile(r"<([A-Za-z0-9{][A-Za-z0-9_.{}*/ -]{0,60})>")


def c21_placeholders():
    """Placeholders outside §7 in configuration (not in comments, Markdown or shell usage text)."""
    cid = "C21"
    known = set()
    with open(CONTRACTS, encoding="utf-8") as f:
        ctext = f.read()
    for m in PLACEHOLDER.finditer(ctext):
        tok = m.group(0)
        known.add(tok)
        for env in ENVS:
            known.add(env_x(tok, env))
        for cl in CLUSTERS:
            known.add(x(tok, cluster=cl, **{"class": cl}))
    patterns = [re.compile("^" + re.escape(p).replace(r"\*", r"[A-Za-z0-9._-]+") + "$")
                for p in C.get("placeholderPatterns", [])]

    def is_known(tok):
        return tok in known or any(p.match(tok) for p in patterns)

    roots = ["argocd", "gitops", ".octopus", "octopus", "codefresh", "policies", "terraform", ".gitleaks.toml",
             "app:.codefresh", "app:containers"]
    for root in roots:
        if root.startswith("app:") and not APP:
            continue
        rels = [root] if exists(root) and os.path.isfile(P(root)) else walk(root, exts=None)
        for rel in rels:
            if rel.endswith((".md", ".sh")):
                continue
            t = code_text(rel) or ""
            unknown = sorted({m.group(0) for m in PLACEHOLDER.finditer(t) if not is_known(m.group(0))})
            if unknown:
                out("WARN", cid, rel, f"placeholders not in §7 or the contracts: {unknown}")

# --------------------------------------------------------------------------- main

for check in (c02_annotations, c03_applications, c04_projects, c05_namespaces, c06_bootstrap, c07_images,
              c08_pins, c09_rendered, c10_connection_strings, c11_key_vault, c12_runbooks, c13_process,
              c14_variables, c15_octopus_terraform, c16_codefresh, c17_env_config, c18_kyverno,
              c19_terraform, c20_previews, c21_placeholders, c22_runbook_inputs):
    try:
        check()
    except Exception as ex:  # a broken check must not hide the others
        out("FAIL", "C99", "-", f"{check.__name__} crashed: {type(ex).__name__}: {ex}")

print(f"consistency: {COUNTS['PASS']} pass, {COUNTS['FAIL']} fail, {COUNTS['WARN']} warn, {COUNTS['SKIP']} skip")
sys.exit(1 if COUNTS["FAIL"] else 0)
PYEOF
