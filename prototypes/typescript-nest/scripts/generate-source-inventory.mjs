import { execFileSync } from 'node:child_process';
import { writeFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const packageDir = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = execFileSync('git', ['rev-parse', '--show-toplevel'], { cwd: packageDir, encoding: 'utf8' }).trim();
const files = execFileSync('git', ['ls-files', '-z', 'src'], { cwd: repoRoot }).toString('utf8').split('\0').filter(Boolean).sort();

function classify(file) {
  const segments = file.split('/');
  const project = segments[1] ?? 'Repository';
  const relative = segments.slice(2).join('/');
  const area = {
    Core: 'Domain and application contracts', DataAccess: 'Persistence and application handlers', Database: 'Database schema and deployment',
    'UI.Server': 'HTTP host, security, operations and server integrations', 'UI.Api': 'HTTP API', 'UI.Client': 'Browser UI',
    'UI.Shared': 'Shared browser UI', Worker: 'Background processing', LlmGateway: 'AI integration', McpServer: 'MCP integration',
    IntegrationTests: 'Integration tests', UnitTests: 'Unit and component tests', AcceptanceTests: 'Browser/system acceptance tests',
    'ChurchBulletin.AppHost': 'Local orchestration', 'ChurchBulletin.ServiceDefaults': 'Telemetry and service defaults',
  }[project] ?? `${project} project`;

  let destination;
  if (/(^|\/)(bin|obj|Generated)(\/|$)/.test(relative) || /\.(csproj|sln|props|targets|razor\.g\.cs)$/i.test(file)) {
    destination = 'Deferred: replace after equivalent behavior lands; retain only as migration reference';
  } else if (project === 'Core') destination = `Nest domain/application: ${relative}`;
  else if (project === 'DataAccess') destination = `Nest persistence/application: ${relative}`;
  else if (project === 'Database') destination = `Prisma migrations or deployment config: ${relative}`;
  else if (project.startsWith('UI.')) destination = `Nest API or React UI (split by responsibility): ${relative}`;
  else if (project.endsWith('Tests')) destination = `Jest/Playwright parity suite: ${relative}`;
  else if (project === 'Worker') destination = `Nest worker/outbox: ${relative}`;
  else if (project === 'LlmGateway' || project === 'McpServer') destination = `Separate TypeScript integration service: ${relative}`;
  else if (project.includes('AppHost') || project.includes('ServiceDefaults')) destination = `Compose/observability configuration: ${relative}`;
  else destination = `Review and map from ${project}: ${relative}`;
  return { area, destination };
}

const rows = files.map(file => {
  const { area, destination } = classify(file);
  return `| \`${file.replaceAll('|', '\\|')}\` | ${area} | ${destination.replaceAll('|', '\\|')} |`;
});
const output = path.resolve(packageDir, '../SOURCE-INVENTORY.md');
writeFileSync(output, [
  '# Tracked source inventory', '',
  `Generated from git ls-files src (${files.length} tracked paths). Each row maps one upstream file to a NestJS rewrite area or records why it remains deferred.`, '',
  'Regenerate after source tree changes with:', '',
  '```sh', 'node prototypes/typescript-nest/scripts/generate-source-inventory.mjs', '```', '',
  '| Tracked source path | Area | NestJS destination / disposition |', '|---|---|---|', ...rows, '',
].join('\n'));
console.log(`Wrote ${files.length} source-file mappings to ${path.relative(repoRoot, output)}`);
