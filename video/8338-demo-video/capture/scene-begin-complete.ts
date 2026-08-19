import {click, finishRecording, launchRecordedContext, loginAs, pause, selectById} from './helpers';

async function filterByAssigneeAndStatus(
	page: import('playwright').Page,
	statusKey: string
): Promise<void> {
	await click(page, 'Search');
	await page.waitForURL('**/workorder/search');
	await page.waitForLoadState('networkidle');
	await selectById(page, 'AssigneeSelect', 'jcuevas');
	await selectById(page, 'StatusSelect', statusKey);
	await page.locator('#SearchButton').click();
	await page.waitForLoadState('networkidle');
}

/**
 * Scene: Joe Cuevas (jcuevas) logs in, opens a work order assigned to him,
 * begins it (Assigned -> InProgress), then completes it (InProgress -> Complete).
 *
 * Each state-transition command (see WorkOrderManage.razor.cs HandleSubmit) navigates
 * back to /workorder/search after saving. The work order is located via the search
 * page's Assignee + Status filters (rather than "first item in a list") so re-running
 * this capture repeatedly always finds an order that is actually in the expected
 * status, regardless of what earlier capture runs left behind.
 */
export async function captureBeginCompleteScene(): Promise<void> {
	console.log('Capturing scene: begin-complete');
	const {browser, context, page} = await launchRecordedContext('begin-complete-scene');

	try {
		await loginAs(page, 'jcuevas');
		await pause(800);

		const numberLocator = page.getByTestId('WorkOrderNumber');
		const statusLocator = page.getByTestId('Status');

		// Find an Assigned order that belongs to jcuevas.
		await filterByAssigneeAndStatus(page, 'Assigned');
		await pause(600);

		const assignedLink = page.locator('[data-testid^="WorkOrderLink"]').first();
		await assignedLink.waitFor({state: 'visible'});
		const orderNumber = (await assignedLink.getAttribute('data-testid'))?.replace(
			'WorkOrderLink',
			''
		);
		await assignedLink.evaluate((el: HTMLElement) => el.click());
		await page.waitForLoadState('networkidle');
		await numberLocator.waitFor({state: 'visible'});
		await pause(800);

		// Assigned -> InProgress. Saving navigates back to the search page.
		await click(page, 'CommandButtonBegin');
		await page.waitForURL('**/workorder/search');
		await page.waitForLoadState('networkidle');
		await pause(800);

		// Re-open the same order (now InProgress) to complete it.
		await filterByAssigneeAndStatus(page, 'InProgress');
		if (orderNumber) {
			const linkAgain = page.getByTestId(`WorkOrderLink${orderNumber}`);
			await linkAgain.waitFor({state: 'visible'});
			await linkAgain.evaluate((el: HTMLElement) => el.click());
		} else {
			await page.locator('[data-testid^="WorkOrderLink"]').first().click();
		}
		await page.waitForLoadState('networkidle');
		await numberLocator.waitFor({state: 'visible'});
		await statusLocator.waitFor({state: 'visible'});
		await pause(900);

		// InProgress -> Complete.
		await click(page, 'CommandButtonComplete');
		await page.waitForURL('**/workorder/search');
		await page.waitForLoadState('networkidle');
		await pause(1200);
	} finally {
		await finishRecording(context, browser, page, 'begin-complete-scene');
	}
}

if (require.main === module) {
	captureBeginCompleteScene().catch((err) => {
		console.error(err);
		process.exit(1);
	});
}
