import { chromium } from 'playwright';
import { spawn } from 'node:child_process';
import { setTimeout as delay } from 'node:timers/promises';
import { copyFileSync, existsSync, mkdirSync, rmSync } from 'node:fs';
import path from 'node:path';

const port = Number(process.env.PORT || 3311);
const artifacts = path.resolve('artifacts');
mkdirSync(artifacts, { recursive: true });
for (const file of ['acceptance-video.webm']) rmSync(path.join(artifacts, file), { force: true });
const server = spawn(process.execPath, ['dist/main.js'], { env: { ...process.env, PORT: String(port), DATABASE_URL: `file:${path.resolve('acceptance.db')}` }, stdio: 'inherit' });
let browser;
try {
  let ready = false;
  for (let i=0; i<60; i++) { try { if ((await fetch(`http://127.0.0.1:${port}/api/health`)).ok) { ready=true; break; } } catch {} await delay(250); }
  if (!ready) throw new Error('Prototype server did not become ready');
  browser = await chromium.launch({ headless: process.env.HEADFUL !== 'true' });
  const context = await browser.newContext({ recordVideo: { dir: artifacts, size: { width: 1280, height: 720 } } });
  const page = await context.newPage();
  const recording = page.video();
  await page.goto(`http://127.0.0.1:${port}`);
  await page.getByText('Service: ok').waitFor();
  await page.getByTestId('title').fill(`Acceptance order ${Date.now()}`);
  const dateParts = new Intl.DateTimeFormat('en-US', { timeZone: 'America/Chicago', year: 'numeric', month: '2-digit', day: '2-digit' }).formatToParts(new Date());
  const chicagoToday = `${dateParts.find(part => part.type === 'year').value}-${dateParts.find(part => part.type === 'month').value}-${dateParts.find(part => part.type === 'day').value}`;
  await page.getByTestId('description').fill('Browser acceptance flow');
  await page.getByTestId('instructions').fill('Use the side entrance');
  await page.getByTestId('room').fill('QA-7');
  await page.getByTestId('due-date').fill(chicagoToday);
  await page.getByTestId('save-draft').click();
  await page.getByText('Draft saved').waitFor();
  await page.getByTestId('due-date-value').getByText('Due Today').waitFor();
  await page.getByRole('button', { name: 'Assign' }).last().click();
  await page.getByText('Work order assigned').waitFor();
  await page.getByTestId('search-assignee').fill('demo.tech');
  await page.getByTestId('search').click();
  await page.getByText('demo.tech').last().waitFor();
  await page.getByRole('button', { name: 'Begin' }).last().click();
  await page.getByText('Work order in progress').waitFor();
  await page.getByRole('button', { name: 'Complete' }).last().click();
  await page.getByText('Work order complete').waitFor();
  await page.getByText('Complete', { exact: true }).last().waitFor();
  console.log('Acceptance passed: create → assign → in progress → complete');
  await context.close();
  if (!recording) throw new Error('Playwright did not create a video recorder for the acceptance page');
  const recordingPath = await recording.path();
  if (!existsSync(recordingPath)) throw new Error(`Playwright recording is missing: ${recordingPath}`);
  copyFileSync(recordingPath, path.join(artifacts, 'acceptance-video.webm'));
  const remotionPublic = path.resolve('remotion/public');
  mkdirSync(remotionPublic, { recursive: true });
  copyFileSync(recordingPath, path.join(remotionPublic, 'acceptance-video.webm'));
  console.log(`Recorded Playwright acceptance video: ${recordingPath}`);
} finally {
  await browser?.close();
  server.kill('SIGTERM');
}
