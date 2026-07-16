#!/usr/bin/env node
// Discovers npm projects under one or more target directories, runs
// `npm audit --json` on each, evaluates them against a severity threshold,
// prints a concise report, and writes aggregate npm-audit-result.{md,json}.
//
// Usage:
//   node audit.mjs [--dir <path> ...] [--fail-on critical|high|moderate|low]
//                  [--out <dir>] [--depth <n>]
//
// A "project" is any directory containing package.json (scanning stops at that
// directory — npm workspaces are audited from their root). node_modules, .git,
// hidden dirs, and the report output dir are skipped during discovery.
//
// Status:
//   pass          — all discovered projects audited clean of threshold breaches.
//   fail          — at least one project breaches the threshold, OR its audit
//                   could not be run (missing lockfile, or node/npm not
//                   installed while a package.json is present) — fail safe.
//   notapplicable — no npm project (package.json) found under the root.
//
// Default threshold is `high` (any high or critical fails).
// Exit code: 0 = pass, 1 = fail, 2 = notapplicable.

import { execFile } from "node:child_process";
import { promisify } from "node:util";
import { mkdirSync, writeFileSync, readFileSync, existsSync, readdirSync } from "node:fs";
import { join, resolve, isAbsolute, relative } from "node:path";

const run = promisify(execFile);

const ORDER = ["low", "moderate", "high", "critical"];
const ZERO = { critical: 0, high: 0, moderate: 0, low: 0 };
const IGNORE_DIRS = new Set(["node_modules", ".git", ".hg", ".svn", "codebase-audit-report"]);

function parseArgs(argv) {
  const args = { dirs: [], failOn: "high", out: null, depth: 4 };
  for (let i = 0; i < argv.length; i++) {
    if (argv[i] === "--dir") args.dirs.push(argv[++i]);
    else if (argv[i] === "--fail-on") args.failOn = argv[++i];
    else if (argv[i] === "--out") args.out = argv[++i];
    else if (argv[i] === "--depth") args.depth = parseInt(argv[++i], 10);
  }
  if (!ORDER.includes(args.failOn)) {
    console.error(`Invalid --fail-on "${args.failOn}". Use one of: ${ORDER.join(", ")}`);
    process.exit(1);
  }
  if (!args.dirs.length) args.dirs = [process.cwd()];
  args.dirs = args.dirs.map((d) => (isAbsolute(d) ? d : resolve(process.cwd(), d)));
  args.root = args.dirs[0];
  args.out = args.out
    ? (isAbsolute(args.out) ? args.out : resolve(process.cwd(), args.out))
    : resolve(args.root, "codebase-audit-report/metrics/npm-audit");
  return args;
}

// Find every npm project (dir with package.json) under the given roots, up to
// `depth` levels deep. Stops descending once a package.json is found.
function discoverProjects(roots, depth) {
  const found = new Set();
  const walk = (dir, level) => {
    if (existsSync(join(dir, "package.json"))) {
      found.add(resolve(dir));
      return; // don't descend into a project's subtree
    }
    if (level >= depth) return;
    let entries;
    try {
      entries = readdirSync(dir, { withFileTypes: true });
    } catch {
      return;
    }
    for (const e of entries) {
      if (!e.isDirectory()) continue;
      if (IGNORE_DIRS.has(e.name) || e.name.startsWith(".")) continue;
      walk(join(dir, e.name), level + 1);
    }
  };
  for (const r of roots) walk(r, 0);
  return [...found].sort();
}

function installedVersions(dir) {
  const map = {};
  for (const file of ["package-lock.json", "npm-shrinkwrap.json"]) {
    try {
      const lock = JSON.parse(readFileSync(join(dir, file), "utf8"));
      for (const [p, meta] of Object.entries(lock.packages || {})) {
        if (!p.startsWith("node_modules/") || !meta.version) continue;
        const name = p.slice(p.lastIndexOf("node_modules/") + "node_modules/".length);
        map[name] = meta.version;
      }
      if (Object.keys(map).length) break;
    } catch {
      /* try next candidate */
    }
  }
  return map;
}

// Verify both Node.js and npm can be invoked. The audit engine already runs
// under Node, but check explicitly so a broken/absent `node` on PATH is caught
// too; `npm audit` needs both. Returns the name of the first missing tool, or
// null if both are runnable.
async function missingToolchain() {
  for (const tool of ["node", "npm"]) {
    try {
      await run(tool, ["--version"]);
    } catch {
      return tool;
    }
  }
  return null;
}

// Returns {report} on success or {error} (human-readable string) on failure.
async function getAuditJson(dir) {
  let stdout;
  try {
    ({ stdout } = await run("npm", ["audit", "--json"], { cwd: dir, maxBuffer: 32 * 1024 * 1024 }));
  } catch (err) {
    if (err.stdout) stdout = err.stdout;
    else return { error: (err.stderr || err.message || "npm audit failed").trim() };
  }
  let report;
  try {
    report = JSON.parse(stdout);
  } catch {
    return { error: "npm audit did not return valid JSON" };
  }
  if (report.error) {
    const e = report.error;
    return { error: `${e.code || "error"}: ${e.summary || "npm audit failed"}` };
  }
  if (!report.metadata || !report.metadata.vulnerabilities) {
    return { error: "npm audit returned no vulnerability metadata" };
  }
  return { report };
}

// Turn a raw audit report into the package list (all severities).
function buildPackages(report, dir, threshold) {
  const versions = installedVersions(dir);
  const packages = [];
  for (const [name, info] of Object.entries(report.vulnerabilities || {})) {
    const version = versions[name];
    const titles = (info.via || [])
      .filter((v) => v && typeof v === "object" && v.title)
      .map((v) => v.title);
    const unique = [...new Set(titles)];
    const entry = {
      package: version ? `${name}@${version}` : name,
      severity: info.severity,
      failsThreshold: ORDER.indexOf(info.severity) >= threshold,
    };
    if (unique.length) {
      const shown = unique.slice(0, 3).join("; ");
      entry.reason = unique.length > 3 ? `${shown} (+${unique.length - 3} more)` : shown;
    }
    if (info.range) entry.vulnerableRange = info.range;
    packages.push(entry);
  }
  packages.sort(
    (a, b) => ORDER.indexOf(b.severity) - ORDER.indexOf(a.severity) || a.package.localeCompare(b.package)
  );
  return packages;
}

// Audit a single project directory -> result object.
async function auditProject(dir, failOn) {
  const threshold = ORDER.indexOf(failOn);
  const { report, error } = await getAuditJson(dir);
  if (error) {
    return {
      dir,
      status: "fail",
      ran: false,
      message: `npm audit could not be run: ${error}. Ensure an installed lockfile exists (run \`npm install\` first).`,
      vulnerabilities: { ...ZERO },
      total: 0,
      packages: [],
    };
  }
  const c = report.metadata.vulnerabilities;
  const sev = { critical: c.critical || 0, high: c.high || 0, moderate: c.moderate || 0, low: c.low || 0 };
  const total = sev.critical + sev.high + sev.moderate + sev.low;
  const passes = !ORDER.some((lvl, i) => i >= threshold && sev[lvl] > 0);
  return {
    dir,
    status: passes ? "pass" : "fail",
    ran: true,
    vulnerabilities: sev,
    total,
    packages: buildPackages(report, dir, threshold),
  };
}

const BADGE = { pass: "✅ PASS", fail: "❌ FAIL", notapplicable: "➖ N/A" };

function sumCounts(projects) {
  const agg = { ...ZERO };
  for (const p of projects) for (const k of ORDER) agg[k] += p.vulnerabilities[k];
  return agg;
}

// Render the aggregate report to console + files, then exit.
function finish({ status, message, projects, failOn, root, out }) {
  const threshold = ORDER.indexOf(failOn);
  const agg = sumCounts(projects);
  const aggTotal = ORDER.reduce((s, k) => s + agg[k], 0);
  const rel = (d) => relative(root, d) || ".";

  // ---- console ----
  const line = "─".repeat(56);
  console.log(line);
  console.log(`  NPM SECURITY AUDIT: ${BADGE[status]}`);
  console.log(`  Threshold: fail on ${failOn}+   Root: ${root}`);
  console.log(`  Projects: ${projects.length}`);
  console.log(line);
  if (message) console.log(`  ${message}`);
  for (const p of projects) {
    const detail = p.ran
      ? (p.total === 0
          ? "clean"
          : ORDER.filter((l) => p.vulnerabilities[l]).map((l) => `${p.vulnerabilities[l]} ${l}`).reverse().join(", "))
      : (p.message || "audit could not run");
    console.log(`  ${BADGE[p.status]}  ${rel(p.dir)}  —  ${detail}`);
  }
  if (projects.length > 1) {
    console.log(line);
    console.log(`  Aggregate: ${aggTotal} vuln(s) — ` +
      (ORDER.filter((l) => agg[l]).map((l) => `${agg[l]} ${l}`).reverse().join(", ") || "none"));
  }
  console.log(line);

  // ---- JSON ----
  const jsonResult = {
    status,
    threshold: failOn,
    auditedRoot: root,
    projectCount: projects.length,
    vulnerabilities: agg,
    total: aggTotal,
  };
  if (message) jsonResult.message = message;
  jsonResult.projects = projects.map((p) => {
    const o = { dir: rel(p.dir), status: p.status, vulnerabilities: p.vulnerabilities, total: p.total };
    if (p.message) o.message = p.message;
    if (p.packages.length) o.packages = p.packages;
    return o;
  });

  // ---- Markdown ----
  const md = [
    "# npm Security Audit",
    "",
    `**Status:** ${BADGE[status]}`,
    "",
    `- **Threshold:** fail on \`${failOn}\` and above`,
    `- **Audited root:** \`${root}\``,
    `- **Projects audited:** ${projects.length}`,
    `- **Total vulnerabilities:** ${aggTotal}`,
  ];
  if (message) md.push(`- **Note:** ${message}`);

  if (projects.length) {
    md.push("", "## Projects", "", "| Project | Status | Critical | High | Moderate | Low |", "| --- | --- | --- | --- | --- | --- |");
    for (const p of projects) {
      const v = p.vulnerabilities;
      md.push(`| \`${rel(p.dir)}\` | ${BADGE[p.status]} | ${v.critical} | ${v.high} | ${v.moderate} | ${v.low} |`);
    }
    for (const p of projects) {
      md.push("", `### \`${rel(p.dir)}\` — ${BADGE[p.status]}`, "");
      if (p.message) md.push(`> ${p.message}`, "");
      if (!p.ran) continue;
      if (!p.packages.length) {
        md.push("_No known vulnerabilities._");
        continue;
      }
      md.push("| Package | Severity | Fails threshold | Reason |", "| --- | --- | --- | --- |");
      for (const c of p.packages) {
        const reason = (c.reason || "—").replace(/\|/g, "\\|");
        md.push(`| ${c.package} | ${c.severity} | ${c.failsThreshold ? "yes" : "no"} | ${reason} |`);
      }
    }
  }
  md.push("", "---", statusFootnote(status), "");

  try {
    mkdirSync(out, { recursive: true });
    writeFileSync(join(out, "npm-audit-result.json"), JSON.stringify(jsonResult, null, 2) + "\n");
    writeFileSync(join(out, "npm-audit-result.md"), md.join("\n"));
    console.log(`  Reports written to: ${out}`);
  } catch (err) {
    console.error(`  Warning: could not write report files to ${out}: ${err.message}`);
  }
  console.log(line);

  process.exit(status === "pass" ? 0 : status === "fail" ? 1 : 2);
}

function statusFootnote(status) {
  if (status === "notapplicable") return "_Not applicable — nothing to audit._";
  if (status === "fail") return "_Resolve the failing packages above (upgrade to a fixed version) to pass._";
  return "_No threshold-breaking vulnerabilities across audited projects._";
}

// ---- Main ----
const { dirs, failOn, out, depth, root } = parseArgs(process.argv.slice(2));

// Discover first: whether the metric applies depends on there being a project,
// not on whether npm happens to be runnable.
const projectDirs = discoverProjects(dirs, depth);
if (!projectDirs.length) {
  finish({
    status: "notapplicable",
    message: `No npm project (package.json) found under: ${dirs.join(", ")}.`,
    projects: [],
    failOn, root, out,
  });
}

// A project exists but node or npm can't be invoked -> we can't verify security,
// so fail safe (not "notapplicable"). Each project reports the run failure.
const missing = await missingToolchain();
const projects = [];
for (const dir of projectDirs) {
  if (missing) {
    projects.push({
      dir,
      status: "fail",
      ran: false,
      message: `${missing} could not be run — audit not performed; failing safe since security is unverified.`,
      vulnerabilities: { ...ZERO },
      total: 0,
      packages: [],
    });
  } else {
    projects.push(await auditProject(dir, failOn));
  }
}

const overall = projects.some((p) => p.status === "fail") ? "fail" : "pass";
finish({ status: overall, projects, failOn, root, out });
