import { spawnSync } from 'node:child_process';
import { rmSync } from 'node:fs';
import path from 'node:path';
const db = path.resolve('test.db');
rmSync(db, { force: true });
const env = { ...process.env, DATABASE_URL: `file:${db}` };
for (const args of [['jest','--runInBand']]) {
  const result = spawnSync(process.platform === 'win32' ? 'npx.cmd' : 'npx', args, { env, stdio: 'inherit' });
  if (result.status !== 0) process.exit(result.status ?? 1);
}
