import { spawnSync } from 'node:child_process';
import { rmSync } from 'node:fs';
import path from 'node:path';

const db = path.resolve('private-build.db');
rmSync(db, { force: true });
const env = { ...process.env, DATABASE_URL: `file:${db}` };
for (const args of [['install','--no-audit','--no-fund'], ['run','build'], ['test']]) {
  const command = args[0] === 'ci' ? 'npm' : 'npm';
  const result = spawnSync(command, args, { env, stdio: 'inherit', shell: process.platform === 'win32' });
  if (result.status !== 0) process.exit(result.status ?? 1);
}
