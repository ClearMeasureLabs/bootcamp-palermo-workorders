import {
	click,
	finishRecording,
	input,
	launchRecordedContext,
	loginAs,
	pause,
	select
} from './helpers';

/**
 * Scene: Reverend Timothy Lovejoy (tlovejoy) logs in, creates a new work order,
 * and assigns it to Joe Cuevas (jcuevas, Fulfillment role).
 */
export async function captureAssignScene(): Promise<void> {
	console.log('Capturing scene: assign');
	const {browser, context, page} = await launchRecordedContext('assign-scene');

	try {
		await loginAs(page, 'tlovejoy');
		await pause(800);

		await click(page, 'NewWorkOrder');
		await page.waitForURL('**/workorder/manage?mode=New');
		await page.waitForLoadState('networkidle');

		const numberLocator = page.getByTestId('WorkOrderNumber');
		await numberLocator.waitFor({state: 'visible'});
		const newOrderNumber = (await numberLocator.innerText()).trim();

		await input(page, 'Title', 'Repair Sanctuary Sound System');
		await input(
			page,
			'Description',
			'Front-of-house speakers are cutting out during Sunday service. Needs diagnosis and repair before next service.'
		);
		await input(page, 'RoomNumber', 'Sanctuary');
		await pause(600);

		await click(page, 'CommandButtonSave');
		await page.waitForURL('**/workorder/search', {timeout: 90_000});
		await page.waitForLoadState('networkidle');
		await pause(600);

		// Re-open the exact order just created (by number) to assign it — a "My Work
		// Orders" list can contain older orders from previous capture runs, so
		// targeting by number avoids picking up an already-assigned/read-only one.
		await click(page, 'MyWorkOrders');
		await page.waitForLoadState('networkidle');
		await pause(400);

		const ownLink = page.getByTestId(`WorkOrderLink${newOrderNumber}`);
		await ownLink.waitFor({state: 'visible'});
		await ownLink.evaluate((el: HTMLElement) => el.click());
		await page.waitForLoadState('networkidle');
		await numberLocator.waitFor({state: 'visible'});
		await pause(500);

		await select(page, 'Assignee', 'jcuevas');
		await pause(500);
		await click(page, 'CommandButtonAssign');
		await page.waitForURL('**/workorder/search');
		await page.waitForLoadState('networkidle');
		await pause(1200);
	} finally {
		await finishRecording(context, browser, page, 'assign-scene');
	}
}

if (require.main === module) {
	captureAssignScene().catch((err) => {
		console.error(err);
		process.exit(1);
	});
}
