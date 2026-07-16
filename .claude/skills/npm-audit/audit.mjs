#!/usr/bin/env node
// Runs `npm audit --json` in the target directory, evaluates it against a
// severity threshold, prints a concise report, and writes
// npm-audit-result.{md,json} report files.
//
// Usage:
//   node audit.mjs [--dir <path>] [--fail-on critical|high|moderate|low] [--out <dir>]
//
// Result status:
//   pass          — audit ran and no vulnerability at/above the threshold.
//   fail          — audit ran and found threshold-breaking vulns, OR the audit
//                   could not be run in a project that IS an npm project
//                   (e.g. missing lockfile) — we fail safe since security is
//                   unverified.
//   notapplicable — there is nothing to test (no package.json / not an npm
//                   project, or npm is not installed).
//
// Threshold semantics: FAIL if there is >=1 vulnerability at or above the
// --fail-on level. Default threshold is `critical`.
// Exit code: 0 = pass, 1 = fail, 2 = notapplicable.

import { execFile } from "node:child_process";
import { promisify } from "node:util";
import { mkdirSync, writeFileSync, readFileSync, existsSync } from "node:fs";
import { join, resolve, isAbsolute } from "node:path";

const run = promisify(execFile);

const ORDER = ["low", "moderate", "high", "critical"];
const ZERO = { critical: 0, high: 0, moderate: 0, low: 0 };

function parseArgs(argv) {
  const args = { dir: process.cwd(), failOn: "high", out: null };
  for (let i = 0; i < argv.length; i++) {
    if (argv[i] === "--dir") args.dir = argv[++i];
    else if (argv[i] === "--fail-on") args.failOn = argv[++i];
    else if (argv[i] === "--out") args.out = argv[++i];
  }
  if (!ORDER.includes(args.failOn)) {
    console.error(`Invalid --fail-on "${args.failOn}". Use one of: ${ORDER.join(", ")}`);
    process.exit(1);
  }
  args.out = args.out
    ? (isAbsolute(args.out) ? args.out : resolve(process.cwd(), args.out))
    : resolve(args.dir, "codebase-audit-report/metrics/npm-audit");
  return args;
}

// Map installed package name -> version from the project's lockfile, so the
// report can show name@version rather than just the vulnerable range.
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

async function npmAvailable() {
  try {
    await run("npm", ["--version"]);
    return true;
  } catch {
    return false;
  }
}

// Runs the audit. Returns {report} on success, or {error} (a human-readable
// string) if the audit command could not produce a usable result.
async function getAuditJson(dir) {
  let stdout;
  try {
    ({ stdout } = await run("npm", ["audit", "--json"], {
      cwd: dir,
      maxBuffer: 32 * 1024 * 1024,
    }));
  } catch (err) {
    // npm audit exits non-zero when it finds vulns — that's still success and
    // the JSON is on stdout. A genuine failure has no parseable stdout.
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

// ---- Render + write + exit ----
// `ran` distinguishes "audit produced results" from N/A or a run failure, so we
// don't print a misleading "no vulnerabilities" line when the audit never ran.
function finish({ status, message, sev = ZERO, packages = [], failOn, dir, out, ran = false }) {
  const counts = { ...ZERO, ...sev };
  const total = counts.critical + counts.high + counts.moderate + counts.low;
  const threshold = ORDER.indexOf(failOn);
  const badge = { pass: "✅ PASS", fail: "❌ FAIL", notapplicable: "➖ N/A" }[status];

  // ---- console ----
  const line = "─".repeat(52);
  console.log(line);
  console.log(`  NPM SECURITY AUDIT: ${badge}`);
  console.log(`  Threshold: fail on ${failOn}+   Directory: ${dir}`);
  console.log(line);
  if (message) console.log(`  ${message}`);
  if (ran) {
    if (total === 0) {
      console.log("  No known vulnerabilities found.");
    } else {
      console.log(`  Found ${total} vulnerabilit${total === 1 ? "y" : "ies"}:`);
      for (const lvl of [...ORDER].reverse()) {
        if (counts[lvl] === 0) continue;
        const gate = ORDER.indexOf(lvl) >= threshold ? " ← fails threshold" : "";
        const names = packages.filter((p) => p.severity === lvl).map((c) => c.package);
        const list = names.length ? ` (${names.join(", ")})` : "";
        console.log(`    • ${lvl.padEnd(8)} ${String(counts[lvl]).padStart(3)}${gate}${list}`);
      }
    }
  }
  console.log(line);

  // ---- JSON ----
  const jsonResult = { status, threshold: failOn, auditedDir: dir, vulnerabilities: counts, total };
  if (message) jsonResult.message = message;
  if (packages.length) jsonResult.packages = packages;

  // ---- Markdown ----
  const md = [
    "# npm Security Audit",
    "",
    `**Status:** ${badge}`,
    "",
    `- **Threshold:** fail on \`${failOn}\` and above`,
    `- **Audited directory:** \`${dir}\``,
  ];
  if (message) md.push(`- **Note:** ${message}`);
  if (ran) {
    md.push(
      `- **Total vulnerabilities:** ${total}`,
      "",
      "## Severity breakdown",
      "",
      "| Severity | Count |",
      "| --- | --- |",
      `| Critical | ${counts.critical} |`,
      `| High | ${counts.high} |`,
      `| Moderate | ${counts.moderate} |`,
      `| Low | ${counts.low} |`
    );
    if (packages.length) {
      md.push("", "## Vulnerable packages", "");
      md.push("| Package | Severity | Fails threshold | Reason |");
      md.push("| --- | --- | --- | --- |");
      for (const c of packages) {
        const reason = (c.reason || "—").replace(/\|/g, "\\|");
        md.push(`| ${c.package} | ${c.severity} | ${c.failsThreshold ? "yes" : "no"} | ${reason} |`);
      }
    }
  }
  const hasFailing = packages.some((p) => p.failsThreshold);
  md.push("", "---", statusFootnote(status, counts, hasFailing), "");

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

function statusFootnote(status, counts, hasFailing) {
  if (status === "notapplicable") return "_Not applicable — nothing to audit._";
  if (status === "fail") {
    return hasFailing
      ? "_Resolve the failing packages above (upgrade to a fixed version) to pass._"
      : "_Failing — see the note above._";
  }
  return counts.high || counts.moderate || counts.low
    ? "_No critical vulnerabilities. See counts above for lower-severity issues._"
    : "_No known vulnerabilities._";
}

// ---- Main ----
const { dir, failOn, out } = parseArgs(process.argv.slice(2));

if (!(await npmAvailable())) {
  finish({ status: "notapplicable", message: "npm is not installed — nothing to audit.", failOn, dir, out });
}
if (!existsSync(join(dir, "package.json"))) {
  finish({
    status: "notapplicable",
    message: "No package.json found — not an npm project; npm audit is not applicable.",
    failOn, dir, out,
  });
}

const { report, error } = await getAuditJson(dir);
if (error) {
  finish({
    status: "fail",
    message: `npm audit could not be run: ${error}. Ensure an installed lockfile exists (run \`npm install\` first).`,
    failOn, dir, out,
  });
}

const counts = report.metadata.vulnerabilities;
const sev = {
  critical: counts.critical || 0,
  high: counts.high || 0,
  moderate: counts.moderate || 0,
  low: counts.low || 0,
};

// Build the full package list — every vulnerable package at every severity —
// each with name@version, severity, reason (advisory titles), and whether it
// breaks the threshold.
const threshold = ORDER.indexOf(failOn);
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
// Most severe first, then by name.
packages.sort(
  (a, b) => ORDER.indexOf(b.severity) - ORDER.indexOf(a.severity) || a.package.localeCompare(b.package)
);

const passes = !ORDER.some((lvl, i) => i >= threshold && sev[lvl] > 0);

finish({ status: passes ? "pass" : "fail", sev, packages, failOn, dir, out, ran: true });
