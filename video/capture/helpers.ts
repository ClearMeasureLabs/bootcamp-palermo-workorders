import {Browser, BrowserContext, Page, chromium} from 'playwright';
import path from 'path';
import fs from 'fs';

export const BASE_URL = process.env.DEMO_BASE_URL ?? 'https://localhost:7175';
export const FOOTAGE_DIR = path.join(__dirname, '..', 'footage');
export const VIEWPORT = {width: 1920, height: 1080};

if (!fs.existsSync(FOOTAGE_DIR)) {
	fs.mkdirSync(FOOTAGE_DIR, {recursive: true});
}

/**
 * Ported from src/AcceptanceTests/AcceptanceTestBase.cs — same data-testid based
 * interaction helpers used by the real Playwright acceptance tests, so the capture
 * scripts drive the actual running app the same way the test suite does.
 */
export async function click(page: Page, testId: string): Promise<void> {
	const locator = page.getByTestId(testId);
	await locator.waitFor({state: 'visible'});
	await locator.evaluate((el: HTMLElement) => el.click());
}

export async function input(page: Page, testId: string, value: string): Promise<void> {
	const locator = page.getByTestId(testId);
	await locator.waitFor({state: 'visible'});
	await locator.fill('');
	await locator.fill(value);
	await locator.blur();
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

/**
 * Opens a fresh Chromium context with video recording enabled, saving a .webm
 * clip into video/footage/<name>.webm sized for the Remotion composition.
 */
export async function launchRecordedContext(name: string): Promise<{
	browser: Browser;
	context: BrowserContext;
	page: Page;
}> {
	const browser = await chromium.launch({headless: true});
	const context = await browser.newContext({
		baseURL: BASE_URL,
		ignoreHTTPSErrors: true,
		viewport: VIEWPORT,
		recordVideo: {
			dir: FOOTAGE_DIR,
			size: VIEWPORT
		}
	});
	context.setDefaultTimeout(60_000);
	const page = await context.newPage();
	return {browser, context, page};
}

/**
 * Closes the context (which flushes the .webm recording to disk) and renames the
 * Playwright-generated random filename to the requested scene name.
 */
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
	await click(page, 'LoginLink');
	await page.waitForURL('**/login');
	await select(page, 'User', username);
	await click(page, 'LoginButton');
	await page.waitForLoadState('networkidle');
	const welcome = page.getByTestId('WelcomeText');
	await welcome.waitFor({state: 'visible'});
}

export async function logout(page: Page): Promise<void> {
	await click(page, 'LogoutLink');
	await page.waitForLoadState('networkidle');
}

export async function pause(ms: number): Promise<void> {
	await new Promise((resolve) => setTimeout(resolve, ms));
}
