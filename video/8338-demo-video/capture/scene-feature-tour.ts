import {
	click,
	finishRecording,
	input,
	launchRecordedContext,
	loginAs,
	pause
} from './helpers';

/**
 * Scene: a tour of additional real features — work order search/filtering, dark
 * mode toggle, and the AI chat page — all driven against the live app.
 */
export async function captureFeatureTourScene(): Promise<void> {
	console.log('Capturing scene: feature-tour');
	const {browser, context, page} = await launchRecordedContext('feature-tour-scene');

	try {
		await loginAs(page, 'tlovejoy');
		await pause(600);

		// Search / filter work orders
		await click(page, 'Search');
		await page.waitForURL('**/workorder/search');
		await page.waitForLoadState('networkidle');
		await pause(500);

		const statusSelect = page.locator('#StatusSelect');
		await statusSelect.waitFor({state: 'visible'});
		await statusSelect.selectOption({value: 'Assigned'});
		await pause(400);
		const searchButton = page.locator('#SearchButton');
		await searchButton.click();
		await page.waitForLoadState('networkidle');
		await pause(1000);

		// Dark mode toggle
		await click(page, 'Settings');
		await page.waitForURL('**/settings');
		await page.waitForLoadState('networkidle');
		const darkSwitch = page.getByTestId('DarkModeSwitch');
		await darkSwitch.waitFor({state: 'visible'});
		await pause(500);
		await darkSwitch.evaluate((el: HTMLElement) => el.click());
		await pause(1200);

		// AI chat page (composer shown; not sent — no live LLM key in this environment)
		await click(page, 'AiAgent');
		await page.waitForURL('**/ai-agent');
		await page.waitForLoadState('networkidle');
		await pause(700);
		const chatInput = page.getByTestId('ChatInput');
		if (await chatInput.isVisible().catch(() => false)) {
			await input(page, 'ChatInput', 'What work orders are assigned to me?');
			await pause(1000);
		}
	} finally {
		await finishRecording(context, browser, page, 'feature-tour-scene');
	}
}

if (require.main === module) {
	captureFeatureTourScene().catch((err) => {
		console.error(err);
		process.exit(1);
	});
}
