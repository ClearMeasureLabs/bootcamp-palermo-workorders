import {
	extractStill,
	finishRecording,
	launchRecordedContext,
	loginAs,
	openSearch,
	pause,
	selectById
} from './helpers';

/**
 * Portal proof for the Saturday batch: search filters + manage screen.
 */
export async function capturePortalBatch(numbers: string[]): Promise<void> {
	const {browser, context, page} = await launchRecordedContext('portal-batch');
	await loginAs(page, 'tlovejoy');
	await pause(800);

	await openSearch(page);
	await pause(600);

	await selectById(page, 'CreatorSelect', 'tlovejoy');
	await selectById(page, 'AssigneeSelect', 'gwillie');
	await selectById(page, 'StatusSelect', 'Assigned');
	await page.locator('#SearchButton').click();
	await page.waitForLoadState('networkidle');
	await pause(2000);

	if (numbers.length > 0) {
		const first = numbers[0];
		const link = page.getByTestId(`WorkOrderLink${first}`);
		if ((await link.count()) > 0) {
			await link.first().click();
			await page.waitForLoadState('networkidle');
			await pause(2500);
		} else {
			console.warn(`Work order ${first} not visible in search results; filming search list only.`);
			await pause(2000);
		}
	} else {
		await pause(2000);
	}

	const webm = await finishRecording(context, browser, page, 'portal-batch');
	await extractStill(webm, 'portal-batch-final');
}
