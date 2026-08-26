import {Browser, BrowserContext, Page, chromium} from 'playwright';
import path from 'path';
import fs from 'fs';

export const PORTAL_URL =
	process.env.TDD_PORTAL_URL ??
	'https://ui-gh.icywave-25720613.southcentralus.azurecontainerapps.io';

export const FOOTAGE_DIR = path.join(__dirname, '..', 'public', 'footage');
export const STILLS_DIR = path.join(__dirname, '..', 'public', 'stills');
export const VIEWPORT = {width: 1280, height: 800};

if (!fs.existsSync(FOOTAGE_DIR)) {
	fs.mkdirSync(FOOTAGE_DIR, {recursive: true});
}
if (!fs.existsSync(STILLS_DIR)) {
	fs.mkdirSync(STILLS_DIR, {recursive: true});
}

export async function click(page: Page, testId: string): Promise<void> {
	const locator = page.getByTestId(testId);
	await locator.waitFor({state: 'visible'});
	await locator.evaluate((el: HTMLElement) => el.click());
}

export async function select(page: Page, testId: string, value: string): Promise<void> {
	const locator = page.getByTestId(testId);
	await locator.waitFor({state: 'visible'});
	await locator.selectOption(value);
}

export async function selectById(page: Page, elementId: string, value: string): Promise<void> {
	const locator = page.locator(`#${elementId}`);
	await locator.waitFor({state: 'visible'});
	await locator.selectOption(value);
}

export async function launchRecordedContext(name: string): Promise<{
	browser: Browser;
	context: BrowserContext;
	page: Page;
}> {
	const browser = await chromium.launch({headless: true});
	const context = await browser.newContext({
		baseURL: PORTAL_URL,
		ignoreHTTPSErrors: true,
		viewport: VIEWPORT,
		recordVideo: {
			dir: FOOTAGE_DIR,
			size: VIEWPORT
		}
	});
	context.setDefaultTimeout(90_000);
	const page = await context.newPage();
	return {browser, context, page};
}

export async function finishRecording(
	context: BrowserContext,
	browser: Browser,
	page: Page,
	name: string
): Promise<string> {
	const video = page.video();
	await context.close();
	await browser.close();

	if (!video) {
		throw new Error(`No video was recorded for scene "${name}"`);
	}

	const recordedPath = await video.path();
	const targetPath = path.join(FOOTAGE_DIR, `${name}.webm`);
	if (fs.existsSync(targetPath)) {
		fs.rmSync(targetPath);
	}
	fs.renameSync(recordedPath, targetPath);
	console.log(`  saved ${targetPath}`);
	return targetPath;
}

export async function loginAs(page: Page, username: string): Promise<void> {
	await page.goto('/');
	await page.waitForLoadState('networkidle');
	const lovejoy = page.getByTestId('LovejoyShortcut');
	if (username === 'tlovejoy' && (await lovejoy.count()) > 0) {
		await lovejoy.click();
	} else {
		await click(page, 'LoginLink');
		await page.waitForURL('**/login');
		await select(page, 'User', username);
		await click(page, 'LoginButton');
	}
	await page.waitForLoadState('networkidle');
	const welcome = page.getByTestId('WelcomeText');
	await welcome.waitFor({state: 'visible'});
}

/** Opens the Work Order Search page (singular /workorder/search). */
export async function openSearch(page: Page): Promise<void> {
	await page.getByTestId('Search').click();
	await page.waitForURL('**/workorder/search**');
	await page.waitForLoadState('networkidle');
	await page.locator('#CreatorSelect').waitFor({state: 'visible'});
	await page.waitForFunction(() => {
		const status = document.querySelectorAll('#StatusSelect option').length;
		const creators = document.querySelectorAll('#CreatorSelect option').length;
		return status > 1 && creators > 2;
	});
}

export async function pause(ms: number): Promise<void> {
	await new Promise((resolve) => setTimeout(resolve, ms));
}

export async function extractStill(webmPath: string, stillName: string): Promise<string> {
	const {execFileSync} = await import('child_process');
	const out = path.join(STILLS_DIR, `${stillName}.png`);
	execFileSync(
		'ffmpeg',
		['-y', '-sseof', '-1.0', '-i', webmPath, '-update', '1', '-q:v', '2', out],
		{stdio: 'inherit'}
	);
	console.log(`  still ${out}`);
	return out;
}

export type ChatTurn =
	| {kind: 'user'; text: string}
	| {kind: 'assistant'; text: string}
	| {kind: 'tool'; name: string; args: string; result: string; missing?: boolean};

export function writeGrokSessionHtml(turns: ChatTurn[], outPath: string): void {
	const body = turns
		.map((t) => {
			if (t.kind === 'user') {
				return `<div class="row user"><div class="bubble"><div class="who">You</div><div class="text">${escapeHtml(
					t.text
				)}</div></div></div>`;
			}
			if (t.kind === 'assistant') {
				return `<div class="row bot"><div class="bubble"><div class="who">Grok</div><div class="text">${escapeHtml(
					t.text
				)}</div></div></div>`;
			}
			const missingClass = t.missing ? ' missing' : '';
			const badge = t.missing ? 'not on TDD yet' : 'TDD MCP';
			return `<div class="row tool${missingClass}"><div class="bubble tool"><div class="who">tool · ${escapeHtml(
				t.name
			)} <span class="badge">${badge}</span></div><pre class="args">${escapeHtml(
				t.args
			)}</pre><pre class="result">${escapeHtml(t.result)}</pre></div></div>`;
		})
		.join('\n');

	const html = `<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8"/>
<title>Grok · Church Work Orders</title>
<style>
  html,body{margin:0;height:100%;background:#0b1220;color:#e8eef7;font-family:Segoe UI,Arial,sans-serif}
  .frame{display:flex;flex-direction:column;height:100vh;width:100vw}
  .top{padding:14px 22px;background:linear-gradient(90deg,#111a2b,#1a2740);border-bottom:1px solid #2a3a55;display:flex;align-items:center;gap:12px}
  .mark{width:28px;height:28px;border-radius:8px;background:linear-gradient(135deg,#7dd3a0,#3d8f66)}
  .title{font-weight:700;font-size:18px}
  .sub{opacity:.7;font-size:13px}
  .chat{flex:1;overflow:auto;padding:18px 28px 40px;display:flex;flex-direction:column;gap:14px}
  .row{display:flex}
  .row.user{justify-content:flex-end}
  .bubble{max-width:920px;background:#162033;border:1px solid #2b3d5c;border-radius:14px;padding:12px 16px}
  .bubble.tool{background:#101a14;border-color:#2f5a40}
  .row.tool.missing .bubble{background:#2a1a10;border-color:#7a4a20}
  .who{font-size:12px;letter-spacing:.04em;text-transform:uppercase;opacity:.75;margin-bottom:6px}
  .text{font-size:16px;line-height:1.45;white-space:pre-wrap}
  pre{margin:8px 0 0;padding:10px;background:rgba(0,0,0,.28);border-radius:8px;font-size:13px;line-height:1.35;white-space:pre-wrap;word-break:break-word}
  .badge{display:inline-block;margin-left:8px;padding:2px 8px;border-radius:999px;background:#2d6b4a;font-size:11px;text-transform:none;letter-spacing:0}
  .missing .badge{background:#8a4d1c}
</style>
</head>
<body>
<div class="frame">
  <div class="top"><div class="mark"></div><div><div class="title">Grok</div><div class="sub">Already on the church portal tools · no connector setup</div></div></div>
  <div class="chat" id="chat">${body}</div>
</div>
<script>
  const chat = document.getElementById('chat');
  chat.scrollTop = chat.scrollHeight;
</script>
</body>
</html>`;
	fs.writeFileSync(outPath, html, 'utf8');
}

function escapeHtml(s: string): string {
	return s
		.replace(/&/g, '&amp;')
		.replace(/</g, '&lt;')
		.replace(/>/g, '&gt;')
		.replace(/"/g, '&quot;');
}
